using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Mono.CSharp;
using System.Reflection;
using System.IO;
#if CPP
using UnhollowerRuntimeLib;
#endif 

namespace Explorer.UI.Main
{
    public class ConsolePage : BaseMainMenuPage
    {
        public static ConsolePage Instance { get; private set; }

        public override string Name { get => "C# Console"; }

        private ScriptEvaluator m_evaluator;

        public const string INPUT_CONTROL_NAME = "consoleInput";
        private string m_input = "";
        private string m_prevInput = "";
        private string m_usingInput = "";

        public static List<AutoComplete> AutoCompletes = new List<AutoComplete>();
        public static List<string> UsingDirectives;

        private Vector2 inputAreaScroll;
        private Vector2 autocompleteScroll;
        public static TextEditor textEditor;
        private bool shouldRefocus;

        public static GUIStyle AutocompleteStyle => autocompleteStyle ?? GetCompletionStyle();
        private static GUIStyle autocompleteStyle;

        public static readonly string[] DefaultUsing = new string[]
        {
            "System",
            "UnityEngine",
            "System.Linq",
            "System.Collections",
            "System.Collections.Generic",
            "System.Reflection"
        };

        public override void Init()
        {
            Instance = this;

            try
            {
                m_input = @"// For a list of helper methods, execute the 'Help();' method.
// Enable the Console Window with your Mod Loader to see log output.

Help();";
                ResetConsole();

                foreach (var use in DefaultUsing)
                {
                    AddUsing(use);
                }
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning($"Error setting up console!\r\nMessage: {e.Message}");
                MainMenu.SetCurrentPage(0);
                MainMenu.Pages.Remove(this);
            }
        }

        public override void Update() { }

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

