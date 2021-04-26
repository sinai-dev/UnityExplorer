//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using UnityEngine;
//using UnityEngine.UI;

//namespace UnityExplorer.UI.Widgets
//{
//    public class CellViewHolder : ICell
//    {
//        public CellViewHolder(GameObject uiRoot)
//        {
//            this.UIRoot = uiRoot;
//            this.Rect = uiRoot.GetComponent<RectTransform>();
//            m_enabled = uiRoot.activeSelf;
//        }

//        public bool Enabled => m_enabled;
//        private bool m_enabled;

//        public GameObject UIRoot { get; }
//        public RectTransform Rect { get; }

//        private GameObject m_content;

//        public GameObject SetContent(GameObject newContent)
//        {
//            var ret = m_content;

//            if (ret && newContent && ret.ReferenceEqual(newContent))
//                return null;

//            newContent.transform.SetParent(this.UIRoot.transform, false);
//            (this as ICell).Enable();

//            m_content = newContent;
//            return ret;
//        }

//        public GameObject DisableContent()
//        {
//            var ret = m_content;
//            (this as ICell).Disable();
//            return ret;
//        }

//        void ICell.Enable()
//        {
//            m_enabled = true;
//            UIRoot.SetActive(true);
//        }

//        void ICell.Disable()
//        {
//            m_enabled = false;
//            UIRoot.SetActive(false);
//        }

//        public static RectTransform CreatePrototypeCell(GameObject parent)
//        {
//            // using an image on the cell view holder is fine, we only need to make about 20-50 of these per pool.
//            var prototype = UIFactory.CreateVerticalGroup(parent, "PrototypeCell", true, true, true, true, 0, new Vector4(0, 0, 0, 0),
//                new Color(0.11f, 0.11f, 0.11f), TextAnchor.MiddleCenter);

//            var rect = prototype.GetComponent<RectTransform>();
//            rect.anchorMin = new Vector2(0, 1);
//            rect.anchorMax = new Vector2(0, 1);
//            rect.pivot = new Vector2(0.5f, 1);
//            rect.sizeDelta = new Vector2(100, 30);

//            prototype.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

//            var sepObj = UIFactory.CreateUIObject("separator", prototype);
//            sepObj.AddComponent<Image>().color = Color.black;
//            UIFactory.SetLayoutElement(sepObj, minHeight: 1, preferredHeight: 1, flexibleHeight: 0);

//            prototype.SetActive(false);

//            return rect;
//        }
//    }
//}
