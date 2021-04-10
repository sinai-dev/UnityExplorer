using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core;
using UnityExplorer.Core.Runtime;
using UnityExplorer.UI.Utility;

namespace UnityExplorer.UI.Inspectors.GameObjects
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

                    text.text = SignatureHighlighter.ParseFullSyntax(ReflectionUtility.GetActualType(comp), true);

                    var toggle = s_compToggles[i];
                    if (comp.TryCast<Behaviour>() is Behaviour behaviour)
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
            comp.TryCast<Behaviour>().enabled = value;
        }

        internal static void OnCompListObjectClicked(int index)
        {
            if (index >= s_compShortlist.Count || !s_compShortlist[index])
                return;

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
            var vertGroupObj = UIFactory.CreateVerticalGroup(parent, "ComponentList", false, true, true, true, 5, default, new Color(1, 1, 1, 0));
            UIFactory.SetLayoutElement(vertGroupObj, minWidth: 120, flexibleWidth: 25000, minHeight: 200, flexibleHeight: 5000);

            var compTitle = UIFactory.CreateLabel(vertGroupObj, "ComponentsTitle", "Components:", TextAnchor.MiddleLeft, Color.grey);
            UIFactory.SetLayoutElement(compTitle.gameObject, minHeight: 30);

            var compScrollObj = UIFactory.CreateScrollView(vertGroupObj, "ComponentListScrollView", out s_compListContent,
                out SliderScrollbar scroller, new Color(0.07f, 0.07f, 0.07f));

            UIFactory.SetLayoutElement(compScrollObj, minHeight: 50, flexibleHeight: 5000);

            s_compListPageHandler = new PageHandler(scroller);
            s_compListPageHandler.ConstructUI(vertGroupObj);
            s_compListPageHandler.OnPageChanged += OnCompListPageTurn;
        }

        internal void AddCompListButton()
        {
            int thisIndex = s_compListTexts.Count;

            GameObject groupObj = UIFactory.CreateHorizontalGroup(s_compListContent, "CompListButton", true, false, true, true, 0, default,
                new Color(0.07f, 0.07f, 0.07f), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(groupObj, minWidth: 25, flexibleWidth: 999, minHeight: 25, flexibleHeight: 0);
            groupObj.AddComponent<Mask>();

            // Behaviour enabled toggle

            var toggleObj = UIFactory.CreateToggle(groupObj, "EnabledToggle", out Toggle toggle, out Text toggleText, new Color(0.3f, 0.3f, 0.3f));
            UIFactory.SetLayoutElement(toggleObj, minWidth: 25, minHeight: 25);
            toggleText.text = "";
            toggle.isOn = true;
            s_compToggles.Add(toggle);
            toggle.onValueChanged.AddListener((bool val) => { OnCompToggleClicked(thisIndex, val); });

            // Main component button

            var mainBtn = UIFactory.CreateButton(groupObj, 
                "MainButton", 
                "",
                () => { OnCompListObjectClicked(thisIndex); });

            RuntimeProvider.Instance.SetColorBlock(mainBtn, new Color(0.07f, 0.07f, 0.07f),
                new Color(0.2f, 0.2f, 0.2f, 1), new Color(0.05f, 0.05f, 0.05f));

            UIFactory.SetLayoutElement(mainBtn.gameObject, minHeight: 25, flexibleHeight: 0, minWidth: 25, flexibleWidth: 999);

            // Component button text

            Text mainText = mainBtn.GetComponentInChildren<Text>();
            mainText.alignment = TextAnchor.MiddleLeft;
            mainText.horizontalOverflow = HorizontalWrapMode.Overflow;
            mainText.resizeTextForBestFit = true;
            mainText.resizeTextMaxSize = 14;
            mainText.resizeTextMinSize = 8;

            s_compListTexts.Add(mainText);
        }

        #endregion
    }
}
