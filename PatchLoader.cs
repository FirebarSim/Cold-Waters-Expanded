using BepInEx;
using HarmonyLib;

namespace Cold_Waters_Expanded
{
	[BepInPlugin( "org.cwe.plugins.load", "Cold Waters Expanded Loader", "1.0.0.2" )]
	class PatchLoader : BaseUnityPlugin
	{
		void Awake() {
			Harmony harmony = new Harmony( "com.cwe.plugins.load" ); // rename "author" and "project"
			harmony.PatchAll();
		}
	}
}
	