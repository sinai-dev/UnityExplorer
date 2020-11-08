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

namespace UnityExplorer.Inspectors.GOInspector
{
    public class ComponentList
    {
        internal static ComponentList Instance;

        public ComponentList()
        {
            Instance = this;
        }

        public static PageHandler s_compListPageHandler;
        private static Component[] s_allComps = new Component[0];
        private static readonly List<Component> s_compShortlist = new List<Component>();
        private static GameObject s_compListContent;
        private static readonly List<Text> s_compListTexts = new List<Text>();
        private static int s_lastCompCount;
        public static readonly List<Toggle> s_compToggles = new List<Toggle>();

        internal void RefreshComponentList()
        {
            var go = GameObjectInspector.ActiveInstance.TargetGO;

            s_allComps = go.GetComponents<Component>().ToArray();

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

        internal static void OnCompToggleClicked(int index, bool value)
        {
            var comp = s_compShortlist[index];

            (comp as Behaviour).enabled = value;
        }

        internal static void OnCompListObjectClicked(int index)
        {
            if (index >= s_compShortlist.Count || !s_compShortlist[index])
            {
                return;
            }

            InspectorManager.Instance.Inspect(s_compShortlist[index]);
        }

        internal static void OnCompListPageTurn()
        {
            if (Instance == null)
                return;

            Instance.RefreshComponentList();
        }


        #region UI CONSTRUCTION

        internal void ConstructCompList(GameObject parent)
        {
            var vertGroupObj = UIFactory.CreateVerticalGroup(parent, new Color(1, 1, 1, 0));
            var vertGroup = vertGroupObj.GetComponent<VerticalLayoutGroup>();
            vertGroup.childForceExpandHeight = false;
            vertGroup.childForceExpandWidth = false;
            vertGroup.childControlWidth = true;
            vertGroup.spacing = 5;
            var vertLayout = vertGroupObj.AddComponent<LayoutElement>();
            vertLayout.minWidth = 120;
            vertLayout.flexibleWidth = 25000;
            vertLayout.minHeight = 200;
            vertLayout.flexibleHeight = 5000;

            var compTitleObj = UIFactory.CreateLabel(vertGroupObj, TextAnchor.MiddleLeft);
            var compTitleText = compTitleObj.GetComponent<Text>();
            compTitleText.text = "Components";
            compTitleText.color = Color.grey;
            compTitleText.fontSize = 14;
            var childTitleLayout = compTitleObj.AddComponent<LayoutElement>();
            childTitleLayout.minHeight = 30;

            var compScrollObj = UIFactory.CreateScrollView(vertGroupObj, out s_compListContent, out SliderScrollbar scroller, new Color(0.07f, 0.07f, 0.07f));
            var contentLayout = compScrollObj.AddComponent<LayoutElement>();
            contentLayout.minHeight = 50;

            s_compListPageHandler = new PageHandler(scroller);
            s_compListPageHandler.ConstructUI(vertGroupObj);
            s_compListPageHandler.OnPageChanged += OnCompListPageTurn;
        }

        internal void AddCompListButton()
        {
            int thisIndex = s_compListTexts.Count;

            GameObject btnGroupObj = UIFactory.CreateHorizontalGroup(s_compListContent, new Color(0.07f, 0.07f, 0.07f));
            HorizontalLayoutGroup btnGroup = btnGroupObj.GetComponent<HorizontalLayoutGroup>();
            btnGroup.childForceExpandWidth = true;
            btnGroup.childControlWidth = true;
            btnGroup.childForceExpandHeight = false;
            btnGroup.childControlHeight = true;
            btnGroup.childAlignment = TextAnchor.MiddleLeft;
            LayoutElement btnLayout = btnGroupObj.AddComponent<LayoutElement>();
            btnLayout.minWidth = 25;
            btnLayout.flexibleWidth = 999;
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
            mainBtnLayout.minWidth = 25;
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
            mainText.resizeTextForBestFit = true;
            mainText.resizeTextMaxSize = 14;
            mainText.resizeTextMinSize = 8;

            s_compListTexts.Add(mainText);

            // TODO remove component button
        }


        #endregion
    }
}
