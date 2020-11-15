using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Cold_Waters_Expanded
{
	[BepInPlugin( "org.cwe.plugins.load", "Cold Waters Expanded Loader", "1.0.0.3" )]
	class PatchLoader : BaseUnityPlugin
	{
		void Awake() {
			Harmony harmony = new Harmony( "com.cwe.plugins.load" ); // rename "author" and "project"
			harmony.PatchAll();
		}
	}
}
	