            m_evaluator.Compile(str, out var compiled);

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
                    ExplorerCore.LogWarning(e.GetType() + ", " + e.Message);
                }
            }

            return ret;
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


        public override void DrawWindow()
        {
            GUILayout.Label("<b><size=15><color=cyan>C# Console</color></size></b>", new GUILayoutOption[0]);

            GUI.skin.label.alignment = TextAnchor.UpperLeft;

            // SCRIPT INPUT

            GUILayout.Label("Enter code here as though it is a method body:", new GUILayoutOption[0]);

            inputAreaScroll = GUIUnstrip.BeginScrollView(
                inputAreaScroll, 
                new GUILayoutOption[] { GUILayout.Height(250), GUILayout.ExpandHeight(true) }
            );

            GUI.SetNextControlName(INPUT_CONTROL_NAME);
            m_input = GUIUnstrip.TextArea(m_input, new GUILayoutOption[] { GUILayout.ExpandHeight(true) });

            GUIUnstrip.EndScrollView();

            // EXECUTE BUTTON

            if (GUILayout.Button("<color=cyan><b>Execute</b></color>", new GUILayoutOption[0]))
            {
                try
                {
                    m_input = m_input.Trim();

                    if (!string.IsNullOrEmpty(m_input))
                    {
                        Evaluate(m_input);

                        //var result = Evaluate(m_input);

                        //if (result != null && !Equals(result, VoidType.Value))
                        //{
                        //    ExplorerCore.Log("[Console Output]\r\n" + result.ToString());
                        //}
                    }
                }
                catch (Exception e)
                {
                    ExplorerCore.LogError("Exception compiling!\r\nMessage: " + e.Message + "\r\nStack: " + e.StackTrace);
                }
            }

            // SUGGESTIONS
            if (AutoCompletes.Count > 0)
            {
                autocompleteScroll = GUIUnstrip.BeginScrollView(autocompleteScroll, new GUILayoutOption[] { GUILayout.Height(150) });

                var origSkin = GUI.skin.button;
                GUI.skin.button = AutocompleteStyle;

                foreach (var autocomplete in AutoCompletes)
                {
                    AutocompleteStyle.normal.textColor = autocomplete.TextColor;
                    if (GUILayout.Button(autocomplete.Full, new GUILayoutOption[] { GUILayout.Width(MainMenu.MainRect.width - 50) }))
                    {
                        UseAutocomplete(autocomplete.Addition);
                        break;
                    }
                }

                GUI.skin.button = origSkin;

                GUIUnstrip.EndScrollView();
            }

            if (shouldRefocus)
            {
                GUI.FocusControl(INPUT_CONTROL_NAME);
                shouldRefocus = false;
            }      

            // USING DIRECTIVES

            GUILayout.Label("<b>Using directives:</b>", new GUILayoutOption[0]);

            GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Label("Add namespace:", new GUILayoutOption[] { GUILayout.Width(105) });
            m_usingInput = GUIUnstrip.TextField(m_usingInput, new GUILayoutOption[] { GUILayout.Width(150) });
            if (GUILayout.Button("<b><color=lime>Add</color></b>", new GUILayoutOption[] { GUILayout.Width(120) }))
            {
                AddUsing(m_usingInput);
            }
            if (GUILayout.Button("<b><color=red>Clear All</color></b>", new GUILayoutOption[] { GUILayout.Width(120) }))
            {
                ResetConsole();
            }
            GUILayout.EndHorizontal();

            foreach (var asm in UsingDirectives)
            {
                GUILayout.Label(AsmToUsing(asm, true), new GUILayoutOption[0]);
            }

            CheckAutocomplete();
        }

        private void CheckAutocomplete()
        {
            // Temporary disabling this check in BepInEx Il2Cpp.
#if BIE
#if CPP
#else
            if (GUI.GetNameOfFocusedControl() != INPUT_CONTROL_NAME)
                return;
#endif
#else
            if (GUI.GetNameOfFocusedControl() != INPUT_CONTROL_NAME)
                return;
#endif

#if CPP
            textEditor = GUIUtility.GetStateObject(Il2CppType.Of<TextEditor>(), GUIUtility.keyboardControl).TryCast<TextEditor>();
#else
            textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
#endif

            var input = m_input;

            if (!string.IsNullOrEmpty(input))
            {
                try
                {
                    var splitChars = new[] { ',', ';', '<', '>', '(', ')', '[', ']', '=', '|', '&' };

                    // Credit ManlyMarco
                    // Separate input into parts, grab only the part with cursor in it
                    var cursorIndex = textEditor.cursorIndex;
                    var start = cursorIndex <= 0 ? 0 : input.LastIndexOfAny(splitChars, cursorIndex - 1) + 1;
                    var end = cursorIndex <= 0 ? input.Length : input.IndexOfAny(splitChars, cursorIndex - 1);
                    if (end < 0 || end < start) end = input.Length;
                    input = input.Substring(start, end - start);
                }
                catch (ArgumentException) { }

                if (!string.IsNullOrEmpty(input) && input != m_prevInput)
                {
                    GetAutocompletes(input);
                }
            }
            else
            {
                ClearAutocompletes();
            }

            m_prevInput = input;
        }

        private void ClearAutocompletes()
        {
            if (AutoCompletes.Any())
            {
                AutoCompletes.Clear();
                shouldRefocus = true;
            }
        }

        private void UseAutocomplete(string suggestion)
        {
            int cursorIndex = textEditor.cursorIndex;
            m_input = m_input.Insert(cursorIndex, suggestion);

            ClearAutocompletes();
            shouldRefocus = true;
        }

        private void GetAutocompletes(string input)
        {
            try
            {
                //ExplorerCore.Log("Fetching suggestions for input " + input);

                // Credit ManylMarco
                AutoCompletes.Clear();
                var completions = m_evaluator.GetCompletions(input, out string prefix);
                if (completions != null)
                {
                    if (prefix == null)
                        prefix = input;

                    AutoCompletes.AddRange(completions
                        .Where(x => !string.IsNullOrEmpty(x))
                        .Select(x => new AutoComplete(x, prefix, AutoComplete.Contexts.Other))
                        );
                }

                var trimmed = input.Trim();
                if (trimmed.StartsWith("using"))
                    trimmed = trimmed.Remove(0, 5).Trim();

                var namespaces = AutoCompleteHelpers.Namespaces
                    .Where(x => x.StartsWith(trimmed) && x.Length > trimmed.Length)
                    .Select(x => new AutoComplete(
                        x.Substring(trimmed.Length), 
                        x.Substring(0, trimmed.Length), 
                        AutoComplete.Contexts.Namespace));

                AutoCompletes.AddRange(namespaces);

                shouldRefocus = true;
            }
            catch (Exception ex)
            {
                ExplorerCore.Log("C# Console error:\r\n" + ex);
                ClearAutocompletes();
            }
        }

        // Credit ManlyMarco
        private static GUIStyle GetCompletionStyle()
        {
            return autocompleteStyle = new GUIStyle(GUI.skin.button)
            {
                border = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                hover = { background = Texture2D.whiteTexture, textColor = Color.black },
                normal = { background = null },
                focused = { background = Texture2D.whiteTexture, textColor = Color.black },
                active = { background = Texture2D.whiteTexture, textColor = Color.black },
                alignment = TextAnchor.MiddleLeft,
            };
        }

        private class VoidType
        {
            public static readonly VoidType Value = new VoidType();
            private VoidType() { }
        }
    }
}
