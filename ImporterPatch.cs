using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Reflection;
using BepInEx;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using System.Linq;
using UnityEngine.SocialPlatforms;

namespace Cold_Waters_Expanded
{
	[BepInPlugin( "org.cwe.plugins.import", "Cold Waters Expanded Import Patches", "1.0.0.4" )]
	public class ImporterPatch : BaseUnityPlugin
	{

		static ImporterPatch patcher;

		List<Vessel> customVessels = new List<Vessel>();

		void Awake() {
			patcher = this;
		}

		static Mesh[] GetModel( string modelPath ) {
			if( modelPath.Length > 0 && modelPath.Contains( ".gltf" ) ) {
				Debug.Log( "Loading custom model: " + modelPath );
				return glTFImporter.GetMeshes( Application.streamingAssetsPath + "/override/" + modelPath.Trim() );
			}
			else if( modelPath.Length > 0 ) {
				return Resources.LoadAll<Mesh>( modelPath.Trim() );
			}
			else {
				Debug.Log( "Cannot process the file format specified at: " + modelPath );
				return null;
			}
		}

		static Material GetMaterial( string materialPathName ) {
			if( materialPathName == "FALSE" ) {
				//Shader shader = Shader.Find( shaderReference );
				//Material material = new Material( shader );
				Material material = new Material( Resources.Load( "ships/usn_ssn_skipjack/usn_ssn_skipjack_mat" ) as Material );
				material.SetTexture( "_MainTex", null );
				material.SetTexture( "_SpecTex", null );
				material.SetTexture( "_BumpMap", null );
				return material;
			}
			else if( materialPathName.Contains( ".mtl" ) ) {
				try {
					//Material material = new Material( Resources.Load( "ships/usn_ssn_skipjack/usn_ssn_skipjack_mat" ) as Material );
					//Debug.Log( Application.streamingAssetsPath + "/override/" + materialPathName );
					string json = File.ReadAllText( Application.streamingAssetsPath + "/override/" + materialPathName );
					//Debug.Log( json );
					Material material = JsonUtility.FromJson<SerialiseMaterial>( json ).GetMaterial();
					Debug.Log( material.ToString() );
					return material;
				}
				catch( Exception e ) {
					Debug.Log( e.ToString() );
					//Shader shader = Shader.Find( "Legacy Shaders/Transparent/Bumped Specular" );
					//Material material = new Material( shader );
					Material material = new Material( Resources.Load( "ships/usn_ssn_skipjack/usn_ssn_skipjack_mat" ) as Material );
					material.SetTexture( "_MainTex", null );
					material.SetTexture( "_SpecTex", null );
					material.SetTexture( "_BumpMap", null );
					return material;
				}
				
			}
			return Resources.Load( materialPathName ) as Material;
		}

