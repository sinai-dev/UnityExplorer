using System;
using Mono.CSharp;
using UnityExplorer.UI;
using UnityExplorer.UI.Main;
using UnityExplorer.Core.Inspectors;
using UnityExplorer.UI.Main.CSConsole;

namespace UnityExplorer.Core.CSharp
{
    public class ScriptInteraction : InteractiveBase
    {
        public static void Log(object message)
        {
            ExplorerCore.Log(message);
        }

        public static void AddUsing(string directive)
        {
            CSharpConsole.Instance.AddUsing(directive);
        }

        public static void GetUsing()
        {
            ExplorerCore.Log(CSharpConsole.Instance.m_evaluator.GetUsing());
        }

        public static void Reset()
        {
            CSharpConsole.Instance.ResetConsole();
        }

        public static object CurrentTarget()
        {
            return InspectorManager.Instance?.m_activeInspector?.Target;
        }

        public static object[] AllTargets()
        {
            int count = InspectorManager.Instance?.m_currentInspectors.Count ?? 0;
            object[] ret = new object[count];
            for (int i = 0; i < count; i++)
            {
                ret[i] = InspectorManager.Instance?.m_currentInspectors[i].Target;
            }
            return ret;
        }

        public static void Inspect(object obj)
        {
            InspectorManager.Instance.Inspect(obj);
        }

        public static void Inspect(Type type)
        {
            InspectorManager.Instance.Inspect(type);
        }
    }
}