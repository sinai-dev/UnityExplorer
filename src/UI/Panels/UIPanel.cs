using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Config;
using UniverseLib.Input;
using UnityExplorer.UI.Widgets;
using UniverseLib.UI.Models;
using UniverseLib.UI;
using UniverseLib;
using System.Collections;

namespace UnityExplorer.UI.Panels
{
    public abstract class UIPanel : UIBehaviourModel
    {
        #region STATIC

        internal static void InvokeOnPanelsReordered() => OnPanelsReordered?.Invoke();

        public static event Action OnPanelsReordered;
        public static event Action OnClickedOutsidePanels;

        internal static readonly List<UIPanel> instances = new();
        internal static readonly Dictionary<int, UIPanel> transformToPanelDict = new();

        public static void UpdateFocus()
        {
            if (PanelDragger.ResizePrompting)
                return;

            // if the user is clicking
            if (DisplayManager.MouseInTargetDisplay 
                && (InputManager.GetMouseButtonDown(0) || InputManager.GetMouseButtonDown(1)))
            {
                int count = UIManager.PanelHolder.transform.childCount;
                var mousePos = DisplayManager.MousePosition;
                bool clickedInAny = false;

                for (int i = count - 1; i >= 0; i--)
                {
                    // make sure this is a real recognized panel
                    var transform = UIManager.PanelHolder.transform.GetChild(i);
                    if (!transformToPanelDict.TryGetValue(transform.GetInstanceID(), out UIPanel panel))
                        continue;

                    // check if our mouse is clicking inside the panel
                    var pos = panel.Rect.InverseTransformPoint(mousePos);
                    if (!panel.Enabled || !panel.Rect.rect.Contains(pos))
                        continue;

                    // if this is not the top panel, reorder and invoke the onchanged event
                    if (transform.GetSiblingIndex() != count - 1)
                    {
                        transform.SetAsLastSibling();
                        OnPanelsReordered?.Invoke();
                    }
                    // panel was found, break
                    clickedInAny = true;
                    break;
                }

                if (!clickedInAny)
                    OnClickedOutsidePanels?.Invoke();
            }
        }

        #endregion

        // INSTANCE

        public UIPanel()
        {
            instances.Add(this);
        }

        public abstract UIManager.Panels PanelType { get; }
        public abstract string Name { get; }
        public abstract int MinWidth { get; }
        public abstract int MinHeight { get; }

        public virtual bool ShowByDefault => false;
        public virtual bool ShouldSaveActiveState => true;
        public virtual bool CanDragAndResize => true;
        public virtual bool NavButtonWanted => true;

        public ButtonRef NavButton { get; internal set; }
        public PanelDragger Dragger { get; internal set; }

        public override GameObject UIRoot => uiRoot;
        protected GameObject uiRoot;
        protected GameObject uiContent;
        public RectTransform Rect { get; private set; }
        public GameObject TitleBar { get; private set; }

        public virtual void OnFinishResize(RectTransform panel)
        {
            SaveInternalData();
        }

        public virtual void OnFinishDrag(RectTransform panel)
        {
            SaveInternalData();
        }

        public override void SetActive(bool active)
        {
            if (this.Enabled == active)
                return;

            base.SetActive(active);

            if (!ApplyingSaveData)
                SaveInternalData();

            if (NavButtonWanted)
            {
                var color = active ? UniversalUI.EnabledButtonColor : UniversalUI.DisabledButtonColor;
                RuntimeHelper.SetColorBlock(NavButton.Component, color, color * 1.2f);
            }

            if (!active)
                this.Dragger.WasDragging = false;
            else
            {
                this.UIRoot.transform.SetAsLastSibling();
                InvokeOnPanelsReordered();
            }
        }

        public override void Destroy()
        {
            instances.Remove(this);
            base.Destroy();
        }

        protected internal abstract void DoSetDefaultPosAndAnchors();

        public void SetTransformDefaults()
        {
            DoSetDefaultPosAndAnchors();
        }

