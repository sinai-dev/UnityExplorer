using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Config;
using UniverseLib;
using UniverseLib.Input;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Panels;

namespace UnityExplorer.UI.Panels
{
    public abstract class UEPanel : PanelBase
    {
        protected UEPanel(UIBase owner) : base(owner) { }

        public abstract UIManager.Panels PanelType { get; }
        public virtual bool ShowByDefault => false;
        public virtual bool ShouldSaveActiveState => true;

        public virtual bool NavButtonWanted => true;
        public ButtonRef NavButton { get; internal set; }

        protected override PanelDragger CreatePanelDragger()
        {
            return new UEPanelDragger(this);
        }

        public override void OnFinishDrag()
        {
            base.OnFinishDrag();
            SaveInternalData();
        }

        public override void OnFinishResize()
        {
            base.OnFinishResize();
            SaveInternalData();
        }

        public override void SetActive(bool active)
        {
            if (this.Enabled != active)
            {
                base.SetActive(active);

                if (!ApplyingSaveData)
                    SaveInternalData();

                if (NavButtonWanted && NavButton != null)
                {
                    Color color = active ? UniversalUI.EnabledButtonColor : UniversalUI.DisabledButtonColor;
                    RuntimeHelper.SetColorBlock(NavButton.Component, color, color * 1.2f);
                }
            }

            if (!active)
            {
                if (Dragger != null)
                    this.Dragger.WasDragging = false;
            }
            else
            {
                this.UIRoot.transform.SetAsLastSibling();
                (this.Owner.Panels as UEPanelManager).DoInvokeOnPanelsReordered();
            }
        }

        // Save Data

        public bool ApplyingSaveData { get; set; }

        public void SaveInternalData()
        {
            if (UIManager.Initializing || ApplyingSaveData)
                return;

            SetSaveDataToConfigValue();
        }

        private void SetSaveDataToConfigValue() 
            => ConfigManager.GetPanelSaveData(this.PanelType).Value = this.ToSaveData();

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

            string[] split = data.Split('|');

            try
            {
                Rect.SetAnchorsFromString(split[1]);
                Rect.SetPositionFromString(split[2]);
                this.SetActive(bool.Parse(split[0]));
            }
            catch
            {
                ExplorerCore.LogWarning("Invalid or corrupt panel save data! Restoring to default.");
                SetDefaultSizeAndPosition();
                SetSaveDataToConfigValue();
            }
        }

        public override void ConstructUI()
        {
            base.ConstructUI();

            if (NavButtonWanted)
            {
                // create navbar button

                NavButton = UIFactory.CreateButton(UIManager.NavbarTabButtonHolder, $"Button_{PanelType}", Name);
                GameObject navBtn = NavButton.Component.gameObject;
                navBtn.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(navBtn, false, true, true, true, 0, 0, 0, 5, 5, TextAnchor.MiddleCenter);
                UIFactory.SetLayoutElement(navBtn, minWidth: 80);

                RuntimeHelper.SetColorBlock(NavButton.Component, UniversalUI.DisabledButtonColor, UniversalUI.DisabledButtonColor * 1.2f);
                NavButton.OnClick += () => { UIManager.TogglePanel(PanelType); };

                GameObject txtObj = navBtn.transform.Find("Text").gameObject;
                txtObj.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            this.SetActive(true);
            this.SetActive(false);
            this.SetActive(ShowByDefault);
        }

        protected override void LateConstructUI()
        {
            ApplyingSaveData = true;

            base.LateConstructUI();

            // apply panel save data or revert to default
            try
            {
                ApplySaveData();
            }
            catch (Exception ex)
            {
                ExplorerCore.Log($"Exception loading panel save data: {ex}");
                SetDefaultSizeAndPosition();
            }

            // simple listener for saving enabled state
            this.OnToggleEnabled += (bool val) =>
            {
                SaveInternalData();
            };

            ApplyingSaveData = false;

            Dragger.OnEndResize();
        }
    }

    #region WINDOW ANCHORS / POSITION SAVE DATA HELPERS

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

            string[] split = stringAnchors.Split(',');

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

            string[] split = stringPosition.Split(',');

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
