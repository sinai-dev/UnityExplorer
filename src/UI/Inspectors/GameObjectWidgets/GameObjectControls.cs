using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Input;

namespace UnityExplorer.UI.Inspectors
{
    public class GameObjectControls
    {
        public GameObjectInspector Parent;
        private GameObject GOTarget => Parent.GOTarget;

        // Top info

        private ButtonRef ViewParentButton;
        private InputFieldRef PathInput;

        private InputFieldRef NameInput;
        private Toggle ActiveSelfToggle;
        private Text ActiveSelfText;
        private Toggle IsStaticToggle;

        private InputFieldRef SceneInput;
        private InputFieldRef InstanceIDInput;
        private InputFieldRef TagInput;

        private Dropdown LayerDropdown;
        private Dropdown FlagsDropdown;

        // transform controls

        private TransformControl PositionControl;
        private TransformControl LocalPositionControl;
        private TransformControl RotationControl;
        private TransformControl ScaleControl;

        private VectorSlider currentSlidingVectorControl;
        private float currentVectorValue;

        public GameObjectControls(GameObjectInspector parent)
        {
            this.Parent = parent;

            ConstructTopInfo();
            ConstructTransformControls();
        }

        #region GO Controls

        private string lastGoName;
        private string lastPath;
        private bool lastParentState;
        private int lastSceneHandle;
        private string lastTag;
        private int lastLayer;
        private int lastFlags;

        public void UpdateGameObjectInfo(bool firstUpdate, bool force)
        {
            if (firstUpdate)
            {
                InstanceIDInput.Text = GOTarget.GetInstanceID().ToString();
            }

            if (force || (!NameInput.Component.isFocused && GOTarget.name != lastGoName))
            {
                lastGoName = GOTarget.name;
                Parent.Tab.TabText.text = $"[G] {GOTarget.name}";
                NameInput.Text = GOTarget.name;
            }

            if (force || !PathInput.Component.isFocused)
            {
                string path = GOTarget.transform.GetTransformPath();
                if (path != lastPath)
                {
                    lastPath = path;
                    PathInput.Text = path;
                }
            }

            if (force || GOTarget.transform.parent != lastParentState)
            {
                lastParentState = GOTarget.transform.parent;
                ViewParentButton.Component.interactable = lastParentState;
                if (lastParentState)
                {
                    ViewParentButton.ButtonText.color = Color.white;
                    ViewParentButton.ButtonText.text = "◄ View Parent";
                }
                else
                {
                    ViewParentButton.ButtonText.color = Color.grey;
                    ViewParentButton.ButtonText.text = "No parent";
                }
            }

            if (force || GOTarget.activeSelf != ActiveSelfToggle.isOn)
            {
                ActiveSelfToggle.Set(GOTarget.activeSelf, false);
                ActiveSelfText.color = ActiveSelfToggle.isOn ? Color.green : Color.red;
            }

            if (force || GOTarget.isStatic != IsStaticToggle.isOn)
            {
                IsStaticToggle.Set(GOTarget.isStatic, false);
            }

            if (force || GOTarget.scene.handle != lastSceneHandle)
            {
                lastSceneHandle = GOTarget.scene.handle;
                SceneInput.Text = GOTarget.scene.IsValid() ? GOTarget.scene.name : "None (Asset/Resource)";
            }

            if (force || (!TagInput.Component.isFocused && GOTarget.tag != lastTag))
            {
                lastTag = GOTarget.tag;
                TagInput.Text = lastTag;
            }

            if (force || (GOTarget.layer != lastLayer))
            {
                lastLayer = GOTarget.layer;
                LayerDropdown.value = GOTarget.layer;
            }

            if (force || ((int)GOTarget.hideFlags != lastFlags))
            {
                lastFlags = (int)GOTarget.hideFlags;
                FlagsDropdown.captionText.text = GOTarget.hideFlags.ToString();
            }
        }

        private void OnViewParentClicked()
        {
            if (this.GOTarget && this.GOTarget.transform.parent)
            {
                Parent.ChangeTarget(this.GOTarget.transform.parent.gameObject);
            }
        }

