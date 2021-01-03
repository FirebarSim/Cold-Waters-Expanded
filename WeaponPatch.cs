using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace Cold_Waters_Expanded
{
	[BepInPlugin( "org.cwe.plugins.weapon", "Cold Waters Expanded Weapon Patches", "1.0.1.2" )]
	class WeaponPatch : BaseUnityPlugin
	{
		public static WeaponPatch weaponPatch;
		public Dictionary<DatabaseWeaponData, DatabaseWeaponDataExtension> weaponDataExtensions = new Dictionary<DatabaseWeaponData, DatabaseWeaponDataExtension>();
		public Dictionary<Torpedo, BallisticTrajectory> weaponTrajectories = new Dictionary<Torpedo, BallisticTrajectory>();

		void Awake() {
			weaponPatch = this;
		}

		[HarmonyPatch( typeof( TextParser ), "ReadWeaponData" )]
		public class TextParser_ReadWeaponData_Patch
		{
			[HarmonyPostfix]
			public static void Postfix( TextParser __instance ) {
				int countWeapons = 0;
				int countCountermeasures = 0;
				int countDepthWeapons = 0;
				string[] lines = __instance.OpenTextDataFile( "weapons" );
				DatabaseWeaponDataExtension weaponDataExtension = null;
				foreach( string line in lines ) {
					switch( line.Split( '=' )[0] ) {
						case "WeaponObjectReference":
							weaponDataExtension = new DatabaseWeaponDataExtension();
							weaponPatch.weaponDataExtensions.Add( __instance.database.databaseweapondata[countWeapons], weaponDataExtension );
							countWeapons++;
							break;
						case "DepthWeaponObjectReference":
							countDepthWeapons++;
							break;
						case "CountermeasureName":
							countCountermeasures++;
							break;
						case "FlightProfile":
							switch( line.Split( '=' )[1].Trim() ) {
								case "BALLISTIC":
									weaponDataExtension.isBallistic = true;
									//Debug.Log( "BALLISTIC" );
									break;
								default:
									weaponDataExtension.isBallistic = false;
									break;
							}
							break;
						case "PeakAltitude":
							weaponDataExtension.ballisticCeiling = float.Parse( line.Split( '=' )[1].Trim() ) / 75.13f;
							break;
						default:
							break;
					}
				}
			}
		}
	}
}
