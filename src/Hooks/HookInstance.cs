using HarmonyLib;
using Mono.CSharp;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityExplorer.CSConsole;
using UniverseLib;

namespace UnityExplorer.Hooks
{
    public class HookInstance
    {
        // Static 

        private static readonly StringBuilder evalOutput = new();
        private static readonly ScriptEvaluator scriptEvaluator = new(new StringWriter(evalOutput));

        static HookInstance()
        {
            scriptEvaluator.Run("using System;");
            scriptEvaluator.Run("using System.Reflection;");
            scriptEvaluator.Run("using System.Collections;");
            scriptEvaluator.Run("using System.Collections.Generic;");
        }

        // Instance

        public bool Enabled;
        public MethodInfo TargetMethod;
        public string PatchSourceCode;

        private readonly string shortSignature;
        private PatchProcessor patchProcessor;

        private MethodInfo postfix;
        private MethodInfo prefix;
        private MethodInfo finalizer;
        private MethodInfo transpiler;

        public HookInstance(MethodInfo targetMethod)
        {
            this.TargetMethod = targetMethod;
            this.shortSignature = $"{targetMethod.DeclaringType.Name}.{targetMethod.Name}";

            GenerateDefaultPatchSourceCode(targetMethod);

            if (CompileAndGenerateProcessor(PatchSourceCode))
                Patch();
        }

        // Evaluator.source_file 
        private static readonly FieldInfo fi_sourceFile = AccessTools.Field(typeof(Evaluator), "source_file");
        // TypeDefinition.Definition
        private static readonly PropertyInfo pi_Definition = AccessTools.Property(typeof(TypeDefinition), "Definition");

        public bool CompileAndGenerateProcessor(string patchSource)
        {
            Unpatch();

            try
            {
                patchProcessor = ExplorerCore.Harmony.CreateProcessor(TargetMethod);

                // Dynamically compile the patch method

                StringBuilder codeBuilder = new();

                codeBuilder.AppendLine($"public class DynamicPatch_{DateTime.Now.Ticks}");
                codeBuilder.AppendLine("{");
                codeBuilder.AppendLine(patchSource);
                codeBuilder.AppendLine("}");

                scriptEvaluator.Run(codeBuilder.ToString());

                if (ScriptEvaluator._reportPrinter.ErrorsCount > 0)
                    throw new FormatException($"Unable to compile the generated patch!");

                // TODO: Publicize MCS to avoid this reflection
                // Get the most recent Patch type in the source file
                TypeContainer typeContainer = ((CompilationSourceFile)fi_sourceFile.GetValue(scriptEvaluator))
                    .Containers
                    .Last(it => it.MemberName.Name.StartsWith("DynamicPatch_"));
                // Get the TypeSpec from the TypeDefinition, then get its "MetaInfo" (System.Type)
                Type patchClass = ((TypeSpec)pi_Definition.GetValue((Class)typeContainer, null)).GetMetaInfo();

                // Create the harmony patches as defined

                postfix = patchClass.GetMethod("Postfix", ReflectionUtility.FLAGS);
                if (postfix != null)
                    patchProcessor.AddPostfix(new HarmonyMethod(postfix));

                prefix = patchClass.GetMethod("Prefix", ReflectionUtility.FLAGS);
                if (prefix != null)
                    patchProcessor.AddPrefix(new HarmonyMethod(prefix));

                finalizer = patchClass.GetMethod("Finalizer", ReflectionUtility.FLAGS);
                if (finalizer != null)
                    patchProcessor.AddFinalizer(new HarmonyMethod(finalizer));

                transpiler = patchClass.GetMethod("Transpiler", ReflectionUtility.FLAGS);
                if (transpiler != null)
                    patchProcessor.AddTranspiler(new HarmonyMethod(transpiler));

                return true;
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception creating patch processor for target method {TargetMethod.FullDescription()}!\r\n{ex}");
                return false;
            }
        }

        private string GenerateDefaultPatchSourceCode(MethodInfo targetMethod)
        {
            StringBuilder codeBuilder = new();
            // Arguments 

            codeBuilder.Append("public static void Postfix(System.Reflection.MethodBase __originalMethod");

            if (!targetMethod.IsStatic)
                codeBuilder.Append($", {targetMethod.DeclaringType.FullName} __instance");

            if (targetMethod.ReturnType != typeof(void))
                codeBuilder.Append($", {targetMethod.ReturnType.FullName} __result");

            ParameterInfo[] parameters = targetMethod.GetParameters();

            int paramIdx = 0;
            foreach (ParameterInfo param in parameters)
            {
                codeBuilder.Append($", {param.ParameterType.FullDescription().Replace("&", "")} __{paramIdx}");
                paramIdx++;
            }

            codeBuilder.Append(")\n");

            // Patch body

            codeBuilder.AppendLine("{");

            codeBuilder.AppendLine("    try {");

            // Log message 

            StringBuilder logMessage = new();
            logMessage.Append($"Patch called: {shortSignature}\\n");

            if (!targetMethod.IsStatic)
                logMessage.Append("__instance: {__instance.ToString()}\\n");

            paramIdx = 0;
            foreach (ParameterInfo param in parameters)
            {
                logMessage.Append($"Parameter {paramIdx} {param.Name}: ");
                Type pType = param.ParameterType;
                if (pType.IsByRef) pType = pType.GetElementType();
                if (pType.IsValueType)
                    logMessage.Append($"{{__{paramIdx}.ToString()}}");
                else
                    logMessage.Append($"{{__{paramIdx}?.ToString() ?? \"null\"}}");
                logMessage.Append("\\n");
                paramIdx++;
            }

            if (targetMethod.ReturnType != typeof(void))
            {
                logMessage.Append("Return value: ");
                if (targetMethod.ReturnType.IsValueType)
                    logMessage.Append("{__result.ToString()}");
                else
                    logMessage.Append("{__result?.ToString() ?? \"null\"}");
                logMessage.Append("\\n");
            }

            codeBuilder.AppendLine($"        UnityExplorer.ExplorerCore.Log($\"{logMessage}\");");
            codeBuilder.AppendLine("    }");
            codeBuilder.AppendLine("    catch (System.Exception ex) {");
            codeBuilder.AppendLine($"        UnityExplorer.ExplorerCore.LogWarning($\"Exception in patch of {shortSignature}:\\n{{ex}}\");");
            codeBuilder.AppendLine("    }");

            // End patch body

            codeBuilder.AppendLine("}");

            //ExplorerCore.Log(codeBuilder.ToString());

            return PatchSourceCode = codeBuilder.ToString();
        }

        public void TogglePatch()
        {
            if (!Enabled)
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
            try
            {
                if (prefix != null)
                    patchProcessor.Unpatch(prefix);
                if (postfix != null)
                    patchProcessor.Unpatch(postfix);
                if (finalizer != null)
                    patchProcessor.Unpatch(finalizer);
                if (transpiler != null)
                    patchProcessor.Unpatch(transpiler);

                Enabled = false;
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception unpatching method: {ex}");
            }
        }
    }
}
