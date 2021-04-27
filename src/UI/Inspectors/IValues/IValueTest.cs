using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.ObjectPool;

namespace UnityExplorer.UI.Inspectors.IValues
{
    public class IValueTest : IPooledObject
    {
        public GameObject UIRoot => uiRoot;
        private GameObject uiRoot;

        public float DefaultHeight => -1f;

        public GameObject CreateContent(GameObject parent)
        {
            uiRoot = UIFactory.CreateUIObject(this.GetType().Name, parent);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(uiRoot, true, true, true, true, 3, childAlignment: TextAnchor.MiddleLeft);



            return uiRoot;
        }
    }
}
