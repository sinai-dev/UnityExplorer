using System;
using System.Collections.Generic;
using ExplorerBeta.Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplorerBeta.UI.Main.Inspectors
{
    // TODO:
    // make page handler for children and component lists
    // -- clicking a child.. open new tab or change this tab target?
    // make top info panel (path, scene, layer, enabled)
    // make controls panel (transform controls, set parent, etc)

    public class GameObjectInspector : InspectorBase
    {
        public override string TabLabel => $" [G] {TargetGO?.name}";

        // just to help with casting in il2cpp
        public GameObject TargetGO;

        // cached ui elements
        public TMP_InputField m_nameInput;
        public TMP_InputField m_pathInput;

        public GameObjectInspector(GameObject target) : base(target)
        {
            TargetGO = target;

            if (!TargetGO)
            {
                ExplorerCore.LogWarning("GameObjectInspector cctor: Target GameObject is null!");
                return;
            }

            ConstructUI();
        }

        public override void Update()
        {
            base.Update();

            if (m_pendingDestroy || InspectorManager.Instance.m_activeInspector != this)
            {
                return;
            }

            m_nameInput.text = TargetGO.name;
            m_pathInput.text = TargetGO.transform.GetTransformPath();
        }

        private void ChangeInspectorTarget(GameObject newTarget)
        {
            if (!newTarget)
                return;

            this.Target = this.TargetGO = newTarget;

            // ?
        }

        #region UI CONSTRUCTION

        private void ConstructUI()
        {
            var parent = InspectorManager.Instance.m_inspectorContent;

            this.Content = UIFactory.CreateScrollView(parent, out GameObject scrollContent, new Color(0.1f, 0.1f, 0.1f, 1));

            var scrollLayout = scrollContent.GetComponent<VerticalLayoutGroup>();
            scrollLayout.childForceExpandHeight = false;
            scrollLayout.childControlHeight = true;
            scrollLayout.spacing = 5;

            ConstructTopArea(scrollContent);

            ConstructChildList(scrollContent);

            ConstructCompList(scrollContent);

            ConstructControls(scrollContent);
        }

        private void ConstructTopArea(GameObject scrollContent)
        {
            // name row

            var nameObj = UIFactory.CreateHorizontalGroup(scrollContent, new Color(0.1f, 0.1f, 0.1f));
            var nameGroup = nameObj.GetComponent<HorizontalLayoutGroup>();
            nameGroup.childForceExpandHeight = false;
            nameGroup.childForceExpandWidth = false;
            nameGroup.childControlHeight = false;
            nameGroup.childControlWidth = true;
            var nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(nameRect.sizeDelta.x, 25);
            var nameLayout = nameObj.AddComponent<LayoutElement>();
            nameLayout.minHeight = 25;
            nameLayout.flexibleHeight = 0;

            var nameTextObj = UIFactory.CreateTMPLabel(nameObj, TextAlignmentOptions.Left);
            var nameTextText = nameTextObj.GetComponent<TextMeshProUGUI>();
            nameTextText.text = "Name:";
            nameTextText.fontSize = 14;
            var nameTextLayout = nameTextObj.AddComponent<LayoutElement>();
            nameTextLayout.minHeight = 25;
            nameTextLayout.flexibleHeight = 0;
            nameTextLayout.minWidth = 60;
            nameTextLayout.flexibleWidth = 0;

            var nameInputObj = UIFactory.CreateTMPInput(nameObj, 14, 0, (int)TextAlignmentOptions.MidlineLeft);
            var nameInputRect = nameInputObj.GetComponent<RectTransform>();
            nameInputRect.sizeDelta = new Vector2(nameInputRect.sizeDelta.x, 25);
            m_nameInput = nameInputObj.GetComponent<TMP_InputField>();
            m_nameInput.text = TargetGO.name;
            m_nameInput.lineType = TMP_InputField.LineType.SingleLine;

            var applyNameBtnObj = UIFactory.CreateButton(nameObj);
            var applyNameBtn = applyNameBtnObj.GetComponent<Button>();
            applyNameBtn.onClick.AddListener(new Action(() =>
            {
                TargetGO.name = m_nameInput.text;
            }));
            var applyNameText = applyNameBtnObj.GetComponentInChildren<Text>();
            applyNameText.text = "Apply";
            applyNameText.fontSize = 14;
            var applyNameLayout = applyNameBtnObj.AddComponent<LayoutElement>();
            applyNameLayout.minWidth = 65;
            applyNameLayout.minHeight = 25;
            applyNameLayout.flexibleHeight = 0;
            var applyNameRect = applyNameBtnObj.GetComponent<RectTransform>();
            applyNameRect.sizeDelta = new Vector2(applyNameRect.sizeDelta.x, 25);

            // path row

            var pathObj = UIFactory.CreateHorizontalGroup(scrollContent, new Color(0.1f, 0.1f, 0.1f));
            var pathGroup = pathObj.GetComponent<HorizontalLayoutGroup>();
            pathGroup.childForceExpandHeight = false;
            pathGroup.childForceExpandWidth = false;
            pathGroup.childControlHeight = false;
            pathGroup.childControlWidth = true;
            var pathRect = pathObj.GetComponent<RectTransform>();
            pathRect.sizeDelta = new Vector2(pathRect.sizeDelta.x, 25);
            var pathLayout = pathObj.AddComponent<LayoutElement>();
            pathLayout.minHeight = 25;
            pathLayout.flexibleHeight = 0;

            var pathTextObj = UIFactory.CreateTMPLabel(pathObj, TextAlignmentOptions.Left);
            var pathTextText = pathTextObj.GetComponent<TextMeshProUGUI>();
            pathTextText.text = "Path:";
            pathTextText.fontSize = 14;
            var pathTextLayout = pathTextObj.AddComponent<LayoutElement>();
            pathTextLayout.minHeight = 25;
            pathTextLayout.flexibleHeight = 0;
            pathTextLayout.minWidth = 60;
            pathTextLayout.flexibleWidth = 0;

            // TODO back button here (if has parent)

            var pathInputObj = UIFactory.CreateTMPInput(pathObj, 14, 0, (int)TextAlignmentOptions.MidlineLeft);
            var pathInputRect = pathInputObj.GetComponent<RectTransform>();
            pathInputRect.sizeDelta = new Vector2(pathInputRect.sizeDelta.x, 25);
            m_pathInput = pathInputObj.GetComponent<TMP_InputField>();
            m_pathInput.text = TargetGO.transform.GetTransformPath();
            var pathInputLayout = pathInputObj.AddComponent<LayoutElement>();
            pathInputLayout.minHeight = 25;
            pathInputLayout.flexibleHeight = 0;

            var applyPathBtnObj = UIFactory.CreateButton(pathObj);
            var applyPathBtn = applyPathBtnObj.GetComponent<Button>();
            applyNameBtn.onClick.AddListener(new Action(() =>
            {
                ExplorerCore.Log("TODO");
            }));
            var applyPathText = applyPathBtnObj.GetComponentInChildren<Text>();
            applyPathText.text = "Apply";
            applyPathText.fontSize = 14;
            var applyBtnLayout = applyPathBtnObj.AddComponent<LayoutElement>();
            applyBtnLayout.minWidth = 65;
            applyBtnLayout.minHeight = 25;
            applyBtnLayout.flexibleHeight = 0;
            var applyBtnRect = applyPathBtnObj.GetComponent<RectTransform>();
            applyBtnRect.sizeDelta = new Vector2(applyNameRect.sizeDelta.x, 25);
        }

        private void ConstructChildList(GameObject scrollContent)
        {
            // todo put this in a RefreshChildren method, and use page handler

            var childTitleObj = UIFactory.CreateLabel(scrollContent, TextAnchor.MiddleLeft);
            var childTitleText = childTitleObj.GetComponent<Text>();
            childTitleText.text = "Children:";

            var childrenScrollObj = UIFactory.CreateScrollView(scrollContent, out GameObject subContent, new Color(0.15f, 0.15f, 0.15f));
            var contentLayout = childrenScrollObj.AddComponent<LayoutElement>();
            contentLayout.minHeight = 50;
            contentLayout.flexibleHeight = 10000;

            var contentGroup = subContent.GetComponent<VerticalLayoutGroup>();
            contentGroup.spacing = 4;

            for (int i = 0; i < TargetGO.transform.childCount; i++)
            {
                var child = TargetGO.transform.GetChild(i);

                var buttonObj = UIFactory.CreateButton(subContent);

                var btnImage = buttonObj.GetComponent<Image>();
                btnImage.color = new Color(0.15f, 0.15f, 0.15f);

                var button = buttonObj.GetComponent<Button>();
                button.onClick.AddListener(new Action(() => 
                {
                    ChangeInspectorTarget(child?.gameObject);
                }));

                var buttonText = buttonObj.GetComponentInChildren<Text>();
                var text = child.name;
                if (child.childCount > 0)
                    text = $"<color=grey>[{child.childCount}]</color> {text}";
                buttonText.text = text;
                buttonText.color = child.gameObject.activeSelf ? Color.green : Color.red;
            }
        }

        private void ConstructCompList(GameObject scrollContent)
        {
            // todo put this in a RefreshComponents method, and use page handler

            var compTitleObj = UIFactory.CreateLabel(scrollContent, TextAnchor.MiddleLeft);
            var compTitleText = compTitleObj.GetComponent<Text>();
            compTitleText.text = "Components:";

            var compScrollObj = UIFactory.CreateScrollView(scrollContent, out GameObject subContent, new Color(0.15f, 0.15f, 0.15f));
            var contentLayout = compScrollObj.AddComponent<LayoutElement>();
            contentLayout.minHeight = 50;
            contentLayout.flexibleHeight = 10000;

            var contentGroup = subContent.GetComponent<VerticalLayoutGroup>();
            contentGroup.spacing = 4;

            foreach (var comp in TargetGO.GetComponents<Component>())
            {
                var buttonObj = UIFactory.CreateButton(subContent);

                var btnImage = buttonObj.GetComponent<Image>();
                btnImage.color = new Color(0.15f, 0.15f, 0.15f);

                var button = buttonObj.GetComponent<Button>();
                button.onClick.AddListener(new Action(() =>
                {
                    InspectorManager.Instance.Inspect(comp);
                }));

                var buttonText = buttonObj.GetComponentInChildren<Text>();
                buttonText.text = ReflectionHelpers.GetActualType(comp).FullName;
            }
        }

        private void ConstructControls(GameObject scrollContent)
        {
            // todo GO controls
        }

        #endregion
    }
}
