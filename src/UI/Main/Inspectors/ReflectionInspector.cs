using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityExplorer.Helpers;
using UnityEngine;

namespace UnityExplorer.UI.Main.Inspectors
{
    public class ReflectionInspector : InspectorBase
    {
        public override string TabLabel => m_targetTypeShortName;

        private GameObject m_content;
        public override GameObject Content
        {
            get => m_content;
            set => m_content = value;
        }

        private readonly string m_targetTypeShortName;

        public ReflectionInspector(object target) : base(target)
        {
            Type type = ReflectionHelpers.GetActualType(target);

            if (type == null)
            {
                // TODO
                return;
            }

            m_targetTypeShortName = type.Name;
        }



    }
}