        private void OnPathEndEdit(string input)
        {
            lastPath = input;

            if (string.IsNullOrEmpty(input))
            {
                DoSetParent(null);
            }
            else
            {
                Transform parentToSet = null;

                if (input.EndsWith("/"))
                    input = input.Remove(input.Length - 1);

                // try the easy way
                if (GameObject.Find(input) is GameObject found)
                {
                    parentToSet = found.transform;
                }
                else
                {
                    // look for inactive objects
                    var name = input.Split('/').Last();
                    var allObjects = RuntimeProvider.Instance.FindObjectsOfTypeAll(typeof(GameObject));
                    var shortList = new List<GameObject>();
                    foreach (var obj in allObjects)
                        if (obj.name == name) shortList.Add(obj.TryCast<GameObject>());
                    foreach (var go in shortList)
                    {
                        var path = go.transform.GetTransformPath(true);
                        if (path.EndsWith("/"))
                            path = path.Remove(path.Length - 1);
                        if (path == input)
                        {
                            parentToSet = go.transform;
                            break;
                        }
                    }
                }

                if (parentToSet)
                    DoSetParent(parentToSet);
                else
                { 
                    ExplorerCore.LogWarning($"Could not find any GameObject name or path '{input}'!");
                    UpdateGameObjectInfo(false, true);
                }
            }

        }

        private void DoSetParent(Transform transform)
        {
            ExplorerCore.Log($"Setting target's transform parent to: {(transform == null ? "null" : $"'{transform.name}'")}");

            if (GOTarget.GetComponent<RectTransform>())
                GOTarget.transform.SetParent(transform, false);
            else
                GOTarget.transform.parent = transform;

            UpdateGameObjectInfo(false, false);
            UpdateTransformControlValues(false);
        }

        private void OnNameEndEdit(string value)
        {
            GOTarget.name = value;
            UpdateGameObjectInfo(false, true);
        }

        private void OnActiveSelfToggled(bool value)
        {
            GOTarget.SetActive(value);
            UpdateGameObjectInfo(false, true);
        }

        private void OnTagEndEdit(string value)
        {
            try
            {
                GOTarget.tag = value;
                UpdateGameObjectInfo(false, true);
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception setting tag! {ex.ReflectionExToString()}");
            }
        }

        private void OnLayerDropdownChanged(int value)
        {
            GOTarget.layer = value;
            UpdateGameObjectInfo(false, true);
        }

        private void OnFlagsDropdownChanged(int value)
        {
            try
            {
                var enumVal = hideFlagsValues[FlagsDropdown.options[value].text];
                GOTarget.hideFlags = enumVal;

                UpdateGameObjectInfo(false, true);
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception setting hideFlags: {ex}");
            }
        }

        private void OnDestroyClicked()
        {
            GameObject.Destroy(this.GOTarget);
            InspectorManager.ReleaseInspector(Parent);
        }

        private void OnInstantiateClicked()
        {
            var clone = GameObject.Instantiate(this.GOTarget);
            InspectorManager.Inspect(clone);
        }

        #endregion


        #region Transform Controls

        private enum TransformType { Position, LocalPosition, Rotation, Scale }

        private class TransformControl
        {
            public TransformType Type;
            public InputFieldRef Input;

            public TransformControl(TransformType type, InputFieldRef input)
            {
                this.Type = type;
                this.Input = input;
            }
        }

        private class VectorSlider
        {
            public int axis;
            public Slider slider;
            public TransformControl parentControl;

            public VectorSlider(int axis, Slider slider, TransformControl parentControl)
            {
                this.axis = axis;
                this.slider = slider;
                this.parentControl = parentControl;
            }
        }

        private Vector3 lastPosValue;
        private Vector3 lastLocalValue;
        private Quaternion lastRotValue;
        private Vector3 lastScaleValue;

