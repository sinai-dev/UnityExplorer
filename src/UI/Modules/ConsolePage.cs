using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityExplorer.Console;

namespace UnityExplorer.UI.Modules
{
    public class ConsolePage : MainMenu.Page
    {
        public override string Name => "C# Console";

        public static ConsolePage Instance { get; private set; }

        public CodeEditor m_codeEditor;
        public ScriptEvaluator m_evaluator;

        public static bool EnableAutocompletes { get; set; } = true;
        public static bool EnableAutoIndent { get; set; } = true;

        public static List<Suggestion> AutoCompletes = new List<Suggestion>();
        public static List<string> UsingDirectives;

        public static readonly string[] DefaultUsing = new string[]
        {
            "System",
            "System.Linq",
            "System.Collections",
            "System.Collections.Generic",
            "System.Reflection",
            "UnityEngine",
#if CPP
            "UnhollowerBaseLib",
            "UnhollowerRuntimeLib",
#endif
        };

        public override void Init()
        {
            Instance = this;

            try
            {
                m_codeEditor = new CodeEditor();

                AutoCompleter.Init();

                ResetConsole();

                // Make sure compiler is supported on this platform
                m_evaluator.Compile("");

                foreach (string use in DefaultUsing)
                {
                    AddUsing(use);
                }
            }
            catch (Exception e)
            {
                // TODO remove page button from menu?
                ExplorerCore.LogWarning($"Error setting up console!\r\nMessage: {e.Message}");
            }
        }

        public override void Update()
        {
            m_codeEditor?.Update();

            AutoCompleter.Update();
        }

        public void AddUsing(string asm)
        {
            if (!UsingDirectives.Contains(asm))
            {
                Evaluate($"using {asm};", true);
                UsingDirectives.Add(asm);
            }
        }

        public void Evaluate(string code, bool suppressWarning = false)
        {
            m_evaluator.Compile(code, out Mono.CSharp.CompiledMethod compiled);

            if (compiled == null)
            {
                if (!suppressWarning)
                    ExplorerCore.LogWarning("Unable to compile the code!");
            }
            else
            {
                try
                {
                    object ret = VoidType.Value;
                    compiled.Invoke(ref ret);
                }
                catch (Exception e)
                {
                    if (!suppressWarning)
                        ExplorerCore.LogWarning($"Exception executing code: {e.GetType()}, {e.Message}\r\n{e.StackTrace}");
                }
            }
        }

        public void ResetConsole()
        {
            if (m_evaluator != null)
            {
                m_evaluator.Dispose();
            }

            m_evaluator = new ScriptEvaluator(new StringWriter(new StringBuilder())) { InteractiveBaseClass = typeof(ScriptInteraction) };

            UsingDirectives = new List<string>();
        }

        internal void OnInputChanged()
        {
            if (!EnableAutocompletes)
                return;

            AutoCompleter.CheckAutocomplete();
            AutoCompleter.SetSuggestions(AutoCompletes.ToArray());
        }

        public void UseAutocomplete(string suggestion)
        {
            int cursorIndex = m_codeEditor.InputField.caretPosition;
            string input = m_codeEditor.InputField.text;
            input = input.Insert(cursorIndex, suggestion);
            m_codeEditor.InputField.text = input;
            m_codeEditor.InputField.caretPosition += suggestion.Length;

            AutoCompleter.ClearAutocompletes();
        }

        private class VoidType
        {
            public static readonly VoidType Value = new VoidType();
            private VoidType() { }
        }
    }
}
