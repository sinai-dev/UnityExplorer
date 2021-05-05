using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.ObjectPool;
using UnityExplorer.UI.Utility;
using UnityExplorer.UI.Widgets.AutoComplete;

namespace UnityExplorer.UI.CacheObject.Views
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

        private Type[] genericArguments;
        private string[] genericInput;

        private GameObject genericArgHolder;
        private readonly List<GameObject> genericArgRows = new List<GameObject>();
        private readonly List<Text> genericArgLabels = new List<Text>();

        private readonly List<InputField> inputFieldCache = new List<InputField>();

        public void OnBorrowedFromPool(CacheMember owner)
        {
            this.Owner = owner;

            arguments = owner.Arguments;
            argumentInput = new string[arguments.Length];

            genericArguments = owner.GenericArguments;
            genericInput = new string[genericArguments.Length];

            SetArgRows();

            this.UIRoot.SetActive(true);
        }

        public void OnReturnToPool()
        {
            foreach (var input in inputFieldCache)
                input.text = "";

            this.Owner = null;
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
                var arg = arguments[i];
                var input = argumentInput[i];

                var type = arg.ParameterType;
                if (type.IsByRef)
                    type = type.GetElementType();

                if (type == typeof(string))
                {
                    outArgs[i] = input;
                    continue;
                }

                if (string.IsNullOrEmpty(input))
                {
                    if (arg.IsOptional)
                        outArgs[i] = arg.DefaultValue;
                    else
                        outArgs[i] = null;
                    continue;
                }

                try
                {
                    var parse = ReflectionUtility.GetMethodInfo(type, "Parse", new Type[] { typeof(string) });
                    outArgs[i] = parse.Invoke(null, new object[] { input });
                }
                catch (Exception ex)
                {
                    ExplorerCore.LogWarning($"Cannot parse argument '{arg.Name}' ({arg.ParameterType.Name}), {ex.GetType().Name}: {ex.Message}");
                    outArgs[i] = null;
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

                var constraints = arg.GetGenericParameterConstraints();

                // TODO show "class" constraints as they dont show up, "struct" does effectively.

                var sb = new StringBuilder($"<color={SignatureHighlighter.CONST}>{arg.Name}</color>");

                for (int j = 0; j < constraints.Length; j++)
                {
                    if (j == 0) sb.Append(' ').Append('(');
                    else sb.Append(',').Append(' ');

                    sb.Append(SignatureHighlighter.ParseType(constraints[j]));

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

                if (i >= argRows.Count)
                    AddArgRow(i, false);

                argRows[i].SetActive(true);
                argLabels[i].text = $"{SignatureHighlighter.ParseType(arg.ParameterType)} <color={SignatureHighlighter.LOCAL_ARG}>{arg.Name}</color>";
            }
        }

        private void AddArgRow(int index, bool generic)
        {
            if (!generic)
                AddArgRow(index, argHolder, argRows, argLabels, argumentInput);//, false);
            else
                AddArgRow(index, genericArgHolder, genericArgRows, genericArgLabels, genericInput);//, true);
        }

        private void AddArgRow(int index, GameObject parent, List<GameObject> objectList, List<Text> labelList, string[] inputArray)//, bool autocomplete)
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

            var inputObj = UIFactory.CreateInputField(horiGroup, "InputField", "...", out InputField inputField);
            UIFactory.SetLayoutElement(inputObj, minHeight: 25, flexibleHeight: 50, minWidth: 100, flexibleWidth: 1000);
            inputField.lineType = InputField.LineType.MultiLineNewline;
            inputObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            inputField.onValueChanged.AddListener((string val) => { inputArray[index] = val; });
            inputFieldCache.Add(inputField);
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
            UIFactory.SetLayoutElement(evalButton.Button.gameObject, minHeight: 25, minWidth: 150, flexibleWidth: 0);
            evalButton.OnClick += () => 
            {
                Owner.EvaluateAndSetCell();
            };

            return UIRoot;
        }
    }
}