        public void UpdateTransformControlValues(bool force)
        {
            var transform = GOTarget.transform;
            if (force || (!PositionControl.Input.Component.isFocused && lastPosValue != transform.position))
            {
                PositionControl.Input.Text = ParseUtility.ToStringForInput(transform.position, typeof(Vector3));
                lastPosValue = transform.position;
            }
            if (force || (!LocalPositionControl.Input.Component.isFocused && lastLocalValue != transform.localPosition))
            {
                LocalPositionControl.Input.Text = ParseUtility.ToStringForInput(transform.localPosition, typeof(Vector3));
                lastLocalValue = transform.localPosition;
            }
            if (force || (!RotationControl.Input.Component.isFocused && lastRotValue != transform.localRotation))
            {
                RotationControl.Input.Text = ParseUtility.ToStringForInput(transform.localRotation, typeof(Quaternion));
                lastRotValue = transform.localRotation;
            }
            if (force || (!ScaleControl.Input.Component.isFocused && lastScaleValue != transform.localScale))
            {
                ScaleControl.Input.Text = ParseUtility.ToStringForInput(transform.localScale, typeof(Vector3));
                lastScaleValue = transform.localScale;
            }
        }

        private void OnTransformInputEndEdit(TransformType type, string input)
        {
            switch (type)
            {
                case TransformType.Position:
                    {
                        if (ParseUtility.TryParse(input, typeof(Vector3), out object boxed, out _))
                            GOTarget.transform.position = (Vector3)boxed;
                    }
                    break;
                case TransformType.LocalPosition:
                    {
                        if (ParseUtility.TryParse(input, typeof(Vector3), out object boxed, out _))
                            GOTarget.transform.localPosition = (Vector3)boxed;
                    }
                    break;
                case TransformType.Rotation:
                    {
                        if (ParseUtility.TryParse(input, typeof(Quaternion), out object boxed, out _))
                            GOTarget.transform.localRotation = (Quaternion)boxed;
                    }
                    break;
                case TransformType.Scale:
                    {
                        if (ParseUtility.TryParse(input, typeof(Vector3), out object boxed, out _))
                            GOTarget.transform.localScale = (Vector3)boxed;
                    }
                    break;
            }

            UpdateTransformControlValues(true);
        }

        private void OnVectorSliderChanged(VectorSlider slider, float value)
        {
            if (value == 0f)
            {
                currentSlidingVectorControl = null;
            }
            else
            {
                currentSlidingVectorControl = slider;
                currentVectorValue = value;
            }
        }

        public void UpdateVectorSlider()
        {
            if (currentSlidingVectorControl == null)
                return;

            if (!InputManager.GetMouseButton(0))
            {
                currentSlidingVectorControl.slider.value = 0f;
                currentSlidingVectorControl = null;
                currentVectorValue = 0f;
                return;
            }

            var transform = GOTarget.transform;

            Vector3 vector = Vector2.zero;
            switch (currentSlidingVectorControl.parentControl.Type)
            {
                case TransformType.Position:
                    vector = transform.position; break;
                case TransformType.LocalPosition:
                    vector = transform.localPosition; break;
                case TransformType.Rotation:
                    vector = transform.eulerAngles; break;
                case TransformType.Scale:
                    vector = transform.localScale; break;
            }

            // apply vector value change
            switch (currentSlidingVectorControl.axis)
            {
                case 0:
                    vector.x += currentVectorValue; break;
                case 1:
                    vector.y += currentVectorValue; break;
                case 2:
                    vector.z += currentVectorValue; break;
            }

            // set vector back to transform
            switch (currentSlidingVectorControl.parentControl.Type)
            {
                case TransformType.Position:
                    transform.position = vector; break;
                case TransformType.LocalPosition:
                    transform.localPosition = vector; break;
                case TransformType.Rotation:
                    transform.eulerAngles = vector; break;
                case TransformType.Scale:
                    transform.localScale = vector; break;
            }

            UpdateTransformControlValues(false);
        }

        #endregion


        #region GO Controls UI Construction

