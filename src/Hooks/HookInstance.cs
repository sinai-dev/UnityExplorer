using HarmonyLib;
using Mono.CSharp;
using System;
using System.Collections.Generic;
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
            scriptEvaluator.Run("using System.Text;");
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

        private static readonly HashSet<string> namespaceUsings = new();

        public HookInstance(MethodInfo targetMethod)
        {
            this.TargetMethod = targetMethod;
            this.shortSignature = TargetMethod.FullDescription();

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

            StringBuilder codeBuilder = new();
            namespaceUsings.Clear();

            try
            {
                patchProcessor = ExplorerCore.Harmony.CreateProcessor(TargetMethod);

                // Dynamically compile the patch method

                foreach (string ns in namespaceUsings)
                    codeBuilder.AppendLine($"using {ns};");

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

                ExplorerCore.Log(codeBuilder.ToString());

                return false;
            }
        }

        private string GenerateDefaultPatchSourceCode(MethodInfo targetMethod)
        {
            StringBuilder codeBuilder = new();

            codeBuilder.Append("public static void Postfix("); // System.Reflection.MethodBase __originalMethod

            bool isStatic = targetMethod.IsStatic;
            if (!isStatic)
                codeBuilder.Append($"{targetMethod.DeclaringType.FullDescription()} __instance");

            if (targetMethod.ReturnType != typeof(void))
            {
                if (!isStatic)
                    codeBuilder.Append(", ");
                codeBuilder.Append($"{targetMethod.ReturnType.FullDescription()} __result");
            }

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
            codeBuilder.AppendLine("       StringBuilder sb = new StringBuilder();");
            codeBuilder.AppendLine($"       sb.AppendLine(\"---- Patched called ----\");");
            codeBuilder.AppendLine($"       sb.AppendLine(\"{shortSignature}\");");

            if (!targetMethod.IsStatic)
                codeBuilder.AppendLine($"       sb.Append(\"- __instance: \").AppendLine(__instance.ToString());");

            paramIdx = 0;
            foreach (ParameterInfo param in parameters)
            {
                codeBuilder.Append($"       sb.Append(\"- Parameter {paramIdx} '{param.Name}': \")");

                Type pType = param.ParameterType;
                if (pType.IsByRef) pType = pType.GetElementType();
                if (pType.IsValueType)
                    codeBuilder.AppendLine($".AppendLine(__{paramIdx}.ToString());");
                else
                    codeBuilder.AppendLine($".AppendLine(__{paramIdx}?.ToString() ?? \"null\");");

                paramIdx++;
            }

            if (targetMethod.ReturnType != typeof(void))
            {
                codeBuilder.Append("       sb.Append(\"- Return value: \")");
                if (targetMethod.ReturnType.IsValueType)
                    codeBuilder.AppendLine(".AppendLine(__result.ToString());");
                else
                    codeBuilder.AppendLine(".AppendLine(__result?.ToString() ?? \"null\");");
            }

            codeBuilder.AppendLine($"       UnityExplorer.ExplorerCore.Log(sb.ToString());");
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
