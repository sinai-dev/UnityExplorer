using System;
using System.Collections.Generic;
using System.Linq;
using UnityExplorer.Helpers;
using UnityExplorer.UI;
using UnityExplorer.Unstrip.LayerMasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Inspectors.GOInspector;

namespace UnityExplorer.Inspectors
{
    public class GameObjectInspector : InspectorBase
    {
        public override string TabLabel => $" [G] {TargetGO?.name}";

        public static GameObjectInspector ActiveInstance { get; private set; }

        public GameObject TargetGO;

        // sub modules
        private static ChildList s_childList;
        private static ComponentList s_compList;
        private static GameObjectControls s_controls;

        // static UI elements (only constructed once)

        private static bool m_UIConstructed;

        private static GameObject s_content;
        public override GameObject Content
        {
            get => s_content;
            set => s_content = value;
        }

        public static TMP_InputField m_nameInput;
        private static string m_lastName;
        public static TMP_InputField m_pathInput;
        private static string m_lastPath;
        private static GameObject m_pathGroupObj;
        private static Text m_hiddenPathText;

        private static Toggle m_enabledToggle;
        private static Text m_enabledText;
        private static bool? m_lastEnabledState;

        private static Dropdown m_layerDropdown;
        private static int m_lastLayer = -1;

        private static Text m_sceneText;
        private static string m_lastScene;

        public GameObjectInspector(GameObject target) : base(target)
        {
            ActiveInstance = this;

            TargetGO = target;

            if (!TargetGO)
            {
                ExplorerCore.LogWarning("GameObjectInspector cctor: Target GameObject is null!");
                return;
            }

            // one UI is used for all gameobject inspectors. no point recreating it.
            if (!m_UIConstructed)
            {
                m_UIConstructed = true;

                s_childList = new ChildList();
                s_compList = new ComponentList();
                s_controls = new GameObjectControls();

                ConstructUI();
            }
        }

        public override void SetContentActive()
        {
            base.SetContentActive();
            ActiveInstance = this;
        }

        public override void SetContentInactive()
        {
            base.SetContentInactive();
            ActiveInstance = null;
        }

        internal void ChangeInspectorTarget(GameObject newTarget)
        {
            if (!newTarget)
                return;

            this.Target = this.TargetGO = newTarget;
        }

        // Update

        public override void Update()
        {
            base.Update();

            if (m_pendingDestroy || ActiveInstance != this)
                return;

            RefreshTopInfo();

            s_childList.RefreshChildObjectList();

            s_compList.RefreshComponentList();

            s_controls.RefreshControls();

            if (GameObjectControls.s_sliderChangedWanted)
            {
                GameObjectControls.UpdateSliderControl();
            }
        }

        private void RefreshTopInfo()
        {
            if (m_lastName != TargetGO.name)
            {
                m_lastName = TargetGO.name;
                m_nameInput.text = m_lastName;
            }

            if (TargetGO.transform.parent)
            {
                if (!m_pathGroupObj.activeSelf)
                    m_pathGroupObj.SetActive(true);

                var path = TargetGO.transform.GetTransformPath(true);
                if (m_lastPath != path)
                {
                    m_lastPath = path;
                    m_pathInput.text = path;
                    m_hiddenPathText.text = path;
                }
            }
            else if (m_pathGroupObj.activeSelf)
                m_pathGroupObj.SetActive(false);

            if (m_lastEnabledState != TargetGO.activeSelf)
            {
                m_lastEnabledState = TargetGO.activeSelf;

                m_enabledToggle.isOn = TargetGO.activeSelf;
                m_enabledText.text = TargetGO.activeSelf ? "Enabled" : "Disabled";
                m_enabledText.color = TargetGO.activeSelf ? Color.green : Color.red;
            }

            if (m_lastLayer != TargetGO.layer)
            {
                m_lastLayer = TargetGO.layer;
                m_layerDropdown.value = TargetGO.layer;
            }

            if (m_lastScene != TargetGO.scene.name)
            {
                m_lastScene = TargetGO.scene.name;

                if (!string.IsNullOrEmpty(TargetGO.scene.name))
                    m_sceneText.text = m_lastScene;
                else
                    m_sceneText.text = "None (Asset/Resource)";
            }
        }

        // UI Callbacks

        private static void OnApplyNameClicked()
        {
            if (ActiveInstance == null) 
                return;

            ActiveInstance.TargetGO.name = m_nameInput.text;
        }

        private static void OnEnableToggled(bool enabled)
        {
            if (ActiveInstance == null) 
                return;

            ActiveInstance.TargetGO.SetActive(enabled);
        }

        private static void OnLayerSelected(int layer)
        {
            if (ActiveInstance == null) 
                return;

            ActiveInstance.TargetGO.layer = layer;
        }

        internal static void OnBackButtonClicked()
        {
            if (ActiveInstance == null)
                return;

            ActiveInstance.ChangeInspectorTarget(ActiveInstance.TargetGO.transform.parent.gameObject);
        }

        #region UI CONSTRUCTION

