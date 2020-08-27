using HarmonyLib;
using UnityEngine;
using System.IO;
using System.Reflection;
using BepInEx;
using System.Collections.Generic;
using System;

namespace Cold_Waters_Expanded
{
    [BepInPlugin( "org.bepinex.plugins.coldwatersexpanded", "Cold Waters Expanded", "1.0.0.0" )]
    public class Patcher : BaseUnityPlugin {

        static Patcher patcher;

        List<Mesh> moddedMeshList = new List<Mesh>();
		List<string> assetBundlePaths = new List<string>();

        static MethodBase TargetMethod() {
            return AccessTools.Method( typeof( VesselBuilder ), "GetMesh" );
        }

        void Awake() {
            patcher = this;
            Debug.Log( "Beginning to load Cold Waters Expanded" );
            LoadAssetBundles();
            LoadCustomMesh();
            Harmony harmony = new Harmony( "com.bepinex.plugins.coldwaterspatched" ); // rename "author" and "project"
            harmony.PatchAll();
            Debug.Log( "Cold Waters Expanded Loaded" );
        }

        void LoadAssetBundles() {
            Debug.Log( "Searching for ASSET BUNDLES " );
            foreach( string filename in Directory.GetFiles( Application.streamingAssetsPath + "/assetbundles/", "AssetBundles", SearchOption.AllDirectories ) ) {
                assetBundlePaths.Add( filename );
                Debug.Log( "ASSET BUNDLE found at  " + filename );
            }
        }

        void LoadCustomMesh() {
            Debug.Log( "Searching for modded MESH files" );
            foreach( string filename in Directory.GetFiles(Application.streamingAssetsPath + "/override/", "*.obj", SearchOption.AllDirectories) ) {
                Mesh[] allMeshes = new ObjImporter( filename ).GetMeshes();
                foreach( Mesh mesh in allMeshes ) {
                    moddedMeshList.Add( mesh );
                    Debug.Log( "Modded MESH " + mesh.name + " loaded" );
                }
            }
        }

		[HarmonyPatch( typeof( VesselBuilder ), "GetMesh" )]
		public class VesselBuilder_GetMesh_Patch
		{
			[HarmonyPrefix]
			public static bool Prefix( VesselBuilder __instance, ref Mesh __result, string meshName ) {
				if( meshName.Length > 0 ) {
					foreach( Mesh mesh in patcher.moddedMeshList ) {
						if( mesh.name == meshName ) {
							__result = mesh;
							Debug.Log( "Modded MESH found " + meshName );
							return false; // false to ensure that the mesh is not overwritten
						}
					}
					return true; // make sure you only skip if really necessary
				}
				else {
					return true; // make sure you only skip if really necessary
				}
			}
		}

