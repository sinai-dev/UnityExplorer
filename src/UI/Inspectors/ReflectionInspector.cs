using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityExplorer.UI.Inspectors
{
    public class InstanceInspector : ReflectionInspector { }

    public class StaticInspector : ReflectionInspector { }

    public class ReflectionInspector : InspectorBase
    {
        public override GameObject UIRoot => throw new NotImplementedException();

        public override GameObject CreateContent(GameObject content)
        {
            throw new NotImplementedException();
        }

        public override void Update()
        {
            throw new NotImplementedException();
        }

        protected override void OnCloseClicked()
        {
            throw new NotImplementedException();
        }
    }
}
