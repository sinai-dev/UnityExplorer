using System;
using System.Collections.Generic;
using System.Linq;
using UnityExplorer.Helpers;
using UnityExplorer.UI;
using UnityExplorer.UI.Shared;
using UnityExplorer.Unstrip.ColorUtility;
using UnityExplorer.Unstrip.LayerMasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Input;

namespace UnityExplorer.Inspectors
{
    public class GameObjectInspector : InspectorBase
    {
        public override string TabLabel => $" [G] {TargetGO?.name}";

        public static GameObjectInspector ActiveInstance { get; private set; }

        // just to help with casting in il2cpp
        public GameObject TargetGO;

        // static UI elements (only constructed once)

        private static bool m_UIConstructed;

        private static GameObject s_content;
        public override GameObject Content
        {
            get => s_content;
            set => s_content = value;
        }

        // cached ui elements
        private static ScrollRect s_contentScroll;
        private static float s_lastScrollPos;

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

        // children list
        public static PageHandler s_childListPageHandler;
        private static GameObject[] s_allChildren = new GameObject[0];
        private static readonly List<GameObject> s_childrenShortlist = new List<GameObject>();
        private static GameObject s_childListContent;
        private static readonly List<Text> s_childListTexts = new List<Text>();
        private static int s_lastChildCount;

        // comp list
        public static PageHandler s_compListPageHandler;
        private static Component[] s_allComps = new Component[0];
        private static readonly List<Component> s_compShortlist = new List<Component>();
        private static GameObject s_compListContent;
        private static readonly List<Text> s_compListTexts = new List<Text>();
        private static int s_lastCompCount;
        public static readonly List<Toggle> s_compToggles = new List<Toggle>();

        // gameobject controls inputs
        private static TMP_InputField s_setParentInput;

        private static ControlEditor s_positionControl;
        private static ControlEditor s_localPosControl;
        private static ControlEditor s_rotationControl;
        private static ControlEditor s_scaleControl;

        internal struct ControlEditor
        {
            public TMP_InputField fullValue;
            public Slider[] sliders;
            public TMP_InputField[] inputs;
            public Text[] values;
        }

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
                ConstructUI();
                m_UIConstructed = true;
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

        public override void Update()
        {
            base.Update();

            if (m_pendingDestroy || ActiveInstance != this)
                return;

            if (!s_sliderChangedWanted)
                s_lastScrollPos = s_contentScroll.verticalScrollbar.value;

            RefreshTopInfo();

            RefreshChildObjectList();

            RefreshComponentList();

            RefreshControls();

            if (s_sliderChangedWanted)
            {
                UpdateSliderControl();
            }
        }

        private void ChangeInspectorTarget(GameObject newTarget)
        {
            if (!newTarget)
                return;

            this.Target = this.TargetGO = newTarget;
        }

#region TOP INFO

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

        private static void OnApplyNameClicked()
        {
            if (ActiveInstance == null) return;

            ActiveInstance.TargetGO.name = m_nameInput.text;
        }

        private static void OnEnableToggled(bool enabled)
        {
            if (ActiveInstance == null) return;

            ActiveInstance.TargetGO.SetActive(enabled);
        }

        private static void OnLayerSelected(int layer)
        {
            if (ActiveInstance == null) return;

            ActiveInstance.TargetGO.layer = layer;
        }

        private static void OnCompToggleClicked(int index, bool value)
        {
            var comp = s_compShortlist[index];

            (comp as Behaviour).enabled = value;
        }

#endregion

#region CHILD LIST

        private void RefreshChildObjectList()
        {
            s_allChildren = new GameObject[TargetGO.transform.childCount];
            for (int i = 0; i < TargetGO.transform.childCount; i++)
            {
                var child = TargetGO.transform.GetChild(i);
                s_allChildren[i] = child.gameObject;
            }

            var objects = s_allChildren;
            s_childListPageHandler.ListCount = objects.Length;

            //int startIndex = m_sceneListPageHandler.StartIndex;

            int newCount = 0;

            foreach (var itemIndex in s_childListPageHandler)
            {
                newCount++;

                // normalized index starting from 0
                var i = itemIndex - s_childListPageHandler.StartIndex;

                if (itemIndex >= objects.Length)
                {
                    if (i > s_lastChildCount || i >= s_childListTexts.Count)
                        break;

                    GameObject label = s_childListTexts[i].transform.parent.parent.gameObject;
                    if (label.activeSelf)
                        label.SetActive(false);
                }
                else
                {
                    GameObject obj = objects[itemIndex];

                    if (!obj)
                        continue;

                    if (i >= s_childrenShortlist.Count)
                    {
                        s_childrenShortlist.Add(obj);
                        AddChildListButton();
                    }
                    else
                    {
                        s_childrenShortlist[i] = obj;
                    }

                    var text = s_childListTexts[i];

                    var name = obj.name;

                    if (obj.transform.childCount > 0)
                        name = $"<color=grey>[{obj.transform.childCount}]</color> {name}";

                    text.text = name;
                    text.color = obj.activeSelf ? Color.green : Color.red;

                    var label = text.transform.parent.parent.gameObject;
                    if (!label.activeSelf)
                    {
                        label.SetActive(true);
                    }
                }
            }

            s_lastChildCount = newCount;
        }