        public void EnsureValidSize()
        {
            if (Rect.rect.width < MinWidth)
                Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, MinWidth);
            if (Rect.rect.height < MinHeight)
                Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, MinHeight);
        }

        public void EnsureValidPosition() => EnsureValidPosition(this.Rect);

        public static void EnsureValidPosition(RectTransform panel)
        {
            var pos = panel.localPosition;

            // Prevent panel going oustide screen bounds
            var halfW = DisplayManager.Width * 0.5f;
            var halfH = DisplayManager.Height * 0.5f;

            pos.x = Math.Max(-halfW - panel.rect.width + 50, Math.Min(pos.x, halfW - 50));
            pos.y = Math.Max(-halfH + 50, Math.Min(pos.y, halfH));

            panel.localPosition = pos;
        }

        // Save Data

        public bool ApplyingSaveData { get; set; }

        public void SaveInternalData()
        {
            if (UIManager.Initializing)
                return;

            SetSaveDataToConfigValue();
        }

        private void SetSaveDataToConfigValue() => ConfigManager.GetPanelSaveData(this.PanelType).Value = this.ToSaveData();

        public virtual string ToSaveData()
        {
            try
            {
                return string.Join("|", new string[] 
                { 
                    $"{ShouldSaveActiveState && Enabled}", 
                    Rect.RectAnchorsToString(), 
                    Rect.RectPositionToString() 
                });
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception generating Panel save data: {ex}");
                return "";
            }
        }

        public virtual void ApplySaveData()
        {
            string data = ConfigManager.GetPanelSaveData(this.PanelType).Value;
            ApplySaveData(data);
        }

        protected virtual void ApplySaveData(string data)
        {
            if (string.IsNullOrEmpty(data))
                return;

            var split = data.Split('|');

            try
            {
                Rect.SetAnchorsFromString(split[1]);
                Rect.SetPositionFromString(split[2]);
                UIManager.SetPanelActive(this.PanelType, bool.Parse(split[0]));
            }
            catch
            {
                ExplorerCore.LogWarning("Invalid or corrupt panel save data! Restoring to default.");
                SetTransformDefaults();
                SetSaveDataToConfigValue();
            }
        }

        // UI Construction

        public abstract void ConstructPanelContent();

        public void ConstructUI()
        {
            //this.Enabled = true;

            if (NavButtonWanted)
            {
                // create navbar button

                NavButton = UIFactory.CreateButton(UIManager.NavbarTabButtonHolder, $"Button_{PanelType}", Name);
                var navBtn = NavButton.Component.gameObject;
                navBtn.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(navBtn, false, true, true, true, 0, 0, 0, 5, 5, TextAnchor.MiddleCenter);
                UIFactory.SetLayoutElement(navBtn, minWidth: 80);

                RuntimeHelper.SetColorBlock(NavButton.Component, UniversalUI.DisabledButtonColor, UniversalUI.DisabledButtonColor * 1.2f);
                NavButton.OnClick += () => { UIManager.TogglePanel(PanelType); };

                var txtObj = navBtn.transform.Find("Text").gameObject;
                txtObj.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            // create core canvas 
            uiRoot = UIFactory.CreatePanel(Name, UIManager.PanelHolder, out uiContent);
            Rect = this.uiRoot.GetComponent<RectTransform>();
            //UIFactory.SetLayoutGroup<VerticalLayoutGroup>(this.uiRoot, false, false, true, true, 0, 2, 2, 2, 2, TextAnchor.UpperLeft);

            int id = this.uiRoot.transform.GetInstanceID();
            transformToPanelDict.Add(id, this);

            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(this.uiContent, false, false, true, true, 2, 2, 2, 2, 2, TextAnchor.UpperLeft);

            // Title bar
            TitleBar = UIFactory.CreateHorizontalGroup(uiContent, "TitleBar", false, true, true, true, 2,
                new Vector4(2, 2, 2, 2), new Color(0.06f, 0.06f, 0.06f));
            UIFactory.SetLayoutElement(TitleBar, minHeight: 25, flexibleHeight: 0);

            // Title text

            var titleTxt = UIFactory.CreateLabel(TitleBar, "TitleBar", Name, TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(titleTxt.gameObject, minWidth: 250, minHeight: 25, flexibleHeight: 0);

            // close button

            var closeHolder = UIFactory.CreateUIObject("CloseHolder", TitleBar);
            UIFactory.SetLayoutElement(closeHolder, minHeight: 25, flexibleHeight: 0, minWidth: 30, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(closeHolder, false, false, true, true, 3, childAlignment: TextAnchor.MiddleRight);
            var closeBtn = UIFactory.CreateButton(closeHolder, "CloseButton", "—");
            UIFactory.SetLayoutElement(closeBtn.Component.gameObject, minHeight: 25, minWidth: 25, flexibleWidth: 0);
            RuntimeHelper.SetColorBlock(closeBtn.Component, new Color(0.33f, 0.32f, 0.31f));

            closeBtn.OnClick += () =>
            {
                UIManager.SetPanelActive(this.PanelType, false);
                SaveInternalData();
            };

            if (!CanDragAndResize)
                TitleBar.SetActive(false);

            // Panel dragger

            Dragger = new PanelDragger(TitleBar.GetComponent<RectTransform>(), Rect, this);
            Dragger.OnFinishResize += OnFinishResize;
            Dragger.OnFinishDrag += OnFinishDrag;

            // content (abstract)

            ConstructPanelContent();

            UIManager.SetPanelActive(this.PanelType, true);
            UIManager.SetPanelActive(this.PanelType, false);
            UIManager.SetPanelActive(this.PanelType, ShowByDefault);

            ApplyingSaveData = true;
            SetTransformDefaults();
            // apply panel save data or revert to default
            try
            {
                ApplySaveData();
            }
            catch (Exception ex)
            {
                ExplorerCore.Log($"Exception loading panel save data: {ex}");
                SetTransformDefaults();
            }

            RuntimeHelper.StartCoroutine(LateSetupCoroutine());

            // simple listener for saving enabled state
            this.OnToggleEnabled += (bool val) =>
            {
                SaveInternalData();
            };

            ApplyingSaveData = false;
        }

        private IEnumerator LateSetupCoroutine()
        {
            yield return null;

            // ensure initialized position is valid
            EnsureValidSize();
            EnsureValidPosition(this.Rect);

            // update dragger and save data
            Dragger.OnEndResize();
        }

        public override void ConstructUI(GameObject parent) => ConstructUI();
    }

    #region WINDOW ANCHORS / POSITION HELPERS

    public static class RectSaveExtensions
    {
        // Window Anchors helpers

        internal static string RectAnchorsToString(this RectTransform rect)
        {
            if (!rect)
                throw new ArgumentNullException("rect");

            return string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", new object[]
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

            if (stringAnchors.Contains(" "))
                // outdated save data, not worth recovering just reset it.
                throw new Exception("invalid save data, resetting.");

            var split = stringAnchors.Split(',');

            if (split.Length != 4)
                throw new Exception($"stringAnchors split is unexpected length: {split.Length}");

            Vector4 anchors;
            anchors.x = float.Parse(split[0], CultureInfo.InvariantCulture);
            anchors.y = float.Parse(split[1], CultureInfo.InvariantCulture);
            anchors.z = float.Parse(split[2], CultureInfo.InvariantCulture);
            anchors.w = float.Parse(split[3], CultureInfo.InvariantCulture);

            panel.anchorMin = new Vector2(anchors.x, anchors.y);
            panel.anchorMax = new Vector2(anchors.z, anchors.w);
        }

        internal static string RectPositionToString(this RectTransform rect)
        {
            if (!rect)
                throw new ArgumentNullException("rect");

            return string.Format(CultureInfo.InvariantCulture, "{0},{1}", new object[]
            {
                rect.anchoredPosition.x, rect.anchoredPosition.y
            });
        }

        internal static void SetPositionFromString(this RectTransform rect, string stringPosition)
        {
            if (string.IsNullOrEmpty(stringPosition))
                throw new ArgumentNullException(stringPosition);

            if (stringPosition.Contains(" "))
                // outdated save data, not worth recovering just reset it.
                throw new Exception("invalid save data, resetting.");

            var split = stringPosition.Split(',');

            if (split.Length != 2)
                throw new Exception($"stringPosition split is unexpected length: {split.Length}");

            Vector3 vector = rect.anchoredPosition;
            vector.x = float.Parse(split[0], CultureInfo.InvariantCulture);
            vector.y = float.Parse(split[1], CultureInfo.InvariantCulture);
            rect.anchoredPosition = vector;
        }
    }

    #endregion
}
