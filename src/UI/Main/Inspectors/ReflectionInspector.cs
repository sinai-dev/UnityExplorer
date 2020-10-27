using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExplorerBeta.Helpers;

namespace ExplorerBeta.UI.Main.Inspectors
{
    public class ReflectionInspector : InspectorBase
    {
        public override string TabLabel => m_targetTypeShortName;

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