        private void ConstructUI()
        {
            var parent = InspectorManager.Instance.m_inspectorContent;

            s_content = UIFactory.CreateScrollView(parent, out GameObject scrollContent, out _, new Color(0.1f, 0.1f, 0.1f));

            var scrollGroup = scrollContent.GetComponent<VerticalLayoutGroup>();
            scrollGroup.childForceExpandHeight = true;
            scrollGroup.childControlHeight = true;
            scrollGroup.spacing = 5;

            ConstructTopArea(scrollContent);

            s_controls.ConstructControls(scrollContent);

            var midGroupObj = ConstructMidGroup(scrollContent);

            s_childList.ConstructChildList(midGroupObj);
            s_compList.ConstructCompList(midGroupObj);
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
#if CPP
            backButton.onClick.AddListener(new Action(OnBackButtonClicked));
#else
            backButton.onClick.AddListener(OnBackButtonClicked);
#endif
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
            m_hiddenPathText.raycastTarget = false;
            var hiddenFitter = pathHiddenTextObj.AddComponent<ContentSizeFitter>();
            hiddenFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var hiddenLayout = pathHiddenTextObj.AddComponent<LayoutElement>();
            hiddenLayout.minHeight = 25;
            hiddenLayout.flexibleHeight = 75;
            hiddenLayout.minWidth = 400;
            hiddenLayout.flexibleWidth = 9000;
            var hiddenGroup = pathHiddenTextObj.AddComponent<HorizontalLayoutGroup>();
            hiddenGroup.childForceExpandWidth = true;
            hiddenGroup.childControlWidth = true;
            hiddenGroup.childForceExpandHeight = true;
            hiddenGroup.childControlHeight = true;

            var pathInputObj = UIFactory.CreateTMPInput(pathHiddenTextObj, 14, 0, (int)TextAlignmentOptions.MidlineLeft);
            var pathInputRect = pathInputObj.GetComponent<RectTransform>();
            pathInputRect.sizeDelta = new Vector2(pathInputRect.sizeDelta.x, 25);
            m_pathInput = pathInputObj.GetComponent<TMP_InputField>();
            m_pathInput.text = TargetGO.transform.GetTransformPath();
            m_pathInput.readOnly = true;
            var pathInputLayout = pathInputObj.AddComponent<LayoutElement>();
            pathInputLayout.minHeight = 25;
            pathInputLayout.flexibleHeight = 75;
            pathInputLayout.preferredWidth = 400;
            pathInputLayout.flexibleWidth = 9999;

            // name row

            var nameRowObj = UIFactory.CreateHorizontalGroup(scrollContent, new Color(0.1f, 0.1f, 0.1f));
            var nameGroup = nameRowObj.GetComponent<HorizontalLayoutGroup>();
            nameGroup.childForceExpandHeight = false;
            nameGroup.childForceExpandWidth = false;
            nameGroup.childControlHeight = false;
            nameGroup.childControlWidth = true;
            nameGroup.spacing = 5;
            var nameRect = nameRowObj.GetComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(nameRect.sizeDelta.x, 25);
            var nameLayout = nameRowObj.AddComponent<LayoutElement>();
            nameLayout.minHeight = 25;
            nameLayout.flexibleHeight = 0;

            var nameTextObj = UIFactory.CreateTMPLabel(nameRowObj, TextAlignmentOptions.Midline);
            var nameTextText = nameTextObj.GetComponent<TextMeshProUGUI>();
            nameTextText.text = "Name:";
            nameTextText.fontSize = 14;
            nameTextText.color = Color.grey;
            var nameTextLayout = nameTextObj.AddComponent<LayoutElement>();
            nameTextLayout.minHeight = 25;
            nameTextLayout.flexibleHeight = 0;
            nameTextLayout.minWidth = 55;
            nameTextLayout.flexibleWidth = 0;

            var nameInputObj = UIFactory.CreateTMPInput(nameRowObj, 14, 0, (int)TextAlignmentOptions.MidlineLeft);
            var nameInputRect = nameInputObj.GetComponent<RectTransform>();
            nameInputRect.sizeDelta = new Vector2(nameInputRect.sizeDelta.x, 25);
            m_nameInput = nameInputObj.GetComponent<TMP_InputField>();
            m_nameInput.text = TargetGO.name;
            m_nameInput.lineType = TMP_InputField.LineType.SingleLine;

            var applyNameBtnObj = UIFactory.CreateButton(nameRowObj);
            var applyNameBtn = applyNameBtnObj.GetComponent<Button>();
#if CPP
            applyNameBtn.onClick.AddListener(new Action(OnApplyNameClicked));
#else
            applyNameBtn.onClick.AddListener(OnApplyNameClicked);
#endif
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
#if CPP
            m_enabledToggle.onValueChanged.AddListener(new Action<bool>(OnEnableToggled));
#else
            m_enabledToggle.onValueChanged.AddListener(OnEnableToggled);
#endif

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
                var layer = LayerMaskUnstrip.LayerToName(i);
                m_layerDropdown.options.Add(new Dropdown.OptionData { text = $"{i}: {layer}" });
            }
            //var itemText = layerDropdownObj.transform.Find("Label").GetComponent<Text>();
            //itemText.resizeTextForBestFit = true;
            var layerDropdownLayout = layerDropdownObj.AddComponent<LayoutElement>();
            layerDropdownLayout.minWidth = 120;
            layerDropdownLayout.flexibleWidth = 2000;
            layerDropdownLayout.minHeight = 25;
#if CPP
            m_layerDropdown.onValueChanged.AddListener(new Action<int>(OnLayerSelected));
#else
            m_layerDropdown.onValueChanged.AddListener(OnLayerSelected);
#endif

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
            var midlayout = midGroupObj.AddComponent<LayoutElement>();
            midlayout.minHeight = 350;
            midlayout.flexibleHeight = 10000;
            midlayout.minWidth = 200;
            midlayout.flexibleWidth = 25000;

            return midGroupObj;
        }
#endregion
    }
}
