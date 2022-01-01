using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI;
using UniverseLib.UI.Models;
using UnityExplorer.UI.Widgets.AutoComplete;
using UniverseLib.UI;
using UniverseLib;

namespace UnityExplorer.CacheObject.Views
{
    public class EvaluateWidget : IPooledObject
    {
        public CacheMember Owner { get; set; }

        public GameObject UIRoot { get; set; }
        public float DefaultHeight => -1f;

        private ParameterInfo[] arguments;
        private string[] argumentInput;

        private GameObject argHolder;
        private readonly List<GameObject> argRows = new List<GameObject>();
        private readonly List<Text> argLabels = new List<Text>();
        private readonly List<InputFieldRef> argInputFields = new List<InputFieldRef>();
        private readonly List<Dropdown> argDropdowns = new List<Dropdown>();

        private Type[] genericArguments;
        private string[] genericInput;

        private GameObject genericArgHolder;
        private readonly List<GameObject> genericArgRows = new List<GameObject>();
        private readonly List<Text> genericArgLabels = new List<Text>();
        private readonly List<TypeCompleter> genericAutocompleters = new List<TypeCompleter>();
        private readonly List<InputFieldRef> genericInputFields = new List<InputFieldRef>();

        public void OnBorrowedFromPool(CacheMember owner)
        {
            this.Owner = owner;

            arguments = owner.Arguments;
            argumentInput = new string[arguments.Length];

            genericArguments = owner.GenericArguments;
            genericInput = new string[genericArguments.Length];

            SetArgRows();

            this.UIRoot.SetActive(true);

            InspectorManager.OnInspectedTabsChanged += InspectorManager_OnInspectedTabsChanged;
        }

        public void OnReturnToPool()
        {
            foreach (var input in argInputFields)
                input.Text = "";
            foreach (var input in genericInputFields)
                input.Text = "";

            this.Owner = null;

            InspectorManager.OnInspectedTabsChanged -= InspectorManager_OnInspectedTabsChanged;
        }

        private void InspectorManager_OnInspectedTabsChanged()
        {
            for (int i = 0; i < argDropdowns.Count; i++)
            {
                var drop = argDropdowns[i];
                PopulateNonPrimitiveArgumentDropdown(drop, i);
            }
        }

        public Type[] TryParseGenericArguments()
        {
            Type[] outArgs = new Type[genericArguments.Length];

            for (int i = 0; i < genericArguments.Length; i++)
            {
                outArgs[i] = ReflectionUtility.GetTypeByName(genericInput[i])
                    ?? throw new Exception($"Could not find any type by name '{genericInput[i]}'!");
            }

            return outArgs;
        }

        public object[] TryParseArguments()
        {
            object[] outArgs = new object[arguments.Length];

            for (int i = 0; i < arguments.Length; i++)
            {
                var paramInfo = arguments[i];

                var argType = paramInfo.ParameterType;
                if (argType.IsByRef)
                    argType = argType.GetElementType();

                if (ParseUtility.CanParse(argType))
                {
                    var input = argumentInput[i];

                    if (argType == typeof(string))
                    {
                        outArgs[i] = input;
                        continue;
                    }

                    if (string.IsNullOrEmpty(input))
                    {
                        if (paramInfo.IsOptional)
                            outArgs[i] = paramInfo.DefaultValue;
                        else
                            outArgs[i] = null;
                        continue;
                    }

                    if (!ParseUtility.TryParse(input, argType, out outArgs[i], out Exception ex))
                    {
                        outArgs[i] = null;
                        ExplorerCore.LogWarning($"Cannot parse argument '{paramInfo.Name}' ({paramInfo.ParameterType.Name})" +
                            $"{(ex == null ? "" : $", {ex.GetType().Name}: {ex.Message}")}");
                    }
                }
                else
                {
                    var dropdown = argDropdowns[i];
                    if (dropdown.value == 0)
                    {
                        outArgs[i] = null;
                    }
                    else
                    {
                        // Probably should do this in a better way...
                        // Parse the text of the dropdown option to get the inspector tab index.
                        string tabIndexString = "";
                        for (int j = 4; ; j++)
                        {
                            char c = dropdown.options[dropdown.value].text[j];
                            if (c == ':')
                                break;
                            tabIndexString += c;
                        }
                        int tabIndex = int.Parse(tabIndexString);
                        var tab = InspectorManager.Inspectors[tabIndex - 1];

                        outArgs[i] = tab.Target;
                    }
                }
            }

