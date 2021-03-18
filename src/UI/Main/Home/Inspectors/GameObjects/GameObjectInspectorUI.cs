using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Unity;
using UnityExplorer.Core.Inspectors;
using UnityExplorer.Core.Runtime;

namespace UnityExplorer.UI.Main.Home.Inspectors
{
    public class GameObjectInspectorUI : InspectorBaseUI
    {
        private static GameObject s_content;
        public override GameObject Content
        {
            get => s_content;
            set => s_content = value;
        }

        private static string m_lastName;
        public static InputField m_nameInput;

        private static string m_lastPath;
        public static InputField m_pathInput;
        private static RectTransform m_pathInputRect;
        private static GameObject m_pathGroupObj;
        private static Text m_hiddenPathText;
        private static RectTransform m_hiddenPathRect;

        private static Toggle m_enabledToggle;
        private static Text m_enabledText;
        private static bool? m_lastEnabledState;

        private static Dropdown m_layerDropdown;
        private static int m_lastLayer = -1;

        private static Text m_sceneText;
        private static string m_lastScene;

        internal void RefreshTopInfo()
        {
            var target = GameObjectInspector.ActiveInstance.TargetGO;
            string name = target.name;

            if (m_lastName != name)
            {
                m_lastName = name;
                m_nameInput.text = m_lastName;
            }

            if (target.transform.parent)
            {
                if (!m_pathGroupObj.activeSelf)
                    m_pathGroupObj.SetActive(true);

                var path = target.transform.GetTransformPath(true);
                if (m_lastPath != path)
                {
                    m_lastPath = path;

                    m_pathInput.text = path;
                    m_hiddenPathText.text = path;

                    LayoutRebuilder.ForceRebuildLayoutImmediate(m_pathInputRect);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(m_hiddenPathRect);
                }
            }
            else if (m_pathGroupObj.activeSelf)
                m_pathGroupObj.SetActive(false);

            if (m_lastEnabledState != target.activeSelf)
            {
                m_lastEnabledState = target.activeSelf;

                m_enabledToggle.isOn = target.activeSelf;
                m_enabledText.text = target.activeSelf ? "Enabled" : "Disabled";
                m_enabledText.color = target.activeSelf ? Color.green : Color.red;
            }

            if (m_lastLayer != target.layer)
            {
                m_lastLayer = target.layer;
                m_layerDropdown.value = target.layer;
            }

            if (string.IsNullOrEmpty(m_lastScene) || m_lastScene != target.scene.name)
            {
                m_lastScene = target.scene.name;

                if (!string.IsNullOrEmpty(target.scene.name))
                    m_sceneText.text = m_lastScene;
                else
                    m_sceneText.text = "None (Asset/Resource)";
            }
        }

        // UI Callbacks

        private static void OnApplyNameClicked()
        {
            if (GameObjectInspector.ActiveInstance == null)
                return;

            GameObjectInspector.ActiveInstance.TargetGO.name = m_nameInput.text;
        }

        private static void OnEnableToggled(bool enabled)
        {
            if (GameObjectInspector.ActiveInstance == null)
                return;

            GameObjectInspector.ActiveInstance.TargetGO.SetActive(enabled);
        }

        private static void OnLayerSelected(int layer)
        {
            if (GameObjectInspector.ActiveInstance == null)
                return;

            GameObjectInspector.ActiveInstance.TargetGO.layer = layer;
        }

        internal static void OnBackButtonClicked()
        {
            if (GameObjectInspector.ActiveInstance == null)
                return;

            GameObjectInspector.ActiveInstance.ChangeInspectorTarget(
                GameObjectInspector.ActiveInstance.TargetGO.transform.parent.gameObject);
        }

        #region UI CONSTRUCTION

