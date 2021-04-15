using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityExplorer.UI.Models
{
    public abstract class UIBehaviourModel : UIModel
    {
        private static readonly List<UIBehaviourModel> Instances = new List<UIBehaviourModel>();

        public static void UpdateInstances()
        {
            if (!Instances.Any())
                return;

            for (int i = Instances.Count - 1; i >= 0; i--)
            {
                var instance = Instances[i];
                if (!instance.UIRoot)
                    Instances.RemoveAt(i);
                else if (instance.NeedsUpdate && instance.Visible)
                    instance.Update();
            }
        }

        /// <summary>
        /// Default false, if true then Update should be implemented.
        /// </summary>
        public virtual bool NeedsUpdate => false;

        public UIBehaviourModel()
        {
            Instances.Add(this);
        }

        /// <summary>
        /// Default empty method, override and implement if NeedsUpdateTick is true.
        /// </summary>
        public virtual void Update()
        {
        }

        public override void Destroy()
        {
            if (Instances.Contains(this))
                Instances.Remove(this);

            base.Destroy();
        }
    }
}
