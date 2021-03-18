using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace UnityExplorer.UI.Main.Home.Inspectors
{
    public class MouseInspectorUI
    {
        internal Text s_objNameLabel;
        internal Text s_objPathLabel;
        internal Text s_mousePosLabel;
        internal GameObject s_UIContent;

        public MouseInspectorUI()
        {
            ConstructUI();
        }

        #region UI Construction

        internal void ConstructUI()
        {
            s_UIContent = UIFactory.CreatePanel(UIManager.CanvasRoot, "MouseInspect", out GameObject content);

            s_UIContent.AddComponent<Mask>();

            var baseRect = s_UIContent.GetComponent<RectTransform>();
            var half = new Vector2(0.5f, 0.5f);
            baseRect.anchorMin = half;
            baseRect.anchorMax = half;
            baseRect.pivot = half;
            baseRect.sizeDelta = new Vector2(700, 150);

            var group = content.GetComponent<VerticalLayoutGroup>();
            group.childForceExpandHeight = true;

            // Title text

            var titleObj = UIFactory.CreateLabel(content, TextAnchor.MiddleCenter);
            var titleText = titleObj.GetComponent<Text>();
            titleText.text = "<b>Mouse Inspector</b> (press <b>ESC</b> to cancel)";

            var mousePosObj = UIFactory.CreateLabel(content, TextAnchor.MiddleCenter);
            s_mousePosLabel = mousePosObj.GetComponent<Text>();
            s_mousePosLabel.text = "Mouse Position:";

            var hitLabelObj = UIFactory.CreateLabel(content, TextAnchor.MiddleLeft);
            s_objNameLabel = hitLabelObj.GetComponent<Text>();
            s_objNameLabel.text = "No hits...";
            s_objNameLabel.horizontalOverflow = HorizontalWrapMode.Overflow;

            var pathLabelObj = UIFactory.CreateLabel(content, TextAnchor.MiddleLeft);
            s_objPathLabel = pathLabelObj.GetComponent<Text>();
            s_objPathLabel.fontStyle = FontStyle.Italic;
            s_objPathLabel.horizontalOverflow = HorizontalWrapMode.Wrap;

            var pathLayout = pathLabelObj.AddComponent<LayoutElement>();
            pathLayout.minHeight = 75;
            pathLayout.flexibleHeight = 0;

            s_UIContent.SetActive(false);
        }

        #endregion
    }
}