        private static void OnChildListObjectClicked(int index)
        {
            if (!(InspectorManager.Instance.m_activeInspector is GameObjectInspector instance)) return;

            if (index >= s_childrenShortlist.Count || !s_childrenShortlist[index])
            {
                return;
            }

            instance.ChangeInspectorTarget(s_childrenShortlist[index]);

            instance.Update();
        }

        private static void OnBackButtonClicked()
        {
            if (!(InspectorManager.Instance.m_activeInspector is GameObjectInspector instance)) return;

            instance.ChangeInspectorTarget(instance.TargetGO.transform.parent.gameObject);
        }

        private static void OnChildListPageTurn()
        {
            if (!(InspectorManager.Instance.m_activeInspector is GameObjectInspector instance)) return;

            instance.RefreshChildObjectList();
        }

#endregion

#region COMPONENT LIST

        private void RefreshComponentList()
        {
            s_allComps = TargetGO.GetComponents<Component>().ToArray();

            var components = s_allComps;
            s_compListPageHandler.ListCount = components.Length;

            //int startIndex = m_sceneListPageHandler.StartIndex;

            int newCount = 0;

            foreach (var itemIndex in s_compListPageHandler)
            {
                newCount++;

                // normalized index starting from 0
                var i = itemIndex - s_compListPageHandler.StartIndex;

                if (itemIndex >= components.Length)
                {
                    if (i > s_lastCompCount || i >= s_compListTexts.Count)
                        break;

                    GameObject label = s_compListTexts[i].transform.parent.parent.gameObject;
                    if (label.activeSelf)
                        label.SetActive(false);
                }
                else
                {
                    Component comp = components[itemIndex];

                    if (!comp)
                        continue;

                    if (i >= s_compShortlist.Count)
                    {
                        s_compShortlist.Add(comp);
                        AddCompListButton();
                    }
                    else
                    {
                        s_compShortlist[i] = comp;
                    }

                    var text = s_compListTexts[i];

                    text.text = ReflectionHelpers.GetActualType(comp).FullName;

                    var toggle = s_compToggles[i];
                    if (comp is Behaviour behaviour)
                    {
                        if (!toggle.gameObject.activeSelf)
                            toggle.gameObject.SetActive(true);

                        toggle.isOn = behaviour.enabled;
                    }
                    else
                    {
                        if (toggle.gameObject.activeSelf)
                            toggle.gameObject.SetActive(false);
                    }

                    var label = text.transform.parent.parent.gameObject;
                    if (!label.activeSelf)
                    {
                        label.SetActive(true);
                    }
                }
            }

            s_lastCompCount = newCount;
        }

        private static void OnCompListObjectClicked(int index)
        {
            if (index >= s_compShortlist.Count || !s_compShortlist[index])
            {
                return;
            }

            InspectorManager.Instance.Inspect(s_compShortlist[index]);
        }

        private static void OnCompListPageTurn()
        {
            if (!(InspectorManager.Instance.m_activeInspector is GameObjectInspector instance)) return;

            instance.RefreshComponentList();
        }

#endregion

#region TRANSFORM CONTROLS

        private void RefreshControls()
        {
            s_positionControl.fullValue.text = TargetGO.transform.position.ToStringLong();
            s_positionControl.values[0].text = TargetGO.transform.position.x.ToString("F3");
            s_positionControl.values[1].text = TargetGO.transform.position.y.ToString("F3");
            s_positionControl.values[2].text = TargetGO.transform.position.z.ToString("F3");

            s_localPosControl.fullValue.text = TargetGO.transform.localPosition.ToStringLong();
            s_localPosControl.values[0].text = TargetGO.transform.position.x.ToString("F3");
            s_localPosControl.values[1].text = TargetGO.transform.position.y.ToString("F3");
            s_localPosControl.values[2].text = TargetGO.transform.position.z.ToString("F3");

            s_rotationControl.fullValue.text = TargetGO.transform.eulerAngles.ToStringLong();
            s_rotationControl.values[0].text = TargetGO.transform.position.x.ToString("F3");
            s_rotationControl.values[1].text = TargetGO.transform.position.y.ToString("F3");
            s_rotationControl.values[2].text = TargetGO.transform.position.z.ToString("F3");

            s_scaleControl.fullValue.text = TargetGO.transform.localScale.ToStringLong();
            s_scaleControl.values[0].text = TargetGO.transform.position.x.ToString("F3");
            s_scaleControl.values[1].text = TargetGO.transform.position.y.ToString("F3");
            s_scaleControl.values[2].text = TargetGO.transform.position.z.ToString("F3");

        }

        private static void OnSetParentClicked()
        {
            var input = s_setParentInput.text;

            ExplorerCore.Log("Todo set parent to '" + input + "'");
        }

        internal enum ControlType
        {
            position,
            localPosition,
            eulerAngles,
            localScale
        }

        internal enum VectorValue
        {
            x, y, z
        };