		[HarmonyPatch( typeof( VesselBuilder ), "CreateAndPlaceMeshes" )]
		public class VesselBuilder_CreateAndPlaceMeshes_Patch
		{
			[HarmonyPrefix]
            public static bool Prefix( VesselBuilder __instance, GameObject vesselTemplate, Vessel activeVessel, bool playerControlled, string vesselPrefabRef ) {
				Transform meshHolder = activeVessel.meshHolder;
				Vector3 meshPosition = Vector3.zero;
				Vector3 meshRotation = Vector3.zero;
				Material material = null;
				__instance.currentMesh = null;
				GameObject gameObject = null;
				if( !playerControlled ) {
					UnityEngine.Object.Destroy( activeVessel.submarineFunctions.gameObject );
					activeVessel.submarineFunctions = null;
					activeVessel.vesselmovement.hasWeaponSource = false;
					activeVessel.vesselmovement.weaponSource = null;
					if( activeVessel.databaseshipdata.numberofnoisemakers > 0 ) {
						activeVessel.vesselai.hasNoiseMaker = true;
					}
					else {
						UnityEngine.Object.Destroy( activeVessel.vesselai.enemynoisemaker.gameObject );
						activeVessel.vesselai.enemynoisemaker = null;
					}
					if( activeVessel.databaseshipdata.missileGameObject != null ) {
						activeVessel.vesselai.hasMissile = true;
						activeVessel.vesselai.enemymissile.missileLaunchers = new Transform[activeVessel.databaseshipdata.missilesPerLauncher.Length];
						activeVessel.vesselai.enemymissile.missileLaunchParticlePositions = new Vector3[activeVessel.databaseshipdata.missilesPerLauncher.Length];
						activeVessel.vesselai.enemymissile.missileLaunchParticles = new ParticleSystem[activeVessel.databaseshipdata.missilesPerLauncher.Length];
					}
					else {
						UnityEngine.Object.Destroy( activeVessel.vesselai.enemymissile.gameObject );
						activeVessel.vesselai.enemymissile = null;
					}
					if( activeVessel.databaseshipdata.torpedotypes != null ) {
						activeVessel.vesselai.hasTorpedo = true;
						int num = activeVessel.databaseshipdata.torpedoConfig.Length;
						activeVessel.vesselai.enemytorpedo.torpedoMounts = new Transform[num];
						activeVessel.vesselai.enemytorpedo.torpedoSpawnPositions = new Transform[num];
						activeVessel.vesselai.enemytorpedo.torpedoParticlePositions = new Vector3[num];
						activeVessel.vesselai.enemytorpedo.torpedoClouds = new ParticleSystem[num];
						activeVessel.vesselai.enemytorpedo.numberOfTorpedoes = new int[num];
						activeVessel.vesselai.enemytorpedo.torpedoMountAngles = new float[num];
						activeVessel.vesselai.enemytorpedo.torpedoStatus = new int[num];
						if( activeVessel.databaseshipdata.shipType == "SUBMARINE" ) {
							activeVessel.vesselai.enemytorpedo.fixedTubes = true;
							activeVessel.vesselai.enemytorpedo.submergedTubes = true;
							activeVessel.isSubmarine = true;
							activeVessel.vesselmovement.isSubmarine = true;
							activeVessel.vesselmovement.planes = new Transform[2];
						}
					}
					else {
						UnityEngine.Object.Destroy( activeVessel.vesselai.enemytorpedo.gameObject );
						activeVessel.vesselai.enemytorpedo = null;
					}
					if( activeVessel.databaseshipdata.gunProbability > 0f ) {
						activeVessel.vesselai.hasMissileDefense = true;
						int num2 = activeVessel.databaseshipdata.gunFiringArcStart.Length;
						activeVessel.vesselai.enemymissiledefense.turrets = new GameObject[num2];
						if( activeVessel.databaseshipdata.gunRadarRestAngles != null ) {
							activeVessel.vesselai.enemymissiledefense.trackingRadars = new GameObject[activeVessel.databaseshipdata.gunRadarRestAngles.Length];
						}
						activeVessel.vesselai.enemymissiledefense.barrels = new Transform[num2];
						activeVessel.vesselai.enemymissiledefense.directionFinders = new Transform[num2];
					}
					else {
						UnityEngine.Object.Destroy( activeVessel.vesselai.enemymissiledefense.gameObject );
						activeVessel.vesselai.enemymissiledefense = null;
					}
					if( activeVessel.databaseshipdata.navalGunTypes != null ) {
						activeVessel.vesselai.hasNavalGuns = true;
						int num3 = activeVessel.databaseshipdata.navalGunTypes.Length;
						activeVessel.vesselai.enemynavalguns.turrets = new Transform[num3];
						activeVessel.vesselai.enemynavalguns.barrels = new Transform[num3];
						activeVessel.vesselai.enemynavalguns.muzzlePositions = new Transform[num3];
					}
					else {
						UnityEngine.Object.Destroy( activeVessel.vesselai.enemynavalguns.gameObject );
						activeVessel.vesselai.enemynavalguns = null;
					}
					if( activeVessel.databaseshipdata.rbuLauncherTypes != null ) {
						activeVessel.vesselai.hasRBU = true;
						int num4 = activeVessel.databaseshipdata.rbuLauncherTypes.Length;
						activeVessel.vesselai.enemyrbu.rbuLaunchers = new Transform[num4];
						activeVessel.vesselai.enemyrbu.rbuPositions = new Transform[num4];
						activeVessel.vesselai.enemyrbu.rbuHubs = new Transform[num4];
						activeVessel.vesselai.enemyrbu.rbuLaunchPositions = new Transform[num4];
						activeVessel.vesselai.enemyrbu.rbuLaunchEffects = new ParticleSystem[num4];
						activeVessel.vesselai.enemyrbu.salvosFired = new int[num4];
					}
					else {
						UnityEngine.Object.Destroy( activeVessel.vesselai.enemyrbu.gameObject );
						activeVessel.vesselai.enemyrbu = null;
					}
				}
				else {
					activeVessel.vesselai = null;
					UnityEngine.Object.Destroy( activeVessel.weaponsHolder.gameObject );
					activeVessel.vesselmovement.weaponSource.torpedoTubes = new Transform[activeVessel.databaseshipdata.torpedoConfig.Length];
					activeVessel.vesselmovement.weaponSource.tubeParticleEffects = new Vector3[activeVessel.databaseshipdata.torpedoConfig.Length];
					activeVessel.playercontrolled = true;
					activeVessel.isSubmarine = true;
					activeVessel.vesselmovement.isSubmarine = true;
					activeVessel.vesselmovement.planes = new Transform[2];
					activeVessel.vesselmovement.telegraphValue = 2;
				}
				int num5 = 0;
				activeVessel.vesselmovement.props = new Transform[0];
				if( activeVessel.databaseshipdata.proprotationspeed.Length > 0 ) {
					activeVessel.vesselmovement.props = new Transform[activeVessel.databaseshipdata.proprotationspeed.Length];
				}
				List<Transform> list = new List<Transform>();
				int num6 = 0;
				float speed = 0f;
				float num7 = 1f;
				int num8 = 0;
				int num9 = 0;
				int num10 = 0;
				int num11 = 0;
				int num12 = 0;
				int num13 = 0;
				List<MeshCollider> list2 = new List<MeshCollider>();
				List<Radar> list3 = new List<Radar>();
				List<Mesh> list4 = new List<Mesh>();
				List<MeshFilter> list5 = new List<MeshFilter>();
				List<GameObject> list6 = new List<GameObject>();
				activeVessel.vesselmovement.submarineFoamDurations = new float[2];
				string str = string.Empty;
				bool flag = false;
				string filename = Path.Combine( "vessels", vesselPrefabRef );
				string[] array = UIFunctions.globaluifunctions.textparser.OpenTextDataFile( filename );
				for( int i = 0; i < array.Length; i++ ) {
					string[] array2 = array[i].Split( '=' );
					if( array2[0].Trim() == "[Model]" ) {
						flag = true;
					}
					if( !flag ) {
						continue;
					}
					switch( array2[0] ) {
						case "ModelFile": {
								__instance.allMeshes = Resources.LoadAll<Mesh>( array2[1].Trim() );
								string[] array15 = array2[1].Trim().Split( '/' );
								str = array15[array15.Length - 1];
								break;
							}
						case "MeshPosition":
							meshPosition = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
							break;
						case "MeshRotation":
							meshRotation = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
							break;
						case "Material":
							material = GetMaterial( array2[1].Trim() );
							break;
						case "MaterialTextures":
							if( material != null ) {
								string[] array28 = array2[1].Trim().Split( ',' );
								if( array28.Length > 0 ) {
									material.SetTexture( "_MainTex", UIFunctions.globaluifunctions.textparser.GetTexture( array28[0] ) );
								}
								if( array28.Length > 1 ) {
									material.SetTexture( "_SpecTex", UIFunctions.globaluifunctions.textparser.GetTexture( array28[1] ) );
								}
								if( array28.Length > 2 ) {
									material.SetTexture( "_BumpMap", UIFunctions.globaluifunctions.textparser.GetTexture( array28[2] ) );
								}
							}
							break;
						case "Mesh":
							if( !array2[1].Trim().Contains( "," ) ) {
								gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array2[1].Trim() );
							}
							else {
								string[] array27 = array2[1].Trim().Split( ',' );
								gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array27[0].Trim() );
								if( array27[1] == "HIDE" ) {
									list6.Add( gameObject );
								}
								else {
									list5.Add( gameObject.GetComponent<MeshFilter>() );
									list4.Add( GetMesh( __instance, array27[1].Trim() ) );
								}
							}
							if( gameObject.name.Contains( "biologic" ) ) {
								gameObject.GetComponent<MeshRenderer>().receiveShadows = false;
								gameObject.transform.parent.parent.gameObject.AddComponent<Whale_AI>().parentVessel = activeVessel;
							}
							break;
						case "MeshLights": {
								if( !GameDataManager.isNight ) {
									break;
								}
								if( !array2[1].Trim().Contains( "," ) ) {
									gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array2[1].Trim() );
									break;
								}
								string[] array16 = array2[1].Trim().Split( ',' );
								gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array16[0].Trim() );
								if( array16[1] == "HIDE" ) {
									list6.Add( gameObject );
									break;
								}
								list5.Add( gameObject.GetComponent<MeshFilter>() );
								list4.Add( GetMesh( __instance, array16[1].Trim() ) );
								break;
							}
						case "MeshHullCollider":
							activeVessel.hullCollider.sharedMesh = GetMesh( __instance, array2[1].Trim() );
							break;
						case "MeshSuperstructureCollider": {
								GameObject gameObject15 = UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.blankTransform, meshHolder.position, Quaternion.identity ) as GameObject;
								gameObject15.transform.SetParent( meshHolder );
								gameObject15.transform.localPosition = Vector3.zero;
								gameObject15.transform.localRotation = Quaternion.identity;
								MeshCollider meshCollider = gameObject15.AddComponent<MeshCollider>();
								meshCollider.convex = true;
								meshCollider.isTrigger = true;
								meshCollider.sharedMesh = GetMesh( __instance, array2[1].Trim() );
								list2.Add( meshCollider );
								break;
							}
						case "MeshHullNumber": {
								if( !array2[1].Trim().Contains( "," ) ) {
									gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array2[1].Trim() );
								}
								else {
									string[] array19 = array2[1].Trim().Split( ',' );
									gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array19[0].Trim() );
									if( array19[1] == "HIDE" ) {
										list6.Add( gameObject );
									}
									else {
										list5.Add( gameObject.GetComponent<MeshFilter>() );
										list4.Add( GetMesh( __instance, array19[1].Trim() ) );
									}
								}
								activeVessel.vesselmovement.hullNumberRenderer = gameObject.GetComponent<MeshRenderer>();
								int num14 = UnityEngine.Random.Range( 0, activeVessel.databaseshipdata.hullnumbers.Length );
								string texturePath = "ships/materials/hullnumbers/" + activeVessel.databaseshipdata.hullnumbers[num14];
								Material material2 = activeVessel.vesselmovement.hullNumberRenderer.material;
								material2.SetTexture( "_MainTex", UIFunctions.globaluifunctions.textparser.GetTexture( texturePath ) );
								break;
							}
						case "MeshRudder":
							if( !array2[1].Trim().Contains( "," ) ) {
								gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array2[1].Trim() );
							}
							else {
								string[] array29 = array2[1].Trim().Split( ',' );
								gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array29[0].Trim() );
								if( array29[1] == "HIDE" ) {
									list6.Add( gameObject );
								}
								else {
									list5.Add( gameObject.GetComponent<MeshFilter>() );
									list4.Add( GetMesh( __instance, array29[1].Trim() ) );
								}
							}
							list.Add( gameObject.transform );
							break;
						case "MeshProp":
							if( !array2[1].Trim().Contains( "," ) ) {
								gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array2[1].Trim() );
							}
							else {
								string[] array11 = array2[1].Trim().Split( ',' );
								gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array11[0].Trim() );
								if( array11[1] == "HIDE" ) {
									list6.Add( gameObject );
								}
								else {
									list5.Add( gameObject.GetComponent<MeshFilter>() );
									list4.Add( GetMesh( __instance, array11[1].Trim() ) );
								}
							}
							activeVessel.vesselmovement.props[num5] = gameObject.transform;
							num5++;
							break;
						case "MeshBowPlanes":
							if( !array2[1].Trim().Contains( "," ) ) {
								gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array2[1].Trim() );
							}
							else {
								string[] array23 = array2[1].Trim().Split( ',' );
								gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array23[0].Trim() );
								if( array23[1] == "HIDE" ) {
									list6.Add( gameObject );
								}
								else {
									list5.Add( gameObject.GetComponent<MeshFilter>() );
									list4.Add( GetMesh( __instance, array23[1].Trim() ) );
								}
							}
							activeVessel.vesselmovement.planes[0] = gameObject.transform;
							break;
						case "MeshSternPlanes":
							if( !array2[1].Trim().Contains( "," ) ) {
								gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array2[1].Trim() );
							}
							else {
								string[] array21 = array2[1].Trim().Split( ',' );
								gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array21[0].Trim() );
								if( array21[1] == "HIDE" ) {
									list6.Add( gameObject );
								}
								else {
									list5.Add( gameObject.GetComponent<MeshFilter>() );
									list4.Add( GetMesh( __instance, array21[1].Trim() ) );
								}
							}
							activeVessel.vesselmovement.planes[1] = gameObject.transform;
							break;
						case "MastHeight":
							if( playerControlled ) {
								float y = float.Parse( array2[1].Trim() );
								activeVessel.submarineFunctions.peiscopeStops[num6].y = y;
								activeVessel.submarineFunctions.mastHeads[num6].transform.localPosition = new Vector3( 0f, y, 0f );
							}
							break;
						case "MeshMast": {
								if( !playerControlled ) {
									break;
								}
								if( !array2[1].Trim().Contains( "," ) ) {
									activeVessel.submarineFunctions.mastTransforms[num6].GetComponent<MeshFilter>().mesh = GetMesh( __instance, array2[1].Trim() );
								}
								else {
									string[] array25 = array2[1].Trim().Split( ',' );
									activeVessel.submarineFunctions.mastTransforms[num6].GetComponent<MeshFilter>().mesh = GetMesh( __instance, array25[0].Trim() );
									if( array25[1] == "HIDE" ) {
										list6.Add( activeVessel.submarineFunctions.mastTransforms[num6].gameObject );
									}
									else {
										list5.Add( activeVessel.submarineFunctions.mastTransforms[num6].GetComponent<MeshFilter>() );
										list4.Add( GetMesh( __instance, array25[1].Trim() ) );
									}
								}
								activeVessel.submarineFunctions.mastTransforms[num6].GetComponent<MeshRenderer>().sharedMaterial = material;
								activeVessel.submarineFunctions.mastTransforms[num6].localPosition = meshPosition;
								activeVessel.submarineFunctions.mastTransforms[num6].localRotation = Quaternion.identity;
								gameObject = activeVessel.submarineFunctions.mastTransforms[num6].gameObject;
								activeVessel.submarineFunctions.mastTransforms[num6] = gameObject.transform;
								ref Vector2 reference4 = ref activeVessel.submarineFunctions.peiscopeStops[num6];
								Vector3 localPosition3 = gameObject.transform.localPosition;
								reference4.x = localPosition3.y;
								activeVessel.submarineFunctions.peiscopeStops[num6].y += activeVessel.submarineFunctions.peiscopeStops[num6].x;
								num6++;
								break;
							}
						case "ChildMesh": {
								Transform transform4 = gameObject.transform;
								if( !array2[1].Trim().Contains( "," ) ) {
									gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array2[1].Trim() );
								}
								else {
									string[] array24 = array2[1].Trim().Split( ',' );
									gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array24[0].Trim() );
									if( array24[1] == "HIDE" ) {
										list6.Add( gameObject );
									}
									else {
										list5.Add( gameObject.GetComponent<MeshFilter>() );
										list4.Add( GetMesh( __instance, array24[1].Trim() ) );
									}
								}
								gameObject.transform.SetParent( transform4, worldPositionStays: false );
								gameObject.transform.localPosition = meshPosition;
								gameObject.transform.localRotation = Quaternion.Slerp( Quaternion.identity, Quaternion.Euler( meshRotation ), 1f );
								break;
							}
						case "MeshMainFlag":
							if( !array2[1].Trim().Contains( "," ) ) {
								gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array2[1].Trim() );
							}
							else {
								string[] array7 = array2[1].Trim().Split( ',' );
								gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array7[0].Trim() );
								if( array7[1] == "HIDE" ) {
									list6.Add( gameObject );
								}
								else {
									list5.Add( gameObject.GetComponent<MeshFilter>() );
									list4.Add( GetMesh( __instance, array7[1].Trim() ) );
								}
							}
							material.color = Environment.whiteLevel;
							gameObject.layer = 17;
							activeVessel.vesselmovement.flagRenderer = gameObject.GetComponent<MeshRenderer>();
							break;
						case "MeshOtherFlags":
							if( !array2[1].Trim().Contains( "," ) ) {
								gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array2[1].Trim() );
							}
							else {
								string[] array4 = array2[1].Trim().Split( ',' );
								gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array4[0].Trim() );
								if( array4[1] == "HIDE" ) {
									list6.Add( gameObject );
								}
								else {
									list5.Add( gameObject.GetComponent<MeshFilter>() );
									list4.Add( GetMesh( __instance, array4[1].Trim() ) );
								}
							}
							material.color = Environment.whiteLevel;
							gameObject.layer = 17;
							break;
						case "RADARSpeed":
							speed = float.Parse( array2[1].Trim() );
							break;
						case "RADARDirection":
							num7 = float.Parse( array2[1].Trim() );
							break;
						case "MeshRADAR": {
								if( !array2[1].Trim().Contains( "," ) ) {
									gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array2[1].Trim() );
								}
								else {
									string[] array20 = array2[1].Trim().Split( ',' );
									gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array20[0].Trim() );
									if( array20[1] == "HIDE" ) {
										list6.Add( gameObject );
									}
									else {
										list5.Add( gameObject.GetComponent<MeshFilter>() );
										list4.Add( GetMesh( __instance, array20[1].Trim() ) );
									}
								}
								Radar radar = gameObject.AddComponent<Radar>();
								radar.speed = speed;
								list3.Add( radar );
								break;
							}
						case "MeshNoisemakerMount":
							if( activeVessel.vesselai != null ) {
								activeVessel.vesselai.enemynoisemaker.noisemakerTubes.transform.localPosition = meshPosition;
							}
							else {
								activeVessel.vesselmovement.weaponSource.noisemakerTubes.transform.localPosition = meshPosition;
							}
							break;
						case "MeshTorpedoMount":
							if( !array2[1].Trim().Contains( "," ) ) {
								gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array2[1].Trim() );
							}
							else {
								string[] array9 = array2[1].Trim().Split( ',' );
								gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array9[0].Trim() );
								if( array9[1] == "HIDE" ) {
									list6.Add( gameObject );
								}
								else {
									list5.Add( gameObject.GetComponent<MeshFilter>() );
									list4.Add( GetMesh( __instance, array9[1].Trim() ) );
								}
							}
							activeVessel.vesselai.enemytorpedo.torpedoMounts[num8] = gameObject.transform;
							break;
						case "TorpedoSpawnPosition": {
								GameObject gameObject13 = UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.blankTransform, meshHolder.position, Quaternion.identity ) as GameObject;
								if( !activeVessel.playercontrolled ) {
									if( activeVessel.vesselai.enemytorpedo != null ) {
										if( !activeVessel.vesselai.enemytorpedo.fixedTubes ) {
											gameObject13.transform.SetParent( gameObject.transform, worldPositionStays: false );
											gameObject13.transform.localPosition = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
											gameObject13.transform.localRotation = Quaternion.identity;
										}
										else {
											gameObject13.transform.SetParent( meshHolder.transform, worldPositionStays: false );
											gameObject13.transform.localPosition = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
											gameObject13.transform.localRotation = Quaternion.Slerp( Quaternion.identity, Quaternion.Euler( meshRotation ), 1f );
										}
									}
								}
								else {
									gameObject13.transform.SetParent( meshHolder.transform, worldPositionStays: false );
									gameObject13.transform.localPosition = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
									gameObject13.transform.localRotation = Quaternion.Slerp( Quaternion.identity, Quaternion.Euler( meshRotation ), 1f );
								}
								if( activeVessel.vesselai != null ) {
									activeVessel.vesselai.enemytorpedo.torpedoSpawnPositions[num8] = gameObject13.transform;
									if( activeVessel.isSubmarine ) {
										activeVessel.vesselai.enemytorpedo.torpedoMounts[num8] = gameObject13.transform;
									}
								}
								else {
									activeVessel.vesselmovement.weaponSource.torpedoTubes[num8] = gameObject13.transform;
								}
								break;
							}
						case "TorpedoEffectPosition":
							if( activeVessel.vesselai != null ) {
								ref Vector3 reference2 = ref activeVessel.vesselai.enemytorpedo.torpedoParticlePositions[num8];
								reference2 = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
							}
							else {
								ref Vector3 reference3 = ref activeVessel.vesselmovement.weaponSource.tubeParticleEffects[num8];
								reference3 = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
							}
							num8++;
							break;
						case "MeshMissileMount":
							if( !array2[1].Trim().Contains( "," ) ) {
								gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array2[1].Trim() );
							}
							else {
								string[] array18 = array2[1].Trim().Split( ',' );
								gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array18[0].Trim() );
								if( array18[1] == "HIDE" ) {
									list6.Add( gameObject );
								}
								else {
									list5.Add( gameObject.GetComponent<MeshFilter>() );
									list4.Add( GetMesh( __instance, array18[1].Trim() ) );
								}
							}
							gameObject.name = "missileLauncher";
							activeVessel.vesselai.enemymissile.missileLaunchers[num9] = gameObject.transform;
							break;
						case "MissileEffectPosition": {
								ref Vector3 reference = ref activeVessel.vesselai.enemymissile.missileLaunchParticlePositions[num9];
								reference = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
								num9++;
								break;
							}
						case "MeshNavalGun":
							if( !array2[1].Trim().Contains( "," ) ) {
								gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array2[1].Trim() );
							}
							else {
								string[] array12 = array2[1].Trim().Split( ',' );
								gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array12[0].Trim() );
								if( array12[1] == "HIDE" ) {
									list6.Add( gameObject );
								}
								else {
									list5.Add( gameObject.GetComponent<MeshFilter>() );
									list4.Add( GetMesh( __instance, array12[1].Trim() ) );
								}
							}
							activeVessel.vesselai.enemynavalguns.turrets[num13] = gameObject.transform;
							break;
						case "MeshNavalGunBarrel": {
								Transform transform = gameObject.transform;
								if( !array2[1].Trim().Contains( "," ) ) {
									gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array2[1].Trim() );
								}
								else {
									string[] array5 = array2[1].Trim().Split( ',' );
									gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array5[0].Trim() );
									if( array5[1] == "HIDE" ) {
										list6.Add( gameObject );
									}
									else {
										list5.Add( gameObject.GetComponent<MeshFilter>() );
										list4.Add( GetMesh( __instance, array5[1].Trim() ) );
									}
								}
								gameObject.transform.SetParent( transform, worldPositionStays: false );
								gameObject.transform.localPosition = meshPosition;
								gameObject.transform.localRotation = Quaternion.Slerp( Quaternion.identity, Quaternion.Euler( meshRotation ), 1f );
								activeVessel.vesselai.enemynavalguns.barrels[num13] = gameObject.transform;
								break;
							}
						case "NavalGunSpawnPosition": {
								GameObject gameObject14 = UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.blankTransform, meshHolder.position, Quaternion.identity ) as GameObject;
								if( activeVessel.vesselai.enemynavalguns != null ) {
									gameObject14.transform.SetParent( gameObject.transform, worldPositionStays: false );
									gameObject14.transform.localPosition = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
									gameObject14.transform.localRotation = Quaternion.identity;
								}
								if( activeVessel.vesselai != null ) {
									activeVessel.vesselai.enemynavalguns.muzzlePositions[num13] = gameObject14.transform;
								}
								num13++;
								break;
							}
						case "MeshCIWSGun": {
								if( !array2[1].Trim().Contains( "," ) ) {
									gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array2[1].Trim() );
								}
								else {
									string[] array26 = array2[1].Trim().Split( ',' );
									gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array26[0].Trim() );
									if( array26[1] == "HIDE" ) {
										list6.Add( gameObject );
									}
									else {
										list5.Add( gameObject.GetComponent<MeshFilter>() );
										list4.Add( GetMesh( __instance, array26[1].Trim() ) );
									}
								}
								activeVessel.vesselai.enemymissiledefense.turrets[num10] = gameObject;
								GameObject gameObject11 = UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.blankTransform, gameObject.transform.position, gameObject.transform.rotation ) as GameObject;
								gameObject11.transform.SetParent( gameObject.transform, worldPositionStays: false );
								gameObject11.name = "directionfinder";
								gameObject11.transform.localPosition = Vector3.zero;
								activeVessel.vesselai.enemymissiledefense.directionFinders[num10] = gameObject11.transform;
								GameObject gameObject12 = UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.blankTransform, gameObject.transform.position, gameObject.transform.rotation ) as GameObject;
								gameObject12.transform.SetParent( gameObject.transform, worldPositionStays: false );
								gameObject12.transform.localPosition = Vector3.zero;
								gameObject12.transform.localRotation = Quaternion.identity;
								gameObject12.name = "barrel";
								activeVessel.vesselai.enemymissiledefense.barrels[num10] = gameObject12.transform;
								num10++;
								break;
							}
						case "MeshCIWSRADAR":
							if( !array2[1].Trim().Contains( "," ) ) {
								gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array2[1].Trim() );
							}
							else {
								string[] array22 = array2[1].Trim().Split( ',' );
								gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array22[0].Trim() );
								if( array22[1] == "HIDE" ) {
									list6.Add( gameObject );
								}
								else {
									list5.Add( gameObject.GetComponent<MeshFilter>() );
									list4.Add( GetMesh( __instance, array22[1].Trim() ) );
								}
							}
							activeVessel.vesselai.enemymissiledefense.trackingRadars[num11] = gameObject;
							num11++;
							break;
						case "MeshRBULauncher": {
								GameObject gameObject7 = UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.blankTransform, meshHolder.position, Quaternion.identity ) as GameObject;
								gameObject7.transform.SetParent( activeVessel.meshHolder, worldPositionStays: false );
								gameObject7.transform.localPosition = meshPosition;
								gameObject7.transform.localRotation = Quaternion.identity;
								gameObject7.name = "rbuMount";
								activeVessel.vesselai.enemyrbu.rbuPositions[num12] = gameObject7.transform;
								if( !array2[1].Trim().Contains( "," ) ) {
									gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array2[1].Trim() );
								}
								else {
									string[] array17 = array2[1].Trim().Split( ',' );
									gameObject = SetupMesh( __instance, meshHolder, meshPosition, meshRotation, material, array17[0].Trim() );
									if( array17[1] == "HIDE" ) {
										list6.Add( gameObject );
									}
									else {
										list5.Add( gameObject.GetComponent<MeshFilter>() );
										list4.Add( GetMesh( __instance, array17[1].Trim() ) );
									}
								}
								gameObject.transform.SetParent( gameObject7.transform, worldPositionStays: false );
								gameObject.transform.localPosition = Vector3.zero;
								gameObject.transform.localRotation = Quaternion.Slerp( Quaternion.identity, Quaternion.Euler( meshRotation ), 1f );
								activeVessel.vesselai.enemyrbu.rbuLaunchers[num12] = gameObject.transform;
								GameObject gameObject8 = UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.blankTransform, meshHolder.position, Quaternion.identity ) as GameObject;
								gameObject8.transform.SetParent( gameObject.transform, worldPositionStays: false );
								gameObject8.transform.localPosition = Vector3.zero;
								gameObject8.transform.localRotation = Quaternion.identity;
								gameObject8.name = "muzzlehub";
								activeVessel.vesselai.enemyrbu.rbuHubs[num12] = gameObject8.transform;
								GameObject gameObject9 = UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.blankTransform, meshHolder.position, Quaternion.identity ) as GameObject;
								gameObject9.transform.SetParent( gameObject8.transform, worldPositionStays: false );
								gameObject9.transform.localPosition = new Vector3( UIFunctions.globaluifunctions.database.databasedepthchargedata[activeVessel.databaseshipdata.rbuLauncherTypes[num12]].firingPositions.x, 0f, UIFunctions.globaluifunctions.database.databasedepthchargedata[activeVessel.databaseshipdata.rbuLauncherTypes[num12]].firingPositions.y );
								gameObject9.transform.localRotation = Quaternion.identity;
								activeVessel.vesselai.enemyrbu.rbuLaunchPositions[num12] = gameObject9.transform;
								GameObject gameObject10 = UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.rbuLaunchFlare, gameObject8.transform.position, gameObject8.transform.rotation ) as GameObject;
								gameObject10.transform.SetParent( gameObject8.transform );
								gameObject10.transform.localPosition = Vector3.zero;
								gameObject10.transform.localRotation = Quaternion.Slerp( gameObject10.transform.localRotation, Quaternion.Euler( 0f, 180f, 0f ), 1f );
								activeVessel.vesselai.enemyrbu.rbuLaunchEffects[num12] = gameObject10.GetComponent<ParticleSystem>();
								num12++;
								break;
							}
						case "MeshRBUMount":
							gameObject = SetupMesh( __instance, activeVessel.vesselai.enemyrbu.rbuPositions[num12 - 1], meshPosition, meshRotation, material, array2[1].Trim() );
							break;
						case "BowWaveParticle": {
								GameObject gameObject6 = UnityEngine.Object.Instantiate( (GameObject) Resources.Load( array2[1].Trim() ), meshPosition, Quaternion.identity ) as GameObject;
								gameObject6.transform.SetParent( activeVessel.vesselmovement.bowwaveHolder.transform );
								gameObject6.transform.localPosition = Vector3.zero;
								gameObject6.transform.localRotation = Quaternion.identity;
								activeVessel.vesselmovement.bowwave = gameObject6.GetComponent<ParticleSystem>();
								gameObject6.layer = 28;
								break;
							}
						case "PropWashParticle": {
								GameObject gameObject5 = UnityEngine.Object.Instantiate( (GameObject) Resources.Load( array2[1].Trim() ), meshPosition, Quaternion.identity ) as GameObject;
								gameObject5.transform.SetParent( activeVessel.vesselmovement.wakeObject.transform );
								gameObject5.transform.localPosition = meshPosition;
								gameObject5.transform.localRotation = Quaternion.identity;
								activeVessel.vesselmovement.propwash = gameObject5.GetComponent<ParticleSystem>();
								gameObject5.layer = 28;
								break;
							}
						case "FunnelSmokeParticle": {
								GameObject gameObject4 = UnityEngine.Object.Instantiate( (GameObject) Resources.Load( array2[1].Trim() ), meshPosition, Quaternion.identity ) as GameObject;
								gameObject4.transform.SetParent( meshHolder.transform );
								gameObject4.transform.localPosition = meshPosition;
								gameObject4.transform.localRotation = Quaternion.identity;
								activeVessel.damagesystem.funnelSmoke = gameObject4.GetComponent<ParticleSystem>();
								break;
							}
						case "EmergencyBlowParticle": {
								GameObject gameObject3 = UnityEngine.Object.Instantiate( (GameObject) Resources.Load( array2[1].Trim() ), meshPosition, Quaternion.identity ) as GameObject;
								gameObject3.transform.SetParent( meshHolder.transform );
								gameObject3.transform.localPosition = meshPosition;
								gameObject3.transform.localRotation = Quaternion.identity;
								activeVessel.damagesystem.emergencyBlow = gameObject3.GetComponent<ParticleSystem>();
								gameObject3.GetComponent<AudioSource>().playOnAwake = false;
								break;
							}
						case "CavitationParticle": {
								GameObject gameObject2 = UnityEngine.Object.Instantiate( (GameObject) Resources.Load( array2[1].Trim() ), meshPosition, Quaternion.identity ) as GameObject;
								gameObject2.transform.SetParent( meshHolder.transform );
								gameObject2.transform.localPosition = meshPosition;
								gameObject2.transform.localRotation = Quaternion.identity;
								activeVessel.vesselmovement.cavBubbles = gameObject2.GetComponent<ParticleSystem>();
								break;
							}
						case "KelvinWaves": {
								Vector3 vector3 = UIFunctions.globaluifunctions.textparser.PopulateVector2( array2[1].Trim() );
								activeVessel.vesselmovement.kelvinWaveOverlay.width = vector3.x;
								activeVessel.vesselmovement.kelvinWaveOverlay.height = vector3.y;
								break;
							}
						case "ParticleBowWavePosition":
							if( array2[1].Trim() != "FALSE" ) {
								activeVessel.vesselmovement.bowwave.transform.parent.transform.localPosition = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
							}
							break;
						case "ParticlePropWashPosition":
							if( array2[1].Trim() != "FALSE" ) {
								activeVessel.vesselmovement.propwash.transform.localPosition = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
							}
							break;
						case "ParticleHullFoamPosition":
							if( array2[1].Trim() != "FALSE" ) {
								activeVessel.vesselmovement.foamTrails[0].transform.localPosition = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
							}
							else {
								UnityEngine.Object.Destroy( activeVessel.vesselmovement.foamTrails[0].gameObject );
							}
							break;
						case "ParticleHullFoamParameters": {
								string[] array14 = array2[1].Trim().Split( ',' );
								activeVessel.vesselmovement.foamTrails[0].duration = float.Parse( array14[0] );
								activeVessel.vesselmovement.foamTrails[0].size = float.Parse( array14[1] );
								activeVessel.vesselmovement.foamTrails[0].spacing = float.Parse( array14[2] );
								activeVessel.vesselmovement.foamTrails[0].expansion = float.Parse( array14[3] );
								activeVessel.vesselmovement.foamTrails[0].momentum = float.Parse( array14[4] );
								activeVessel.vesselmovement.foamTrails[0].spin = float.Parse( array14[5] );
								activeVessel.vesselmovement.foamTrails[0].jitter = float.Parse( array14[6] );
								activeVessel.vesselmovement.submarineFoamDurations[0] = activeVessel.vesselmovement.foamTrails[0].duration;
								break;
							}
						case "ParticleSternFoamPosition":
							activeVessel.vesselmovement.foamTrails[1].transform.localPosition = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
							break;
						case "ParticleSternFoamParameters": {
								string[] array13 = array2[1].Trim().Split( ',' );
								activeVessel.vesselmovement.foamTrails[1].duration = float.Parse( array13[0] );
								activeVessel.vesselmovement.foamTrails[1].size = float.Parse( array13[1] );
								activeVessel.vesselmovement.foamTrails[1].spacing = float.Parse( array13[2] );
								activeVessel.vesselmovement.foamTrails[1].expansion = float.Parse( array13[3] );
								activeVessel.vesselmovement.foamTrails[1].momentum = float.Parse( array13[4] );
								activeVessel.vesselmovement.foamTrails[1].spin = float.Parse( array13[5] );
								activeVessel.vesselmovement.foamTrails[1].jitter = float.Parse( array13[6] );
								activeVessel.vesselmovement.submarineFoamDurations[1] = activeVessel.vesselmovement.foamTrails[1].duration;
								break;
							}
						case "EngineAudioClip":
							activeVessel.vesselmovement.engineSound.clip = UIFunctions.globaluifunctions.textparser.GetAudioClip( array2[1].Trim() );
							break;
						case "EngineAudioRollOff":
							if( array2[1].Trim() != "LOGARITHMIC" ) {
								activeVessel.vesselmovement.engineSound.rolloffMode = AudioRolloffMode.Linear;
							}
							else {
								activeVessel.vesselmovement.engineSound.rolloffMode = AudioRolloffMode.Logarithmic;
							}
							break;
						case "EngineAudioDistance": {
								string[] array10 = array2[1].Trim().Split( ',' );
								activeVessel.vesselmovement.engineSound.minDistance = float.Parse( array10[0] );
								activeVessel.vesselmovement.engineSound.maxDistance = float.Parse( array10[1] );
								break;
							}
						case "EngineAudioPitchRange":
							activeVessel.vesselmovement.enginePitchRange = UIFunctions.globaluifunctions.textparser.PopulateVector2( array2[1].Trim() );
							break;
						case "PropAudioClip": {
								activeVessel.vesselmovement.propSound.clip = UIFunctions.globaluifunctions.textparser.GetAudioClip( array2[1].Trim() );
								Transform transform3 = activeVessel.vesselmovement.propSound.transform;
								Vector3 localPosition2 = activeVessel.vesselmovement.props[0].transform.localPosition;
								transform3.localPosition = new Vector3( 0f, 0f, localPosition2.z );
								break;
							}
						case "PropAudioRollOff":
							if( array2[1].Trim() != "LOGARITHMIC" ) {
								activeVessel.vesselmovement.propSound.rolloffMode = AudioRolloffMode.Linear;
							}
							else {
								activeVessel.vesselmovement.propSound.rolloffMode = AudioRolloffMode.Logarithmic;
							}
							break;
						case "PropAudioDistance": {
								string[] array8 = array2[1].Trim().Split( ',' );
								activeVessel.vesselmovement.propSound.minDistance = float.Parse( array8[0] );
								activeVessel.vesselmovement.propSound.maxDistance = float.Parse( array8[1] );
								break;
							}
						case "PropAudioPitchRange":
							activeVessel.vesselmovement.propPitchRange = UIFunctions.globaluifunctions.textparser.PopulateVector2( array2[1].Trim() );
							break;
						case "PingAudioClip": {
								activeVessel.vesselmovement.pingSound.enabled = true;
								activeVessel.vesselmovement.pingSound.clip = UIFunctions.globaluifunctions.textparser.GetAudioClip( array2[1].Trim() );
								activeVessel.vesselmovement.pingSound.loop = false;
								Transform transform2 = activeVessel.vesselmovement.pingSound.transform;
								Vector3 localPosition = activeVessel.vesselmovement.bowwaveHolder.transform.localPosition;
								transform2.localPosition = new Vector3( 0f, 0f, localPosition.z );
								break;
							}
						case "PingAudioRollOff":
							if( array2[1].Trim() != "LOGARITHMIC" ) {
								activeVessel.vesselmovement.pingSound.rolloffMode = AudioRolloffMode.Linear;
							}
							else {
								activeVessel.vesselmovement.pingSound.rolloffMode = AudioRolloffMode.Logarithmic;
							}
							break;
						case "PingAudioDistance": {
								string[] array6 = array2[1].Trim().Split( ',' );
								activeVessel.vesselmovement.pingSound.minDistance = float.Parse( array6[0] );
								activeVessel.vesselmovement.pingSound.maxDistance = float.Parse( array6[1] );
								break;
							}
						case "PingAudioPitch":
							activeVessel.vesselmovement.pingSound.pitch = float.Parse( array2[1].Trim() );
							break;
						case "BowwaveAudioClip":
							activeVessel.vesselmovement.bowwaveSound.enabled = true;
							activeVessel.vesselmovement.bowwaveSound.clip = UIFunctions.globaluifunctions.textparser.GetAudioClip( array2[1].Trim() );
							activeVessel.vesselmovement.bowwaveSound.loop = true;
							break;
						case "BowwaveAudioRollOff":
							if( array2[1].Trim() != "LOGARITHMIC" ) {
								activeVessel.vesselmovement.bowwaveSound.rolloffMode = AudioRolloffMode.Linear;
							}
							else {
								activeVessel.vesselmovement.bowwaveSound.rolloffMode = AudioRolloffMode.Logarithmic;
							}
							break;
						case "BowwaveAudioDistance": {
								string[] array3 = array2[1].Trim().Split( ',' );
								activeVessel.vesselmovement.bowwaveSound.minDistance = float.Parse( array3[0] );
								activeVessel.vesselmovement.bowwaveSound.maxDistance = float.Parse( array3[1] );
								break;
							}
						case "BowwaveAudioPitch":
							activeVessel.vesselmovement.bowwaveSound.pitch = float.Parse( array2[1].Trim() );
							break;
					}
				}
				activeVessel.vesselmovement.rudder = list.ToArray();
				if( !activeVessel.playercontrolled && activeVessel.vesselai.hasTorpedo ) {
					activeVessel.vesselai.enemytorpedo.launcherPositions = new int[activeVessel.vesselai.enemytorpedo.torpedoMounts.Length];
					if( !activeVessel.vesselai.enemytorpedo.fixedTubes ) {
						for( int j = 0; j < activeVessel.vesselai.enemytorpedo.torpedoMounts.Length; j++ ) {
							Vector3 localPosition4 = activeVessel.vesselai.enemytorpedo.torpedoMounts[j].localPosition;
							if( localPosition4.x < 0f ) {
								activeVessel.vesselai.enemytorpedo.launcherPositions[j] = -1;
								continue;
							}
							Vector3 localPosition5 = activeVessel.vesselai.enemytorpedo.torpedoMounts[j].localPosition;
							if( localPosition5.x > 0f ) {
								activeVessel.vesselai.enemytorpedo.launcherPositions[j] = 1;
							}
							else {
								activeVessel.vesselai.enemytorpedo.launcherPositions[j] = 0;
							}
						}
					}
				}
				if( activeVessel.databaseshipdata.shipType != "BIOLOGIC" && activeVessel.databaseshipdata.shipType != "OILRIG" ) {
					activeVessel.damagesystem.hullDamageMeshes = new Mesh[10];
					activeVessel.damagesystem.hullDamageMeshes[0] = GetMesh(__instance, str + "_damage_11" );
					activeVessel.damagesystem.hullDamageMeshes[1] = GetMesh(__instance, str + "_damage_12" );
					activeVessel.damagesystem.hullDamageMeshes[2] = GetMesh(__instance, str + "_damage_21" );
					activeVessel.damagesystem.hullDamageMeshes[3] = GetMesh(__instance, str + "_damage_22" );
					activeVessel.damagesystem.hullDamageMeshes[4] = GetMesh(__instance, str + "_damage_31" );
					activeVessel.damagesystem.hullDamageMeshes[5] = GetMesh(__instance, str + "_damage_32" );
					activeVessel.damagesystem.hullDamageMeshes[6] = GetMesh(__instance, str + "_damage_41" );
					activeVessel.damagesystem.hullDamageMeshes[7] = GetMesh(__instance, str + "_damage_42" );
					activeVessel.damagesystem.hullDamageMeshes[8] = GetMesh(__instance, str + "_damage_51" );
					activeVessel.damagesystem.hullDamageMeshes[9] = GetMesh( __instance, str + "_damage_52" );
				}
				activeVessel.damagesystem.damageMeshFilters = list5.ToArray();
				activeVessel.damagesystem.damageMeshes = list4.ToArray();
				activeVessel.damagesystem.objectMeshesToHide = list6.ToArray();
				activeVessel.damagesystem.radars = list3.ToArray();
				if( list2.Count > 0 ) {
					activeVessel.superstructureColliders = list2.ToArray();
				}
				meshHolder = null;
				material = null;
				gameObject = null;
				return false;
			}

			private static GameObject SetupMesh( VesselBuilder veselBuilder, Transform vesselMesholder, Vector3 meshPosition, Vector3 meshRotation, Material meshMaterial, string meshName ) {
				GameObject gameObject = UnityEngine.Object.Instantiate( (GameObject) Resources.Load( "template_objects/meshTemplate" ), vesselMesholder.position, Quaternion.identity ) as GameObject;
				gameObject.transform.SetParent( vesselMesholder, worldPositionStays: false );
				gameObject.transform.localPosition = meshPosition;
				gameObject.transform.localRotation = Quaternion.Slerp( Quaternion.identity, Quaternion.Euler( meshRotation ), 1f );
				gameObject.GetComponent<MeshRenderer>().sharedMaterial = meshMaterial;
				Mesh mesh = GetMesh( veselBuilder, meshName );
				gameObject.GetComponent<MeshFilter>().mesh = mesh;
				gameObject.name = meshName;
				return gameObject;
			}

			private static Mesh GetMesh( VesselBuilder veselBuilder, string meshName ) {
				if( meshName.Length > 0 ) {
					foreach( Mesh mesh in patcher.moddedMeshList ) {
						if( mesh.name == meshName ) {
							Debug.Log( "Modded MESH found " + meshName );
							return mesh;
						}
					}
					for( int i = 0; i < veselBuilder.allMeshes.Length; i++ ) {
						if( veselBuilder.allMeshes[i].name == meshName ) {
							return veselBuilder.allMeshes[i];
						}
					}
					Debug.Log( "MESH not found " + meshName );
					return null;
				}
				return null;
			}

			private static Material GetMaterial( string materialPathName ) {
				Debug.Log(materialPathName);
				if( materialPathName.Contains(".mtl") ) {
					return new MatImporter( Resources.Load( "ships/usn_ssn_skipjack/usn_ssn_skipjack_mat" ) as Material, Application.streamingAssetsPath + "/override/" + materialPathName ).GetMaterial();
				}
				return Resources.Load( materialPathName ) as Material;
			}
		}
    }
}