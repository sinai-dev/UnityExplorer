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

        public override void OnLevelWasLoaded(int level)
        {
            ExplorerCore.OnSceneChange();
        }

        public override void OnUpdate()
        {
            ExplorerCore.Update();
        }

        public override void OnApplicationQuit()
        {
            DebugConsole.OnQuit();
        }

        //public override void OnGUI()
        //{
        //    ExplorerCore.OnGUI();
        //}
    }
}
#endif