        internal void ConstructUI()
        {
            var parent = InspectorManager.UI.m_inspectorContent;

            s_content = UIFactory.CreateScrollView(parent, out GameObject scrollContent, out _, new Color(0.1f, 0.1f, 0.1f));

            var parentLayout = scrollContent.transform.parent.gameObject.AddComponent<VerticalLayoutGroup>();
            parentLayout.childForceExpandWidth = true;
            parentLayout.childControlWidth = true;
            parentLayout.childForceExpandHeight = true;
            parentLayout.childControlHeight = true;

            var scrollGroup = scrollContent.GetComponent<VerticalLayoutGroup>();
            scrollGroup.childForceExpandHeight = true;
            scrollGroup.childControlHeight = true;
            scrollGroup.childForceExpandWidth = true;
            scrollGroup.childControlWidth = true;
            scrollGroup.spacing = 5;
            var contentFitter = scrollContent.GetComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            ConstructTopArea(scrollContent);

            GameObjectInspector.s_controls.ConstructControls(scrollContent);

            var midGroupObj = ConstructMidGroup(scrollContent);

            GameObjectInspector.s_childList.ConstructChildList(midGroupObj);
            GameObjectInspector.s_compList.ConstructCompList(midGroupObj);

            LayoutRebuilder.ForceRebuildLayoutImmediate(s_content.GetComponent<RectTransform>());
            Canvas.ForceUpdateCanvases();
        }