            return outArgs;
        }

        private void SetArgRows()
        {
            if (genericArguments.Any())
            {
                genericArgHolder.SetActive(true);
                SetGenericRows();
            }
            else
                genericArgHolder.SetActive(false);

            if (arguments.Any())
            {
                argHolder.SetActive(true);
                SetNormalArgRows();
            }
            else
                argHolder.SetActive(false);
        }

        private void SetGenericRows()
        {
            for (int i = 0; i < genericArguments.Length || i < genericArgRows.Count; i++)
            {
                if (i >= genericArguments.Length)
                {
                    if (i >= genericArgRows.Count)
                        break;
                    else
                        // exceeded actual args, but still iterating so there must be views left, disable them
                        genericArgRows[i].SetActive(false);
                    continue;
                }

                var arg = genericArguments[i];

                if (i >= genericArgRows.Count)
                    AddArgRow(i, true);

                genericArgRows[i].SetActive(true);

                var autoCompleter = genericAutocompleters[i];
                autoCompleter.BaseType = arg;
                autoCompleter.CacheTypes();

                var constraints = arg.GetGenericParameterConstraints();
                autoCompleter.GenericConstraints = constraints;

                var sb = new StringBuilder($"<color={SignatureHighlighter.CONST}>{arg.Name}</color>");

                for (int j = 0; j < constraints.Length; j++)
                {
                    if (j == 0) sb.Append(' ').Append('(');
                    else sb.Append(',').Append(' ');

                    sb.Append(SignatureHighlighter.Parse(constraints[j], false));

                    if (j + 1 == constraints.Length)
                        sb.Append(')');
                }

                genericArgLabels[i].text = sb.ToString();
            }
        }

        private void SetNormalArgRows()
        {
            for (int i = 0; i < arguments.Length || i < argRows.Count; i++)
            {
                if (i >= arguments.Length)
                {
                    if (i >= argRows.Count)
                        break;
                    else
                        // exceeded actual args, but still iterating so there must be views left, disable them
                        argRows[i].SetActive(false);
                    continue;
                }

                var arg = arguments[i];
                var argType = arg.ParameterType;
                if (argType.IsByRef)
                    argType = argType.GetElementType();

                if (i >= argRows.Count)
                    AddArgRow(i, false);

                argRows[i].SetActive(true);
                argLabels[i].text = 
                    $"{SignatureHighlighter.Parse(argType, false)} <color={SignatureHighlighter.LOCAL_ARG}>{arg.Name}</color>";

                if (ParseUtility.CanParse(argType))
                {
                    argInputFields[i].Component.gameObject.SetActive(true);
                    argDropdowns[i].gameObject.SetActive(false);

                    if (arg.ParameterType == typeof(string))
                        argInputFields[i].PlaceholderText.text = "";
                    else
                        argInputFields[i].PlaceholderText.text = $"eg. {ParseUtility.GetExampleInput(argType)}";
                }
                else
                {
                    argInputFields[i].Component.gameObject.SetActive(false);
                    argDropdowns[i].gameObject.SetActive(true);

                    PopulateNonPrimitiveArgumentDropdown(argDropdowns[i], i);
                }
            }
        }

        private void PopulateNonPrimitiveArgumentDropdown(Dropdown dropdown, int argIndex)
        {
            dropdown.options.Clear();
            dropdown.options.Add(new Dropdown.OptionData("null"));

            var argType = arguments[argIndex].ParameterType;

            int tabIndex = 0;
            foreach (var tab in InspectorManager.Inspectors)
            {
                tabIndex++;

                if (argType.IsAssignableFrom(tab.Target.GetActualType()))
                    dropdown.options.Add(new Dropdown.OptionData($"Tab {tabIndex}: {tab.Tab.TabText.text}"));
            }
        }

