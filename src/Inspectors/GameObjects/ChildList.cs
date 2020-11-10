using System;
using System.Collections.Generic;
using System.Linq;
using UnityExplorer.Helpers;
using UnityExplorer.UI;
using UnityExplorer.UI.Shared;
//using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Input;

namespace UnityExplorer.Inspectors.GameObjects
{
    public class ChildList
    {
        internal static ChildList Instance;

        public ChildList()
        {
            Instance = this;
        }

        public static PageHandler s_childListPageHandler;
        private static GameObject[] s_allChildren = new GameObject[0];
        private static readonly List<GameObject> s_childrenShortlist = new List<GameObject>();
        private static GameObject s_childListContent;
        private static readonly List<Text> s_childListTexts = new List<Text>();
        private static int s_lastChildCount;

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

        #region UI CONSTRUCTION

        internal void ConstructChildList(GameObject parent)
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
            mainBtnLayout.minWidth = 25;
            mainBtnLayout.flexibleWidth = 999;
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
            mainText.resizeTextForBestFit = true;
            mainText.resizeTextMaxSize = 14;
            mainText.resizeTextMinSize = 10;
            s_childListTexts.Add(mainText);
        }

        #endregion
    }
}
