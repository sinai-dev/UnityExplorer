using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Config;
using UnityExplorer.Core.Input;
using UnityExplorer.UI.Utility;

namespace UnityExplorer.UI.Models
{
    public abstract class UIPanel : UIBehaviourModel
    {
        public static event Action OnPanelsReordered;

        public static void UpdateFocus()
        {
            if (InputManager.GetMouseButtonDown(0) || InputManager.GetMouseButtonDown(1))
            {
                int count = UIManager.CanvasRoot.transform.childCount;
                var mousePos = InputManager.MousePosition;
                for (int i = count - 1; i >= 0; i--)
                {
                    var transform = UIManager.CanvasRoot.transform.GetChild(i);
                    if (transformToPanelDict.TryGetValue(transform.GetInstanceID(), out UIPanel panel))
                    {
                        var pos = panel.mainPanelRect.InverseTransformPoint(mousePos);
                        if (panel.Enabled && panel.mainPanelRect.rect.Contains(pos))
                        {
                            if (transform.GetSiblingIndex() != count - 1)
                            {
                                transform.SetAsLastSibling();
                                OnPanelsReordered?.Invoke();
                            }
                            break;
                        }
                    }
                }
            }
        }

        private static readonly List<UIPanel> instances = new List<UIPanel>();
        private static readonly Dictionary<int, UIPanel> transformToPanelDict = new Dictionary<int, UIPanel>();

        public UIPanel()
        {
            instances.Add(this);
        }

        public override void Destroy()
        {
            instances.Remove(this);
            base.Destroy();
        }

        public override GameObject UIRoot => uiRoot;
        protected GameObject uiRoot;
        protected RectTransform mainPanelRect;
        public GameObject content;
        public PanelDragger dragger;

        public abstract string Name { get; }

        public override void ConstructUI(GameObject parent)
        {
            // create core canvas 
            uiRoot = UIFactory.CreatePanel(Name, out GameObject panelContent);
            mainPanelRect = this.uiRoot.GetComponent<RectTransform>();
            content = panelContent;

            transformToPanelDict.Add(this.uiRoot.transform.GetInstanceID(), this);

            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(this.uiRoot, true, true, true, true, 0, 0, 0, 0, 0, TextAnchor.UpperLeft);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(content, true, true, true, true, 2, 2, 2, 2, 2, TextAnchor.UpperLeft);

            // always apply default pos and anchors (save data may only be partial)
            SetDefaultPosAndAnchors();

            // Title bar

            var titleBar = UIFactory.CreateLabel(content, "TitleBar", Name, TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(titleBar.gameObject, minHeight: 25, flexibleHeight: 0);

            dragger = new PanelDragger(titleBar.GetComponent<RectTransform>(), mainPanelRect);
            dragger.OnFinishResize += OnFinishResize;
            dragger.OnFinishDrag += OnFinishDrag;

            // content (abstract)

            ConstructPanelContent();

            // apply panel save data or revert to default
            try
            {
                LoadSaveData();
                dragger.OnEndResize();
            }
            catch
            {
                SetDefaultPosAndAnchors();
            }

            // simple listener for saving enabled state
            this.OnToggleEnabled += (bool val) =>
            {
                SaveToConfigManager();
            };
        }

        public abstract void ConstructPanelContent();

        public virtual void OnFinishResize(RectTransform panel)
        {
            SaveToConfigManager();
        }

        public virtual void OnFinishDrag(RectTransform panel)
        {
            SaveToConfigManager();
        }

        public abstract void SaveToConfigManager();

        public abstract void SetDefaultPosAndAnchors();

        public abstract void LoadSaveData();

        public string ToSaveData()
        {
            try
            {
                return $"{Enabled}" +
                $"|{mainPanelRect.RectAnchorsToString()}" +
                $"|{mainPanelRect.RectPositionToString()}";
            }
            catch
            {
                return "";
            }
        }

        public void ApplySaveData(string data)
        {
            if (string.IsNullOrEmpty(data))
                return;

            var split = data.Split('|');

            try
            {
                uiRoot.SetActive(bool.Parse(split[0]));
                mainPanelRect.SetAnchorsFromString(split[1]);
                mainPanelRect.SetPositionFromString(split[2]);
            }
            catch { }
        }
    }

    public static class RectSaveExtensions
    {
        #region WINDOW ANCHORS / POSITION HELPERS

        // Window Anchors helpers

        //private const string DEFAULT_WINDOW_ANCHORS = "0.25,0.10,0.78,0.95";
        //private const string DEFAULT_WINDOW_POSITION = "0,0";

        internal static CultureInfo _enCulture = new CultureInfo("en-US");

        internal static string RectAnchorsToString(this RectTransform rect)
        {
            if (!rect)
                throw new ArgumentNullException("rect");

            return string.Format(_enCulture, "{0},{1},{2},{3}", new object[]
            {
                rect.anchorMin.x,
                rect.anchorMin.y,
                rect.anchorMax.x,
                rect.anchorMax.y
            });
        }

        internal static void SetAnchorsFromString(this RectTransform panel, string stringAnchors)
        {
            if (string.IsNullOrEmpty(stringAnchors))
                throw new ArgumentNullException("stringAnchors");

            var split = stringAnchors.Split(',');

            if (split.Length != 4)
                throw new Exception($"stringAnchors split is unexpected length: {split.Length}");

            Vector4 anchors;
            anchors.x = float.Parse(split[0], _enCulture);
            anchors.y = float.Parse(split[1], _enCulture);
            anchors.z = float.Parse(split[2], _enCulture);
            anchors.w = float.Parse(split[3], _enCulture);

            panel.anchorMin = new Vector2(anchors.x, anchors.y);
            panel.anchorMax = new Vector2(anchors.z, anchors.w);
        }

        internal static string RectPositionToString(this RectTransform rect)
        {
            if (!rect)
                throw new ArgumentNullException("rect");

            return string.Format(_enCulture, "{0},{1}", new object[]
            {
                rect.localPosition.x, rect.localPosition.y
            });
        }

        internal static void SetPositionFromString(this RectTransform rect, string stringPosition)
        {
            var split = stringPosition.Split(',');

            if (split.Length != 2)
                throw new Exception($"stringPosition split is unexpected length: {split.Length}");

            Vector3 vector = rect.localPosition;
            vector.x = float.Parse(split[0], _enCulture);
            vector.y = float.Parse(split[1], _enCulture);
            rect.localPosition = vector;
        }

        #endregion
    }
}
