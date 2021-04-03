using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Runtime;
using UnityExplorer.Core.Unity;

namespace UnityExplorer.UI.Inspectors.GameObjects
{
    public class GameObjectInspector : InspectorBase
    {
        public override string TabLabel => $" <color=cyan>[G]</color> {TargetGO?.name}";

        public static GameObjectInspector ActiveInstance { get; private set; }

        public GameObject TargetGO;

        // sub modules
        internal static ChildList s_childList;
        internal static ComponentList s_compList;
        internal static GameObjectControls s_controls;

        internal static bool m_UIConstructed;

        public GameObjectInspector(GameObject target) : base(target)
        {
            ActiveInstance = this;

            TargetGO = target;

            if (!TargetGO)
            {
                ExplorerCore.LogWarning("Target GameObject is null!");
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

        public override void SetActive()
        {
            base.SetActive();
            ActiveInstance = this;
        }

        public override void SetInactive()
        {
            base.SetInactive();
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

            if (m_pendingDestroy || !this.IsActive)
                return;

            RefreshTopInfo();

            s_childList.RefreshChildObjectList();

            s_compList.RefreshComponentList();

            s_controls.RefreshControls();

            if (GameObjectControls.s_sliderChangedWanted)
                GameObjectControls.UpdateSliderControl();
        }

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
            var target = TargetGO;
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

        internal void ConstructUI()
        {
            var parent = InspectorManager.m_inspectorContent;

            s_content = UIFactory.CreateScrollView(parent, "GameObjectInspector_Content", out GameObject scrollContent, out _, 
                new Color(0.1f, 0.1f, 0.1f));

            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(scrollContent.transform.parent.gameObject, true, true, true, true);

            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(scrollContent, true, true, true, true, 5);
            var contentFitter = scrollContent.GetComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            ConstructTopArea(scrollContent);

            s_controls.ConstructControls(scrollContent);

            var midGroupObj = ConstructMidGroup(scrollContent);

            s_childList.ConstructChildList(midGroupObj);
            s_compList.ConstructCompList(midGroupObj);

            LayoutRebuilder.ForceRebuildLayoutImmediate(s_content.GetComponent<RectTransform>());
            Canvas.ForceUpdateCanvases();
        }

        private void ConstructTopArea(GameObject scrollContent)
        {
            // path row

            m_pathGroupObj = UIFactory.CreateHorizontalGroup(scrollContent, "TopArea", false, false, true, true, 5, default, new Color(0.1f, 0.1f, 0.1f));
            
            var pathRect = m_pathGroupObj.GetComponent<RectTransform>();
            pathRect.sizeDelta = new Vector2(pathRect.sizeDelta.x, 20);
            UIFactory.SetLayoutElement(m_pathGroupObj, minHeight: 20, flexibleHeight: 75);

            // Back button

            var backButton = UIFactory.CreateButton(m_pathGroupObj, "BackButton", "◄", OnBackButtonClicked, new Color(0.15f, 0.15f, 0.15f));
            UIFactory.SetLayoutElement(backButton.gameObject, minWidth: 55, flexibleWidth: 0, minHeight: 25, flexibleHeight: 0);

            m_hiddenPathText = UIFactory.CreateLabel(m_pathGroupObj, "HiddenPathText", "", TextAnchor.MiddleLeft);
            m_hiddenPathText.color = Color.clear;
            m_hiddenPathText.fontSize = 14;
            m_hiddenPathText.raycastTarget = false;

            var hiddenFitter = m_hiddenPathText.gameObject.AddComponent<ContentSizeFitter>();
            hiddenFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            UIFactory.SetLayoutElement(m_hiddenPathText.gameObject, minHeight: 25, flexibleHeight: 125, minWidth: 250, flexibleWidth: 9000);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(m_hiddenPathText.gameObject, true, true, true, true);

            // Path input 

            var pathInputObj = UIFactory.CreateInputField(m_hiddenPathText.gameObject, "PathInputField", "...");
            UIFactory.SetLayoutElement(pathInputObj, minHeight: 25, flexibleHeight: 75, preferredWidth: 400, flexibleWidth: 9999);
            var pathInputRect = pathInputObj.GetComponent<RectTransform>();
            pathInputRect.sizeDelta = new Vector2(pathInputRect.sizeDelta.x, 25);

            m_pathInput = pathInputObj.GetComponent<InputField>();
            m_pathInput.text = ActiveInstance.TargetGO.transform.GetTransformPath();
            m_pathInput.readOnly = true;
            m_pathInput.lineType = InputField.LineType.MultiLineNewline;
            m_pathInput.textComponent.color = new Color(0.75f, 0.75f, 0.75f);

            var textRect = m_pathInput.textComponent.GetComponent<RectTransform>();
            textRect.offsetMin = new Vector2(3, 3);
            textRect.offsetMax = new Vector2(3, 3);

            m_pathInputRect = m_pathInput.GetComponent<RectTransform>();
            m_hiddenPathRect = m_hiddenPathText.GetComponent<RectTransform>();

            // name and enabled row

            var nameRowObj = UIFactory.CreateHorizontalGroup(scrollContent, "NameGroup", false, false, true, true, 5, default, new Color(0.1f, 0.1f, 0.1f));
            UIFactory.SetLayoutElement(nameRowObj, minHeight: 25, flexibleHeight: 0);
            var nameRect = nameRowObj.GetComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(nameRect.sizeDelta.x, 25);

            var nameLabel = UIFactory.CreateLabel(nameRowObj, "NameLabel", "Name:", TextAnchor.MiddleCenter, Color.grey, true, 14);
            UIFactory.SetLayoutElement(nameLabel.gameObject, minHeight: 25, flexibleHeight: 0, minWidth: 55, flexibleWidth: 0);

            var nameInputObj = UIFactory.CreateInputField(nameRowObj, "NameInput", "...");
            var nameInputRect = nameInputObj.GetComponent<RectTransform>();
            nameInputRect.sizeDelta = new Vector2(nameInputRect.sizeDelta.x, 25);
            m_nameInput = nameInputObj.GetComponent<InputField>();
            m_nameInput.text = ActiveInstance.TargetGO.name;

            var applyNameBtn = UIFactory.CreateButton(nameRowObj, "ApplyNameButton", "Apply", OnApplyNameClicked);
            UIFactory.SetLayoutElement(applyNameBtn.gameObject, minWidth: 65, minHeight: 25, flexibleHeight: 0);
            var applyNameRect = applyNameBtn.GetComponent<RectTransform>();
            applyNameRect.sizeDelta = new Vector2(applyNameRect.sizeDelta.x, 25);

            var activeLabel = UIFactory.CreateLabel(nameRowObj, "ActiveLabel", "Active:", TextAnchor.MiddleCenter, Color.grey, true, 14);
            UIFactory.SetLayoutElement(activeLabel.gameObject, minWidth: 55, minHeight: 25);

            var enabledToggleObj = UIFactory.CreateToggle(nameRowObj, "EnabledToggle", out m_enabledToggle, out m_enabledText);
            UIFactory.SetLayoutElement(enabledToggleObj, minHeight: 25, minWidth: 100, flexibleWidth: 0);
            m_enabledText.text = "Enabled";
            m_enabledText.color = Color.green;
            m_enabledToggle.onValueChanged.AddListener(OnEnableToggled);

            // layer and scene row

            var sceneLayerRow = UIFactory.CreateHorizontalGroup(scrollContent, "SceneLayerRow", false, true, true, true, 5, default, new Color(0.1f, 0.1f, 0.1f));

            // layer

            var layerLabel = UIFactory.CreateLabel(sceneLayerRow, "LayerLabel", "Layer:", TextAnchor.MiddleCenter, Color.grey, true, 14);
            UIFactory.SetLayoutElement(layerLabel.gameObject, minWidth: 55, flexibleWidth: 0);

            var layerDropdownObj = UIFactory.CreateDropdown(sceneLayerRow, out m_layerDropdown, "", 14, OnLayerSelected);
            m_layerDropdown.options.Clear();
            for (int i = 0; i < 32; i++)
            {
                var layer = RuntimeProvider.Instance.LayerToName(i);
                m_layerDropdown.options.Add(new Dropdown.OptionData { text = $"{i}: {layer}" });
            }
            UIFactory.SetLayoutElement(layerDropdownObj, minWidth: 120, flexibleWidth: 2000, minHeight: 25);

            // scene

            var sceneLabel = UIFactory.CreateLabel(sceneLayerRow, "SceneLabel", "Scene:", TextAnchor.MiddleCenter, Color.grey, true, 14);
            UIFactory.SetLayoutElement(sceneLabel.gameObject, minWidth: 55, flexibleWidth: 0);

            m_sceneText = UIFactory.CreateLabel(sceneLayerRow, "SceneText", "", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(m_sceneText.gameObject, minWidth: 120, flexibleWidth: 2000);
        }

        private GameObject ConstructMidGroup(GameObject parent)
        {
            var midGroupObj = UIFactory.CreateHorizontalGroup(parent, "MidGroup", true, true, true, true, 5, default, new Color(1, 1, 1, 0));
            UIFactory.SetLayoutElement(midGroupObj, minHeight: 300, flexibleHeight: 3000);
            return midGroupObj;
        }
        #endregion
    }
}
