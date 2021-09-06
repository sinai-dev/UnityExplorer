using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Microsoft.CSharp;
using UnityExplorer.CSConsole;

namespace UnityExplorer.Hooks
{
    public class HookInstance
    {
        private static readonly StringBuilder evalOutput = new StringBuilder();
        private static readonly ScriptEvaluator scriptEvaluator = new ScriptEvaluator(new StringWriter(evalOutput));

        // Instance

        public bool Enabled;
        public MethodInfo TargetMethod;
        public string GeneratedSource;

        private string shortSignature;
        private PatchProcessor patchProcessor;
        private HarmonyMethod patchDelegate;
        private MethodInfo patchDelegateMethodInfo;

        public HookInstance(MethodInfo targetMethod)
        {
            this.TargetMethod = targetMethod;
            this.shortSignature = $"{targetMethod.DeclaringType.Name}.{targetMethod.Name}";
            GenerateProcessorAndDelegate();
            Patch();
        }

        private void GenerateProcessorAndDelegate()
        {
            try
            {
                patchProcessor = ExplorerCore.Harmony.CreateProcessor(TargetMethod);

                // Dynamically compile the patch method

                scriptEvaluator.Run(GeneratePatchSourceCode(TargetMethod));

                // Get the compiled method and check for errors

                string output = scriptEvaluator._textWriter.ToString();
                var outputSplit = output.Split('\n');
                if (outputSplit.Length >= 2)
                    output = outputSplit[outputSplit.Length - 2];
                evalOutput.Clear();
                if (ScriptEvaluator._reportPrinter.ErrorsCount > 0)
                    throw new FormatException($"Unable to compile the code. Evaluator's last output was:\r\n{output}");

                // Could publicize MCS to avoid this reflection, but not bothering for now
                var source = (Mono.CSharp.CompilationSourceFile)ReflectionUtility.GetFieldInfo(typeof(Mono.CSharp.Evaluator), "source_file")
                             .GetValue(scriptEvaluator);
                var type = (Mono.CSharp.Class)source.Containers.Last();
                var systemType = ((Mono.CSharp.TypeSpec)ReflectionUtility.GetPropertyInfo(typeof(Mono.CSharp.TypeDefinition), "Definition")
                                 .GetValue(type, null))
                                 .GetMetaInfo();

                this.patchDelegateMethodInfo = systemType.GetMethod("Patch", ReflectionUtility.FLAGS);

                // Actually create the harmony patch
                this.patchDelegate = new HarmonyMethod(patchDelegateMethodInfo);
                patchProcessor.AddPostfix(patchDelegate);
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception creating patch processor for target method {TargetMethod.FullDescription()}!\r\n{ex}");
            }
        }

        private string GeneratePatchSourceCode(MethodInfo targetMethod)
        {
            var codeBuilder = new StringBuilder();

            codeBuilder.AppendLine($"public class DynamicPatch_{DateTime.Now.Ticks}");
            codeBuilder.AppendLine("{");

            // Arguments 

            codeBuilder.Append("    public static void Patch(System.Reflection.MethodBase __originalMethod");

            if (!targetMethod.IsStatic)
                codeBuilder.Append($", {targetMethod.DeclaringType.FullName} __instance");

            if (targetMethod.ReturnType != typeof(void))
                codeBuilder.Append($", {targetMethod.ReturnType.FullName} __result");

            int paramIdx = 0;
            var parameters = targetMethod.GetParameters();
            foreach (var param in parameters)
            {
                codeBuilder.Append($", {param.ParameterType.FullName} __{paramIdx}");
                paramIdx++;
            }

            codeBuilder.Append(")\n");

            // Patch body

            codeBuilder.AppendLine("    {");

            codeBuilder.AppendLine("        try {");

            // Log message 

            var logMessage = new StringBuilder();
            logMessage.AppendLine($"$@\"Patch called: {shortSignature}");
            
            if (!targetMethod.IsStatic)
                logMessage.AppendLine("__instance: {__instance.ToString()}");

            paramIdx = 0;
            foreach (var param in parameters)
            {
                if (param.ParameterType.IsValueType)
                    logMessage.AppendLine($"Parameter {paramIdx}: {{__{paramIdx}.ToString()}}");
                else
                    logMessage.AppendLine($"Parameter {paramIdx}: {{__{paramIdx}?.ToString() ?? \"null\"}}");
                paramIdx++;
            }

            if (targetMethod.ReturnType != typeof(void))
            {
                if (targetMethod.ReturnType.IsValueType)
                    logMessage.AppendLine("Return value: {__result.ToString()}");
                else
                    logMessage.AppendLine("Return value: {__result?.ToString() ?? \"null\"}");
            }

            logMessage.Append('"');

            codeBuilder.AppendLine($"            UnityExplorer.ExplorerCore.Log({logMessage});");
            codeBuilder.AppendLine("        }");
            codeBuilder.AppendLine("        catch (System.Exception ex) {");
            codeBuilder.AppendLine($"            UnityExplorer.ExplorerCore.LogWarning($\"Exception in patch of {shortSignature}:\\n{{ex}}\");");
            codeBuilder.AppendLine("        }");

            // End patch body

            codeBuilder.AppendLine("    }");

            // End class

            codeBuilder.AppendLine("}");

            return GeneratedSource = codeBuilder.ToString();
        }

        public void TogglePatch()
        {
            Enabled = !Enabled;
            if (Enabled)
                Patch();
            else
                Unpatch();
        }

        public void Patch()
        {
            try
            {
                patchProcessor.Patch();
                Enabled = true;
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception hooking method!\r\n{ex}");
            }
        }

        public void Unpatch()
        {
            if (!Enabled)
                return;

            try
            {
                this.patchProcessor.Unpatch(patchDelegateMethodInfo);
                Enabled = false;
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception unpatching method: {ex}");
            }
        }
    }
}
