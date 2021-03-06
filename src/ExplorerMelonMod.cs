#if ML
using System;
using MelonLoader;

namespace UnityExplorer
{
    public class ExplorerMelonMod : MelonMod
    {
        public static ExplorerMelonMod Instance;

        public override void OnApplicationStart()
        {
            Instance = this;

            new ExplorerCore();
        }

        public override void OnUpdate()
        {
            ExplorerCore.Update();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            ExplorerCore.Instance.OnSceneLoaded();
        }
    }
}
#endif