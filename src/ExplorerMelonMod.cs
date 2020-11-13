#if ML
using MelonLoader;
using UnityExplorer.UI.Modules;

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

        public override void OnLevelWasLoaded(int level)
        {
            ExplorerCore.Instance.OnSceneLoaded();
        }
    }
}
#endif