        private static bool s_sliderChangedWanted;
        private static Slider s_currentSlider;
        private static ControlType s_currentSliderType;
        private static VectorValue s_currentSliderValueType;
        private static float s_currentSliderValue;

        private static void OnSliderControlChanged(float value, Slider slider, ControlType controlType, VectorValue vectorValue)
        {
            if (value == 0)
                s_sliderChangedWanted = false;
            else
            {
                if (!s_sliderChangedWanted)
                {
                    s_sliderChangedWanted = true;
                    s_currentSlider = slider;
                    s_currentSliderType = controlType;
                    s_currentSliderValueType = vectorValue;
                    //ExplorerCore.LogWarning("Starting slide with scroll value of " + s_lastScrollPos);
                }

                s_currentSliderValue = value;
            }
        }

        private static void UpdateSliderControl()
        {
            //s_contentScroll.verticalNormalizedPosition = s_lastScrollPos;
            ////s_contentScroll.verticalScrollbar.value = s_lastScrollPos;
            //ExplorerCore.Log("Set scroll pos to " + s_lastScrollPos);

            if (!InputManager.GetMouseButton(0))
            {
                s_sliderChangedWanted = false;
                s_currentSlider.value = 0;

                return;
            }

            if (ActiveInstance == null) return;

            var transform = ActiveInstance.TargetGO.transform;

            // get the current vector for the control type
            Vector3 vector = Vector2.zero;
            switch (s_currentSliderType)
            {
                case ControlType.position:
                    vector = transform.position; break;
                case ControlType.localPosition:
                    vector = transform.localPosition; break;
                case ControlType.eulerAngles:
                    vector = transform.eulerAngles; break;
                case ControlType.localScale:
                    vector = transform.localScale; break;
            }

            // apply vector value change
            switch (s_currentSliderValueType)
            {
                case VectorValue.x:
                    vector.x += s_currentSliderValue; break;
                case VectorValue.y:
                    vector.y += s_currentSliderValue; break;
                case VectorValue.z:
                    vector.z += s_currentSliderValue; break;
            }

            // set vector to transform member
            switch (s_currentSliderType)
            {
                case ControlType.position:
                    transform.position = vector; break;
                case ControlType.localPosition:
                    transform.localPosition = vector; break;
                case ControlType.eulerAngles:
                    transform.eulerAngles = vector; break;
                case ControlType.localScale:
                    transform.localScale = vector; break;
            }
        }

        private static void OnVectorControlInputApplied(ControlType controlType, VectorValue vectorValue)
        {
            if (!(InspectorManager.Instance.m_activeInspector is GameObjectInspector instance)) return;

            // get relevant input for controltype + value

            TMP_InputField[] inputs = null;
            switch (controlType)
            {
                case ControlType.position:
                    inputs = s_positionControl.inputs; break;
                case ControlType.localPosition:
                    inputs = s_localPosControl.inputs; break;
                case ControlType.eulerAngles:
                    inputs = s_rotationControl.inputs; break;
                case ControlType.localScale:
                    inputs = s_scaleControl.inputs; break;
            }
            TMP_InputField input = inputs[(int)vectorValue];

            float val = float.Parse(input.text);

            // apply transform value

            Vector3 vector = Vector3.zero;
            var transform = instance.TargetGO.transform;
            switch (controlType)
            {
                case ControlType.position:
                    vector = transform.position; break;
                case ControlType.localPosition:
                    vector = transform.localPosition; break;
                case ControlType.eulerAngles:
                    vector = transform.eulerAngles; break;
                case ControlType.localScale:
                    vector = transform.localScale; break;
            }

            switch (vectorValue)
            {
                case VectorValue.x:
                    vector.x = val; break;
                case VectorValue.y:
                    vector.y = val; break;
                case VectorValue.z:
                    vector.z = val; break;
            }

            // set back to transform
            switch (controlType)
            {
                case ControlType.position:
                    transform.position = vector; break;
                case ControlType.localPosition:
                    transform.localPosition = vector; break;
                case ControlType.eulerAngles:
                    transform.eulerAngles = vector; break;
                case ControlType.localScale:
                    transform.localScale = vector; break;
            }
        }

#endregion

#region UI CONSTRUCTION

