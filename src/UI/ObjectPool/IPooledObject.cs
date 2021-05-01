using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityExplorer.UI.ObjectPool
{
    public interface IPooledObject
    {
        GameObject UIRoot { get; set; }

        GameObject CreateContent(GameObject parent);

        float DefaultHeight { get; }

        //GameObject CreatePrototype();
    }
}