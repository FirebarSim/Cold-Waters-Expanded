using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Cold_Waters_Expanded
{
	[BepInPlugin( "org.cwe.plugins.diag", "Cold Waters Expanded Diagnostics", "1.0.1.2" )]
	class Diagnostics : BaseUnityPlugin
	{
		[HarmonyPatch( typeof( TextParser ), "ReadShipData" )]
		public class TextParser_ReadShipData_Patch
		{
			//[HarmonyPrefix]
			//public static bool Prefix( TextParser __instance ) {
			//	Debug.Log( "ReadShipData" );
			//	string filePathFromString = __instance.GetFilePathFromString( "vessels/_vessel_list" );
			//	string[] array = __instance.OpenTextDataFile( filePathFromString );
			//	Debug.Log( "\tLoaded Vessel List" );
			//	__instance.database.databaseshipdata = new DatabaseShipData[array.Length];
			//	Vector2 vector = Vector2.zero;
			//	bool flag = false;
			//	string[] array2 = new string[0];
			//	int num = -1;
			//	Debug.Log( 28 );
			//	for( int i = 0; i < array.Length; i++ ) {
			//		filePathFromString = __instance.GetFilePathFromString( "vessels/" + array[i].Trim() );
			//		string[] array3 = __instance.OpenTextDataFile( filePathFromString );
			//		flag = false;
			//		for( int j = 0; j < array3.Length; j++ ) {
			//			string[] array4 = array3[j].Split( '=' );
			//			switch( array4[0] ) {
			//				case "Designation":
			//					num++;
			//					__instance.database.databaseshipdata[i] = ScriptableObject.CreateInstance<DatabaseShipData>();
			//					__instance.database.databaseshipdata[i].shipID = num;
			//					__instance.database.databaseshipdata[i].shipPrefabName = array[i].Trim();
			//					__instance.ReadVesselDescriptionData( __instance.database.databaseshipdata[i] );
			//					__instance.database.databaseshipdata[i].proprotationspeed = new float[0];
			//					__instance.database.databaseshipdata[i].subsystemPrimaryPositions = new string[__instance.database.databasesubsystemsdata.Length];
			//					__instance.database.databaseshipdata[i].subsystemSecondaryPositions = new string[__instance.database.databasesubsystemsdata.Length];
			//					__instance.database.databaseshipdata[i].subsystemTertiaryPositions = new string[__instance.database.databasesubsystemsdata.Length];
			//					__instance.database.databaseshipdata[i].subsystemLabelPositions = new Vector2[__instance.database.databasesubsystemsdata.Length];
			//					__instance.database.databaseshipdata[i].telegraphSpeeds = new float[7];
			//					__instance.database.databaseshipdata[i].shipDesignation = array4[1].Trim();
			//					__instance.database.databaseshipdata[i].aircraftNumbers = new int[0];
			//					__instance.database.databaseshipdata[i].aircraftIDs = new int[0];
			//					break;
			//				case "ShipType":
			//					__instance.database.databaseshipdata[i].shipType = array4[1].Trim();
			//					break;
			//				case "PlayerHUD":
			//					__instance.database.databaseshipdata[i].playerHUD = array4[1].Trim();
			//					break;
			//				case "Length":
			//					if( array4[1].Contains( '|' ) ) {
			//						string[] array8 = array4[1].Trim().Split( '|' );
			//						__instance.database.databaseshipdata[i].length = float.Parse( array8[0] );
			//						__instance.database.databaseshipdata[i].displayLength = float.Parse( array8[1] );
			//					}
			//					else {
			//						__instance.database.databaseshipdata[i].length = float.Parse( array4[1] );
			//					}
			//					__instance.database.databaseshipdata[i].minCameraDistance = __instance.database.databaseshipdata[i].length / 84f;
			//					if( __instance.database.databaseshipdata[i].shipType == "OILRIG" ) {
			//						__instance.database.databaseshipdata[i].minCameraDistance = 65f / 84f;
			//						__instance.database.databaseshipdata[i].minCameraDistance *= 2f;
			//					}
			//					if( __instance.database.databaseshipdata[i].minCameraDistance < 0.7f ) {
			//						__instance.database.databaseshipdata[i].minCameraDistance = 0.7f;
			//					}
			//					break;
			//				case "Beam":
			//					if( array4[1].Contains( '|' ) ) {
			//						string[] array10 = array4[1].Trim().Split( '|' );
			//						__instance.database.databaseshipdata[i].beam = float.Parse( array10[0] );
			//						__instance.database.databaseshipdata[i].displayBeam = float.Parse( array10[1] );
			//					}
			//					else {
			//						__instance.database.databaseshipdata[i].beam = float.Parse( array4[1] );
			//					}
			//					break;
			//				case "HullHeight":
			//					__instance.database.databaseshipdata[i].hullHeight = float.Parse( array4[1] );
			//					break;
			//				case "Displacement":
			//					__instance.database.databaseshipdata[i].displacement = float.Parse( array4[1] );
			//					break;
			//				case "Crew":
			//					__instance.database.databaseshipdata[i].crew = float.Parse( array4[1] );
			//					break;
			//				case "AircraftNumbers":
			//					__instance.database.databaseshipdata[i].aircraftNumbers = Traverse.Create( __instance ).Method( "PopulateIntArray", new object[] { array4[1] } ).GetValue<int[]>();
			//				break;
			//				case "AircraftTypes": {
			//						string[] array9 = __instance.PopulateStringArray( array4[1] );
			//						__instance.database.databaseshipdata[i].aircraftIDs = new int[array9.Length];
			//						for( int num7 = 0; num7 < array9.Length; num7++ ) {
			//							__instance.database.databaseshipdata[i].aircraftIDs[num7] = __instance.GetAircraftID( array9[num7] );
			//						}
			//						break;
			//					}
			//				case "Waterline":
			//					__instance.database.databaseshipdata[i].waterline = 1000f - float.Parse( array4[1] );
			//					break;
			//				case "PeriscopeDepthInFeet":
			//					__instance.database.databaseshipdata[i].periscopeDepthInFeet = int.Parse( array4[1] );
			//					break;
			//				case "HullNumbers":
			//					if( array4[1].Trim() == "FALSE" ) {
			//						__instance.database.databaseshipdata[i].hullnumbers = new string[0];
			//					}
			//					else {
			//						__instance.database.databaseshipdata[i].hullnumbers = __instance.PopulateStringArray( array4[1] );
			//					}
			//					break;
			//				case "AccelerationRate":
			//					__instance.database.databaseshipdata[i].accelerationrate = float.Parse( array4[1] );
			//					break;
			//				case "DecelerationRate":
			//					__instance.database.databaseshipdata[i].decellerationrate = float.Parse( array4[1] );
			//					break;
			//				case "RudderTurnRate":
			//					__instance.database.databaseshipdata[i].rudderturnrate = float.Parse( array4[1] );
			//					break;
			//				case "TurnRate":
			//					__instance.database.databaseshipdata[i].turnrate = float.Parse( array4[1] );
			//					break;
			//				case "PivotPointTurning":
			//					__instance.database.databaseshipdata[i].pivotpointturning = float.Parse( array4[1] );
			//					break;
			//				case "DiveRate":
			//					__instance.database.databaseshipdata[i].diverate = float.Parse( array4[1] );
			//					break;
			//				case "SurfaceRate":
			//					__instance.database.databaseshipdata[i].surfacerate = float.Parse( array4[1] );
			//					break;
			//				case "BallastRate":
			//					__instance.database.databaseshipdata[i].ballastrate = float.Parse( array4[1] );
			//					break;
			//				case "SubmergedAt":
			//					__instance.database.databaseshipdata[i].submergedat = float.Parse( array4[1] );
			//					__instance.database.databaseshipdata[i].submergedat = 1000f - __instance.database.databaseshipdata[i].submergedat;
			//					break;
			//				case "SurfaceSpeed":
			//					__instance.database.databaseshipdata[i].surfacespeed = float.Parse( array4[1] );
			//					break;
			//				case "SubmergedSpeed":
			//					__instance.database.databaseshipdata[i].submergedspeed = float.Parse( array4[1] );
			//					break;
			//				case "TelegraphSpeeds": {
			//						__instance.database.databaseshipdata[i].telegraphSpeeds = Traverse.Create( __instance ).Method( "PopulateFloatArray", new object[] { array4[1] } ).GetValue<float[]>();
			//						for( int num8 = 0; num8 < __instance.database.databaseshipdata[i].telegraphSpeeds.Length; num8++ ) {
			//							__instance.database.databaseshipdata[i].telegraphSpeeds[num8] *= 0.1f;
			//						}
			//						flag = true;
			//						break;
			//					}
			//				case "CavitationParameters":
			//					__instance.database.databaseshipdata[i].cavitationparameters = __instance.PopulateVector2( array4[1] );
			//					break;
			//				case "PropRotationSpeed":
			//					__instance.database.databaseshipdata[i].proprotationspeed = Traverse.Create( __instance ).Method( "PopulateFloatArray", new object[] { array4[1] } ).GetValue<float[]>();
			//					break;
			//				case "TestDepth":
			//					__instance.database.databaseshipdata[i].testDepth = float.Parse( array4[1] );
			//					__instance.database.databaseshipdata[i].actualTestDepth = 1000f - __instance.database.databaseshipdata[i].testDepth / GameDataManager.unitsToFeet;
			//					break;
			//				case "EscapeDepth":
			//					__instance.database.databaseshipdata[i].escapeDepth = float.Parse( array4[1] );
			//					break;
			//				case "SelfNoise":
			//					__instance.database.databaseshipdata[i].selfnoise = float.Parse( array4[1] );
			//					break;
			//				case "ActiveSonarReflection":
			//					__instance.database.databaseshipdata[i].activesonarreflection = float.Parse( array4[1] );
			//					break;
			//				case "ActiveSonarModel":
			//					__instance.database.databaseshipdata[i].activeSonarID = Traverse.Create( __instance ).Method( "GetSonarID", new object[] { array4[1] } ).GetValue<int>();
			//					break;
			//				case "PassiveSonarModel":
			//					__instance.database.databaseshipdata[i].passiveSonarID = Traverse.Create( __instance ).Method( "GetSonarID", new object[] { array4[1] } ).GetValue<int>();
			//					break;
			//				case "TowedArrayModel":
			//					__instance.database.databaseshipdata[i].towedSonarID = Traverse.Create( __instance ).Method( "GetSonarID", new object[] { array4[1] } ).GetValue<int>();
			//					if( __instance.database.databaseshipdata[i].towedSonarID != -1 ) {
			//						__instance.database.databaseshipdata[i].passiveArrayBonus = __instance.database.databasesonardata[__instance.database.databaseshipdata[i].towedSonarID].sonarPassiveSensitivity - __instance.database.databasesonardata[__instance.database.databaseshipdata[i].passiveSonarID].sonarPassiveSensitivity;
			//						__instance.database.databaseshipdata[i].activeArrayBonus = __instance.database.databasesonardata[__instance.database.databaseshipdata[i].towedSonarID].sonarActiveSensitivity - __instance.database.databasesonardata[__instance.database.databaseshipdata[i].activeSonarID].sonarActiveSensitivity;
			//						if( __instance.database.databaseshipdata[i].passiveArrayBonus < 0f ) {
			//							__instance.database.databaseshipdata[i].passiveArrayBonus = 0f;
			//						}
			//						if( __instance.database.databaseshipdata[i].activeArrayBonus < 0f ) {
			//							__instance.database.databaseshipdata[i].activeArrayBonus = 0f;
			//						}
			//					}
			//					break;
			//				case "AnechoicCoating":
			//					__instance.database.databaseshipdata[i].anechoicCoating = Traverse.Create( __instance ).Method( "SetBoolean", new object[] { array4[1] } ).GetValue<bool>();
			//					break;
			//				case "RADAR":
			//					__instance.database.databaseshipdata[i].radarID = Traverse.Create( __instance ).Method( "GetRADARID", new object[] { array4[1] } ).GetValue<int>();
			//					break;
			//				case "RADARSignature":
			//					__instance.database.databaseshipdata[i].radarSignature = array4[1].Trim();
			//					break;
			//				case "MissileTargetPoint":
			//					if( array4[1].Trim() == "FALSE" ) {
			//						__instance.database.databaseshipdata[i].targetPoint.x = 0f;
			//					}
			//					else {
			//						__instance.database.databaseshipdata[i].targetPoint.x = float.Parse( array4[1] );
			//					}
			//					break;
			//				case "TorpedoTargetPoint":
			//					__instance.database.databaseshipdata[i].targetPoint.y = float.Parse( array4[1] );
			//					break;
			//				case "TowedArrayPosition":
			//					__instance.database.databaseshipdata[i].towedArrayPosition = __instance.PopulateVector3( array4[1].Trim() );
			//					break;
			//				case "TorpedoTypes": {
			//						__instance.database.databaseshipdata[i].torpedotypes = __instance.PopulateStringArray( array4[1] );
			//						__instance.database.databaseshipdata[i].torpedoIDs = new int[__instance.database.databaseshipdata[i].torpedotypes.Length];
			//						for( int num4 = 0; num4 < __instance.database.databaseshipdata[i].torpedotypes.Length; num4++ ) {
			//							__instance.database.databaseshipdata[i].torpedoIDs[num4] = Traverse.Create( __instance ).Method( "GetWeaponID", new object[] { __instance.database.databaseshipdata[i].torpedotypes[num4] } ).GetValue<int>();
			//						}
			//						__instance.database.databaseshipdata[i].torpedoGameObjects = new GameObject[__instance.database.databaseshipdata[i].torpedoIDs.Length];
			//						for( int num5 = 0; num5 < __instance.database.databaseshipdata[i].torpedoIDs.Length; num5++ ) {
			//							__instance.database.databaseshipdata[i].torpedoGameObjects[num5] = __instance.database.databaseweapondata[__instance.database.databaseshipdata[i].torpedoIDs[num5]].weaponObject;
			//						}
			//						for( int num6 = 0; num6 < __instance.database.databaseshipdata[i].torpedotypes.Length; num6++ ) {
			//							__instance.database.databaseshipdata[i].torpedotypes[num6] = __instance.database.databaseweapondata[__instance.database.databaseshipdata[i].torpedoIDs[num6]].weaponName;
			//						}
			//						break;
			//					}
			//				case "TorpedoNumbers":
			//					__instance.database.databaseshipdata[i].torpedoNumbers = Traverse.Create(__instance).Method("PopulateIntArray", new object[] { array4[1] } ).GetValue<int[]>();
			//					break;
			//				case "TorpedoTubes":
			//					__instance.database.databaseshipdata[i].torpedotubes = int.Parse( array4[1] );
			//					break;
			//				case "NumberOfWires":
			//					__instance.database.databaseshipdata[i].numberOfWires = int.Parse( array4[1] );
			//					break;
			//				case "TubeConfig":
			//					__instance.database.databaseshipdata[i].torpedoConfig = Traverse.Create(__instance).Method("PopulateIntArray", new object[] { array4[1] } ).GetValue<int[]>();
			//					break;
			//				case "TorpedoTubeSize":
			//					__instance.database.databaseshipdata[i].torpedotubeSize = float.Parse( array4[1] );
			//					break;
			//				case "TubeReloadTime":
			//					__instance.database.databaseshipdata[i].tubereloadtime = float.Parse( array4[1] );
			//					break;
			//				case "VLSTorpedoTypes": {
			//						__instance.database.databaseshipdata[i].vlsTorpedotypes = __instance.PopulateStringArray( array4[1] );
			//						__instance.database.databaseshipdata[i].vlsTorpedoIDs = new int[__instance.database.databaseshipdata[i].vlsTorpedotypes.Length];
			//						for( int n = 0; n < __instance.database.databaseshipdata[i].vlsTorpedotypes.Length; n++ ) {
			//							__instance.database.databaseshipdata[i].vlsTorpedoIDs[n] = Traverse.Create( __instance ).Method( "GetWeaponID", new object[] { __instance.database.databaseshipdata[i].vlsTorpedotypes[n] } ).GetValue<int>();
			//						}
			//						__instance.database.databaseshipdata[i].vlsTorpedoGameObjects = new GameObject[__instance.database.databaseshipdata[i].vlsTorpedoIDs.Length];
			//						for( int num2 = 0; num2 < __instance.database.databaseshipdata[i].vlsTorpedoIDs.Length; num2++ ) {
			//							__instance.database.databaseshipdata[i].vlsTorpedoGameObjects[num2] = __instance.database.databaseweapondata[__instance.database.databaseshipdata[i].vlsTorpedoIDs[num2]].weaponObject;
			//						}
			//						for( int num3 = 0; num3 < __instance.database.databaseshipdata[i].vlsTorpedotypes.Length; num3++ ) {
			//							__instance.database.databaseshipdata[i].vlsTorpedotypes[num3] = __instance.database.databaseweapondata[__instance.database.databaseshipdata[i].vlsTorpedoIDs[num3]].weaponName;
			//						}
			//						break;
			//					}
			//				case "VLSTorpedoNumbers":
			//					__instance.database.databaseshipdata[i].vlsTorpedoNumbers = Traverse.Create(__instance).Method("PopulateIntArray", new object[] { array4[1] } ).GetValue<int[]>();
			//					break;
			//				case "VLSMaxDepthToFire":
			//					__instance.database.databaseshipdata[i].vlsMaxDepthToFire = float.Parse( array4[1] );
			//					break;
			//				case "VLSMaxSpeedToFire":
			//					__instance.database.databaseshipdata[i].vlsMaxSpeedToFire = float.Parse( array4[1] ) / 10f;
			//					break;
			//				case "MissileType":
			//					__instance.database.databaseshipdata[i].missileType = Traverse.Create( __instance ).Method( "GetWeaponID", new object[] { array4[1].Trim() } ).GetValue<int>();
			//					__instance.database.databaseshipdata[i].missileGameObject = __instance.database.databaseweapondata[__instance.database.databaseshipdata[i].missileType].weaponObject;
			//					break;
			//				case "MissilesPerLauncher":
			//					__instance.database.databaseshipdata[i].missilesPerLauncher = Traverse.Create(__instance).Method("PopulateIntArray", new object[] { array4[1] } ).GetValue<int[]>();
			//					break;
			//				case "MissileLauncherElevates":
			//					__instance.database.databaseshipdata[i].missileLauncherElevates = Traverse.Create( __instance ).Method( "PopulateBoolArray", new object[] { array4[1] } ).GetValue<bool[]>();
			//					break;
			//				case "MissileLauncherElevationMin":
			//					__instance.database.databaseshipdata[i].missileLauncherElevationMin = Traverse.Create( __instance ).Method( "PopulateFloatArray", new object[] { array4[1] } ).GetValue<float[]>();
			//					break;
			//				case "MissileLauncherElevationMax":
			//					__instance.database.databaseshipdata[i].missileLauncherElevationMax = Traverse.Create( __instance ).Method( "PopulateFloatArray", new object[] { array4[1] } ).GetValue<float[]>();
			//					break;
			//				case "NavalGuns": {
			//						string[] array7 = __instance.PopulateStringArray( array4[1] );
			//						__instance.database.databaseshipdata[i].navalGunTypes = new int[array7.Length];
			//						for( int m = 0; m < __instance.database.databaseshipdata[i].navalGunTypes.Length; m++ ) {
			//							__instance.database.databaseshipdata[i].navalGunTypes[m] = __instance.GetDepthWeaponID( array7[m] );
			//						}
			//						break;
			//					}
			//				case "NavalGunFiringArcBearingMin":
			//					__instance.database.databaseshipdata[i].navalGunFiringArcMin = Traverse.Create( __instance ).Method( "PopulateFloatArray", new object[] { array4[1] } ).GetValue<float[]>();
			//					break;
			//				case "NavalGunFiringArcBearingMax":
			//					__instance.database.databaseshipdata[i].navalGunFiringArcMax = Traverse.Create( __instance ).Method( "PopulateFloatArray", new object[] { array4[1] } ).GetValue<float[]>();
			//					break;
			//				case "NavalGunRestAngle": {
			//						__instance.database.databaseshipdata[i].navalGunRestAngle = Traverse.Create( __instance ).Method( "PopulateFloatArray", new object[] { array4[1] } ).GetValue<float[]>();
			//						__instance.database.databaseshipdata[i].rearArcFiring = new bool[__instance.database.databaseshipdata[i].navalGunRestAngle.Length];
			//						for( int l = 0; l < __instance.database.databaseshipdata[i].navalGunRestAngle.Length; l++ ) {
			//							if( __instance.database.databaseshipdata[i].navalGunRestAngle[l] == 180f && __instance.database.databaseshipdata[i].navalGunFiringArcMin[l] > 0f && __instance.database.databaseshipdata[i].navalGunFiringArcMax[l] < 0f ) {
			//								__instance.database.databaseshipdata[i].rearArcFiring[l] = true;
			//							}
			//						}
			//						break;
			//					}
			//				case "NavalGunParticle":
			//					__instance.database.databaseshipdata[i].navalGunParticleEffect = Resources.Load<GameObject>( array4[1].Trim() );
			//					break;
			//				case "NavalGunSmokeParticle":
			//					__instance.database.databaseshipdata[i].navalGunSmokeEffect = Resources.Load<GameObject>( array4[1].Trim() );
			//					break;
			//				case "RBULaunchers": {
			//						string[] array6 = __instance.PopulateStringArray( array4[1] );
			//						__instance.database.databaseshipdata[i].rbuLauncherTypes = new int[array6.Length];
			//						for( int k = 0; k < __instance.database.databaseshipdata[i].rbuLauncherTypes.Length; k++ ) {
			//							__instance.database.databaseshipdata[i].rbuLauncherTypes[k] = __instance.GetDepthWeaponID( array6[k] );
			//						}
			//						break;
			//					}
			//				case "RBUSalvos":
			//					__instance.database.databaseshipdata[i].rbuSalvos = Traverse.Create(__instance).Method("PopulateIntArray", new object[] { array4[1] } ).GetValue<int[]>();
			//					break;
			//				case "RBUFiringArcBearingMin":
			//					__instance.database.databaseshipdata[i].rbuFiringArcMin = Traverse.Create( __instance ).Method( "PopulateFloatArray", new object[] { array4[1] } ).GetValue<float[]>();
			//					break;
			//				case "RBUFiringArcBearingMax":
			//					__instance.database.databaseshipdata[i].rbuFiringArcMax = Traverse.Create( __instance ).Method( "PopulateFloatArray", new object[] { array4[1] } ).GetValue<float[]>();
			//					break;
			//				case "Anti-MissileGunHitProbability":
			//					__instance.database.databaseshipdata[i].gunProbability = float.Parse( array4[1] );
			//					break;
			//				case "Anti-MissileGunRange":
			//					__instance.database.databaseshipdata[i].gunRange = float.Parse( array4[1] );
			//					break;
			//				case "Anti-MissileGunFiringArcStart":
			//					__instance.database.databaseshipdata[i].gunFiringArcStart = Traverse.Create( __instance ).Method( "PopulateFloatArray", new object[] { array4[1] } ).GetValue<float[]>();
			//					break;
			//				case "Anti-MissileGunFiringArcFinish":
			//					__instance.database.databaseshipdata[i].gunFiringArcFinish = Traverse.Create( __instance ).Method( "PopulateFloatArray", new object[] { array4[1] } ).GetValue<float[]>();
			//					break;
			//				case "Anti-MissileGunRestAngle":
			//					__instance.database.databaseshipdata[i].gunRestAngle = Traverse.Create( __instance ).Method( "PopulateFloatArray", new object[] { array4[1] } ).GetValue<float[]>();
			//					break;
			//				case "Anti-MissileRADARRestAngle":
			//					__instance.database.databaseshipdata[i].gunRadarRestAngles = Traverse.Create( __instance ).Method( "PopulateFloatArray", new object[] { array4[1] } ).GetValue<float[]>();
			//					break;
			//				case "Anti-MissileGunUsesRADAR":
			//					__instance.database.databaseshipdata[i].gunUsesRadar = Traverse.Create(__instance).Method("PopulateIntArray", new object[] { array4[1] } ).GetValue<int[]>();
			//					break;
			//				case "Anti-MissileGunParticle":
			//					__instance.database.databaseshipdata[i].ciwsParticle = array4[1].Trim();
			//					break;
			//				case "ChaffType":
			//					__instance.database.databaseshipdata[i].chaffID = Traverse.Create( __instance ).Method( "GetCountermeasureID", new object[] { array4[1].Trim() } ).GetValue<int>();
			//					break;
			//				case "ChaffProbability":
			//					__instance.database.databaseshipdata[i].chaffProbability = float.Parse( array4[1] );
			//					break;
			//				case "NumberChaffLaunched":
			//					__instance.database.databaseshipdata[i].numberChafflaunched = int.Parse( array4[1] );
			//					break;
			//				case "NoisemakerName":
			//					__instance.database.databaseshipdata[i].noiseMakerID = Traverse.Create( __instance ).Method( "GetCountermeasureID", new object[] { array4[1].Trim() } ).GetValue<int>();
			//					break;
			//				case "NumberOfNoisemakers":
			//					__instance.database.databaseshipdata[i].numberofnoisemakers = int.Parse( array4[1] );
			//					if( __instance.database.databaseshipdata[i].numberofnoisemakers > 0 ) {
			//						__instance.database.databaseshipdata[i].hasnoisemaker = true;
			//					}
			//					break;
			//				case "NoisemakerReloadTime":
			//					__instance.database.databaseshipdata[i].noisemakerreloadtime = float.Parse( array4[1] );
			//					break;
			//				case "LabelPosition":
			//					vector = __instance.PopulateVector2( array4[1].Trim() );
			//					vector.x -= 517f;
			//					break;
			//				case "BOWSONAR":
			//					Traverse.Create(__instance).Method("PopulateSubsystemArray", new object[] { i, array4[0], array4[1] }).GetValue();
			//					__instance.database.databaseshipdata[i].subsystemLabelPositions[DamageControl.GetSubsystemIndex( array4[0].Trim() )] = vector;
			//					break;
			//				case "TOWED":
			//					Traverse.Create(__instance).Method("PopulateSubsystemArray", new object[] { i, array4[0], array4[1] }).GetValue();
			//					__instance.database.databaseshipdata[i].subsystemLabelPositions[DamageControl.GetSubsystemIndex( array4[0].Trim() )] = vector;
			//					break;
			//				case "PERISCOPE":
			//					Traverse.Create(__instance).Method("PopulateSubsystemArray", new object[] { i, array4[0], array4[1] }).GetValue();
			//					__instance.database.databaseshipdata[i].subsystemLabelPositions[DamageControl.GetSubsystemIndex( array4[0].Trim() )] = vector;
			//					break;
			//				case "ESM_MAST":
			//					Traverse.Create(__instance).Method("PopulateSubsystemArray", new object[] { i, array4[0], array4[1] }).GetValue();
			//					__instance.database.databaseshipdata[i].subsystemLabelPositions[DamageControl.GetSubsystemIndex( array4[0].Trim() )] = vector;
			//					break;
			//				case "RADAR_MAST":
			//					Traverse.Create(__instance).Method("PopulateSubsystemArray", new object[] { i, array4[0], array4[1] }).GetValue();
			//					__instance.database.databaseshipdata[i].subsystemLabelPositions[DamageControl.GetSubsystemIndex( array4[0].Trim() )] = vector;
			//					break;
			//				case "TUBES":
			//					Traverse.Create(__instance).Method("PopulateSubsystemArray", new object[] { i, array4[0], array4[1] }).GetValue();
			//					__instance.database.databaseshipdata[i].subsystemLabelPositions[DamageControl.GetSubsystemIndex( array4[0].Trim() )] = vector;
			//					break;
			//				case "FIRECONTROL":
			//					Traverse.Create(__instance).Method("PopulateSubsystemArray", new object[] { i, array4[0], array4[1] }).GetValue();
			//					__instance.database.databaseshipdata[i].subsystemLabelPositions[DamageControl.GetSubsystemIndex( array4[0].Trim() )] = vector;
			//					break;
			//				case "PUMPS":
			//					Traverse.Create(__instance).Method("PopulateSubsystemArray", new object[] { i, array4[0], array4[1] }).GetValue();
			//					__instance.database.databaseshipdata[i].subsystemLabelPositions[DamageControl.GetSubsystemIndex( array4[0].Trim() )] = vector;
			//					break;
			//				case "PROPULSION":
			//					Traverse.Create(__instance).Method("PopulateSubsystemArray", new object[] { i, array4[0], array4[1] }).GetValue();
			//					__instance.database.databaseshipdata[i].subsystemLabelPositions[DamageControl.GetSubsystemIndex( array4[0].Trim() )] = vector;
			//					break;
			//				case "RUDDER":
			//					Traverse.Create(__instance).Method("PopulateSubsystemArray", new object[] { i, array4[0], array4[1] }).GetValue();
			//					__instance.database.databaseshipdata[i].subsystemLabelPositions[DamageControl.GetSubsystemIndex( array4[0].Trim() )] = vector;
			//					break;
			//				case "PLANES":
			//					Traverse.Create(__instance).Method("PopulateSubsystemArray", new object[] { i, array4[0], array4[1] }).GetValue();
			//					__instance.database.databaseshipdata[i].subsystemLabelPositions[DamageControl.GetSubsystemIndex( array4[0].Trim() )] = vector;
			//					break;
			//				case "BALLAST":
			//					Traverse.Create(__instance).Method("PopulateSubsystemArray", new object[] { i, array4[0], array4[1] }).GetValue();
			//					__instance.database.databaseshipdata[i].subsystemLabelPositions[DamageControl.GetSubsystemIndex( array4[0].Trim() )] = vector;
			//					break;
			//				case "REACTOR":
			//					Traverse.Create(__instance).Method("PopulateSubsystemArray", new object[] { i, array4[0], array4[1] }).GetValue();
			//					__instance.database.databaseshipdata[i].subsystemLabelPositions[DamageControl.GetSubsystemIndex( array4[0].Trim() )] = vector;
			//					break;
			//				case "FLOODING1":
			//					__instance.database.databaseshipdata[i].compartmentFloodingRanges = new Vector2[5];
			//					__instance.database.databaseshipdata[i].compartmentPositionsAndWidth = new Vector2[5];
			//					array2 = array4[1].Trim().Split( ',' );
			//					__instance.database.databaseshipdata[i].compartmentFloodingRanges[0].x = float.Parse( array2[2] );
			//					__instance.database.databaseshipdata[i].compartmentFloodingRanges[0].y = float.Parse( array2[3] );
			//					__instance.database.databaseshipdata[i].compartmentPositionsAndWidth[0].x = float.Parse( array2[0] );
			//					__instance.database.databaseshipdata[i].compartmentPositionsAndWidth[0].y = float.Parse( array2[1] );
			//					break;
			//				case "FLOODING2":
			//					array2 = array4[1].Trim().Split( ',' );
			//					__instance.database.databaseshipdata[i].compartmentFloodingRanges[1].x = float.Parse( array2[2] );
			//					__instance.database.databaseshipdata[i].compartmentFloodingRanges[1].y = float.Parse( array2[3] );
			//					__instance.database.databaseshipdata[i].compartmentPositionsAndWidth[1].x = float.Parse( array2[0] );
			//					__instance.database.databaseshipdata[i].compartmentPositionsAndWidth[1].y = float.Parse( array2[1] );
			//					break;
			//				case "FLOODING3":
			//					array2 = array4[1].Trim().Split( ',' );
			//					__instance.database.databaseshipdata[i].compartmentFloodingRanges[2].x = float.Parse( array2[2] );
			//					__instance.database.databaseshipdata[i].compartmentFloodingRanges[2].y = float.Parse( array2[3] );
			//					__instance.database.databaseshipdata[i].compartmentPositionsAndWidth[2].x = float.Parse( array2[0] );
			//					__instance.database.databaseshipdata[i].compartmentPositionsAndWidth[2].y = float.Parse( array2[1] );
			//					break;
			//				case "FLOODING4":
			//					array2 = array4[1].Trim().Split( ',' );
			//					__instance.database.databaseshipdata[i].compartmentFloodingRanges[3].x = float.Parse( array2[2] );
			//					__instance.database.databaseshipdata[i].compartmentFloodingRanges[3].y = float.Parse( array2[3] );
			//					__instance.database.databaseshipdata[i].compartmentPositionsAndWidth[3].x = float.Parse( array2[0] );
			//					__instance.database.databaseshipdata[i].compartmentPositionsAndWidth[3].y = float.Parse( array2[1] );
			//					break;
			//				case "FLOODING5": {
			//						string[] array5 = array4[1].Trim().Split( ',' );
			//						__instance.database.databaseshipdata[i].compartmentFloodingRanges[4].x = float.Parse( array5[2] );
			//						__instance.database.databaseshipdata[i].compartmentFloodingRanges[4].y = float.Parse( array5[3] );
			//						__instance.database.databaseshipdata[i].compartmentPositionsAndWidth[4].x = float.Parse( array5[0] );
			//						__instance.database.databaseshipdata[i].compartmentPositionsAndWidth[4].y = float.Parse( array5[1] );
			//						break;
			//					}
			//				case "DamageControlPartyY":
			//					__instance.database.databaseshipdata[i].damageControlPartyY = float.Parse( array4[1] );
			//					break;
			//			}
			//			if( !flag ) {
			//				float num9 = 0f;
			//				bool flag2 = false;
			//				num9 = ( ( !( __instance.database.databaseshipdata[i].submergedspeed > __instance.database.databaseshipdata[i].surfacespeed ) ) ? __instance.database.databaseshipdata[i].surfacespeed : __instance.database.databaseshipdata[i].submergedspeed );
			//				float num10 = -0.2f;
			//				for( int num11 = 0; num11 < 7; num11++ ) {
			//					__instance.database.databaseshipdata[i].telegraphSpeeds[num11] = num10 * num9 * 0.1f;
			//					num10 += 0.2f;
			//				}
			//				if( __instance.database.databaseshipdata[i].shipType == "SUBMARINE" ) {
			//					__instance.database.databaseshipdata[i].telegraphSpeeds[2] = 0.5f;
			//					__instance.database.databaseshipdata[i].telegraphSpeeds[3] = 1f;
			//				}
			//			}
			//		}
			//	}
			//	return false;
			//}
		}
	}
}
