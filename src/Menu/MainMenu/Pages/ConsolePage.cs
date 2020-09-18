using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using MelonLoader;
using Mono.CSharp;
using UnityEngine;

namespace Explorer
{
    public class ConsolePage : WindowPage
    {
        public override string Name { get => "C# Console"; }

        private ScriptEvaluator _evaluator;
        private readonly StringBuilder _sb = new StringBuilder();

        private Vector2 inputAreaScroll;

        private string MethodInput = "";
        private string UsingInput = "";

        public static List<string> UsingDirectives;

        private static readonly string[] m_defaultUsing = new string[]
        {
            "System",
            "UnityEngine",
            "System.Linq",
            "System.Collections",
            "System.Collections.Generic",
            "System.Reflection",
            "MelonLoader"
        };

        public override void Init()
        {
            UnhollowerRuntimeLib.ClassInjector.RegisterTypeInIl2Cpp<ReplHelper>();

            try
            {
                MethodInput = @"// This is a basic C# console. 
// Some common using directives are added by default, you can add more below.
// If you want to return some output, MelonLogger.Log() it.

MelonLogger.Log(""hello world"");";

                ResetConsole();

                foreach (var use in m_defaultUsing)
                {
                    AddUsing(use);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Log($"Error setting up console!\r\nMessage: {e.Message}");
                MainMenu.SetCurrentPage(0);
                MainMenu.Pages.Remove(this);
            }
        }

        public void ResetConsole()
        {
            if (_evaluator != null)
            {
                _evaluator.Dispose();
            }

            _evaluator = new ScriptEvaluator(new StringWriter(_sb)) { InteractiveBaseClass = typeof(REPL) };

            UsingDirectives = new List<string>();
        }

        public string AsmToUsing(string asm, bool richtext = false)
        {
            if (richtext)
            {
                return $"<color=#569cd6>using</color> {asm};";
            }
            return $"using {asm};";
        }

        public void AddUsing(string asm)
        {
            if (!UsingDirectives.Contains(asm))
            {
                UsingDirectives.Add(asm);
                Evaluate(AsmToUsing(asm), true);
            }
        }

        public object Evaluate(string str, bool suppressWarning = false)
        {
            object ret = VoidType.Value;

            _evaluator.Compile(str, out var compiled);

            try
            {
                if (compiled == null)
                {
                    throw new Exception("Mono.Csharp Service was unable to compile the code provided.");
                }

                compiled.Invoke(ref ret);
            }
            catch (Exception e)
            {
                if (!suppressWarning)
                {
                    MelonLogger.LogWarning(e.GetType() + ", " + e.Message);
                }
            }

            return ret;
        }


        public override void DrawWindow()
        {
            GUILayout.Label("<b><size=15><color=cyan>C# Console</color></size></b>", null);

            GUI.skin.label.alignment = TextAnchor.UpperLeft;

            GUILayout.Label("Enter code here as though it is a method body:", null);

            inputAreaScroll = GUIUnstrip.BeginScrollView(inputAreaScroll, new GUILayoutOption[] { GUILayout.Height(250) });

            MethodInput = GUILayout.TextArea(MethodInput, new GUILayoutOption[] { GUILayout.ExpandHeight(true) });

            GUIUnstrip.EndScrollView();

            if (GUILayout.Button("<color=cyan><b>Execute</b></color>", null))
            {
                try
                {
                    MethodInput = MethodInput.Trim();

                    if (!string.IsNullOrEmpty(MethodInput))
                    {
                        var result = Evaluate(MethodInput);

                        if (result != null && !Equals(result, VoidType.Value))
                        {
                            MelonLogger.Log("[Console Output]\r\n" + result.ToString());
                        }
                    }
                }
                catch (Exception e)
                {
                    MelonLogger.LogError("Exception compiling!\r\nMessage: " + e.Message + "\r\nStack: " + e.StackTrace);
                }
            }

            GUILayout.Label("<b>Using directives:</b>", null);

            GUILayout.BeginHorizontal(null);
            GUILayout.Label("Add namespace:", new GUILayoutOption[] { GUILayout.Width(105) });
            UsingInput = GUILayout.TextField(UsingInput, new GUILayoutOption[] { GUILayout.Width(150) });
            if (GUILayout.Button("<b><color=lime>Add</color></b>", new GUILayoutOption[] { GUILayout.Width(120) }))
            {
                AddUsing(UsingInput);
            }
            if (GUILayout.Button("<b><color=red>Clear All</color></b>", new GUILayoutOption[] { GUILayout.Width(120) }))
            {
                ResetConsole();
            }
            GUILayout.EndHorizontal();

            foreach (var asm in UsingDirectives)
            {
                GUILayout.Label(AsmToUsing(asm, true), null);
            }            
        }

        public override void Update() { }

        private class VoidType
        {
            public static readonly VoidType Value = new VoidType();
            private VoidType() { }
        }

    }

    internal class ScriptEvaluator : Evaluator, IDisposable
    {
        private static readonly HashSet<string> StdLib =
                new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) { "mscorlib", "System.Core", "System", "System.Xml" };

        private readonly TextWriter _logger;

        public ScriptEvaluator(TextWriter logger) : base(BuildContext(logger))
        {
            _logger = logger;

            ImportAppdomainAssemblies(ReferenceAssembly);
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
            _logger.Dispose();
        }

        private void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            string name = args.LoadedAssembly.GetName().Name;
            if (StdLib.Contains(name))
                return;
            ReferenceAssembly(args.LoadedAssembly);
        }

        private static CompilerContext BuildContext(TextWriter tw)
        {
            var reporter = new StreamReportPrinter(tw);

            var settings = new CompilerSettings
            {
                Version = LanguageVersion.Experimental,
                GenerateDebugInfo = false,
                StdLib = true,
                Target = Target.Library,
                WarningLevel = 0,
                EnhancedWarnings = false
            };

            return new CompilerContext(settings, reporter);
        }

        private static void ImportAppdomainAssemblies(Action<Assembly> import)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string name = assembly.GetName().Name;
                if (StdLib.Contains(name))
                    continue;
                import(assembly);
            }
        }
    }
}
