using HarmonyLib;
using Mono.CSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityExplorer.Runtime;
using UnityExplorer.UI.Panels;
using UniverseLib;

namespace UnityExplorer.CSConsole
{
    public class ScriptInteraction : InteractiveBase
    {
        public static object CurrentTarget
            => InspectorManager.ActiveInspector?.Target;

        public static object[] AllTargets
            => InspectorManager.Inspectors.Select(it => it.Target).ToArray();

        public static void Log(object message)
            => ExplorerCore.Log(message);

        public static void Inspect(object obj)
            => InspectorManager.Inspect(obj);

        public static void Inspect(Type type)
            => InspectorManager.Inspect(type);

        public static Coroutine Start(IEnumerator ienumerator) 
            => RuntimeHelper.StartCoroutine(ienumerator);

        public static void Stop(Coroutine coro)
            => RuntimeHelper.StopCoroutine(coro);

        public static void Copy(object obj) 
            => ClipboardPanel.Copy(obj);

        public static object Paste() 
            => ClipboardPanel.Current;

        public static void GetUsing()
            => Log(Evaluator.GetUsing());

        public static void GetVars()
        {
            var vars = Evaluator.GetVars()?.Trim();
            if (string.IsNullOrEmpty(vars))
                ExplorerCore.LogWarning("No variables seem to be defined!");
            else
                Log(vars);
        }

        public static void GetClasses()
        {
            if (AccessTools.Field(typeof(Evaluator), "source_file")
                    .GetValue(Evaluator) is CompilationSourceFile sourceFile
                && sourceFile.Containers.Any())
            {
                var sb = new StringBuilder();
                sb.Append($"There are {sourceFile.Containers.Count} defined classes:");
                foreach (TypeDefinition type in sourceFile.Containers.Where(it => it is TypeDefinition))
                {
                    sb.Append($"\n\n{type.MemberName.Name}:");
                    foreach (var member in type.Members)
                        sb.Append($"\n\t- {member.AttributeTargets}: \"{member.MemberName.Name}\" ({member.ModFlags})");
                }
                Log(sb.ToString());
            }
            else
                ExplorerCore.LogWarning("No classes seem to be defined.");

        }
    }
}
