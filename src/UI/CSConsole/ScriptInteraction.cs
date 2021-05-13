using System;
using Mono.CSharp;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityExplorer.Core.Runtime;
using System.Text;

namespace UnityExplorer.UI.CSConsole
{
    public class ScriptInteraction : InteractiveBase
    {
        internal const string STARTUP_TEXT = @"<color=#5d8556>// Compile a using directive to add it to the console (until Reset)</color>
using SomeNamespace;

<color=#5d8556>// Compile a C# class and it will exist until Reset</color>
<color=#5a728c>public class</color> SomeClass {
    <color=#5a728c>public static void</color> SomeMethod() {
    }
}

<color=#5d8556>// If not compiling any usings or classes, the code will run immediately (REPL).
// Variables you define in REPL mode will also exist until Reset.
// In REPL context, the following helpers are available:</color>

* System.Object <color=#add490>CurrentTarget</color> - the target of the active Inspector tab
* System.Object[] <color=#add490>AllTargets</color> - an array containing the targets of all Inspector tabs
* void <color=#add490>Log(""message"")</color> - prints a message to the console log
* void <color=#add490>Inspect(someObject)</color> - inspect an instance, eg. Inspect(Camera.main);
* void <color=#add490>Inspect(typeof(SomeClass))</color> - inspect a Class with static reflection
* void <color=#add490>StartCoroutine(ienumerator)</color> - start the IEnumerator as a Coroutine
* void <color=#add490>GetUsing()</color> - prints the current using directives to the console log
* void <color=#add490>GetVars()</color> - prints the variables you have defined and their current values
* void <color=#add490>GetClasses()</color> - prints the names of the classes you have defined, and their members";

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

        public static void GetUsing()
        {
            Log(Evaluator.GetUsing());
        }

        public static void GetVars()
        {
            Log(Evaluator.GetVars());
        }

        public static void GetClasses()
        {
            if (ReflectionUtility.GetFieldInfo(typeof(Evaluator), "source_file")
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