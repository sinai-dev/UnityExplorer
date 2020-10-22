#if ML
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MelonLoader;

namespace ExplorerBeta
{
    public class ExplorerMelonMod : MelonMod
    {
        public static ExplorerMelonMod Instance;

        public override void OnApplicationStart()
        {
            Instance = this;

            new ExplorerCore();
        }

        public override void OnLevelWasLoaded(int level)
        {
            ExplorerCore.OnSceneChange();
        }

        public override void OnUpdate()
        {
            ExplorerCore.Update();
        }

        //public override void OnGUI()
        //{
        //    ExplorerCore.OnGUI();
        //}
    }
}
#endif