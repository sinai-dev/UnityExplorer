using System;
using Mono.CSharp;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityExplorer.Core.Runtime;

namespace UnityExplorer.UI.CSConsole
{
    public class ScriptInteraction : InteractiveBase
    {
        internal const string STARTUP_TEXT = @"Welcome to the UnityExplorer C# Console.

Use this console to declare temporary C# classes, or evaluate standalone expressions as though you were executing a method body. "+
@"Use the <b>Reset</b> button to clear all classes and variables, and restore the default using directives.

* <color=#add490>using SomeNamespace;</color> to add a namespace to the Console. This is not required if you use full namespaces to access classes.

When evaluating standalone expressions, these helpers are available:

* System.Object <color=#96b570>CurrentTarget</color> - the target of the active Inspector tab

* System.Object[] <color=#96b570>AllTargets</color> - an array containing the targets of all Inspector tabs

* void <color=#add490>Log(""message"")</color> - logs a message to the console log

* void <color=#add490>Inspect(someObject)</color> - inspect an instance, eg. Inspect(Camera.main);

* void <color=#add490>Inspect(typeof(SomeClass))</color> - inspect a Class with static reflection

* void <color=#add490>StartCoroutine(ienumerator)</color> - start the IEnumerator as a Coroutine

";

        public static void Log(object message)
        {
            ExplorerCore.Log(message);
        }

        public static object CurrentTarget => InspectorManager.ActiveInspector?.Target;

        public static object[] AllTargets() => InspectorManager.Inspectors.Select(it => it.Target).ToArray();

        public static void Inspect(object obj)
        {
            InspectorManager.Inspect(obj);
        }

        public static void Inspect(Type type)
        {
            InspectorManager.Inspect(type);
        }

        public static void StartCoroutine(IEnumerator ienumerator)
        {
            RuntimeProvider.Instance.StartCoroutine(ienumerator);
        }
    }
}