        private void ConstructUI()
        {
            var parent = InspectorManager.Instance.m_inspectorContent;

            s_content = UIFactory.CreateScrollView(parent, out GameObject scrollContent, new Color(0.1f, 0.1f, 0.1f));
            s_contentScroll = s_content.GetComponent<ScrollRect>();
            //s_contentScroll.onValueChanged.AddListener((Vector2 val) => 
            //{
            //    if (s_sliderChangedWanted)
            //    {
            //        s_contentScroll.verticalNormalizedPosition = s_lastScrollPos;
            //    }
            //});

            var scrollGroup = scrollContent.GetComponent<VerticalLayoutGroup>();
            scrollGroup.childForceExpandHeight = false;
            scrollGroup.childControlHeight = true;
            scrollGroup.spacing = 5;

            ConstructTopArea(scrollContent);

            ConstructControls(scrollContent);

            // not sure if i'll use this mid group...

            //var midGroupObj = UIFactory.CreateHorizontalGroup(scrollContent, new Color(1,1,1,0));
            //var midGroup = midGroupObj.GetComponent<HorizontalLayoutGroup>();
            //midGroup.spacing = 5;
            //midGroup.childForceExpandWidth = true;
            //midGroup.childControlWidth = true;
            //midGroup.childForceExpandHeight = true;
            //midGroup.childControlHeight = false;
            //var midlayout = midGroupObj.AddComponent<LayoutElement>();
            //midlayout.minHeight = 40;
            //midlayout.flexibleHeight = 10000;
            //midlayout.flexibleWidth = 25000;
            //midlayout.minWidth = 200;

            ConstructChildList(scrollContent);

            ConstructCompList(scrollContent);
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
            var backText = backButtonObj.GetComponentInChildren<Text>();
            backText.text = "<";
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
            applyNameBtn.onClick.AddListener(new Action(() => { ApplyNameClicked(); }));
#else
            applyNameBtn.onClick.AddListener(() => { OnApplyNameClicked(); });
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
            var itemText = layerDropdownObj.transform.Find("Label").GetComponent<Text>();
            itemText.resizeTextForBestFit = true;
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

        private void ConstructChildList(GameObject parent)
        {
            var vertGroupObj = UIFactory.CreateVerticalGroup(parent, new Color(1, 1, 1, 0));
            var vertGroup = vertGroupObj.GetComponent<VerticalLayoutGroup>();
            vertGroup.childForceExpandHeight = false;
            vertGroup.childForceExpandWidth = false;
            vertGroup.childControlWidth = true;
            var vertLayout = vertGroupObj.AddComponent<LayoutElement>();
            vertLayout.minWidth = 120;
            vertLayout.flexibleWidth = 25000;
            vertLayout.minHeight = 150;
            vertLayout.flexibleHeight = 5000;
            vertLayout.preferredHeight = 250;

            var childTitleObj = UIFactory.CreateLabel(vertGroupObj, TextAnchor.MiddleLeft);
            var childTitleText = childTitleObj.GetComponent<Text>();
            childTitleText.text = "Children";
            childTitleText.color = Color.grey;
            childTitleText.fontSize = 14;
            var childTitleLayout = childTitleObj.AddComponent<LayoutElement>();
            childTitleLayout.minHeight = 30;

            s_childListPageHandler = new PageHandler();
            s_childListPageHandler.ConstructUI(vertGroupObj);
            s_childListPageHandler.OnPageChanged += OnChildListPageTurn;

            var childrenScrollObj = UIFactory.CreateScrollView(vertGroupObj, out s_childListContent, new Color(0.07f, 0.07f, 0.07f));
            var contentLayout = childrenScrollObj.AddComponent<LayoutElement>();
            contentLayout.minHeight = 50;
            contentLayout.flexibleHeight = 10000;

            var horiScroll = childrenScrollObj.transform.Find("Scrollbar Horizontal");
            horiScroll.gameObject.SetActive(false);

            var scrollRect = childrenScrollObj.GetComponentInChildren<ScrollRect>();
            scrollRect.horizontalScrollbar = null;

            var childGroup = s_childListContent.GetComponent<VerticalLayoutGroup>();
            childGroup.childControlHeight = true;
            childGroup.spacing = 2;
        }

        private void AddChildListButton()
        {
            int thisIndex = s_childListTexts.Count;

            GameObject btnGroupObj = UIFactory.CreateHorizontalGroup(s_childListContent, new Color(0.1f, 0.1f, 0.1f));
            HorizontalLayoutGroup btnGroup = btnGroupObj.GetComponent<HorizontalLayoutGroup>();
            btnGroup.childForceExpandWidth = true;
            btnGroup.childControlWidth = true;
            btnGroup.childForceExpandHeight = false;
            btnGroup.childControlHeight = true;
            LayoutElement btnLayout = btnGroupObj.AddComponent<LayoutElement>();
            btnLayout.flexibleWidth = 320;
            btnLayout.minHeight = 25;
            btnLayout.flexibleHeight = 0;
            btnGroupObj.AddComponent<Mask>();

            GameObject mainButtonObj = UIFactory.CreateButton(btnGroupObj);
            LayoutElement mainBtnLayout = mainButtonObj.AddComponent<LayoutElement>();
            mainBtnLayout.minHeight = 25;
            mainBtnLayout.flexibleHeight = 0;
            mainBtnLayout.minWidth = 240;
            mainBtnLayout.flexibleWidth = 0;
            Button mainBtn = mainButtonObj.GetComponent<Button>();
            ColorBlock mainColors = mainBtn.colors;
            mainColors.normalColor = new Color(0.07f, 0.07f, 0.07f);
            mainColors.highlightedColor = new Color(0.2f, 0.2f, 0.2f, 1);
            mainBtn.colors = mainColors;
#if CPP
            mainBtn.onClick.AddListener(new Action(() => { OnChildListObjectClicked(thisIndex); }));
#else
            mainBtn.onClick.AddListener(() => { OnChildListObjectClicked(thisIndex); });
#endif

            Text mainText = mainButtonObj.GetComponentInChildren<Text>();
            mainText.alignment = TextAnchor.MiddleLeft;
            mainText.horizontalOverflow = HorizontalWrapMode.Overflow;
            s_childListTexts.Add(mainText);
        }

        private void ConstructCompList(GameObject parent)
        {
            var vertGroupObj = UIFactory.CreateVerticalGroup(parent, new Color(1, 1, 1, 0));
            var vertGroup = vertGroupObj.GetComponent<VerticalLayoutGroup>();
            vertGroup.childForceExpandHeight = false;
            vertGroup.childForceExpandWidth = false;
            vertGroup.childControlWidth = true;
            var vertLayout = vertGroupObj.AddComponent<LayoutElement>();
            vertLayout.minWidth = 120;
            vertLayout.flexibleWidth = 25000;
            vertLayout.minHeight = 150;
            vertLayout.flexibleHeight = 5000;
            vertLayout.preferredHeight = 250;

            var compTitleObj = UIFactory.CreateLabel(vertGroupObj, TextAnchor.MiddleLeft);
            var compTitleText = compTitleObj.GetComponent<Text>();
            compTitleText.text = "Components";
            compTitleText.color = Color.grey;
            compTitleText.fontSize = 14;
            var childTitleLayout = compTitleObj.AddComponent<LayoutElement>();
            childTitleLayout.minHeight = 30;

            s_compListPageHandler = new PageHandler();
            s_compListPageHandler.ConstructUI(vertGroupObj);
            s_compListPageHandler.OnPageChanged += OnCompListPageTurn;

            var compScrollObj = UIFactory.CreateScrollView(vertGroupObj, out s_compListContent, new Color(0.07f, 0.07f, 0.07f));
            var contentLayout = compScrollObj.AddComponent<LayoutElement>();
            contentLayout.minHeight = 50;
            contentLayout.flexibleHeight = 10000;

            var contentGroup = s_compListContent.GetComponent<VerticalLayoutGroup>();
            contentGroup.childControlHeight = true;
            contentGroup.spacing = 2;

            var horiScroll = compScrollObj.transform.Find("Scrollbar Horizontal");
            horiScroll.gameObject.SetActive(false);

            var scrollRect = compScrollObj.GetComponentInChildren<ScrollRect>();
            scrollRect.horizontalScrollbar = null;
        }

        private void AddCompListButton()
        {
            int thisIndex = s_compListTexts.Count;

            GameObject btnGroupObj = UIFactory.CreateHorizontalGroup(s_compListContent, new Color(0.07f, 0.07f, 0.07f));
            HorizontalLayoutGroup btnGroup = btnGroupObj.GetComponent<HorizontalLayoutGroup>();
            btnGroup.childForceExpandWidth = false;
            btnGroup.childControlWidth = true;
            btnGroup.childForceExpandHeight = false;
            btnGroup.childControlHeight = true;
            btnGroup.childAlignment = TextAnchor.MiddleLeft;
            LayoutElement btnLayout = btnGroupObj.AddComponent<LayoutElement>();
            btnLayout.flexibleWidth = 320;
            btnLayout.minHeight = 25;
            btnLayout.flexibleHeight = 0;
            btnGroupObj.AddComponent<Mask>();

            // Behaviour enabled toggle

            var toggleObj = UIFactory.CreateToggle(btnGroupObj, out Toggle toggle, out Text toggleText);
            var togBg = toggleObj.transform.Find("Background").GetComponent<Image>();
            togBg.color = new Color(0.1f, 0.1f, 0.1f, 1);
            var toggleLayout = toggleObj.AddComponent<LayoutElement>();
            toggleLayout.minWidth = 25;
            toggleLayout.flexibleWidth = 0;
            toggleLayout.minHeight = 25;
            toggleLayout.flexibleHeight = 0;
            var checkImg = toggleObj.transform.Find("Background/Checkmark").GetComponent<Image>();
            checkImg.color = SyntaxColors.Class_Instance.ToColor();
            checkImg.color *= 0.66f;
#if CPP
            toggle.onValueChanged.AddListener(new Action<bool>((bool val) => { OnCompToggleClicked(thisIndex, val); }));
#else
            toggle.onValueChanged.AddListener((bool val) => { OnCompToggleClicked(thisIndex, val); });
#endif
            toggleText.text = "";
            s_compToggles.Add(toggle);

            // Main component button

            GameObject mainButtonObj = UIFactory.CreateButton(btnGroupObj);
            LayoutElement mainBtnLayout = mainButtonObj.AddComponent<LayoutElement>();
            mainBtnLayout.minHeight = 25;
            mainBtnLayout.flexibleHeight = 0;
            mainBtnLayout.minWidth = 240;
            mainBtnLayout.flexibleWidth = 999;
            Button mainBtn = mainButtonObj.GetComponent<Button>();
            ColorBlock mainColors = mainBtn.colors;
            mainColors.normalColor = new Color(0.07f, 0.07f, 0.07f);
            mainColors.highlightedColor = new Color(0.2f, 0.2f, 0.2f, 1);
            mainBtn.colors = mainColors;
#if CPP
            mainBtn.onClick.AddListener(new Action(() => { OnCompListObjectClicked(thisIndex); }));
#else
            mainBtn.onClick.AddListener(() => { OnCompListObjectClicked(thisIndex); });
#endif

            // Component button text

            Text mainText = mainButtonObj.GetComponentInChildren<Text>();
            mainText.alignment = TextAnchor.MiddleLeft;
            mainText.horizontalOverflow = HorizontalWrapMode.Overflow;
            mainText.color = SyntaxColors.Class_Instance.ToColor();
            s_compListTexts.Add(mainText);

            // TODO remove component button
        }

        //private const int CONTROLS_MIN_HEIGHT = 220;

        private void ConstructControls(GameObject parent)
        {
            var controlsObj = UIFactory.CreateVerticalGroup(parent, new Color(0.07f, 0.07f, 0.07f));
            var controlsGroup = controlsObj.GetComponent<VerticalLayoutGroup>();
            controlsGroup.childForceExpandWidth = false;
            controlsGroup.childControlWidth = true;
            controlsGroup.childForceExpandHeight = false;
            controlsGroup.spacing = 5;
            controlsGroup.padding.top = 4;
            controlsGroup.padding.left = 4;
            controlsGroup.padding.right = 4;
            controlsGroup.padding.bottom = 4;

            // ~~~~~~ Top row ~~~~~~

            var topRow = UIFactory.CreateHorizontalGroup(controlsObj, new Color(1, 1, 1, 0));
            var topRowGroup = topRow.GetComponent<HorizontalLayoutGroup>();
            topRowGroup.childForceExpandWidth = false;
            topRowGroup.childControlWidth = true;
            topRowGroup.childForceExpandHeight = false;
            topRowGroup.childControlHeight = true;
            topRowGroup.spacing = 5;

            var hideButtonObj = UIFactory.CreateButton(topRow);
            var hideButton = hideButtonObj.GetComponent<Button>();
            var hideColors = hideButton.colors;
            hideColors.normalColor = new Color(0.16f, 0.16f, 0.16f);
            hideButton.colors = hideColors;
            var hideText = hideButtonObj.GetComponentInChildren<Text>();
            hideText.text = "Show";
            hideText.fontSize = 14;
            var hideButtonLayout = hideButtonObj.AddComponent<LayoutElement>();
            hideButtonLayout.minWidth = 40;
            hideButtonLayout.flexibleWidth = 0;
            hideButtonLayout.minHeight = 25;
            hideButtonLayout.flexibleHeight = 0;

            var topTitle = UIFactory.CreateLabel(topRow, TextAnchor.MiddleLeft);
            var topText = topTitle.GetComponent<Text>();
            topText.text = "Transform Controls";
            var titleLayout = topTitle.AddComponent<LayoutElement>();
            titleLayout.minWidth = 100;
            titleLayout.flexibleWidth = 9500;
            titleLayout.minHeight = 25;

            //// ~~~~~~~~ Content ~~~~~~~~ //

            //var contentObj = UIFactory.CreateVerticalGroup(controlsObj, new Color(1, 1, 1, 0));
            //contentObj.SetActive(false);
            //var contentGroup = contentObj.GetComponent<VerticalLayoutGroup>();
            //contentGroup.childForceExpandHeight = false;
            //contentGroup.childControlHeight = true;
            //contentGroup.spacing = 5;
            //contentGroup.childForceExpandWidth = true;
            //contentGroup.childControlWidth = true;

            var contentObj = UIFactory.CreateVerticalGroup(controlsObj, new Color(1, 1, 1, 0));
            var contentGroup = contentObj.GetComponent<VerticalLayoutGroup>();
            contentGroup.childForceExpandHeight = false;
            contentGroup.childControlHeight = true;
            contentGroup.spacing = 5;
            contentGroup.childForceExpandWidth = true;
            contentGroup.childControlWidth = true;

            // ~~ add hide button callback now that we have scroll reference ~~
#if CPP
            hideButton.onClick.AddListener(new Action(OnHideClicked));
#else
            hideButton.onClick.AddListener(OnHideClicked);
#endif
            void OnHideClicked()
            {
                if (hideText.text == "Show")
                {
                    hideText.text = "Hide";
                    contentObj.SetActive(true);
                }
                else
                {
                    hideText.text = "Show";
                    contentObj.SetActive(false);
                }
            }

            // ~~~ set parent ~~~

            var setParentGroupObj = UIFactory.CreateHorizontalGroup(contentObj, new Color(1, 1, 1, 0));
            var setParentGroup = setParentGroupObj.GetComponent<HorizontalLayoutGroup>();
            setParentGroup.childForceExpandHeight = false;
            setParentGroup.childControlHeight = true;
            setParentGroup.childForceExpandWidth = false;
            setParentGroup.childControlWidth = true;
            setParentGroup.spacing = 5;
            var setParentLayout = setParentGroupObj.AddComponent<LayoutElement>();
            setParentLayout.minHeight = 25;
            setParentLayout.flexibleHeight = 0;

            var setParentLabelObj = UIFactory.CreateLabel(setParentGroupObj, TextAnchor.MiddleLeft);
            var setParentLabel = setParentLabelObj.GetComponent<Text>();
            setParentLabel.text = "Set Parent:";
            setParentLabel.color = Color.grey;
            setParentLabel.fontSize = 14;
            var setParentLabelLayout = setParentLabelObj.AddComponent<LayoutElement>();
            setParentLabelLayout.minWidth = 110;
            setParentLabelLayout.minHeight = 25;
            setParentLabelLayout.flexibleWidth = 0;

            var setParentInputObj = UIFactory.CreateTMPInput(setParentGroupObj, 14, 0, (int)TextAlignmentOptions.MidlineLeft);
            s_setParentInput = setParentInputObj.GetComponent<TMP_InputField>();
            var placeholderInput = setParentInputObj.transform.Find("TextArea/Placeholder").GetComponent<TextMeshProUGUI>();
            placeholderInput.text = "Enter a GameObject name or path...";
            var setParentInputLayout = setParentInputObj.AddComponent<LayoutElement>();
            setParentInputLayout.minHeight = 25;
            setParentInputLayout.preferredWidth = 400;
            setParentInputLayout.flexibleWidth = 9999;

            var applyButtonObj = UIFactory.CreateButton(setParentGroupObj);
            var applyButton = applyButtonObj.GetComponent<Button>();
#if CPP
            applyButton.onClick.AddListener(new Action(OnSetParentClicked));
#else
            applyButton.onClick.AddListener(OnSetParentClicked);
#endif
            var applyText = applyButtonObj.GetComponentInChildren<Text>();
            applyText.text = "Apply";
            var applyLayout = applyButtonObj.AddComponent<LayoutElement>();
            applyLayout.minWidth = 55;
            applyLayout.flexibleWidth = 0;
            applyLayout.minHeight = 25;
            applyLayout.flexibleHeight = 0;

            // transform controls

            ConstructVector3Editor(contentObj, "Position", ControlType.position, out s_positionControl);

            ConstructVector3Editor(contentObj, "Local Position", ControlType.localPosition, out s_localPosControl);

            ConstructVector3Editor(contentObj, "Rotation", ControlType.eulerAngles, out s_rotationControl);

            ConstructVector3Editor(contentObj, "Scale", ControlType.localScale, out s_scaleControl);

            // todo bottom row buttons



            // set controls content inactive now that content is made?
            contentObj.SetActive(false);
        }

        private void ConstructVector3Editor(GameObject parent, string title, ControlType type, out ControlEditor editor)
        {
            editor = new ControlEditor();

            var topBarObj = UIFactory.CreateHorizontalGroup(parent, new Color(1, 1, 1, 0));
            var topGroup = topBarObj.GetComponent<HorizontalLayoutGroup>();
            topGroup.childForceExpandWidth = false;
            topGroup.childControlWidth = true;
            topGroup.childForceExpandHeight = false;
            topGroup.childControlHeight = true;
            topGroup.spacing = 5;
            var topLayout = topBarObj.AddComponent<LayoutElement>();
            topLayout.minHeight = 25;
            topLayout.flexibleHeight = 0;

            var titleObj = UIFactory.CreateLabel(topBarObj, TextAnchor.MiddleLeft);
            var titleText = titleObj.GetComponent<Text>();
            titleText.text = title;
            titleText.color = Color.grey;
            titleText.fontSize = 14;
            var titleLayout = titleObj.AddComponent<LayoutElement>();
            titleLayout.minWidth = 110;
            titleLayout.flexibleWidth = 0;
            titleLayout.minHeight = 25;

            // expand button
            var expandButtonObj = UIFactory.CreateButton(topBarObj);
            var expandButton = expandButtonObj.GetComponent<Button>();
            var expandText = expandButtonObj.GetComponentInChildren<Text>();
            expandText.text = "▼";
            expandText.fontSize = 12;
            var btnLayout = expandButtonObj.AddComponent<LayoutElement>();
            btnLayout.minWidth = 35;
            btnLayout.flexibleWidth = 0;
            btnLayout.minHeight = 25;
            btnLayout.flexibleHeight = 0;

            // readonly value input

            var valueInputObj = UIFactory.CreateTMPInput(topBarObj, 14, 0, (int)TextAlignmentOptions.MidlineLeft);
            var valueInput = valueInputObj.GetComponent<TMP_InputField>();
            valueInput.readOnly = true;
            var valueInputLayout = valueInputObj.AddComponent<LayoutElement>();
            valueInputLayout.minHeight = 25;
            valueInputLayout.flexibleHeight = 0;
            valueInputLayout.preferredWidth = 400;
            valueInputLayout.flexibleWidth = 9999;

            editor.fullValue = valueInput;

            editor.sliders = new Slider[3];
            editor.inputs = new TMP_InputField[3];
            editor.values = new Text[3];

            var xRow = ConstructEditorRow(parent, editor, type, VectorValue.x);
            xRow.SetActive(false);
            var yRow = ConstructEditorRow(parent, editor, type, VectorValue.y);
            yRow.SetActive(false);
            var zRow = ConstructEditorRow(parent, editor, type, VectorValue.z);
            zRow.SetActive(false);

            // add expand callback now that we have group reference
#if CPP
            expandButton.onClick.AddListener(new Action(ToggleExpand));
#else
            expandButton.onClick.AddListener(ToggleExpand);
#endif
            void ToggleExpand()
            {
                if (xRow.activeSelf)
                {
                    xRow.SetActive(false);
                    yRow.SetActive(false);
                    zRow.SetActive(false);
                    expandText.text = "▼";
                }
                else
                {
                    xRow.SetActive(true);
                    yRow.SetActive(true);
                    zRow.SetActive(true);
                    expandText.text = "▲";
                }
            }
        }

        private GameObject ConstructEditorRow(GameObject parent, ControlEditor editor, ControlType type, VectorValue vectorValue)
        {
            var rowObject = UIFactory.CreateHorizontalGroup(parent, new Color(1, 1, 1, 0));
            var rowGroup = rowObject.GetComponent<HorizontalLayoutGroup>();
            rowGroup.childForceExpandWidth = false;
            rowGroup.childControlWidth = true;
            rowGroup.childForceExpandHeight = false;
            rowGroup.childControlHeight = true;
            rowGroup.spacing = 5;
            var rowLayout = rowObject.AddComponent<LayoutElement>();
            rowLayout.minHeight = 25;
            rowLayout.flexibleHeight = 0;
            rowLayout.minWidth = 100;

            // Value labels

            var labelObj = UIFactory.CreateLabel(rowObject, TextAnchor.MiddleLeft);
            var labelText = labelObj.GetComponent<Text>();
            labelText.color = Color.cyan;
            labelText.text = $"{vectorValue.ToString().ToUpper()}:";
            labelText.fontSize = 14;
            labelText.resizeTextMaxSize = 14;
            labelText.resizeTextForBestFit = true;
            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.minHeight = 25;
            labelLayout.flexibleHeight = 0;
            labelLayout.minWidth = 25;
            labelLayout.flexibleWidth = 0;

            // actual value label
            var valueLabelObj = UIFactory.CreateLabel(rowObject, TextAnchor.MiddleLeft);
            var valueLabel = valueLabelObj.GetComponent<Text>();
            editor.values[(int)vectorValue] = valueLabel;
            var valueLabelLayout = valueLabelObj.AddComponent<LayoutElement>();
            valueLabelLayout.minWidth = 85;
            valueLabelLayout.flexibleWidth = 0;
            valueLabelLayout.minHeight = 25;

            // Slider

            var sliderObj = UIFactory.CreateSlider(rowObject);
            var sliderLayout = sliderObj.AddComponent<LayoutElement>();
            sliderLayout.minHeight = 20;
            sliderLayout.flexibleHeight = 0;
            sliderLayout.minWidth = 200;
            sliderLayout.flexibleWidth = 9000;
            var slider = sliderObj.GetComponent<Slider>();
            slider.minValue = -2;
            slider.maxValue = 2;
            slider.value = 0;
#if CPP
            xSlider.onValueChanged.AddListener(new Action((float val) => { OnSliderControlChanged(val, xSlider, type, vectorValue); }));
#else
            slider.onValueChanged.AddListener((float val) => { OnSliderControlChanged(val, slider, type, vectorValue); });
#endif
            editor.sliders[(int)vectorValue] = slider;

            // input field

            var inputHolder = UIFactory.CreateVerticalGroup(rowObject, new Color(1, 1, 1, 0));
            var inputHolderGroup = inputHolder.GetComponent<VerticalLayoutGroup>();
            inputHolderGroup.childForceExpandHeight = false;
            inputHolderGroup.childControlHeight = true;
            inputHolderGroup.childForceExpandWidth = false;
            inputHolderGroup.childControlWidth = true;

            var inputObj = UIFactory.CreateTMPInput(inputHolder, 14, 0, (int)TextAlignmentOptions.MidlineLeft);
            var input = inputObj.GetComponent<TMP_InputField>();
            input.characterValidation = TMP_InputField.CharacterValidation.Decimal;

            var inputLayout = inputObj.AddComponent<LayoutElement>();
            inputLayout.minHeight = 25;
            inputLayout.flexibleHeight = 0;
            inputLayout.minWidth = 90;
            inputLayout.flexibleWidth = 50;

            editor.inputs[(int)vectorValue] = input;

            // apply button

            var applyBtnObj = UIFactory.CreateButton(rowObject);
            var applyBtn = applyBtnObj.GetComponent<Button>();
            var applyText = applyBtnObj.GetComponentInChildren<Text>();
            applyText.text = "Apply";
            applyText.fontSize = 14;
            var applyLayout = applyBtnObj.AddComponent<LayoutElement>();
            applyLayout.minWidth = 60;
            applyLayout.minHeight = 25;

            applyBtn.onClick.AddListener(() => { OnVectorControlInputApplied(type, vectorValue); });

            return rowObject;
        }

#endregion
    }
}
