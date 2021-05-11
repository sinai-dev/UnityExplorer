using System;
using Mono.CSharp;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityExplorer.Core.Runtime;

namespace UnityExplorer.UI.CSharpConsole
{
    public class ScriptInteraction : InteractiveBase
    {
        public static void Log(object message)
        {
            ExplorerCore.Log(message);
        }

        public static void StartCoroutine(IEnumerator ienumerator)
        {
            RuntimeProvider.Instance.StartCoroutine(ienumerator);
        }

        public static void AddUsing(string directive)
        {
            CSConsole.AddUsing(directive);
        }

        public static void GetUsing()
        {
            ExplorerCore.Log(CSConsole.Evaluator.GetUsing());
        }

        public static void Reset()
        {
            CSConsole.ResetConsole();
        }

        public static object CurrentTarget()
        {
            return InspectorManager.ActiveInspector?.Target;
        }

        public static object[] AllTargets()
        {
            return InspectorManager.Inspectors.Select(it => it.Target).ToArray();
        }

        public static void Inspect(object obj)
        {
            InspectorManager.Inspect(obj);
        }

        public static void Inspect(Type type)
        {
            InspectorManager.Inspect(type);
        }
    }
}