using System;
using System.Collections.Generic;
using System.Linq;
using UnityExplorer.Core.Unity;
using UnityExplorer.UI;
using UnityExplorer.UI.Reusable;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Input;
using UnityExplorer.Core.Inspectors;

namespace UnityExplorer.UI.Main.Home.Inspectors
{
    public class ChildList
    {
        internal static ChildList Instance;

        public ChildList()
        {
            Instance = this;
        }

        public static PageHandler s_childListPageHandler;
        private static GameObject s_childListContent;

        private static GameObject[] s_allChildren = new GameObject[0];
        private static readonly List<GameObject> s_childrenShortlist = new List<GameObject>();
        private static int s_lastChildCount;

        private static readonly List<Text> s_childListTexts = new List<Text>();
        private static readonly List<Toggle> s_childListToggles = new List<Toggle>();

        internal void RefreshChildObjectList()
        {
            var go = GameObjectInspector.ActiveInstance.TargetGO;

            s_allChildren = new GameObject[go.transform.childCount];
            for (int i = 0; i < go.transform.childCount; i++)
            {
                var child = go.transform.GetChild(i);
                s_allChildren[i] = child.gameObject;
            }

            var objects = s_allChildren;
            s_childListPageHandler.ListCount = objects.Length;

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

                    var tog = s_childListToggles[i];
                    tog.isOn = obj.activeSelf;

                    var label = text.transform.parent.parent.gameObject;
                    if (!label.activeSelf)
                    {
                        label.SetActive(true);
                    }
                }
            }

            s_lastChildCount = newCount;
        }

        internal static void OnChildListObjectClicked(int index)
        {
            if (GameObjectInspector.ActiveInstance == null)
                return;

            if (index >= s_childrenShortlist.Count || !s_childrenShortlist[index])
                return;

            GameObjectInspector.ActiveInstance.ChangeInspectorTarget(s_childrenShortlist[index]);
            GameObjectInspector.ActiveInstance.Update();
        }

        internal static void OnChildListPageTurn()
        {
            if (Instance == null)
                return;

            Instance.RefreshChildObjectList();
        }

        internal static void OnToggleClicked(int index, bool newVal)
        {
            if (GameObjectInspector.ActiveInstance == null)
                return;

            if (index >= s_childrenShortlist.Count || !s_childrenShortlist[index])
                return;

            var obj = s_childrenShortlist[index];
            obj.SetActive(newVal);
        }

        #region UI CONSTRUCTION

        internal void ConstructChildList(GameObject parent)
        {
            var vertGroupObj = UIFactory.CreateVerticalGroup(parent, new Color(1, 1, 1, 0));
            var vertGroup = vertGroupObj.GetComponent<VerticalLayoutGroup>();
            vertGroup.childForceExpandHeight = true;
            vertGroup.childForceExpandWidth = false;
            vertGroup.SetChildControlWidth(true);
            vertGroup.spacing = 5;
            var vertLayout = vertGroupObj.AddComponent<LayoutElement>();
            vertLayout.minWidth = 120;
            vertLayout.flexibleWidth = 25000;
            vertLayout.minHeight = 200;
            vertLayout.flexibleHeight = 5000;

            var childTitleObj = UIFactory.CreateLabel(vertGroupObj, TextAnchor.MiddleLeft);
            var childTitleText = childTitleObj.GetComponent<Text>();
            childTitleText.text = "Children";
            childTitleText.color = Color.grey;
            childTitleText.fontSize = 14;
            var childTitleLayout = childTitleObj.AddComponent<LayoutElement>();
            childTitleLayout.minHeight = 30;

            var childrenScrollObj = UIFactory.CreateScrollView(vertGroupObj, out s_childListContent, out SliderScrollbar scroller, new Color(0.07f, 0.07f, 0.07f));
            var contentLayout = childrenScrollObj.GetComponent<LayoutElement>();
            contentLayout.minHeight = 50;

            s_childListPageHandler = new PageHandler(scroller);
            s_childListPageHandler.ConstructUI(vertGroupObj);
            s_childListPageHandler.OnPageChanged += OnChildListPageTurn;
        }

        internal void AddChildListButton()
        {
            int thisIndex = s_childListTexts.Count;

            GameObject btnGroupObj = UIFactory.CreateHorizontalGroup(s_childListContent, new Color(0.07f, 0.07f, 0.07f));
            HorizontalLayoutGroup btnGroup = btnGroupObj.GetComponent<HorizontalLayoutGroup>();
            btnGroup.childForceExpandWidth = true;
            btnGroup.SetChildControlWidth(true);
            btnGroup.childForceExpandHeight = false;
            btnGroup.SetChildControlHeight(true);
            LayoutElement btnLayout = btnGroupObj.AddComponent<LayoutElement>();
            btnLayout.flexibleWidth = 320;
            btnLayout.minHeight = 25;
            btnLayout.flexibleHeight = 0;
            btnGroupObj.AddComponent<Mask>();

            var toggleObj = UIFactory.CreateToggle(btnGroupObj, out Toggle toggle, out Text toggleText, new Color(0.3f, 0.3f, 0.3f));
            var toggleLayout = toggleObj.AddComponent<LayoutElement>();
            toggleLayout.minHeight = 25;
            toggleLayout.minWidth = 25;
            toggleText.text = "";
            toggle.isOn = false;
            s_childListToggles.Add(toggle);
            toggle.onValueChanged.AddListener((bool val) => { OnToggleClicked(thisIndex, val); });

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
            mainBtn.onClick.AddListener(() => { OnChildListObjectClicked(thisIndex); });

            Text mainText = mainButtonObj.GetComponentInChildren<Text>();
            mainText.alignment = TextAnchor.MiddleLeft;
            mainText.horizontalOverflow = HorizontalWrapMode.Overflow;
            mainText.resizeTextForBestFit = true;
            mainText.resizeTextMaxSize = 14;
            mainText.resizeTextMinSize = 10;
            s_childListTexts.Add(mainText);
        }

        #endregion
    }
}