		[HarmonyPatch( typeof( VesselBuilder ), "CreateAndPlaceMeshes" )]
		public class VesselBuilder_CreateAndPlaceMeshes_Patch
		{
			[HarmonyPrefix]
			public static bool Prefix( VesselBuilder __instance, GameObject vesselTemplate, Vessel activeVessel, bool playerControlled, string vesselPrefabRef ) {
				// Check if applying custom logic
				string filename = Path.Combine( "vessels", vesselPrefabRef );
				string[] array = UIFunctions.globaluifunctions.textparser.OpenTextDataFile( filename );
				bool customSurfaceShip = false;
				foreach( var line in array ) {
					switch( line.Split( '=' )[0] ) {
						case "ShipType":
							if( line.Split( '=' )[1].Trim() != "SUBMARINE" ) {
								customSurfaceShip = true;
							}
							break;
						case "ModelFile":
							if( !line.Split( '=' )[1].Trim().Contains( "." ) ) {
								return true; //break out to the normal method at this point if it isn't a custom model file
							}
							break;
						default:
							break;
					}
				}
				Debug.Log( "Custom Vessel: " + vesselPrefabRef );
				patcher.customVessels.Add( activeVessel );
				// Continue with custom logic
				Transform meshHolder = activeVessel.meshHolder;
				Vector3 meshPosition = Vector3.zero;
				Vector3 meshRotation = Vector3.zero;
				Vector3 ciwsBarrelOffset = Vector3.zero;
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
					if( activeVessel.databaseshipdata.torpedoConfig != null ) {
						activeVessel.vesselmovement.weaponSource.torpedoTubes = new Transform[activeVessel.databaseshipdata.torpedoConfig.Length];
						activeVessel.vesselmovement.weaponSource.tubeParticleEffects = new Vector3[activeVessel.databaseshipdata.torpedoConfig.Length];
					}
					activeVessel.playercontrolled = true;
					if( customSurfaceShip ) {
						activeVessel.isSubmarine = false;
						activeVessel.vesselmovement.isSubmarine = false;
					}
					else {
						activeVessel.isSubmarine = true;
						activeVessel.vesselmovement.isSubmarine = true;
						activeVessel.vesselmovement.planes = new Transform[2];
					}
					activeVessel.vesselmovement.telegraphValue = 2;
				}
				int countProps = 0;
				activeVessel.vesselmovement.props = new Transform[0];
				if( activeVessel.databaseshipdata.proprotationspeed.Length > 0 ) {
					activeVessel.vesselmovement.props = new Transform[activeVessel.databaseshipdata.proprotationspeed.Length];
				}
				List<Transform> listRudderTransforms = new List<Transform>();
				int countMasts = 0;
				float speed = 0f;
				float num7 = 1f;
				int num8 = 0;
				int num9 = 0;
				int countCIWSTurrets = 0;
				int countCIWSRadar = 0;
				int num12 = 0;
				int gunTurretIndex = 0;
				List<MeshCollider> listMeshColliders = new List<MeshCollider>();
				List<Radar> list3 = new List<Radar>();
				List<Mesh> damageMeshes = new List<Mesh>();
				List<MeshFilter> damageMeshFilters = new List<MeshFilter>();
				List<GameObject> hiddenObjectsList = new List<GameObject>();
				activeVessel.vesselmovement.submarineFoamDurations = new float[2];
				string str = string.Empty;
				bool flag = false;
				for( int i = 0; i < array.Length; i++ ) {
					string[] dataLineArray = array[i].Split( '=' );
					if( dataLineArray[0].Trim() == "[Model]" ) {
						flag = true;
					}
					if( !flag ) {
						continue;
					}
					switch( dataLineArray[0] ) {
						case "ModelFile": {
								__instance.allMeshes = GetModel( dataLineArray[1].Trim() );
								if( !dataLineArray[1].Trim().Contains( "." ) ) {
									return true; //break out to the normal method at this point if it isn't a custom model file
								}
								string[] array15 = dataLineArray[1].Trim().Split( '/' );
								str = array15[array15.Length - 1].Replace(".gltf","");
								break;
							}
						case "MeshPosition":
							meshPosition = UIFunctions.globaluifunctions.textparser.PopulateVector3( dataLineArray[1].Trim() );
							break;
						case "MeshRotation":
							meshRotation = UIFunctions.globaluifunctions.textparser.PopulateVector3( dataLineArray[1].Trim() );
							break;
						case "Material":
							material = GetMaterial( dataLineArray[1].Trim() );
							break;
						case "MaterialTextures":
							if( material != null ) {
								string[] array28 = dataLineArray[1].Trim().Split( ',' );
								if( array28.Length > 0 ) {
									material.SetTexture( "_MainTex", UIFunctions.globaluifunctions.textparser.GetTexture( array28[0] ) );
									material.SetTexture( "_SpecTex", null );
									material.SetTexture( "_BumpMap", null );
								}
								if( array28.Length > 1 ) {
									material.SetTexture( "_SpecTex", UIFunctions.globaluifunctions.textparser.GetTexture( array28[1] ) );
									material.SetTexture( "_BumpMap", null );
								}
								if( array28.Length > 2 ) {
									material.SetTexture( "_BumpMap", UIFunctions.globaluifunctions.textparser.GetTexture( array28[2] ) );
								}
							}
							break;
						case "Mesh":
							if( !dataLineArray[1].Trim().Contains( "," ) ) {
								//gameObject = SetupMesh( __instance, true, meshHolder, meshPosition, meshRotation, material, dataLineArray[1].Trim() );
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, dataLineArray[1].Trim() } ).GetValue<GameObject>();
							}
							else {
								// Damage Meshes
								string[] array27 = dataLineArray[1].Trim().Split( ',' );
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, array27[0].Trim() } ).GetValue<GameObject>();
								if( array27[1] == "HIDE" ) {
									hiddenObjectsList.Add( gameObject );
								}
								else {
									damageMeshFilters.Add( gameObject.GetComponent<MeshFilter>() );
									damageMeshes.Add( Traverse.Create( __instance ).Method( "GetMesh", array27[1].Trim() ).GetValue<Mesh>() );
								}
							}
							if( gameObject.name.Contains( "biologic" ) ) {
								gameObject.GetComponent<MeshRenderer>().receiveShadows = false;
								gameObject.transform.parent.parent.gameObject.AddComponent<Whale_AI>().parentVessel = activeVessel;
							}
							break;
						case "MeshLights":
							if( !GameDataManager.isNight ) {
								break;
							}
							if( !dataLineArray[1].Trim().Contains( "," ) ) {
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, dataLineArray[1].Trim() } ).GetValue<GameObject>();
								break;
							}
							string[] array16 = dataLineArray[1].Trim().Split( ',' );
							gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, array16[0].Trim() } ).GetValue<GameObject>();
							if( array16[1] == "HIDE" ) {
								hiddenObjectsList.Add( gameObject );
								break;
							}
							damageMeshFilters.Add( gameObject.GetComponent<MeshFilter>() );
							damageMeshes.Add( Traverse.Create( __instance ).Method( "GetMesh", array16[1].Trim() ).GetValue<Mesh>() );
							break;
						case "MeshHullCollider":
							activeVessel.hullCollider.sharedMesh = Traverse.Create( __instance ).Method( "GetMesh", dataLineArray[1].Trim() ).GetValue<Mesh>();
							break;
						case "MeshSuperstructureCollider":
							GameObject goSuperstructureCollider = UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.blankTransform, meshHolder.position, Quaternion.identity ) as GameObject;
							goSuperstructureCollider.transform.SetParent( meshHolder );
							goSuperstructureCollider.transform.localPosition = Vector3.zero;
							goSuperstructureCollider.transform.localRotation = Quaternion.identity;
							MeshCollider meshCollider = goSuperstructureCollider.AddComponent<MeshCollider>();
							meshCollider.convex = true;
							meshCollider.isTrigger = true;
							meshCollider.sharedMesh = Traverse.Create( __instance ).Method( "GetMesh", dataLineArray[1].Trim() ).GetValue<Mesh>();
							listMeshColliders.Add( meshCollider );
							break;
						case "MeshHullNumber":
							if( !dataLineArray[1].Trim().Contains( "," ) ) {
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, dataLineArray[1].Trim() } ).GetValue<GameObject>();
							}
							else {
								string[] array19 = dataLineArray[1].Trim().Split( ',' );
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, array19[0].Trim() } ).GetValue<GameObject>();
								if( array19[1] == "HIDE" ) {
									hiddenObjectsList.Add( gameObject );
								}
								else {
									damageMeshFilters.Add( gameObject.GetComponent<MeshFilter>() );
									damageMeshes.Add( Traverse.Create( __instance ).Method( "GetMesh", array19[1].Trim() ).GetValue<Mesh>() );
								}
							}
							activeVessel.vesselmovement.hullNumberRenderer = gameObject.GetComponent<MeshRenderer>();
							int num14 = UnityEngine.Random.Range( 0, activeVessel.databaseshipdata.hullnumbers.Length );
							string texturePath = "ships/materials/hullnumbers/" + activeVessel.databaseshipdata.hullnumbers[num14];
							Material material2 = activeVessel.vesselmovement.hullNumberRenderer.material;
							material2.SetTexture( "_MainTex", UIFunctions.globaluifunctions.textparser.GetTexture( texturePath ) );
							break;
						case "MeshRudder":
							if( !dataLineArray[1].Trim().Contains( "," ) ) {
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, dataLineArray[1].Trim() } ).GetValue<GameObject>();
							}
							else {
								// Damage Meshes
								string[] array29 = dataLineArray[1].Trim().Split( ',' );
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, array29[0].Trim() } ).GetValue<GameObject>();
								if( array29[1] == "HIDE" ) {
									hiddenObjectsList.Add( gameObject );
								}
								else {
									damageMeshFilters.Add( gameObject.GetComponent<MeshFilter>() );
									damageMeshes.Add( Traverse.Create( __instance ).Method( "GetMesh", array29[1].Trim() ).GetValue<Mesh>() );
								}
							}
							listRudderTransforms.Add( gameObject.transform );
							break;
						case "MeshProp":
							if( !dataLineArray[1].Trim().Contains( "," ) ) {
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, dataLineArray[1].Trim() } ).GetValue<GameObject>();
							}
							else {
								// Damage Meshes
								string[] array11 = dataLineArray[1].Trim().Split( ',' );
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, array11[0].Trim() } ).GetValue<GameObject>();
								if( array11[1] == "HIDE" ) {
									hiddenObjectsList.Add( gameObject );
								}
								else {
									damageMeshFilters.Add( gameObject.GetComponent<MeshFilter>() );
									damageMeshes.Add( Traverse.Create( __instance ).Method( "GetMesh", array11[1].Trim() ).GetValue<Mesh>() );
								}
							}
							activeVessel.vesselmovement.props[countProps] = gameObject.transform;
							countProps++;
							break;
						case "MeshBowPlanes":
							if( !dataLineArray[1].Trim().Contains( "," ) ) {
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, dataLineArray[1].Trim() } ).GetValue<GameObject>();
							}
							else {
								// Damage Meshes
								string[] array23 = dataLineArray[1].Trim().Split( ',' );
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, array23[0].Trim() } ).GetValue<GameObject>();
								if( array23[1] == "HIDE" ) {
									hiddenObjectsList.Add( gameObject );
								}
								else {
									damageMeshFilters.Add( gameObject.GetComponent<MeshFilter>() );
									damageMeshes.Add( Traverse.Create( __instance ).Method( "GetMesh", array23[1].Trim() ).GetValue<Mesh>() );
								}
							}
							activeVessel.vesselmovement.planes[0] = gameObject.transform;
							break;
						case "MeshSternPlanes":
							if( !dataLineArray[1].Trim().Contains( "," ) ) {
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, dataLineArray[1].Trim() } ).GetValue<GameObject>();
							}
							else {
								// Damage Meshes
								string[] array21 = dataLineArray[1].Trim().Split( ',' );
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, array21[0].Trim() } ).GetValue<GameObject>();
								if( array21[1] == "HIDE" ) {
									hiddenObjectsList.Add( gameObject );
								}
								else {
									damageMeshFilters.Add( gameObject.GetComponent<MeshFilter>() );
									damageMeshes.Add( Traverse.Create( __instance ).Method( "GetMesh", array21[1].Trim() ).GetValue<Mesh>() );
								}
							}
							activeVessel.vesselmovement.planes[1] = gameObject.transform;
							break;
						case "MastHeight":
							if( playerControlled ) {
								float y = float.Parse( dataLineArray[1].Trim() );
								activeVessel.submarineFunctions.peiscopeStops[countMasts].y = y;
								activeVessel.submarineFunctions.mastHeads[countMasts].transform.localPosition = new Vector3( 0f, y, 0f );
							}
							break;
						case "MeshMast":
							if( playerControlled ) {
								if( !dataLineArray[1].Trim().Contains( "," ) ) {
									activeVessel.submarineFunctions.mastTransforms[countMasts].GetComponent<MeshFilter>().mesh = Traverse.Create( __instance ).Method( "GetMesh", dataLineArray[1].Trim() ).GetValue<Mesh>();
								}
								else {
									string[] array25 = dataLineArray[1].Trim().Split( ',' );
									activeVessel.submarineFunctions.mastTransforms[countMasts].GetComponent<MeshFilter>().mesh = Traverse.Create( __instance ).Method( "GetMesh", array25[0].Trim() ).GetValue<Mesh>();
									if( array25[1] == "HIDE" ) {
										hiddenObjectsList.Add( activeVessel.submarineFunctions.mastTransforms[countMasts].gameObject );
									}
									else {
										damageMeshFilters.Add( activeVessel.submarineFunctions.mastTransforms[countMasts].GetComponent<MeshFilter>() );
										damageMeshes.Add( Traverse.Create( __instance ).Method( "GetMesh", array25[1].Trim() ).GetValue<Mesh>() );
									}
								}
								activeVessel.submarineFunctions.mastTransforms[countMasts].GetComponent<MeshRenderer>().sharedMaterial = material;
								activeVessel.submarineFunctions.mastTransforms[countMasts].localPosition = meshPosition;
								activeVessel.submarineFunctions.mastTransforms[countMasts].localRotation = Quaternion.identity;
								gameObject = activeVessel.submarineFunctions.mastTransforms[countMasts].gameObject;
								activeVessel.submarineFunctions.mastTransforms[countMasts] = gameObject.transform;
								ref Vector2 reference4 = ref activeVessel.submarineFunctions.peiscopeStops[countMasts];
								Vector3 localPosition3 = gameObject.transform.localPosition;
								reference4.x = localPosition3.y;
								activeVessel.submarineFunctions.peiscopeStops[countMasts].y += activeVessel.submarineFunctions.peiscopeStops[countMasts].x;
								countMasts++;
							}
							break;
						case "ChildMesh":
							Transform parentTransform = gameObject.transform;
							if( !dataLineArray[1].Trim().Contains( "," ) ) {
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, dataLineArray[1].Trim() } ).GetValue<GameObject>();
							}
							else {
								// Damage Meshes
								string[] meshList = dataLineArray[1].Trim().Split( ',' );
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, meshList[0].Trim() } ).GetValue<GameObject>();
								if( meshList[1] == "HIDE" ) {
									hiddenObjectsList.Add( gameObject );
								}
								else {
									damageMeshFilters.Add( gameObject.GetComponent<MeshFilter>() );
									damageMeshes.Add( Traverse.Create( __instance ).Method( "GetMesh", meshList[1].Trim() ).GetValue<Mesh>() );
								}
							}
							gameObject.transform.SetParent( parentTransform, worldPositionStays: false );
							gameObject.transform.localPosition = meshPosition;
							gameObject.transform.localRotation = Quaternion.Slerp( Quaternion.identity, Quaternion.Euler( meshRotation ), 1f );
							break;
						case "MeshMainFlag":
							Debug.Log( "Warning: MeshMainFlag is Experimental." );
							if( !dataLineArray[1].Trim().Contains( "," ) ) {
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, dataLineArray[1].Trim() } ).GetValue<GameObject>();
							}
							else {
								string[] array7 = dataLineArray[1].Trim().Split( ',' );
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, array7[0].Trim() } ).GetValue<GameObject>();
								if( array7[1] == "HIDE" ) {
									hiddenObjectsList.Add( gameObject );
								}
								else {
									damageMeshFilters.Add( gameObject.GetComponent<MeshFilter>() );
									damageMeshes.Add( Traverse.Create( __instance ).Method( "GetMesh", array7[1].Trim() ).GetValue<Mesh>() );
								}
							}
							material.color = Environment.whiteLevel;
							gameObject.layer = 17;
							activeVessel.vesselmovement.flagRenderer = gameObject.GetComponent<MeshRenderer>();
							break;
						case "MeshOtherFlags":
							Debug.Log( "Warning: MeshOtherFlags is Experimental." );
							if( !dataLineArray[1].Trim().Contains( "," ) ) {
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, dataLineArray[1].Trim() } ).GetValue<GameObject>();
							}
							else {
								string[] array4 = dataLineArray[1].Trim().Split( ',' );
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, array4[0].Trim() } ).GetValue<GameObject>();
								if( array4[1] == "HIDE" ) {
									hiddenObjectsList.Add( gameObject );
								}
								else {
									damageMeshFilters.Add( gameObject.GetComponent<MeshFilter>() );
									damageMeshes.Add( Traverse.Create( __instance ).Method( "GetMesh", array4[1].Trim() ).GetValue<Mesh>() );
								}
							}
							material.color = Environment.whiteLevel;
							gameObject.layer = 17;
							break;
						case "RADARSpeed":
							speed = float.Parse( dataLineArray[1].Trim() );
							break;
						case "RADARDirection":
							num7 = float.Parse( dataLineArray[1].Trim() );
							break;
						case "MeshRADAR":
							if( !dataLineArray[1].Trim().Contains( "," ) ) {
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, dataLineArray[1].Trim() } ).GetValue<GameObject>();
							}
							else {
								string[] array20 = dataLineArray[1].Trim().Split( ',' );
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, array20[0].Trim() } ).GetValue<GameObject>();
								if( array20[1] == "HIDE" ) {
									hiddenObjectsList.Add( gameObject );
								}
								else {
									damageMeshFilters.Add( gameObject.GetComponent<MeshFilter>() );
									damageMeshes.Add( Traverse.Create( __instance ).Method( "GetMesh", array20[1].Trim() ).GetValue<Mesh>() );
								}
							}
							Radar radar = gameObject.AddComponent<Radar>();
							radar.speed = speed;
							list3.Add( radar );
							break;
						case "MeshNoisemakerMount":
							if( activeVessel.vesselai != null ) {
								activeVessel.vesselai.enemynoisemaker.noisemakerTubes.transform.localPosition = meshPosition;
							}
							else {
								activeVessel.vesselmovement.weaponSource.noisemakerTubes.transform.localPosition = meshPosition;
							}
							break;
						case "MeshTorpedoMount":
							Debug.Log( "Warning: MeshTorpedoMount is Experimental and may be broken." );
							if( !dataLineArray[1].Trim().Contains( "," ) ) {
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, dataLineArray[1].Trim() } ).GetValue<GameObject>();
							}
							else {
								string[] array9 = dataLineArray[1].Trim().Split( ',' );
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, array9[0].Trim() } ).GetValue<GameObject>();
								if( array9[1] == "HIDE" ) {
									hiddenObjectsList.Add( gameObject );
								}
								else {
									damageMeshFilters.Add( gameObject.GetComponent<MeshFilter>() );
									damageMeshes.Add( Traverse.Create( __instance ).Method( "GetMesh", array9[1].Trim() ).GetValue<Mesh>() );
								}
							}
							activeVessel.vesselai.enemytorpedo.torpedoMounts[num8] = gameObject.transform;
							break;
						case "TorpedoSpawnPosition":
							Debug.Log( "Warning: TorpedoSpawnPosition is Experimental and may be broken." );
							GameObject gameObject13 = UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.blankTransform, meshHolder.position, Quaternion.identity ) as GameObject;
							if( !activeVessel.playercontrolled ) {
								if( activeVessel.vesselai.enemytorpedo != null ) {
									if( !activeVessel.vesselai.enemytorpedo.fixedTubes ) {
										gameObject13.transform.SetParent( gameObject.transform, worldPositionStays: false );
										gameObject13.transform.localPosition = UIFunctions.globaluifunctions.textparser.PopulateVector3( dataLineArray[1].Trim() );
										gameObject13.transform.localRotation = Quaternion.identity;
									}
									else {
										gameObject13.transform.SetParent( meshHolder.transform, worldPositionStays: false );
										gameObject13.transform.localPosition = UIFunctions.globaluifunctions.textparser.PopulateVector3( dataLineArray[1].Trim() );
										gameObject13.transform.localRotation = Quaternion.Slerp( Quaternion.identity, Quaternion.Euler( meshRotation ), 1f );
									}
								}
							}
							else {
								gameObject13.transform.SetParent( meshHolder.transform, worldPositionStays: false );
								gameObject13.transform.localPosition = UIFunctions.globaluifunctions.textparser.PopulateVector3( dataLineArray[1].Trim() );
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
						case "TorpedoEffectPosition":
							Debug.Log( "Warning: TorpedoEffectPosition is Experimental and may be broken." );
							if( activeVessel.vesselai != null ) {
								ref Vector3 reference2 = ref activeVessel.vesselai.enemytorpedo.torpedoParticlePositions[num8];
								reference2 = UIFunctions.globaluifunctions.textparser.PopulateVector3( dataLineArray[1].Trim() );
							}
							else {
								ref Vector3 reference3 = ref activeVessel.vesselmovement.weaponSource.tubeParticleEffects[num8];
								reference3 = UIFunctions.globaluifunctions.textparser.PopulateVector3( dataLineArray[1].Trim() );
							}
							num8++;
							break;
						case "MeshMissileMount":
							Debug.Log( "Warning: MeshMissileMount is Experimental and may be broken." );
							if( !dataLineArray[1].Trim().Contains( "," ) ) {
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, dataLineArray[1].Trim() } ).GetValue<GameObject>();
							}
							else {
								string[] array18 = dataLineArray[1].Trim().Split( ',' );
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, array18[0].Trim() } ).GetValue<GameObject>();
								if( array18[1] == "HIDE" ) {
									hiddenObjectsList.Add( gameObject );
								}
								else {
									damageMeshFilters.Add( gameObject.GetComponent<MeshFilter>() );
									damageMeshes.Add( Traverse.Create( __instance ).Method( "GetMesh", array18[1].Trim() ).GetValue<Mesh>() );
								}
							}
							gameObject.name = "missileLauncher";
							activeVessel.vesselai.enemymissile.missileLaunchers[num9] = gameObject.transform;
							break;
						case "MissileEffectPosition": {
								Debug.Log( "Warning: MeshMissileMount are Experimental and may be broken." );
								ref Vector3 reference = ref activeVessel.vesselai.enemymissile.missileLaunchParticlePositions[num9];
								reference = UIFunctions.globaluifunctions.textparser.PopulateVector3( dataLineArray[1].Trim() );
								num9++;
								break;
							}
						case "MeshNavalGun":
							if( !dataLineArray[1].Trim().Contains( "," ) ) {
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, dataLineArray[1].Trim() } ).GetValue<GameObject>();
							}
							else {
								// Damage Meshes
								string[] array12 = dataLineArray[1].Trim().Split( ',' );
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, array12[0].Trim() } ).GetValue<GameObject>();
								if( array12[1] == "HIDE" ) {
									hiddenObjectsList.Add( gameObject );
								}
								else {
									damageMeshFilters.Add( gameObject.GetComponent<MeshFilter>() );
									damageMeshes.Add( Traverse.Create( __instance ).Method( "GetMesh", array12[1].Trim() ).GetValue<Mesh>() );
								}
							}
							if( !playerControlled ) {
								activeVessel.vesselai.enemynavalguns.turrets[gunTurretIndex] = gameObject.transform;
							}
							break;
						case "MeshNavalGunBarrel":
							Transform turretTransform = gameObject.transform;
							if( !dataLineArray[1].Trim().Contains( "," ) ) {
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, dataLineArray[1].Trim() } ).GetValue<GameObject>();
							}
							else {
								// Damage Meshes
								string[] array5 = dataLineArray[1].Trim().Split( ',' );
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, array5[0].Trim() } ).GetValue<GameObject>();
								if( array5[1] == "HIDE" ) {
									hiddenObjectsList.Add( gameObject );
								}
								else {
									damageMeshFilters.Add( gameObject.GetComponent<MeshFilter>() );
									damageMeshes.Add( Traverse.Create( __instance ).Method( "GetMesh", array5[1].Trim() ).GetValue<Mesh>() );
								}
							}
							gameObject.transform.SetParent( turretTransform, worldPositionStays: false );
							gameObject.transform.localPosition = meshPosition;
							gameObject.transform.localRotation = Quaternion.Slerp( Quaternion.identity, Quaternion.Euler( meshRotation ), 1f );
							if( !playerControlled ) {
								activeVessel.vesselai.enemynavalguns.barrels[gunTurretIndex] = gameObject.transform;
							}
							break;
						case "NavalGunSpawnPosition":
							GameObject gameObjectGunSpawnPosition = UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.blankTransform, meshHolder.position, Quaternion.identity ) as GameObject;
							gameObjectGunSpawnPosition.transform.SetParent( gameObject.transform );
							gameObjectGunSpawnPosition.transform.localPosition = UIFunctions.globaluifunctions.textparser.PopulateVector3( dataLineArray[1].Trim() );
							gameObjectGunSpawnPosition.transform.localRotation = Quaternion.identity;
							if( activeVessel.vesselai != null ) {
								activeVessel.vesselai.enemynavalguns.muzzlePositions[gunTurretIndex] = gameObjectGunSpawnPosition.transform;
							}
							gunTurretIndex++;
							break;
						case "CIWSBarrelOffset":
							ciwsBarrelOffset = UIFunctions.globaluifunctions.textparser.PopulateVector3( dataLineArray[1].Trim() );
							break;
						case "MeshCIWSGun":
							if( !dataLineArray[1].Trim().Contains( "," ) ) {
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, dataLineArray[1].Trim() } ).GetValue<GameObject>();
							}
							else {
								// Damage Mesh
								string[] array26 = dataLineArray[1].Trim().Split( ',' );
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, array26[0].Trim() } ).GetValue<GameObject>();
								if( array26[1] == "HIDE" ) {
									hiddenObjectsList.Add( gameObject );
								}
								else {
									damageMeshFilters.Add( gameObject.GetComponent<MeshFilter>() );
									damageMeshes.Add( Traverse.Create( __instance ).Method( "GetMesh", array26[1].Trim() ).GetValue<Mesh>() );
								}
							}
							if( !playerControlled ) {
								activeVessel.vesselai.enemymissiledefense.turrets[countCIWSTurrets] = gameObject;
							}
							GameObject gameObjectCIWSDirectionFinder = UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.blankTransform, gameObject.transform.position, gameObject.transform.rotation ) as GameObject;
							gameObjectCIWSDirectionFinder.transform.SetParent( gameObject.transform, worldPositionStays: false );
							gameObjectCIWSDirectionFinder.name = "directionfinder";
							gameObjectCIWSDirectionFinder.transform.localPosition = Vector3.zero;
							if( !playerControlled ) {
								activeVessel.vesselai.enemymissiledefense.directionFinders[countCIWSTurrets] = gameObjectCIWSDirectionFinder.transform;
							}
							GameObject gameObjectCIWSBarrel = UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.blankTransform, gameObject.transform.position, gameObject.transform.rotation ) as GameObject;
							gameObjectCIWSBarrel.transform.SetParent( gameObject.transform, worldPositionStays: false );
							gameObjectCIWSBarrel.transform.localPosition = ciwsBarrelOffset;
							gameObjectCIWSBarrel.transform.localRotation = Quaternion.identity;
							gameObjectCIWSBarrel.name = "barrel";
							if( !playerControlled ) {
								activeVessel.vesselai.enemymissiledefense.barrels[countCIWSTurrets] = gameObjectCIWSBarrel.transform;
							}
							countCIWSTurrets++;
							break;
						case "MeshCIWSRADAR":
							if( !dataLineArray[1].Trim().Contains( "," ) ) {
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, dataLineArray[1].Trim() } ).GetValue<GameObject>();
							}
							else {
								string[] array22 = dataLineArray[1].Trim().Split( ',' );
								gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, array22[0].Trim() } ).GetValue<GameObject>();
								if( array22[1] == "HIDE" ) {
									hiddenObjectsList.Add( gameObject );
								}
								else {
									damageMeshFilters.Add( gameObject.GetComponent<MeshFilter>() );
									damageMeshes.Add( Traverse.Create( __instance ).Method( "GetMesh", array22[1].Trim() ).GetValue<Mesh>() );
								}
							}
							if( !playerControlled ) {
								activeVessel.vesselai.enemymissiledefense.trackingRadars[countCIWSRadar] = gameObject;
							}
							countCIWSRadar++;
							break;
						case "MeshRBULauncher": {
								Debug.Log( "Warning: MeshRBULauncher is Experimental and may be broken." );
								GameObject gameObject7 = UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.blankTransform, meshHolder.position, Quaternion.identity ) as GameObject;
								gameObject7.transform.SetParent( activeVessel.meshHolder, worldPositionStays: false );
								gameObject7.transform.localPosition = meshPosition;
								gameObject7.transform.localRotation = Quaternion.identity;
								gameObject7.name = "rbuMount";
								activeVessel.vesselai.enemyrbu.rbuPositions[num12] = gameObject7.transform;
								if( !dataLineArray[1].Trim().Contains( "," ) ) {
									gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, dataLineArray[1].Trim() } ).GetValue<GameObject>();
								}
								else {
									string[] array17 = dataLineArray[1].Trim().Split( ',' );
									gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { meshHolder, meshPosition, meshRotation, material, array17[0].Trim() } ).GetValue<GameObject>();
									if( array17[1] == "HIDE" ) {
										hiddenObjectsList.Add( gameObject );
									}
									else {
										damageMeshFilters.Add( gameObject.GetComponent<MeshFilter>() );
										damageMeshes.Add( Traverse.Create( __instance ).Method( "GetMesh", array17[1].Trim() ).GetValue<Mesh>() );
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
							Debug.Log( "Warning: MeshRBUMount is Experimental and may be broken." );
							gameObject = Traverse.Create( __instance ).Method( "SetupMesh", new object[] { activeVessel.vesselai.enemyrbu.rbuPositions[num12 - 1], meshPosition, meshRotation, material, dataLineArray[1].Trim() } ).GetValue<GameObject>();
							break;
						case "BowWaveParticle":
							GameObject gameObject6 = UnityEngine.Object.Instantiate( (GameObject) Resources.Load( dataLineArray[1].Trim() ), meshPosition, Quaternion.identity ) as GameObject;
							gameObject6.transform.SetParent( activeVessel.vesselmovement.bowwaveHolder.transform );
							gameObject6.transform.localPosition = Vector3.zero;
							gameObject6.transform.localRotation = Quaternion.identity;
							activeVessel.vesselmovement.bowwave = gameObject6.GetComponent<ParticleSystem>();
							gameObject6.layer = 28;
							break;
						case "PropWashParticle":
							GameObject gameObject5 = UnityEngine.Object.Instantiate( (GameObject) Resources.Load( dataLineArray[1].Trim() ), meshPosition, Quaternion.identity ) as GameObject;
							gameObject5.transform.SetParent( activeVessel.vesselmovement.wakeObject.transform );
							gameObject5.transform.localPosition = meshPosition;
							gameObject5.transform.localRotation = Quaternion.identity;
							activeVessel.vesselmovement.propwash = gameObject5.GetComponent<ParticleSystem>();
							gameObject5.layer = 28;
							break;
						case "FunnelSmokeParticle":
							GameObject gameObjectFunnelSmoke;
							if( dataLineArray[1].Trim().Split( ',' )[0] == "NEW" ) {
								gameObjectFunnelSmoke = new GameObject();
								gameObjectFunnelSmoke.transform.SetParent( meshHolder.transform );
								gameObjectFunnelSmoke.transform.localPosition = meshPosition;
								gameObjectFunnelSmoke.transform.localRotation = Quaternion.Euler( meshRotation );
								Shader funnelSmokeShader = Shader.Find( "Particles/Additive (Soft)" );
								Material funnelSmokeMaterial = new Material( funnelSmokeShader );
								funnelSmokeMaterial.SetTexture( "_MainTex", UIFunctions.globaluifunctions.textparser.GetTexture( dataLineArray[1].Trim().Split( ',' )[1] ) );
								ParticleSystem ps = gameObjectFunnelSmoke.AddComponent<ParticleSystem>();

								// Renderer Controls
								ParticleSystemRenderer pr = gameObjectFunnelSmoke.GetComponent<ParticleSystemRenderer>();
								pr.sharedMaterial = funnelSmokeMaterial;
								pr.minParticleSize = 0f;
								pr.maxParticleSize = float.Parse( dataLineArray[1].Trim().Split( ',' )[3] );

								ps.startLifetime = float.Parse( dataLineArray[1].Trim().Split( ',' )[4] );
								ps.startSpeed = float.Parse( dataLineArray[1].Trim().Split( ',' )[5] );
								ps.scalingMode = ParticleSystemScalingMode.Local;
								ps.simulationSpace = ParticleSystemSimulationSpace.World;

								var ems = ps.emission;
								ems.enabled = true;
								ems.rate = float.Parse( dataLineArray[1].Trim().Split( ',' )[6] );

								// Shape Module
								var shp = ps.shape;
								shp.enabled = true;
								shp.shapeType = ParticleSystemShapeType.Mesh;
								shp.mesh = Traverse.Create( __instance ).Method( "GetMesh", dataLineArray[1].Trim().Split( ',' )[2] ).GetValue<Mesh>();

								// Particle Colour Setup - Works
								Gradient funnelSmokeGradient = new Gradient();
								funnelSmokeGradient.SetKeys( new GradientColorKey[] { new GradientColorKey( new Color( 0.1176471f, 0.1176471f, 0.1176471f ), 0f ), new GradientColorKey( new Color( 0.1176471f, 0.1176471f, 0.1176471f ), 1f ) }, new GradientAlphaKey[] { new GradientAlphaKey( 1f, 0f ), new GradientAlphaKey( 0f, 1f ) } );
								var col = ps.colorOverLifetime;
								col.enabled = true;
								col.color = funnelSmokeGradient;

								// Sizing
								var sz = ps.sizeOverLifetime;
								sz.enabled = true;
								sz.separateAxes = false;
								AnimationCurve curveSize = new AnimationCurve( new Keyframe[] { new Keyframe( 0f, float.Parse( dataLineArray[1].Trim().Split( ',' )[7] ), 0f, 1f ), new Keyframe( 1f, float.Parse( dataLineArray[1].Trim().Split( ',' )[8] ), 0f, 0f ) } );
								var sz2 = sz.size;
								sz.size = new ParticleSystem.MinMaxCurve( 1f, curveSize );

								// Size/Speed
								var spd = ps.sizeBySpeed;
								spd.enabled = true;
								AnimationCurve curveSizeBySpeed = new AnimationCurve( new Keyframe[] { new Keyframe( 0f, 0.25f, 0f, 0f ), new Keyframe( 1f, 1f, 1f, 0f ) } );
								spd.size = new ParticleSystem.MinMaxCurve( 1f, curveSizeBySpeed );

								// Rotation
								var rot = ps.rotationOverLifetime;
								rot.enabled = true;
								rot.separateAxes = false;
								AnimationCurve curveMax = new AnimationCurve( new Keyframe[] { new Keyframe( 0f, 1f, 0f, 0f ), new Keyframe( 1f, 1f, 0f, 0f ) } );
								AnimationCurve curveMin = new AnimationCurve( new Keyframe[] { new Keyframe( 0f, -1f, 0f, 0f ), new Keyframe( 1f, 0.85f, 0f, 0f ) } );
								var rot2 = rot.z;
								rot2.constant = 35f;
								rot.z = new ParticleSystem.MinMaxCurve( 1f, curveMin, curveMax );
							}
							else {
								gameObjectFunnelSmoke = UnityEngine.Object.Instantiate( (GameObject) Resources.Load( dataLineArray[1].Trim() ), meshPosition, Quaternion.identity ) as GameObject;
								gameObjectFunnelSmoke.transform.SetParent( meshHolder.transform );
								gameObjectFunnelSmoke.transform.localPosition = meshPosition;
								gameObjectFunnelSmoke.transform.localRotation = Quaternion.identity;
							}
							activeVessel.damagesystem.funnelSmoke = gameObjectFunnelSmoke.GetComponent<ParticleSystem>();
							break;
						case "EmergencyBlowParticle":
							GameObject gameObject3 = UnityEngine.Object.Instantiate( (GameObject) Resources.Load( dataLineArray[1].Trim() ), meshPosition, Quaternion.identity ) as GameObject;
							gameObject3.transform.SetParent( meshHolder.transform );
							gameObject3.transform.localPosition = meshPosition;
							gameObject3.transform.localRotation = Quaternion.identity;
							activeVessel.damagesystem.emergencyBlow = gameObject3.GetComponent<ParticleSystem>();
							gameObject3.GetComponent<AudioSource>().playOnAwake = false;
							break;
						case "CavitationParticle":
							GameObject gameObjectCavBubbles;
							if( dataLineArray[1].Trim().Split( ',' )[0] == "NEW" ) {
								//CavitationParticle=NEW,ships/uk_ddg_type42/smiley.png,uk_ddg_type42_propwash,maxParticleSize,startLifetime,startSpeed,rate,minSize,maxSize
								gameObjectCavBubbles = new GameObject();
								gameObjectCavBubbles.transform.SetParent( meshHolder.transform );
								gameObjectCavBubbles.transform.localPosition = meshPosition;
								gameObjectCavBubbles.transform.localRotation = Quaternion.Euler( meshRotation );
								Shader cavBubbleShader = Shader.Find( "Particles/Additive (Soft)" );
								Material cavBubbleMaterial = new Material( cavBubbleShader );
								cavBubbleMaterial.SetTexture( "_MainTex", UIFunctions.globaluifunctions.textparser.GetTexture( dataLineArray[1].Trim().Split( ',' )[1] ) );
								ParticleSystem ps = gameObjectCavBubbles.AddComponent<ParticleSystem>();

								// Renderer Controls - Seem OK
								ParticleSystemRenderer pr = gameObjectCavBubbles.GetComponent<ParticleSystemRenderer>();
								pr.sharedMaterial = cavBubbleMaterial;
								pr.minParticleSize = 0f;
								pr.maxParticleSize = float.Parse( dataLineArray[1].Trim().Split( ',' )[3] );

								ps.startLifetime = float.Parse( dataLineArray[1].Trim().Split( ',' )[4] );
								ps.startSpeed = float.Parse( dataLineArray[1].Trim().Split( ',' )[5] );
								ps.scalingMode = ParticleSystemScalingMode.Local;
								ps.simulationSpace = ParticleSystemSimulationSpace.World;

								var ems = ps.emission;
								ems.enabled = true;
								ems.rate = float.Parse( dataLineArray[1].Trim().Split( ',' )[6] );

								// Shape Module
								var shp = ps.shape;
								shp.enabled = true;
								shp.shapeType = ParticleSystemShapeType.Mesh;
								shp.mesh = Traverse.Create( __instance ).Method( "GetMesh", dataLineArray[1].Trim().Split( ',' )[2] ).GetValue<Mesh>();

								// Particle Colour Setup - Works
								Gradient cavBubbleGradient = new Gradient();
								cavBubbleGradient.SetKeys( new GradientColorKey[] { new GradientColorKey( new Color( 0.5f, 0.5f, 0.5f ), 0f ), new GradientColorKey( new Color( 0.5f, 0.5f, 0.5f ), 1f ) }, new GradientAlphaKey[] { new GradientAlphaKey( 0.25f, 0f ), new GradientAlphaKey( 0f, 1f ) } );
								var col = ps.colorOverLifetime;
								col.enabled = true;
								col.color = cavBubbleGradient;

								// Sizing
								var sz = ps.sizeOverLifetime;
								sz.enabled = true;
								sz.separateAxes = false;
								//AnimationCurve curveSize = new AnimationCurve( new Keyframe[] { new Keyframe( 0f, 0.2f, 0f, 1f ), new Keyframe( 1f, 0.95f, 0f, 0f ) } );
								AnimationCurve curveSize = new AnimationCurve( new Keyframe[] { new Keyframe( 0f, float.Parse( dataLineArray[1].Trim().Split( ',' )[7] ), 0f, 1f ), new Keyframe( 1f, float.Parse( dataLineArray[1].Trim().Split( ',' )[8] ), 0f, 0f ) } );
								var sz2 = sz.size;
								sz.size = new ParticleSystem.MinMaxCurve( 1f, curveSize );

								// Size/Speed
								var spd = ps.sizeBySpeed;
								spd.enabled = true;
								AnimationCurve curveSizeBySpeed = new AnimationCurve( new Keyframe[] { new Keyframe( 0f, 0.25f, 0f, 0f ), new Keyframe( 1f, 1f, 1f, 0f ) } );
								spd.size = new ParticleSystem.MinMaxCurve( 1f, curveSizeBySpeed );

								// Rotation
								var rot = ps.rotationOverLifetime;
								rot.enabled = true;
								rot.separateAxes = false;
								AnimationCurve curveMax = new AnimationCurve( new Keyframe[] { new Keyframe( 0f, 1f, 0f, 0f ), new Keyframe( 1f, 1f, 0f, 0f ) } );
								AnimationCurve curveMin = new AnimationCurve( new Keyframe[] { new Keyframe( 0f, -1f, 0f, 0f ), new Keyframe( 1f, 0.85f, 0f, 0f ) } );
								var rot2 = rot.z;
								rot2.constant = 35f;
								rot.z = new ParticleSystem.MinMaxCurve( 1f, curveMin, curveMax );
								ps.Stop();
							}
							else {
								gameObjectCavBubbles = UnityEngine.Object.Instantiate( (GameObject) Resources.Load( dataLineArray[1].Trim() ), meshPosition, Quaternion.identity ) as GameObject;
								gameObjectCavBubbles.transform.SetParent( meshHolder.transform );
								gameObjectCavBubbles.transform.localPosition = meshPosition;
								gameObjectCavBubbles.transform.localRotation = Quaternion.identity;
							}
							activeVessel.vesselmovement.cavBubbles = gameObjectCavBubbles.GetComponent<ParticleSystem>();
							break;
						case "KelvinWaves":
							Vector3 vector3 = UIFunctions.globaluifunctions.textparser.PopulateVector2( dataLineArray[1].Trim() );
							activeVessel.vesselmovement.kelvinWaveOverlay.width = vector3.x;
							activeVessel.vesselmovement.kelvinWaveOverlay.height = vector3.y;
							break;
						case "ParticleBowWavePosition":
							if( dataLineArray[1].Trim() != "FALSE" ) {
								activeVessel.vesselmovement.bowwave.transform.parent.transform.localPosition = UIFunctions.globaluifunctions.textparser.PopulateVector3( dataLineArray[1].Trim() );
							}
							break;
						case "ParticlePropWashPosition":
							if( dataLineArray[1].Trim() != "FALSE" ) {
								activeVessel.vesselmovement.propwash.transform.localPosition = UIFunctions.globaluifunctions.textparser.PopulateVector3( dataLineArray[1].Trim() );
							}
							break;
						case "ParticleHullFoamPosition":
							if( dataLineArray[1].Trim() != "FALSE" ) {
								activeVessel.vesselmovement.foamTrails[0].transform.localPosition = UIFunctions.globaluifunctions.textparser.PopulateVector3( dataLineArray[1].Trim() );
							}
							else {
								UnityEngine.Object.Destroy( activeVessel.vesselmovement.foamTrails[0].gameObject );
							}
							break;
						case "ParticleHullFoamParameters":
							string[] array14 = dataLineArray[1].Trim().Split( ',' );
							activeVessel.vesselmovement.foamTrails[0].duration = float.Parse( array14[0] );
							activeVessel.vesselmovement.foamTrails[0].size = float.Parse( array14[1] );
							activeVessel.vesselmovement.foamTrails[0].spacing = float.Parse( array14[2] );
							activeVessel.vesselmovement.foamTrails[0].expansion = float.Parse( array14[3] );
							activeVessel.vesselmovement.foamTrails[0].momentum = float.Parse( array14[4] );
							activeVessel.vesselmovement.foamTrails[0].spin = float.Parse( array14[5] );
							activeVessel.vesselmovement.foamTrails[0].jitter = float.Parse( array14[6] );
							activeVessel.vesselmovement.submarineFoamDurations[0] = activeVessel.vesselmovement.foamTrails[0].duration;
							break;
						case "ParticleSternFoamPosition":
							activeVessel.vesselmovement.foamTrails[1].transform.localPosition = UIFunctions.globaluifunctions.textparser.PopulateVector3( dataLineArray[1].Trim() );
							break;
						case "ParticleSternFoamParameters":
							string[] array13 = dataLineArray[1].Trim().Split( ',' );
							activeVessel.vesselmovement.foamTrails[1].duration = float.Parse( array13[0] );
							activeVessel.vesselmovement.foamTrails[1].size = float.Parse( array13[1] );
							activeVessel.vesselmovement.foamTrails[1].spacing = float.Parse( array13[2] );
							activeVessel.vesselmovement.foamTrails[1].expansion = float.Parse( array13[3] );
							activeVessel.vesselmovement.foamTrails[1].momentum = float.Parse( array13[4] );
							activeVessel.vesselmovement.foamTrails[1].spin = float.Parse( array13[5] );
							activeVessel.vesselmovement.foamTrails[1].jitter = float.Parse( array13[6] );
							activeVessel.vesselmovement.submarineFoamDurations[1] = activeVessel.vesselmovement.foamTrails[1].duration;
							break;
						case "EngineAudioClip":
							activeVessel.vesselmovement.engineSound.clip = UIFunctions.globaluifunctions.textparser.GetAudioClip( dataLineArray[1].Trim() );
							break;
						case "EngineAudioRollOff":
							if( dataLineArray[1].Trim() != "LOGARITHMIC" ) {
								activeVessel.vesselmovement.engineSound.rolloffMode = AudioRolloffMode.Linear;
							}
							else {
								activeVessel.vesselmovement.engineSound.rolloffMode = AudioRolloffMode.Logarithmic;
							}
							break;
						case "EngineAudioDistance":
							string[] array10 = dataLineArray[1].Trim().Split( ',' );
							activeVessel.vesselmovement.engineSound.minDistance = float.Parse( array10[0] );
							activeVessel.vesselmovement.engineSound.maxDistance = float.Parse( array10[1] );
							break;
						case "EngineAudioPitchRange":
							activeVessel.vesselmovement.enginePitchRange = UIFunctions.globaluifunctions.textparser.PopulateVector2( dataLineArray[1].Trim() );
							break;
						case "PropAudioClip":
							activeVessel.vesselmovement.propSound.clip = UIFunctions.globaluifunctions.textparser.GetAudioClip( dataLineArray[1].Trim() );
							Transform transform3 = activeVessel.vesselmovement.propSound.transform;
							Vector3 localPosition2 = activeVessel.vesselmovement.props[0].transform.localPosition;
							transform3.localPosition = new Vector3( 0f, 0f, localPosition2.z );
							break;
						case "PropAudioRollOff":
							if( dataLineArray[1].Trim() != "LOGARITHMIC" ) {
								activeVessel.vesselmovement.propSound.rolloffMode = AudioRolloffMode.Linear;
							}
							else {
								activeVessel.vesselmovement.propSound.rolloffMode = AudioRolloffMode.Logarithmic;
							}
							break;
						case "PropAudioDistance":
							string[] array8 = dataLineArray[1].Trim().Split( ',' );
							activeVessel.vesselmovement.propSound.minDistance = float.Parse( array8[0] );
							activeVessel.vesselmovement.propSound.maxDistance = float.Parse( array8[1] );
							break;
						case "PropAudioPitchRange":
							activeVessel.vesselmovement.propPitchRange = UIFunctions.globaluifunctions.textparser.PopulateVector2( dataLineArray[1].Trim() );
							break;
						case "PingAudioClip":
							activeVessel.vesselmovement.pingSound.enabled = true;
							activeVessel.vesselmovement.pingSound.clip = UIFunctions.globaluifunctions.textparser.GetAudioClip( dataLineArray[1].Trim() );
							activeVessel.vesselmovement.pingSound.loop = false;
							Transform transform2 = activeVessel.vesselmovement.pingSound.transform;
							Vector3 localPosition = activeVessel.vesselmovement.bowwaveHolder.transform.localPosition;
							transform2.localPosition = new Vector3( 0f, 0f, localPosition.z );
							break;
						case "PingAudioRollOff":
							if( dataLineArray[1].Trim() != "LOGARITHMIC" ) {
								activeVessel.vesselmovement.pingSound.rolloffMode = AudioRolloffMode.Linear;
							}
							else {
								activeVessel.vesselmovement.pingSound.rolloffMode = AudioRolloffMode.Logarithmic;
							}
							break;
						case "PingAudioDistance":
							string[] array6 = dataLineArray[1].Trim().Split( ',' );
							activeVessel.vesselmovement.pingSound.minDistance = float.Parse( array6[0] );
							activeVessel.vesselmovement.pingSound.maxDistance = float.Parse( array6[1] );
							break;
						case "PingAudioPitch":
							activeVessel.vesselmovement.pingSound.pitch = float.Parse( dataLineArray[1].Trim() );
							break;
						case "BowwaveAudioClip":
							activeVessel.vesselmovement.bowwaveSound.enabled = true;
							activeVessel.vesselmovement.bowwaveSound.clip = UIFunctions.globaluifunctions.textparser.GetAudioClip( dataLineArray[1].Trim() );
							activeVessel.vesselmovement.bowwaveSound.loop = true;
							break;
						case "BowwaveAudioRollOff":
							if( dataLineArray[1].Trim() != "LOGARITHMIC" ) {
								activeVessel.vesselmovement.bowwaveSound.rolloffMode = AudioRolloffMode.Linear;
							}
							else {
								activeVessel.vesselmovement.bowwaveSound.rolloffMode = AudioRolloffMode.Logarithmic;
							}
							break;
						case "BowwaveAudioDistance":
							string[] array3 = dataLineArray[1].Trim().Split( ',' );
							activeVessel.vesselmovement.bowwaveSound.minDistance = float.Parse( array3[0] );
							activeVessel.vesselmovement.bowwaveSound.maxDistance = float.Parse( array3[1] );
							break;
						case "BowwaveAudioPitch":
							activeVessel.vesselmovement.bowwaveSound.pitch = float.Parse( dataLineArray[1].Trim() );
							break;
					}
				}
				activeVessel.vesselmovement.rudder = listRudderTransforms.ToArray();
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
					activeVessel.damagesystem.hullDamageMeshes[0] = Traverse.Create( __instance ).Method( "GetMesh", str + "_damage_11" ).GetValue<Mesh>();
					activeVessel.damagesystem.hullDamageMeshes[1] = Traverse.Create( __instance ).Method( "GetMesh", str + "_damage_12" ).GetValue<Mesh>();
					activeVessel.damagesystem.hullDamageMeshes[2] = Traverse.Create( __instance ).Method( "GetMesh", str + "_damage_21" ).GetValue<Mesh>();
					activeVessel.damagesystem.hullDamageMeshes[3] = Traverse.Create( __instance ).Method( "GetMesh", str + "_damage_22" ).GetValue<Mesh>();
					activeVessel.damagesystem.hullDamageMeshes[4] = Traverse.Create( __instance ).Method( "GetMesh", str + "_damage_31" ).GetValue<Mesh>();
					activeVessel.damagesystem.hullDamageMeshes[5] = Traverse.Create( __instance ).Method( "GetMesh", str + "_damage_32" ).GetValue<Mesh>();
					activeVessel.damagesystem.hullDamageMeshes[6] = Traverse.Create( __instance ).Method( "GetMesh", str + "_damage_41" ).GetValue<Mesh>();
					activeVessel.damagesystem.hullDamageMeshes[7] = Traverse.Create( __instance ).Method( "GetMesh", str + "_damage_42" ).GetValue<Mesh>();
					activeVessel.damagesystem.hullDamageMeshes[8] = Traverse.Create( __instance ).Method( "GetMesh", str + "_damage_51" ).GetValue<Mesh>();
					activeVessel.damagesystem.hullDamageMeshes[9] = Traverse.Create( __instance ).Method( "GetMesh", str + "_damage_52" ).GetValue<Mesh>();
				}
				activeVessel.damagesystem.damageMeshFilters = damageMeshFilters.ToArray();
				activeVessel.damagesystem.damageMeshes = damageMeshes.ToArray();
				activeVessel.damagesystem.objectMeshesToHide = hiddenObjectsList.ToArray();
				activeVessel.damagesystem.radars = list3.ToArray();
				if( listMeshColliders.Count > 0 ) {
					activeVessel.superstructureColliders = listMeshColliders.ToArray();
				}
				meshHolder = null;
				material = null;
				gameObject = null;
				return false;
			}
		}

		[HarmonyPatch( typeof( VesselBuilder ), "CreateAndPlaceWeaponMeshes" )]
		public class VesselBuilder_CreateAndPlaceWeaponMeshes_Patch
		{
			[HarmonyPrefix]
			public static bool Prefix( VesselBuilder __instance, GameObject weaponTemplate, int weaponID, string weaponPrefabRef ) {
				Torpedo component = UIFunctions.globaluifunctions.database.databaseweapondata[weaponID].weaponObject.GetComponent<Torpedo>();
				Vector3 localPosition = Vector3.zero;
				Vector3 zero = Vector3.zero;
				Vector3 localScale = Vector3.one;
				Texture texture = null;
				Material material = null;
				AudioSource audioSource = null;
				Transform transform = weaponTemplate.transform;
				__instance.currentMesh = null;
				GameObject gameObject = null;
				float speed = 0f;
				int countProps = 0;
				if( UIFunctions.globaluifunctions.database.databaseweapondata[weaponID].weaponType == "MISSILE" ) {
					component.boxcollider.size = new Vector3( 0.02f, 0.02f, 0.5f );
				}
				bool flag = false;
				bool flag2 = false;
				string[] array = UIFunctions.globaluifunctions.textparser.OpenTextDataFile( "weapons" );
				bool isCustom = false;
				for( int i = 0; i < array.Length; i++ ) {
					string[] array2 = array[i].Split( '=' );
					if( array2[0].Trim() == "WeaponObjectReference" ) {
						if( array2[1].Trim() == weaponPrefabRef ) {
							flag = true;
						}
						isCustom = false;
					}
					else if( array2[0].Trim() == "[Model]" ) {
						flag2 = true;
					}
					else if( array2[0].Trim() == "[/Model]" && flag ) {
						break;
					}
					if( !flag2 || !flag ) {
						continue;
					}
					switch( array2[0] ) {
						case "ModelFile":
							__instance.allMeshes = GetModel( array2[1].Trim() );
							if( array2[1].Trim().Contains( "." ) ) {
								isCustom = true;
							}
							break;
						case "MeshPosition":
							localPosition = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
							break;
						case "MeshRotation":
							zero = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
							break;
						case "MeshScale":
							localScale = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
							break;
						case "Material":
							material = GetMaterial( array2[1].Trim() );
							break;
						case "MaterialTextures":
							if( material != null ) {
								string[] array4 = array2[1].Trim().Split( ',' );
								if( array4.Length > 0 ) {
									material.SetTexture( "_MainTex", UIFunctions.globaluifunctions.textparser.GetTexture( array4[0] ) );
								}
								if( array4.Length > 1 ) {
									material.SetTexture( "_SpecTex", UIFunctions.globaluifunctions.textparser.GetTexture( array4[1] ) );
								}
								if( array4.Length > 2 ) {
									material.SetTexture( "_BumpMap", UIFunctions.globaluifunctions.textparser.GetTexture( array4[2] ) );
								}
							}
							break;
						case "MeshWeapon":
							component.torpedoMeshes[0].GetComponent<MeshFilter>().mesh = Traverse.Create( __instance ).Method( "GetMesh", array2[1].Trim() ).GetValue<Mesh>();
							component.torpedoMeshes[0].GetComponent<MeshRenderer>().sharedMaterial = material;
							component.torpedoMeshes[0].transform.localPosition = localPosition;
							component.torpedoMeshes[0].transform.localScale = localScale;
							break;
						case "MeshWeaponCanister":
							component.torpedoMeshes[1].GetComponent<MeshFilter>().mesh = Traverse.Create( __instance ).Method( "GetMesh", array2[1].Trim() ).GetValue<Mesh>();
							component.torpedoMeshes[1].GetComponent<MeshRenderer>().sharedMaterial = material;
							component.torpedoMeshes[1].transform.localPosition = localPosition;
							component.torpedoMeshes[1].transform.localScale = localScale;
							component.torpedoMeshes[1].gameObject.SetActive( value: true );
							component.torpedoMeshes[0].layer = 17;
							break;
						case "MeshWeaponPropRotation":
							speed = float.Parse( array2[1].Trim() );
							break;
						case "MeshWeaponProp":
							if( isCustom ) {
								component.torpedoPropMeshes[countProps].transform.localPosition = localPosition;
								component.torpedoPropMeshes[countProps].transform.localRotation = Quaternion.Euler( -90f, 0f, 0f );
								component.torpedoPropMeshes[countProps].transform.localScale = localScale;
								Destroy( component.torpedoPropMeshes[countProps].GetComponent<MeshFilter>() );
								Destroy( component.torpedoPropMeshes[countProps].GetComponent<MeshRenderer>() );
								GameObject goProp = new GameObject();
								goProp.transform.position = component.torpedoPropMeshes[countProps].transform.parent.position;
								goProp.AddComponent<MeshFilter>().sharedMesh = Traverse.Create( __instance ).Method( "GetMesh", array2[1].Trim() ).GetValue<Mesh>();
								goProp.AddComponent<MeshRenderer>().sharedMaterial = material;
								goProp.transform.SetParent( component.torpedoPropMeshes[countProps].transform );
								component.propRotations[countProps].speed = speed;
								countProps++;
							}
							else {
								component.torpedoPropMeshes[countProps].GetComponent<MeshFilter>().mesh = Traverse.Create( __instance ).Method( "GetMesh", array2[1].Trim() ).GetValue<Mesh>();
								component.torpedoPropMeshes[countProps].GetComponent<MeshRenderer>().sharedMaterial = material;
								component.torpedoPropMeshes[countProps].transform.localPosition = localPosition;
								component.torpedoPropMeshes[countProps].transform.localScale = localScale;
								component.torpedoPropMeshes[countProps].transform.localRotation = Quaternion.Slerp( Quaternion.identity, Quaternion.Euler( -90f, 0f, 0f ), 1f );
								component.propRotations[countProps].speed = speed;
								countProps++;
							}
							break;
						case "MeshMissileBooster":
							component.boosterMesh.GetComponent<MeshFilter>().mesh = Traverse.Create( __instance ).Method( "GetMesh", array2[1].Trim() ).GetValue<Mesh>();
							component.boosterMesh.GetComponent<MeshRenderer>().sharedMaterial = material;
							component.boosterMesh.transform.localPosition = localPosition;
							component.boosterMesh.transform.localScale = localScale;
							break;
						case "AudioSource":
							switch( array2[1].Trim() ) {
								case "TorpedoSonarPing":
									audioSource = component.activePingAudioSource;
									break;
								case "TorpedoEngine":
									audioSource = component.cavitationAudioSource;
									break;
								case "MissileLaunch":
									audioSource = component.launchAudioSource;
									break;
								case "MissileEngine":
									audioSource = component.engineAudioSource;
									break;
							}
							break;
						case "AudioClip":
							audioSource.clip = UIFunctions.globaluifunctions.textparser.GetAudioClip( array2[1].Trim() );
							break;
						case "AudioRollOff":
							if( array2[1].Trim() != "LOGARITHMIC" ) {
								audioSource.rolloffMode = AudioRolloffMode.Linear;
							}
							else {
								audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
							}
							break;
						case "AudioDistance": {
								string[] array3 = array2[1].Trim().Split( ',' );
								audioSource.minDistance = float.Parse( array3[0] );
								audioSource.maxDistance = float.Parse( array3[1] );
								break;
							}
						case "AudioPitch":
							audioSource.pitch = float.Parse( array2[1].Trim() );
							break;
						case "AudioLoop":
							if( array2[1].Trim() == "TRUE" ) {
								audioSource.loop = true;
							}
							else {
								audioSource.loop = false;
							}
							break;
						case "CavitationParticle":
							component.cavitationTransform.localPosition = localPosition;
							break;
						case "MissileTrailParticle":
							component.missileTrailTransform.localPosition = localPosition;
							break;
						case "BoosterParticle":
							component.boosterParticleTransform.localPosition = localPosition;
							break;
						case "ParachuteParticle":
							component.parachuteTransform.localPosition = localPosition;
							break;
					}
				}
				return false;
			}
		}

		[HarmonyPatch( typeof( Enemy_AntiMissileGuns ), "InitialiseEnemyMissileDefense" )]
		public class Enemy_AntiMissileGuns_InitialiseEnemyMissileDefense_Patch
		{
			[HarmonyPrefix]
			public static bool Prefix( Enemy_AntiMissileGuns __instance ) {
				__instance.gunRangeCollider.radius = __instance.parentVessel.databaseshipdata.gunRange * GameDataManager.inverseYardsScale;
				__instance.tracers = new ParticleSystem[__instance.turrets.Length];
				__instance.tracerPointLights = new PointLight[__instance.turrets.Length];
				__instance.tracerAudios = new AudioSource[__instance.turrets.Length];
				string ciwsParticle = __instance.parentVessel.databaseshipdata.ciwsParticle;
				for( int i = 0; i < __instance.barrels.Length; i++ ) {
					GameObject gameObject = UnityEngine.Object.Instantiate( (GameObject) Resources.Load( ciwsParticle ), __instance.barrels[i].position, __instance.barrels[i].rotation ) as GameObject;
					gameObject.transform.SetParent( __instance.barrels[i] );
					if( patcher.customVessels.Contains( __instance.parentVessel ) ) {
						gameObject.transform.localPosition = Vector3.zero;
						foreach( Transform transform in gameObject.transform ) {
							transform.localPosition = Vector3.zero;
						}
					}
					else {
						gameObject.transform.localPosition = new Vector3( 0f, 0.0046f, 0.0234f );
					}
					gameObject.transform.localRotation = Quaternion.Slerp( Quaternion.identity, Quaternion.Euler( 90f, 0f, 0f ), 1f );
					__instance.tracers[i] = gameObject.GetComponent<ParticleSystem>();
					__instance.tracerPointLights[i] = gameObject.GetComponentInChildren<PointLight>();
					__instance.tracerAudios[i] = gameObject.GetComponent<AudioSource>();
				}
				GameObject gameObject2 = UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.blankTransform, __instance.gameObject.transform.position, Quaternion.identity ) as GameObject;
				gameObject2.transform.SetParent( __instance.parentVessel.transform, worldPositionStays: true );
				gameObject2.transform.localRotation = Quaternion.identity;
				gameObject2.transform.localPosition = Vector3.zero;
				__instance.directionFinder = gameObject2.transform;
				gameObject2.name = "Anti-Missile Direction Finder";
				return false;
			}
		}

		[HarmonyPatch( typeof( VesselMovement ), "FixedUpdate" )]
		public class VesselMovement_FixedUpdate_Patch
		{
			[HarmonyPrefix]
			public static bool Prefix( VesselMovement __instance ) {
				__instance.engineSoundsTimer += Time.deltaTime;
				if( __instance.engineSoundsTimer > 0.2f ) {
					Traverse.Create( __instance ).Method( "RecalculateEngineSounds" ).GetValue();
					__instance.engineSoundsTimer -= 0.2f;
				}
				if( __instance.cavTimer > 0f ) {
					__instance.cavTimer -= Time.deltaTime;
					if( __instance.cavTimer <= 0f ) {
						Traverse.Create( __instance ).Method( "CheckIfCavitating" ).GetValue();
					}
				}
				return false;
			}
		}
	}
}