        private void ConstructTopInfo()
        {
            var topInfoHolder = UIFactory.CreateVerticalGroup(Parent.Content, "TopInfoHolder", false, false, true, true, 3, 
                new Vector4(3, 3, 3, 3), new Color(0.1f, 0.1f, 0.1f), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(topInfoHolder, minHeight: 100, flexibleWidth: 9999);
            topInfoHolder.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // first row (parent, path)

            var firstRow = UIFactory.CreateUIObject("ParentRow", topInfoHolder);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(firstRow, false, false, true, true, 5, 0, 0, 0, 0, default);
            UIFactory.SetLayoutElement(firstRow, minHeight: 25, flexibleWidth: 9999);

            ViewParentButton = UIFactory.CreateButton(firstRow, "ViewParentButton", "◄ View Parent", new Color(0.2f, 0.2f, 0.2f));
            ViewParentButton.ButtonText.fontSize = 13;
            UIFactory.SetLayoutElement(ViewParentButton.Component.gameObject, minHeight: 25, minWidth: 100);
            ViewParentButton.OnClick += OnViewParentClicked;

            this.PathInput = UIFactory.CreateInputField(firstRow, "PathInput", "...");
            PathInput.Component.textComponent.color = Color.grey;
            PathInput.Component.textComponent.fontSize = 14;
            UIFactory.SetLayoutElement(PathInput.UIRoot, minHeight: 25, minWidth: 100, flexibleWidth: 9999);
            PathInput.Component.lineType = InputField.LineType.MultiLineSubmit;

            //var pathApplyBtn = UIFactory.CreateButton(firstRow, "PathButton", "Set Parent Path", new Color(0.2f, 0.2f, 0.2f));
            //UIFactory.SetLayoutElement(pathApplyBtn.Component.gameObject, minHeight: 25, minWidth: 120);
            //pathApplyBtn.OnClick += () => { OnPathEndEdit(PathInput.Text); };

            PathInput.Component.onEndEdit.AddListener((string val) => { OnPathEndEdit(val); });

            // Title and update row

            var titleRow = UIFactory.CreateUIObject("TitleRow", topInfoHolder);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(titleRow, false, false, true, true, 5);

            var titleLabel = UIFactory.CreateLabel(titleRow, "Title", SignatureHighlighter.Parse(typeof(GameObject), false),
                TextAnchor.MiddleLeft, fontSize: 17);
            UIFactory.SetLayoutElement(titleLabel.gameObject, minHeight: 30, minWidth: 100);

            // name

            NameInput = UIFactory.CreateInputField(titleRow, "NameInput", "untitled");
            UIFactory.SetLayoutElement(NameInput.Component.gameObject, minHeight: 30, minWidth: 100, flexibleWidth: 9999);
            NameInput.Component.textComponent.fontSize = 15;
            NameInput.Component.onEndEdit.AddListener((string val) => { OnNameEndEdit(val); });

            // second row (toggles, instanceID, tag, buttons)

            var secondRow = UIFactory.CreateUIObject("ParentRow", topInfoHolder);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(secondRow, false, false, true, true, 5, 0, 0, 0, 0, default);
            UIFactory.SetLayoutElement(secondRow, minHeight: 25, flexibleWidth: 9999);

            // activeSelf
            var activeToggleObj = UIFactory.CreateToggle(secondRow, "ActiveSelf", out ActiveSelfToggle, out ActiveSelfText);
            UIFactory.SetLayoutElement(activeToggleObj, minHeight: 25, minWidth: 100);
            ActiveSelfText.text = "ActiveSelf";
            ActiveSelfToggle.onValueChanged.AddListener(OnActiveSelfToggled);

            // isStatic
            var isStaticObj = UIFactory.CreateToggle(secondRow, "IsStatic", out IsStaticToggle, out Text staticText);
            UIFactory.SetLayoutElement(isStaticObj, minHeight: 25, minWidth: 80);
            staticText.text = "IsStatic";
            staticText.color = Color.grey;
            IsStaticToggle.interactable = false;

            // InstanceID
            var instanceIdLabel = UIFactory.CreateLabel(secondRow, "InstanceIDLabel", "Instance ID:", TextAnchor.MiddleRight, Color.grey);
            UIFactory.SetLayoutElement(instanceIdLabel.gameObject, minHeight: 25, minWidth: 90);

            InstanceIDInput = UIFactory.CreateInputField(secondRow, "InstanceIDInput", "error");
            UIFactory.SetLayoutElement(InstanceIDInput.Component.gameObject, minHeight: 25, minWidth: 110);
            InstanceIDInput.Component.textComponent.color = Color.grey;
            InstanceIDInput.Component.readOnly = true;

            //Tag
            var tagLabel = UIFactory.CreateLabel(secondRow, "TagLabel", "Tag:", TextAnchor.MiddleRight, Color.grey);
            UIFactory.SetLayoutElement(tagLabel.gameObject, minHeight: 25, minWidth: 40);

            TagInput = UIFactory.CreateInputField(secondRow, "TagInput", "none");
            UIFactory.SetLayoutElement(TagInput.Component.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 999);
            TagInput.Component.textComponent.color = Color.white;
            TagInput.Component.onEndEdit.AddListener((string val) => { OnTagEndEdit(val); });

            // Instantiate
            var instantiateBtn = UIFactory.CreateButton(secondRow, "InstantiateBtn", "Instantiate", new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(instantiateBtn.Component.gameObject, minHeight: 25, minWidth: 120);
            instantiateBtn.OnClick += OnInstantiateClicked;

            // Destroy
            var destroyBtn = UIFactory.CreateButton(secondRow, "DestroyBtn", "Destroy", new Color(0.3f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(destroyBtn.Component.gameObject, minHeight: 25, minWidth: 80);
            destroyBtn.OnClick += OnDestroyClicked;

            // third row (scene, layer, flags)

            var thirdrow = UIFactory.CreateUIObject("ParentRow", topInfoHolder);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(thirdrow, false, false, true, true, 5, 0, 0, 0, 0, default);
            UIFactory.SetLayoutElement(thirdrow, minHeight: 25, flexibleWidth: 9999);

            // Scene
            var sceneLabel = UIFactory.CreateLabel(thirdrow, "SceneLabel", "Scene:", TextAnchor.MiddleLeft, Color.grey);
            UIFactory.SetLayoutElement(sceneLabel.gameObject, minHeight: 25, minWidth: 50);

            SceneInput = UIFactory.CreateInputField(thirdrow, "SceneInput", "untitled");
            UIFactory.SetLayoutElement(SceneInput.Component.gameObject, minHeight: 25, minWidth: 120, flexibleWidth: 999);
            SceneInput.Component.readOnly = true;
            SceneInput.Component.textComponent.color = new Color(0.7f, 0.7f, 0.7f);

            // Layer
            var layerLabel = UIFactory.CreateLabel(thirdrow, "LayerLabel", "Layer:", TextAnchor.MiddleLeft, Color.grey);
            UIFactory.SetLayoutElement(layerLabel.gameObject, minHeight: 25, minWidth: 50);

            var layerDrop = UIFactory.CreateDropdown(thirdrow, out LayerDropdown, "0", 14, OnLayerDropdownChanged);
            UIFactory.SetLayoutElement(layerDrop, minHeight: 25, minWidth: 120, flexibleWidth: 999);
            LayerDropdown.captionText.color = SignatureHighlighter.EnumGreen;
            if (layerToNames == null)
                GetLayerNames();
            foreach (var name in layerToNames)
                LayerDropdown.options.Add(new Dropdown.OptionData(name));
            LayerDropdown.value = 0;
            LayerDropdown.RefreshShownValue();

            // Flags
            var flagsLabel = UIFactory.CreateLabel(thirdrow, "FlagsLabel", "Flags:", TextAnchor.MiddleRight, Color.grey);
            UIFactory.SetLayoutElement(flagsLabel.gameObject, minHeight: 25, minWidth: 50);

            var flagsDrop = UIFactory.CreateDropdown(thirdrow, out FlagsDropdown, "None", 14, OnFlagsDropdownChanged);
            FlagsDropdown.captionText.color = SignatureHighlighter.EnumGreen;
            UIFactory.SetLayoutElement(flagsDrop, minHeight: 25, minWidth: 135, flexibleWidth: 999);
            if (hideFlagsValues == null) 
                GetHideFlagNames();
            foreach (var name in hideFlagsValues.Keys)
                FlagsDropdown.options.Add(new Dropdown.OptionData(name));
            FlagsDropdown.value = 0;
            FlagsDropdown.RefreshShownValue();
        }

        private static List<string> layerToNames;

        private static void GetLayerNames()
        {
            layerToNames = new List<string>();
            for (int i = 0; i < 32; i++)
            {
                var name = RuntimeProvider.Instance.LayerToName(i);
                if (string.IsNullOrEmpty(name))
                    name = i.ToString();
                layerToNames.Add(name);
            }
        }

        private static Dictionary<string, HideFlags> hideFlagsValues;

        private static void GetHideFlagNames()
        {
            hideFlagsValues = new Dictionary<string, HideFlags>();

            var names = Enum.GetValues(typeof(HideFlags));
            foreach (HideFlags value in names)
            {
                hideFlagsValues.Add(value.ToString(), value);
            }
        }

        #endregion


        #region Transform Controls UI Construction

        private void ConstructTransformControls()
        {
            var transformGroup = UIFactory.CreateVerticalGroup(Parent.Content, "TransformControls", false, false, true, true, 2,
                new Vector4(2, 2, 0, 0), new Color(0.1f, 0.1f, 0.1f));
            UIFactory.SetLayoutElement(transformGroup, minHeight: 100, flexibleWidth: 9999);
            //transformGroup.SetActive(false);
            //var groupRect = transformGroup.GetComponent<RectTransform>();
            //groupRect.anchorMin = new Vector2(0, 1);
            //groupRect.anchorMax = new Vector2(1, 1);
            //groupRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 100);

            PositionControl = AddTransformRow(transformGroup, "Position:", TransformType.Position);
            LocalPositionControl = AddTransformRow(transformGroup, "Local Position:", TransformType.LocalPosition);
            RotationControl = AddTransformRow(transformGroup, "Rotation:", TransformType.Rotation);
            ScaleControl = AddTransformRow(transformGroup, "Scale:", TransformType.Scale);
        }

        private TransformControl AddTransformRow(GameObject transformGroup, string title, TransformType type)
        {
            var rowObj = UIFactory.CreateUIObject("Row_" + title, transformGroup);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(rowObj, false, false, true, true, 5, 0, 0, 0, 0, default);
            UIFactory.SetLayoutElement(rowObj, minHeight: 25, flexibleWidth: 9999);

            var titleLabel = UIFactory.CreateLabel(rowObj, "PositionLabel", title, TextAnchor.MiddleRight, Color.grey);
            UIFactory.SetLayoutElement(titleLabel.gameObject, minHeight: 25, minWidth: 110);

            var inputField = UIFactory.CreateInputField(rowObj, "InputField", "...");
            UIFactory.SetLayoutElement(inputField.Component.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 999);

            inputField.Component.onEndEdit.AddListener((string value) => { OnTransformInputEndEdit(type, value); });

            var control = new TransformControl(type, inputField);

            AddVectorAxisSlider(rowObj, "X", 0, control);
            AddVectorAxisSlider(rowObj, "Y", 1, control);
            AddVectorAxisSlider(rowObj, "Z", 2, control);

            return control;
        }

        private VectorSlider AddVectorAxisSlider(GameObject parent, string title, int axis, TransformControl control)
        {
            var label = UIFactory.CreateLabel(parent, "Label_" + title, title + ":", TextAnchor.MiddleRight, Color.grey);
            UIFactory.SetLayoutElement(label.gameObject, minHeight: 25, minWidth: 30);

            var sliderObj = UIFactory.CreateSlider(parent, "Slider_" + title, out var slider);
            UIFactory.SetLayoutElement(sliderObj, minHeight: 25, minWidth: 120, flexibleWidth: 0);
            slider.m_FillImage.color = Color.clear;

            slider.minValue = -1;
            slider.maxValue = 1;
            var sliderControl = new VectorSlider(axis, slider, control);

            slider.onValueChanged.AddListener((float val) =>
            {
                OnVectorSliderChanged(sliderControl, val);
            });

            return sliderControl;
        }

        #endregion
    }
}
