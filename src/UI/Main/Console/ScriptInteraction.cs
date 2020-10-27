using System;
using Mono.CSharp;

namespace ExplorerBeta.UI.Main.Console
{
    public class ScriptInteraction : InteractiveBase
    {
        public static void Log(object message)
        {
            ExplorerCore.Log(message);
        }

        public static void AddUsing(string directive)
        {
            ConsolePage.Instance.AddUsing(directive);
        }

        public static void GetUsing()
        {
            ExplorerCore.Log(ConsolePage.Instance.m_evaluator.GetUsing());
        }

        public static void Reset()
        {
            ConsolePage.Instance.ResetConsole();
        }

        public static object CurrentTarget()
        {
            throw new NotImplementedException("TODO");
        }

        public static object[] AllTargets()
        {
            throw new NotImplementedException("TODO");
        }

        public static void Inspect(object obj)
        {
            throw new NotImplementedException("TODO");
        }

        public static void Inspect(Type type)
        {
            throw new NotImplementedException("TODO");
        }

        //        public static void Help()
        //        {
        //            ExplorerCore.Log(@"
        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        //              C# Console Help

        //The following helper methods are available:

        //void Log(object message)
        //    prints a message to the console window and debug log
        //    usage: Log(""hello world"");

        //void AddUsing(string directive)
        //    adds a using directive to the console.
        //    usage: AddUsing(""UnityEngine.UI"");

        //void GetUsing()
        //    logs the current using directives to the debug console
        //    usage: GetUsing();

        //void Reset()
        //    resets the C# console, clearing all variables and using directives.
        //    usage: Reset();
        //");

        //TODO:

        //ExplorerCore.Log("object CurrentTarget()");
        //ExplorerCore.Log("    returns the target object of the current tab (in tab view mode only)");
        //ExplorerCore.Log("    usage: var target = CurrentTarget();");
        //ExplorerCore.Log("");
        //ExplorerCore.Log("object[] AllTargets()");
        //ExplorerCore.Log("    returns an object[] array containing all currently inspected objects");
        //ExplorerCore.Log("    usage: var targets = AllTargets();");
        //ExplorerCore.Log("");
        //ExplorerCore.Log("void Inspect(object obj)");
        //ExplorerCore.Log("    inspects the provided object in a new window.");
        //ExplorerCore.Log("    usage: Inspect(Camera.main);");
        //ExplorerCore.Log("");
        //ExplorerCore.Log("void Inspect(Type type)");
        //ExplorerCore.Log("    attempts to inspect the provided type with static-only reflection.");
        //ExplorerCore.Log("    usage: Inspect(typeof(Camera));");
        //}
    }
}