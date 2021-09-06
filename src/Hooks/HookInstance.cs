using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Mono.CSharp;
using UnityExplorer.CSConsole;

namespace UnityExplorer.Hooks
{
    public class HookInstance
    {
        // Static 

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
            if (GenerateProcessorAndDelegate())
                Patch();
        }

        // Evaluator.source_file 
        private static readonly FieldInfo fi_sourceFile = ReflectionUtility.GetFieldInfo(typeof(Evaluator), "source_file");
        // TypeDefinition.Definition
        private static readonly PropertyInfo pi_Definition = ReflectionUtility.GetPropertyInfo(typeof(TypeDefinition), "Definition");

        private bool GenerateProcessorAndDelegate()
        {
            try
            {
                patchProcessor = ExplorerCore.Harmony.CreateProcessor(TargetMethod);

                // Dynamically compile the patch method

                scriptEvaluator.Run(GeneratePatchSourceCode(TargetMethod));

                if (ScriptEvaluator._reportPrinter.ErrorsCount > 0)
                    throw new FormatException($"Unable to compile the generated patch!");

                // TODO: Publicize MCS to avoid this reflection
                // Get the last defined type in the source file
                var typeContainer = ((CompilationSourceFile)fi_sourceFile.GetValue(scriptEvaluator)).Containers.Last();
                // Get the TypeSpec from the TypeDefinition, then get its "MetaInfo" (System.Type), then get the method called Patch.
                this.patchDelegateMethodInfo = ((TypeSpec)pi_Definition.GetValue((Class)typeContainer, null))
                    .GetMetaInfo()
                    .GetMethod("Patch", ReflectionUtility.FLAGS);

                // Actually create the harmony patch
                this.patchDelegate = new HarmonyMethod(patchDelegateMethodInfo);
                patchProcessor.AddPostfix(patchDelegate);

                return true;
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception creating patch processor for target method {TargetMethod.FullDescription()}!\r\n{ex}");
                return false;
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
                Type pType = param.ParameterType;
                if (pType.IsByRef) pType = pType.GetElementType();
                codeBuilder.Append($", {pType.FullName} __{paramIdx}");
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
                Type pType = param.ParameterType;
                if (pType.IsByRef) pType = pType.GetElementType();
                if (pType.IsValueType)
                    logMessage.AppendLine($"Parameter {paramIdx} {param.Name}: {{__{paramIdx}.ToString()}}");
                else
                    logMessage.AppendLine($"Parameter {paramIdx} {param.Name}: {{__{paramIdx}?.ToString() ?? \"null\"}}");
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