        private void AddArgRow(int index, bool generic)
        {
            if (!generic)
                AddArgRow(index, argHolder, argRows, argLabels, argumentInput, false);
            else
                AddArgRow(index, genericArgHolder, genericArgRows, genericArgLabels, genericInput, true);
        }

        private void AddArgRow(int index, GameObject parent, List<GameObject> objectList, List<Text> labelList, string[] inputArray, bool generic)
        {
            var horiGroup = UIFactory.CreateUIObject("ArgRow_" + index, parent);
            UIFactory.SetLayoutElement(horiGroup, minHeight: 25, flexibleHeight: 50, minWidth: 50, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(horiGroup, false, false, true, true, 5);
            horiGroup.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            objectList.Add(horiGroup);

            var label = UIFactory.CreateLabel(horiGroup, "ArgLabel", "not set", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(label.gameObject, minWidth: 40, flexibleWidth: 90, minHeight: 25, flexibleHeight: 50);
            labelList.Add(label);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;

            var inputField = UIFactory.CreateInputField(horiGroup, "InputField", "...");
            UIFactory.SetLayoutElement(inputField.UIRoot, minHeight: 25, flexibleHeight: 50, minWidth: 100, flexibleWidth: 1000);
            inputField.Component.lineType = InputField.LineType.MultiLineNewline;
            inputField.UIRoot.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            inputField.OnValueChanged += (string val) => { inputArray[index] = val; };

            if (!generic)
            {
                var dropdownObj = UIFactory.CreateDropdown(horiGroup, out Dropdown dropdown, "Select argument...", 14, (int val) =>
                {
                    ArgDropdownChanged(index, val);
                });
                UIFactory.SetLayoutElement(dropdownObj, minHeight: 25, flexibleHeight: 50, minWidth: 100, flexibleWidth: 1000);
                dropdownObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                argInputFields.Add(inputField);
                argDropdowns.Add(dropdown);
            }
            else
                genericInputFields.Add(inputField);

            if (generic)
                genericAutocompleters.Add(new TypeCompleter(null, inputField));
        }

        private void ArgDropdownChanged(int argIndex, int value)
        {
            // not sure if necessary
        }

        public GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateVerticalGroup(parent, "EvaluateWidget", false, false, true, true, 3, new Vector4(2, 2, 2, 2),
                new Color(0.15f, 0.15f, 0.15f));
            UIFactory.SetLayoutElement(UIRoot, minWidth: 50, flexibleWidth: 9999, minHeight: 50, flexibleHeight: 800);
            //UIRoot.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // generic args
            this.genericArgHolder = UIFactory.CreateUIObject("GenericHolder", UIRoot);
            UIFactory.SetLayoutElement(genericArgHolder, flexibleWidth: 1000);
            var genericsTitle = UIFactory.CreateLabel(genericArgHolder, "GenericsTitle", "Generic Arguments", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(genericsTitle.gameObject, minHeight: 25, flexibleWidth: 1000);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(genericArgHolder, false, false, true, true, 3);
            UIFactory.SetLayoutElement(genericArgHolder, minHeight: 25, flexibleHeight: 750, minWidth: 50, flexibleWidth: 9999);
            //genericArgHolder.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // args
            this.argHolder = UIFactory.CreateUIObject("ArgHolder", UIRoot);
            UIFactory.SetLayoutElement(argHolder, flexibleWidth: 1000);
            var argsTitle = UIFactory.CreateLabel(argHolder, "ArgsTitle", "Arguments", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(argsTitle.gameObject, minHeight: 25, flexibleWidth: 1000);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(argHolder, false, false, true, true, 3);
            UIFactory.SetLayoutElement(argHolder, minHeight: 25, flexibleHeight: 750, minWidth: 50, flexibleWidth: 9999);
            //argHolder.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // evaluate button
            var evalButton = UIFactory.CreateButton(UIRoot, "EvaluateButton", "Evaluate", new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(evalButton.Component.gameObject, minHeight: 25, minWidth: 150, flexibleWidth: 0);
            evalButton.OnClick += () =>
            {
                Owner.EvaluateAndSetCell();
            };

            return UIRoot;
        }
    }
}