        private void ConstructTopArea(GameObject scrollContent)
        {
            // path row

            m_pathGroupObj = UIFactory.CreateHorizontalGroup(scrollContent, new Color(0.1f, 0.1f, 0.1f));
            var pathGroup = m_pathGroupObj.GetComponent<HorizontalLayoutGroup>();
            pathGroup.childForceExpandHeight = false;
            pathGroup.childForceExpandWidth = false;
            pathGroup.childControlHeight = false;
            pathGroup.childControlWidth = true;
            pathGroup.spacing = 5;
            var pathRect = m_pathGroupObj.GetComponent<RectTransform>();
            pathRect.sizeDelta = new Vector2(pathRect.sizeDelta.x, 20);
            var pathLayout = m_pathGroupObj.AddComponent<LayoutElement>();
            pathLayout.minHeight = 20;
            pathLayout.flexibleHeight = 75;

            var backButtonObj = UIFactory.CreateButton(m_pathGroupObj);
            var backButton = backButtonObj.GetComponent<Button>();

            backButton.onClick.AddListener(OnBackButtonClicked);

            var backColors = backButton.colors;
            backColors.normalColor = new Color(0.15f, 0.15f, 0.15f);
            backButton.colors = backColors;
            var backText = backButtonObj.GetComponentInChildren<Text>();
            backText.text = "◄";
            var backLayout = backButtonObj.AddComponent<LayoutElement>();
            backLayout.minWidth = 55;
            backLayout.flexibleWidth = 0;
            backLayout.minHeight = 25;
            backLayout.flexibleHeight = 0;

            var pathHiddenTextObj = UIFactory.CreateLabel(m_pathGroupObj, TextAnchor.MiddleLeft);
            m_hiddenPathText = pathHiddenTextObj.GetComponent<Text>();
            m_hiddenPathText.color = Color.clear;
            m_hiddenPathText.fontSize = 14;
            //m_hiddenPathText.lineSpacing = 1.5f;
            m_hiddenPathText.raycastTarget = false;
            var hiddenFitter = pathHiddenTextObj.AddComponent<ContentSizeFitter>();
            hiddenFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var hiddenLayout = pathHiddenTextObj.AddComponent<LayoutElement>();
            hiddenLayout.minHeight = 25;
            hiddenLayout.flexibleHeight = 125;
            hiddenLayout.minWidth = 250;
            hiddenLayout.flexibleWidth = 9000;
            var hiddenGroup = pathHiddenTextObj.AddComponent<HorizontalLayoutGroup>();
            hiddenGroup.childForceExpandWidth = true;
            hiddenGroup.childControlWidth = true;
            hiddenGroup.childForceExpandHeight = true;
            hiddenGroup.childControlHeight = true;

            var pathInputObj = UIFactory.CreateInputField(pathHiddenTextObj);
            var pathInputRect = pathInputObj.GetComponent<RectTransform>();
            pathInputRect.sizeDelta = new Vector2(pathInputRect.sizeDelta.x, 25);
            m_pathInput = pathInputObj.GetComponent<InputField>();
            m_pathInput.text = GameObjectInspector.ActiveInstance.TargetGO.transform.GetTransformPath();
            m_pathInput.readOnly = true;
            m_pathInput.lineType = InputField.LineType.MultiLineNewline;
            var pathInputLayout = pathInputObj.AddComponent<LayoutElement>();
            pathInputLayout.minHeight = 25;
            pathInputLayout.flexibleHeight = 75;
            pathInputLayout.preferredWidth = 400;
            pathInputLayout.flexibleWidth = 9999;
            var textRect = m_pathInput.textComponent.GetComponent<RectTransform>();
            textRect.offsetMin = new Vector2(3, 3);
            textRect.offsetMax = new Vector2(3, 3);
            m_pathInput.textComponent.color = new Color(0.75f, 0.75f, 0.75f);
            //m_pathInput.textComponent.lineSpacing = 1.5f;

            m_pathInputRect = m_pathInput.GetComponent<RectTransform>();
            m_hiddenPathRect = m_hiddenPathText.GetComponent<RectTransform>();

            // name row

            var nameRowObj = UIFactory.CreateHorizontalGroup(scrollContent, new Color(0.1f, 0.1f, 0.1f));
            var nameGroup = nameRowObj.GetComponent<HorizontalLayoutGroup>();
            nameGroup.childForceExpandHeight = false;
            nameGroup.childForceExpandWidth = false;
            nameGroup.childControlHeight = true;
            nameGroup.childControlWidth = true;
            nameGroup.spacing = 5;
            var nameRect = nameRowObj.GetComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(nameRect.sizeDelta.x, 25);
            var nameLayout = nameRowObj.AddComponent<LayoutElement>();
            nameLayout.minHeight = 25;
            nameLayout.flexibleHeight = 0;

            var nameTextObj = UIFactory.CreateLabel(nameRowObj, TextAnchor.MiddleCenter);
            var nameTextText = nameTextObj.GetComponent<Text>();
            nameTextText.text = "Name:";
            nameTextText.fontSize = 14;
            nameTextText.color = Color.grey;
            var nameTextLayout = nameTextObj.AddComponent<LayoutElement>();
            nameTextLayout.minHeight = 25;
            nameTextLayout.flexibleHeight = 0;
            nameTextLayout.minWidth = 55;
            nameTextLayout.flexibleWidth = 0;

            var nameInputObj = UIFactory.CreateInputField(nameRowObj);
            var nameInputRect = nameInputObj.GetComponent<RectTransform>();
            nameInputRect.sizeDelta = new Vector2(nameInputRect.sizeDelta.x, 25);
            m_nameInput = nameInputObj.GetComponent<InputField>();
            m_nameInput.text = GameObjectInspector.ActiveInstance.TargetGO.name;

            var applyNameBtnObj = UIFactory.CreateButton(nameRowObj);
            var applyNameBtn = applyNameBtnObj.GetComponent<Button>();

            applyNameBtn.onClick.AddListener(OnApplyNameClicked);

            var applyNameText = applyNameBtnObj.GetComponentInChildren<Text>();
            applyNameText.text = "Apply";
            applyNameText.fontSize = 14;
            var applyNameLayout = applyNameBtnObj.AddComponent<LayoutElement>();
            applyNameLayout.minWidth = 65;
            applyNameLayout.minHeight = 25;
            applyNameLayout.flexibleHeight = 0;
            var applyNameRect = applyNameBtnObj.GetComponent<RectTransform>();
            applyNameRect.sizeDelta = new Vector2(applyNameRect.sizeDelta.x, 25);

            var activeLabel = UIFactory.CreateLabel(nameRowObj, TextAnchor.MiddleCenter);
            var activeLabelLayout = activeLabel.AddComponent<LayoutElement>();
            activeLabelLayout.minWidth = 55;
            activeLabelLayout.minHeight = 25;
            var activeText = activeLabel.GetComponent<Text>();
            activeText.text = "Active:";
            activeText.color = Color.grey;
            activeText.fontSize = 14;

            var enabledToggleObj = UIFactory.CreateToggle(nameRowObj, out m_enabledToggle, out m_enabledText);
            var toggleLayout = enabledToggleObj.AddComponent<LayoutElement>();
            toggleLayout.minHeight = 25;
            toggleLayout.minWidth = 100;
            toggleLayout.flexibleWidth = 0;
            m_enabledText.text = "Enabled";
            m_enabledText.color = Color.green;

            m_enabledToggle.onValueChanged.AddListener(OnEnableToggled);

            // layer and scene row

            var sceneLayerRow = UIFactory.CreateHorizontalGroup(scrollContent, new Color(0.1f, 0.1f, 0.1f));
            var sceneLayerGroup = sceneLayerRow.GetComponent<HorizontalLayoutGroup>();
            sceneLayerGroup.childForceExpandWidth = false;
            sceneLayerGroup.childControlWidth = true;
            sceneLayerGroup.spacing = 5;

            var layerLabel = UIFactory.CreateLabel(sceneLayerRow, TextAnchor.MiddleCenter);
            var layerText = layerLabel.GetComponent<Text>();
            layerText.text = "Layer:";
            layerText.fontSize = 14;
            layerText.color = Color.grey;
            var layerTextLayout = layerLabel.AddComponent<LayoutElement>();
            layerTextLayout.minWidth = 55;
            layerTextLayout.flexibleWidth = 0;

            var layerDropdownObj = UIFactory.CreateDropdown(sceneLayerRow, out m_layerDropdown);
            m_layerDropdown.options.Clear();
            for (int i = 0; i < 32; i++)
            {
                var layer = RuntimeProvider.Instance.LayerToName(i);
                m_layerDropdown.options.Add(new Dropdown.OptionData { text = $"{i}: {layer}" });
            }
            //var itemText = layerDropdownObj.transform.Find("Label").GetComponent<Text>();
            //itemText.resizeTextForBestFit = true;
            var layerDropdownLayout = layerDropdownObj.AddComponent<LayoutElement>();
            layerDropdownLayout.minWidth = 120;
            layerDropdownLayout.flexibleWidth = 2000;
            layerDropdownLayout.minHeight = 25;

            m_layerDropdown.onValueChanged.AddListener(OnLayerSelected);

            var scenelabelObj = UIFactory.CreateLabel(sceneLayerRow, TextAnchor.MiddleCenter);
            var sceneLabel = scenelabelObj.GetComponent<Text>();
            sceneLabel.text = "Scene:";
            sceneLabel.color = Color.grey;
            sceneLabel.fontSize = 14;
            var sceneLabelLayout = scenelabelObj.AddComponent<LayoutElement>();
            sceneLabelLayout.minWidth = 55;
            sceneLabelLayout.flexibleWidth = 0;

            var objectSceneText = UIFactory.CreateLabel(sceneLayerRow, TextAnchor.MiddleLeft);
            m_sceneText = objectSceneText.GetComponent<Text>();
            m_sceneText.fontSize = 14;
            m_sceneText.horizontalOverflow = HorizontalWrapMode.Overflow;
            var sceneTextLayout = objectSceneText.AddComponent<LayoutElement>();
            sceneTextLayout.minWidth = 120;
            sceneTextLayout.flexibleWidth = 2000;
        }

        private GameObject ConstructMidGroup(GameObject parent)
        {
            var midGroupObj = UIFactory.CreateHorizontalGroup(parent, new Color(1, 1, 1, 0));
            var midGroup = midGroupObj.GetComponent<HorizontalLayoutGroup>();
            midGroup.spacing = 5;
            midGroup.childForceExpandWidth = true;
            midGroup.childControlWidth = true;
            midGroup.childForceExpandHeight = true;
            midGroup.childControlHeight = true;

            var midLayout = midGroupObj.AddComponent<LayoutElement>();
            midLayout.minHeight = 300;
            midLayout.flexibleHeight = 5000;

            return midGroupObj;
        }
        #endregion
    }
}
