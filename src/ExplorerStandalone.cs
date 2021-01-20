#if STANDALONE
using HarmonyLib;

namespace UnityExplorer
{
	public class ExplorerStandalone
	{
		public static readonly Harmony HarmonyInstance = new Harmony(ExplorerCore.GUID);
	}
}
#endif