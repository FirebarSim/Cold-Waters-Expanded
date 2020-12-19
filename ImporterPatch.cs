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
	public static class TransformDeepChildExtension
	{
		/*
		//Breadth-first search
		public static Transform FindDeepChild( this Transform aParent, string aName ) {
			Queue<Transform> queue = new Queue<Transform>();
			queue.Enqueue( aParent );
			while( queue.Count > 0 ) {
				var c = queue.Dequeue();
				if( c.name == aName )
					return c;
				foreach( Transform t in c )
					queue.Enqueue( t );
			}
			return null;
		}
		*/
		
		//Depth-first search
		public static Transform FindDeepChild(this Transform aParent, string aName)
		{
			foreach(Transform child in aParent)
			{
				if(child.name == aName )
					return child;
				var result = child.FindDeepChild(aName);
				if (result != null)
					return result;
			}
			return null;
		}
		
	}

	[BepInPlugin( "org.cwe.plugins.import", "Cold Waters Expanded Import Patches", "1.0.1.0" )]
	public class ImporterPatch : BaseUnityPlugin {

		static ImporterPatch patcher;
		string[] weaponList = null;
		string[] aircraftList = null;

		void Awake() {
			patcher = this;
		}


		static Material[] GetMaterial( VesselBuilder __instance, string materialName, string modelPath, AssetBundle assetBundle ) {
			//Debug.Log( "\tGetMaterial: " + materialName );
			if( materialName == "" || materialName == "FALSE" ) {
				Debug.LogWarning( "\tEmpty or FALSE material, this may be harmless" );
				return null;
            }
            else if( materialName == "BLANK" ) {
				Debug.LogWarning( "\tTemporary BLANK Material Instantiated for: " + " " + materialName.Trim() );
				Material material = new Material( Resources.Load( "ships/usn_ssn_skipjack/usn_ssn_skipjack_mat" ) as Material );
				material.SetTexture( "_MainTex", null );
				material.SetTexture( "_SpecTex", null );
				material.SetTexture( "_BumpMap", null );
				return new Material[] { material };
			}
            else if( assetBundle != null && materialName.Contains( ".mat" ) ) {
				Debug.Log( "\tCustom Mat: " + assetBundle.name + " " + materialName.Trim() );
				return new Material[] { assetBundle.LoadAsset<Material>( materialName.Trim() ) };
			}
			else if( assetBundle == null && materialName.Contains( ".mtl" ) ) {
				Debug.Log( "\tJSON Mat: " + " " + materialName.Trim() );
				Material material = SerialiseMaterial.LoadMaterial( Application.streamingAssetsPath + "/override/" + materialName.Trim() );
				//Debug.Log( material.ToString() );
				return new Material[] { material };
			}
			else {
				Material material = Resources.Load( materialName.Trim() ) as Material;
                if( material != null ) {
					return new Material[] { material };
				}
                else {
					Debug.LogError( "\tUnable to load material from: " + materialName );
					return null;
                }
			}
		}

		static Mesh GetMesh( VesselBuilder __instance, string meshName, string modelPath, AssetBundle assetBundle, ref Material[] materials ) {
			//Debug.Log( "\tGetMesh: " + meshName );
			if( meshName == "" || meshName == "FALSE" ) {
				Debug.LogWarning( "\tEmpty or FALSE mesh, this may be harmless" );
				return null;
            }
			if( assetBundle != null && assetBundle.Contains( modelPath ) ) {
				//Debug.Log( "\tLoaded from AssetBundle: " + meshName );
				Transform transform = assetBundle.LoadAsset<GameObject>( modelPath ).transform.FindDeepChild( meshName );
                if( transform != null ) {
					GameObject template = transform.gameObject;
					if( template != null ) {
						materials = template.GetComponent<MeshRenderer>().sharedMaterials;
						return template.GetComponent<MeshFilter>().sharedMesh;
					}
				}
                else {
					Debug.LogError( "\tMesh not found in AssetBundle: " + meshName );
					return null;
				}
			}
			for( int i = 0; i < __instance.allMeshes.Length; i++ ) {
				if( __instance.allMeshes[i].name == meshName ) {
					return __instance.allMeshes[i];
				}
			}
			Debug.LogError( "\tMesh not found " + meshName );
			return null;
		}

		static GameObject SetupMesh( VesselBuilder __instance, Transform vesselMesholder, Vector3 meshPosition, Vector3 meshRotation, Material[] meshMaterial, string meshName, string modelPath, AssetBundle assetBundle ) {
			//Debug.Log( "\tSetupMesh: " + meshName );
			GameObject gameObject = UnityEngine.Object.Instantiate( (GameObject) Resources.Load( "template_objects/meshTemplate" ), vesselMesholder.position, Quaternion.identity ) as GameObject;
			gameObject.transform.SetParent( vesselMesholder, false );
			gameObject.transform.localPosition = meshPosition;
			gameObject.transform.localRotation = Quaternion.Slerp( Quaternion.identity, Quaternion.Euler( meshRotation ), 1f );
            if( assetBundle != null && assetBundle.Contains( modelPath ) ) {
				Transform transform = assetBundle.LoadAsset<GameObject>( modelPath ).transform.FindDeepChild( meshName );
				if( transform != null ) {
					GameObject template = transform.gameObject;
					if( meshMaterial == null ) {
						gameObject.GetComponent<MeshRenderer>().sharedMaterials = template.GetComponent<MeshRenderer>().sharedMaterials;
					}
					else {
						gameObject.GetComponent<MeshRenderer>().sharedMaterials = meshMaterial;

					}
					gameObject.GetComponent<MeshFilter>().mesh = template.GetComponent<MeshFilter>().sharedMesh;
				}
                else {
					Debug.LogError( "\tUnable to create GameObject from AssetBundle: " + meshName );
					return null;
				}
			}
            else {
                if( meshMaterial != null ) {
					gameObject.GetComponent<MeshRenderer>().sharedMaterials = meshMaterial;
				}
                else {
					Debug.LogError( "\tCannot apply null material to Mesh: " + meshName );
                }
				Material[] mat = null;
				__instance.currentMesh = GetMesh( __instance, meshName, null, null, ref mat );
				gameObject.GetComponent<MeshFilter>().mesh = __instance.currentMesh;
			}
			gameObject.name = meshName;
			return gameObject;
		}

		[HarmonyPatch( typeof( VesselBuilder ), "CreateAndPlaceMeshes" )]
		public class VesselBuilder_CreateAndPlaceMeshes_Patch
		{
			[HarmonyPrefix]
			public static bool Prefix( VesselBuilder __instance, GameObject vesselTemplate, Vessel activeVessel, bool playerControlled, string vesselPrefabRef ) {
				Debug.Log( "Building Vessel: " + vesselPrefabRef );
				//Debug.Log( __instance +"; "+ vesselTemplate.name + "; " + activeVessel + "; " + playerControlled + "; " + vesselPrefabRef );
				Transform meshHolder = activeVessel.meshHolder;
				__instance.currentMesh = null;
				GameObject gameObject = null;
				//Debug.Log( 126 );
				if( !playerControlled ) {
					//Debug.Log( 128 );
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
					//Debug.Log( 172 );
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
				//Debug.Log( 224 );
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
				Vector3 localPosition = Vector3.zero;
				Vector3 localRotation = Vector3.zero;
				Vector3 localScale = Vector3.one;
				List<MeshCollider> list2 = new List<MeshCollider>();
				List<Radar> list3 = new List<Radar>();
				List<Mesh> list4 = new List<Mesh>();
				List<MeshFilter> list5 = new List<MeshFilter>();
				List<GameObject> list6 = new List<GameObject>();
				Material[] material = null;
				Material[] bin = null;
				string assetBundlePath = null;
				AssetBundle assetBundle = null;
				string modelPath = null;
				activeVessel.vesselmovement.submarineFoamDurations = new float[2];
				string str = string.Empty;
				bool flag = false;
				string filename = Path.Combine( "vessels", vesselPrefabRef );
				string[] array = UIFunctions.globaluifunctions.textparser.OpenTextDataFile( filename );
				for( int i = 0; i < array.Length; i++ ) {
					//Debug.Log( 259 );
					string[] array2 = array[i].Split( '=' );
					if( array2[0].Trim() == "[Model]" ) {
						flag = true;
					}
					if( !flag ) {
						continue;
					}
					switch( array2[0] ) {
						case "AssetBundle":
							if( assetBundlePath != null ) {
								assetBundle.Unload( false );
							}
							assetBundlePath = array2[1].Trim();
							assetBundle = AssetBundle.LoadFromFile( Application.streamingAssetsPath + "/override/" + assetBundlePath );
							break;
						case "ModelFile": {
								if( assetBundlePath != null ) {
									modelPath = array2[1].Trim();
								}
                                else if( array2[1].Trim().Contains(".gltf" ) ) {
									Debug.Log( "\tGetModel (GLTF): " + array2[1] );
									__instance.allMeshes = glTFImporter.GetMeshes( Application.streamingAssetsPath + "/override/" + array2[1].Trim() );
								}
								else if( array2[1].Trim().Contains( ".glb" ) ) {
									Debug.LogError( "\tGLTF meshes must be in embedded format not binary format!" );
								}
								else {
									__instance.allMeshes = Resources.LoadAll<Mesh>( array2[1].Trim() );
								}
								//string[] array15 = array2[1].Trim().Split( '/' );
								//str = array15[array15.Length - 1];
								str = Path.GetFileNameWithoutExtension( array2[1].Trim() );
								break;
							}
						case "DamageMeshName":
							str = array2[1].Trim();
							break;
						case "MeshPosition":
							localPosition = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
							break;
						case "MeshRotation":
							localRotation = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
							break;
						case "MeshScale":
							localScale = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
							break;
						case "Material":
							material = GetMaterial( __instance, array2[1], modelPath, assetBundle );
        //                    if( material != null ) {
								//Debug.Log( material.ToString() );
								//if( material[0] != null ) {
        //                                Debug.Log( material[0].ToString() );
        //                            }
        //                    }
							break;
						case "MaterialTextures":
                            if( array2[1] == "" || array2[1] == "FALSE" ) {
								Debug.LogWarning( "\tEmpty or FALSE Textures Line in " + vesselPrefabRef + ", this may be harmless" );
                            }
							if( material != null ) {
								if( material[0] != null ) {
									string[] array28 = array2[1].Trim().Split( ',' );
									material[0].SetTexture( "_MainTex", UIFunctions.globaluifunctions.textparser.GetTexture( array28[0] ) );
									if( array28.Length > 1 ) {
										material[0].SetTexture( "_SpecTex", UIFunctions.globaluifunctions.textparser.GetTexture( array28[1] ) );
									}
									if( array28.Length > 2 ) {
										material[0].SetTexture( "_BumpMap", UIFunctions.globaluifunctions.textparser.GetTexture( array28[2] ) );
									}
								}
								else {
									Debug.LogError( "\tMaterial Not Avilable for Texturing in " + vesselPrefabRef );
								}
							}
                            else {
								Debug.LogError( "\tMaterial Not Avilable for Texturing in " + vesselPrefabRef );
							}
							break;
						case "Mesh":
							if( !array2[1].Trim().Contains( "," ) ) {
								gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array2[1].Trim(), modelPath, assetBundle );
							}
							else {
								string[] array27 = array2[1].Trim().Split( ',' );
								gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array27[0].Trim(), modelPath, assetBundle );
								if( array27[1] == "HIDE" ) {
									list6.Add( gameObject );
								}
								else {
									list5.Add( gameObject.GetComponent<MeshFilter>() );
									list4.Add( GetMesh( __instance, array27[1].Trim(), modelPath, assetBundle, ref bin ) );
								}
							}
							if( gameObject != null ) {
								if( gameObject.name.Contains( "biologic" ) ) {
									gameObject.GetComponent<MeshRenderer>().receiveShadows = false;
									gameObject.transform.parent.parent.gameObject.AddComponent<Whale_AI>().parentVessel = activeVessel;
								}
							}
							break;
						case "MeshVisibility":
                            if( gameObject != null ) {
								MeshVisibility mv = gameObject.AddComponent<MeshVisibility>();
								mv.vessel = activeVessel;
								mv.conditionString = array2[1].Trim();
							}
                            else {
								Debug.LogError( "\tCannot apply Mesh Visibility component to null GameObject" );
                            }
							break;
						case "MeshTranslate":
							if( gameObject != null ) {
								MeshAnimate ma = gameObject.AddComponent<MeshAnimate>();
								ma.vessel = activeVessel;
								ma.conditionString = array2[1].Trim();
							}
							else {
								Debug.LogError( "\tCannot apply Mesh Translate component to null GameObject" );
							}
							break;
						case "MeshLights": {
								if( !GameDataManager.isNight ) {
									break;
								}
								if( !array2[1].Trim().Contains( "," ) ) {
									gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array2[1].Trim(), modelPath, assetBundle );
									break;
								}
								string[] array16 = array2[1].Trim().Split( ',' );
								gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array16[0].Trim(), modelPath, assetBundle );
								if( array16[1] == "HIDE" ) {
									list6.Add( gameObject );
									break;
								}
								list5.Add( gameObject.GetComponent<MeshFilter>() );
								list4.Add( GetMesh( __instance, array16[1].Trim(), modelPath, assetBundle, ref bin ) );
								break;
							}
						case "MeshHullCollider":
							activeVessel.hullCollider.sharedMesh = GetMesh( __instance, array2[1].Trim(), modelPath, assetBundle, ref bin );
							break;
						case "MeshSuperstructureCollider": {
								GameObject gameObject15 = UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.blankTransform, meshHolder.position, Quaternion.identity ) as GameObject;
								gameObject15.name = "This One";
								gameObject15.transform.SetParent( meshHolder );
								gameObject15.transform.localPosition = Vector3.zero;
								gameObject15.transform.localRotation = Quaternion.identity;
								MeshCollider meshCollider = gameObject15.AddComponent<MeshCollider>();
								meshCollider.convex = true;
								meshCollider.isTrigger = true;
								meshCollider.sharedMesh = GetMesh( __instance, array2[1].Trim(), modelPath, assetBundle, ref bin );
								list2.Add( meshCollider );
								break;
							}
						case "MeshHullNumber": {
								if( !array2[1].Trim().Contains( "," ) ) {
									gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array2[1].Trim(), modelPath, assetBundle );
								}
								else {
									string[] array19 = array2[1].Trim().Split( ',' );
									gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array19[0].Trim(), modelPath, assetBundle );
									if( array19[1] == "HIDE" ) {
										list6.Add( gameObject );
									}
									else {
										list5.Add( gameObject.GetComponent<MeshFilter>() );
										list4.Add( GetMesh( __instance, array19[1].Trim(), modelPath, assetBundle, ref bin ) );
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
								gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array2[1].Trim(), modelPath, assetBundle );
							}
							else {
								string[] array29 = array2[1].Trim().Split( ',' );
								gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array29[0].Trim(), modelPath, assetBundle );
								if( array29[1] == "HIDE" ) {
									list6.Add( gameObject );
								}
								else {
									list5.Add( gameObject.GetComponent<MeshFilter>() );
									list4.Add( GetMesh( __instance, array29[1].Trim(), modelPath, assetBundle, ref bin ) );
								}
							}
							list.Add( gameObject.transform );
							break;
						case "MeshProp":
							if( !array2[1].Trim().Contains( "," ) ) {
								gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array2[1].Trim(), modelPath, assetBundle );
							}
							else {
								string[] array11 = array2[1].Trim().Split( ',' );
								gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array11[0].Trim(), modelPath, assetBundle );
								if( array11[1] == "HIDE" ) {
									list6.Add( gameObject );
								}
								else {
									list5.Add( gameObject.GetComponent<MeshFilter>() );
									list4.Add( GetMesh( __instance, array11[1].Trim(), modelPath, assetBundle, ref bin ) );
								}
							}
							activeVessel.vesselmovement.props[num5] = gameObject.transform;
							num5++;
							break;
						case "MeshBowPlanes":
							if( !array2[1].Trim().Contains( "," ) ) {
								gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array2[1].Trim(), modelPath, assetBundle );
							}
							else {
								string[] array23 = array2[1].Trim().Split( ',' );
								gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array23[0].Trim(), modelPath, assetBundle );
								if( array23[1] == "HIDE" ) {
									list6.Add( gameObject );
								}
								else {
									list5.Add( gameObject.GetComponent<MeshFilter>() );
									list4.Add( GetMesh( __instance, array23[1].Trim(), modelPath, assetBundle, ref bin ) );
								}
							}
							activeVessel.vesselmovement.planes[0] = gameObject.transform;
							break;
						case "MeshSternPlanes":
							if( !array2[1].Trim().Contains( "," ) ) {
								gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array2[1].Trim(), modelPath, assetBundle );
							}
							else {
								string[] array21 = array2[1].Trim().Split( ',' );
								gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array21[0].Trim(), modelPath, assetBundle );
								if( array21[1] == "HIDE" ) {
									list6.Add( gameObject );
								}
								else {
									list5.Add( gameObject.GetComponent<MeshFilter>() );
									list4.Add( GetMesh( __instance, array21[1].Trim(), modelPath, assetBundle, ref bin ) );
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
									activeVessel.submarineFunctions.mastTransforms[num6].GetComponent<MeshFilter>().mesh = GetMesh( __instance, array2[1].Trim(), modelPath, assetBundle, ref bin );
								}
								else {
									string[] array25 = array2[1].Trim().Split( ',' );
									activeVessel.submarineFunctions.mastTransforms[num6].GetComponent<MeshFilter>().mesh = GetMesh( __instance, array25[0].Trim(), modelPath, assetBundle, ref bin );
									if( array25[1] == "HIDE" ) {
										list6.Add( activeVessel.submarineFunctions.mastTransforms[num6].gameObject );
									}
									else {
										list5.Add( activeVessel.submarineFunctions.mastTransforms[num6].GetComponent<MeshFilter>() );
										list4.Add( GetMesh( __instance, array25[1].Trim(), modelPath, assetBundle, ref bin ) );
									}
								}
								activeVessel.submarineFunctions.mastTransforms[num6].GetComponent<MeshRenderer>().sharedMaterials = material;
								activeVessel.submarineFunctions.mastTransforms[num6].localPosition = localPosition;
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
								Transform transform4;
								if( gameObject != null ) {
									transform4 = gameObject.transform;
								}
                                else {
									Debug.LogError( "\tUnable to make a null GameObject the Parent of another: " + array2[1].Trim() );
									transform4 = meshHolder;

								}
								if( !array2[1].Trim().Contains( "," ) ) {
									gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array2[1].Trim(), modelPath, assetBundle );
								}
								else {
									string[] array24 = array2[1].Trim().Split( ',' );
									gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array24[0].Trim(), modelPath, assetBundle );
									if( array24[1] == "HIDE" ) {
										list6.Add( gameObject );
									}
									else {
										list5.Add( gameObject.GetComponent<MeshFilter>() );
										list4.Add( GetMesh( __instance, array24[1].Trim(), modelPath, assetBundle, ref bin ) );
									}
								}
                                if( gameObject != null ) {
									gameObject.transform.SetParent( transform4, false );
									gameObject.transform.localPosition = localPosition;
									gameObject.transform.localRotation = Quaternion.Slerp( Quaternion.identity, Quaternion.Euler( localRotation ), 1f );
								}
                                else {
									Debug.LogError( "\tUnable to parent a null GameObject to anything: " + array2[1].Trim() );
                                }
								break;
							}
						case "MeshMainFlag":
							if( !array2[1].Trim().Contains( "," ) ) {
								gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array2[1].Trim(), modelPath, assetBundle );
							}
							else {
								string[] array7 = array2[1].Trim().Split( ',' );
								gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array7[0].Trim(), modelPath, assetBundle );
								if( array7[1] == "HIDE" ) {
									list6.Add( gameObject );
								}
								else {
									list5.Add( gameObject.GetComponent<MeshFilter>() );
									list4.Add( GetMesh( __instance, array7[1].Trim(), modelPath, assetBundle, ref bin ) );
								}
							}
							material[0].color = Environment.whiteLevel;
							gameObject.layer = 17;
							activeVessel.vesselmovement.flagRenderer = gameObject.GetComponent<MeshRenderer>();
							break;
						case "MeshOtherFlags":
							if( !array2[1].Trim().Contains( "," ) ) {
								gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array2[1].Trim(), modelPath, assetBundle );
							}
							else {
								string[] array4 = array2[1].Trim().Split( ',' );
								gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array4[0].Trim(), modelPath, assetBundle );
								if( array4[1] == "HIDE" ) {
									list6.Add( gameObject );
								}
								else {
									list5.Add( gameObject.GetComponent<MeshFilter>() );
									list4.Add( GetMesh( __instance, array4[1].Trim(), modelPath, assetBundle, ref bin ) );
								}
							}
							material[0].color = Environment.whiteLevel;
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
									gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array2[1].Trim(), modelPath, assetBundle );
								}
								else {
									string[] array20 = array2[1].Trim().Split( ',' );
									gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array20[0].Trim(), modelPath, assetBundle );
									if( array20[1] == "HIDE" ) {
										list6.Add( gameObject );
									}
									else {
										list5.Add( gameObject.GetComponent<MeshFilter>() );
										list4.Add( GetMesh( __instance, array20[1].Trim(), modelPath, assetBundle, ref bin ) );
									}
								}
								Radar radar = gameObject.AddComponent<Radar>();
								radar.speed = speed;
								list3.Add( radar );
								break;
							}
						case "MeshNoisemakerMount":
							if( activeVessel.vesselai != null ) {
								activeVessel.vesselai.enemynoisemaker.noisemakerTubes.transform.localPosition = localPosition;
							}
							else {
								activeVessel.vesselmovement.weaponSource.noisemakerTubes.transform.localPosition = localPosition;
							}
							break;
						case "MeshTorpedoMount":
							if( !array2[1].Trim().Contains( "," ) ) {
								gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array2[1].Trim(), modelPath, assetBundle );
							}
							else {
								string[] array9 = array2[1].Trim().Split( ',' );
								gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array9[0].Trim(), modelPath, assetBundle );
								if( array9[1] == "HIDE" ) {
									list6.Add( gameObject );
								}
								else {
									list5.Add( gameObject.GetComponent<MeshFilter>() );
									list4.Add( GetMesh( __instance, array9[1].Trim(), modelPath, assetBundle, ref bin ) );
								}
							}
							activeVessel.vesselai.enemytorpedo.torpedoMounts[num8] = gameObject.transform;
							break;
						case "TorpedoSpawnPosition": {
								GameObject gameObject13 = UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.blankTransform, meshHolder.position, Quaternion.identity ) as GameObject;
								if( !activeVessel.playercontrolled ) {
									if( activeVessel.vesselai.enemytorpedo != null ) {
										if( !activeVessel.vesselai.enemytorpedo.fixedTubes ) {
											gameObject13.transform.SetParent( gameObject.transform, false );
											gameObject13.transform.localPosition = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
											gameObject13.transform.localRotation = Quaternion.identity;
										}
										else {
											gameObject13.transform.SetParent( meshHolder.transform, false );
											gameObject13.transform.localPosition = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
											gameObject13.transform.localRotation = Quaternion.Slerp( Quaternion.identity, Quaternion.Euler( localRotation ), 1f );
										}
									}
								}
								else {
									gameObject13.transform.SetParent( meshHolder.transform, false );
									gameObject13.transform.localPosition = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
									gameObject13.transform.localRotation = Quaternion.Slerp( Quaternion.identity, Quaternion.Euler( localRotation ), 1f );
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
								gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array2[1].Trim(), modelPath, assetBundle );
							}
							else {
								string[] array18 = array2[1].Trim().Split( ',' );
								gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array18[0].Trim(), modelPath, assetBundle );
								if( array18[1] == "HIDE" ) {
									list6.Add( gameObject );
								}
								else {
									list5.Add( gameObject.GetComponent<MeshFilter>() );
									list4.Add( GetMesh( __instance, array18[1].Trim(), modelPath, assetBundle, ref bin ) );
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
								gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array2[1].Trim(), modelPath, assetBundle );
							}
							else {
								string[] array12 = array2[1].Trim().Split( ',' );
								gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array12[0].Trim(), modelPath, assetBundle );
								if( array12[1] == "HIDE" ) {
									list6.Add( gameObject );
								}
								else {
									list5.Add( gameObject.GetComponent<MeshFilter>() );
									list4.Add( GetMesh( __instance, array12[1].Trim(), modelPath, assetBundle, ref bin ) );
								}
							}
							activeVessel.vesselai.enemynavalguns.turrets[num13] = gameObject.transform;
							break;
						case "MeshNavalGunBarrel": {
								Transform transform = gameObject.transform;
								if( !array2[1].Trim().Contains( "," ) ) {
									gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array2[1].Trim(), modelPath, assetBundle );
								}
								else {
									string[] array5 = array2[1].Trim().Split( ',' );
									gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array5[0].Trim(), modelPath, assetBundle );
									if( array5[1] == "HIDE" ) {
										list6.Add( gameObject );
									}
									else {
										list5.Add( gameObject.GetComponent<MeshFilter>() );
										list4.Add( GetMesh( __instance, array5[1].Trim(), modelPath, assetBundle, ref bin ) );
									}
								}
								gameObject.transform.SetParent( transform, false );
								gameObject.transform.localPosition = localPosition;
								gameObject.transform.localRotation = Quaternion.Slerp( Quaternion.identity, Quaternion.Euler( localRotation ), 1f );
								activeVessel.vesselai.enemynavalguns.barrels[num13] = gameObject.transform;
								break;
							}
						case "NavalGunSpawnPosition": {
								GameObject gameObject14 = UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.blankTransform, meshHolder.position, Quaternion.identity ) as GameObject;
								if( activeVessel.vesselai.enemynavalguns != null ) {
									gameObject14.transform.SetParent( gameObject.transform, false );
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
									gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array2[1].Trim(), modelPath, assetBundle );
								}
								else {
									string[] array26 = array2[1].Trim().Split( ',' );
									gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array26[0].Trim(), modelPath, assetBundle );
									if( array26[1] == "HIDE" ) {
										list6.Add( gameObject );
									}
									else {
										list5.Add( gameObject.GetComponent<MeshFilter>() );
										list4.Add( GetMesh( __instance, array26[1].Trim(), modelPath, assetBundle, ref bin ) );
									}
								}
								activeVessel.vesselai.enemymissiledefense.turrets[num10] = gameObject;
								GameObject gameObject11 = UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.blankTransform, gameObject.transform.position, gameObject.transform.rotation ) as GameObject;
								gameObject11.transform.SetParent( gameObject.transform, false );
								gameObject11.name = "directionfinder";
								gameObject11.transform.localPosition = Vector3.zero;
								activeVessel.vesselai.enemymissiledefense.directionFinders[num10] = gameObject11.transform;
								GameObject gameObject12 = UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.blankTransform, gameObject.transform.position, gameObject.transform.rotation ) as GameObject;
								gameObject12.transform.SetParent( gameObject.transform, false );
								gameObject12.transform.localPosition = Vector3.zero;
								gameObject12.transform.localRotation = Quaternion.identity;
								gameObject12.name = "barrel";
								activeVessel.vesselai.enemymissiledefense.barrels[num10] = gameObject12.transform;
								num10++;
								break;
							}
						case "MeshCIWSRADAR":
							if( !array2[1].Trim().Contains( "," ) ) {
								gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array2[1].Trim(), modelPath, assetBundle );
							}
							else {
								string[] array22 = array2[1].Trim().Split( ',' );
								gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array22[0].Trim(), modelPath, assetBundle );
								if( array22[1] == "HIDE" ) {
									list6.Add( gameObject );
								}
								else {
									list5.Add( gameObject.GetComponent<MeshFilter>() );
									list4.Add( GetMesh( __instance, array22[1].Trim(), modelPath, assetBundle, ref bin ) );
								}
							}
							activeVessel.vesselai.enemymissiledefense.trackingRadars[num11] = gameObject;
							num11++;
							break;
						case "MeshRBULauncher": {
								GameObject gameObject7 = UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.blankTransform, meshHolder.position, Quaternion.identity ) as GameObject;
								gameObject7.transform.SetParent( activeVessel.meshHolder, false );
								gameObject7.transform.localPosition = localPosition;
								gameObject7.transform.localRotation = Quaternion.identity;
								gameObject7.name = "rbuMount";
								activeVessel.vesselai.enemyrbu.rbuPositions[num12] = gameObject7.transform;
								if( !array2[1].Trim().Contains( "," ) ) {
									gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array2[1].Trim(), modelPath, assetBundle );
								}
								else {
									string[] array17 = array2[1].Trim().Split( ',' );
									gameObject = SetupMesh( __instance, meshHolder, localPosition, localRotation, material, array17[0].Trim(), modelPath, assetBundle );
									if( array17[1] == "HIDE" ) {
										list6.Add( gameObject );
									}
									else {
										list5.Add( gameObject.GetComponent<MeshFilter>() );
										list4.Add( GetMesh( __instance, array17[1].Trim(), modelPath, assetBundle, ref bin ) );
									}
								}
								gameObject.transform.SetParent( gameObject7.transform, false );
								gameObject.transform.localPosition = Vector3.zero;
								gameObject.transform.localRotation = Quaternion.Slerp( Quaternion.identity, Quaternion.Euler( localRotation ), 1f );
								activeVessel.vesselai.enemyrbu.rbuLaunchers[num12] = gameObject.transform;
								GameObject gameObject8 = UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.blankTransform, meshHolder.position, Quaternion.identity ) as GameObject;
								gameObject8.transform.SetParent( gameObject.transform, false );
								gameObject8.transform.localPosition = Vector3.zero;
								gameObject8.transform.localRotation = Quaternion.identity;
								gameObject8.name = "muzzlehub";
								activeVessel.vesselai.enemyrbu.rbuHubs[num12] = gameObject8.transform;
								GameObject gameObject9 = UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.blankTransform, meshHolder.position, Quaternion.identity ) as GameObject;
								gameObject9.transform.SetParent( gameObject8.transform, false );
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
							gameObject = SetupMesh( __instance, activeVessel.vesselai.enemyrbu.rbuPositions[num12 - 1], localPosition, localRotation, material, array2[1].Trim(), modelPath, assetBundle );
							break;
						case "BowWaveParticle": {
								try {
									GameObject gameObject6 = UnityEngine.Object.Instantiate( (GameObject) Resources.Load( array2[1].Trim() ), localPosition, Quaternion.identity ) as GameObject;
									gameObject6.transform.SetParent( activeVessel.vesselmovement.bowwaveHolder.transform );
									gameObject6.transform.localPosition = Vector3.zero;
									gameObject6.transform.localRotation = Quaternion.identity;
									activeVessel.vesselmovement.bowwave = gameObject6.GetComponent<ParticleSystem>();
									gameObject6.layer = 28;
								}
                                catch {
									Debug.LogError( "\tCould not create BowWaveParticle" );
                                }
								break;
							}
						case "PropWashParticle": {
								try {
									GameObject gameObject5 = UnityEngine.Object.Instantiate( (GameObject) Resources.Load( array2[1].Trim() ), localPosition, Quaternion.identity ) as GameObject;
									gameObject5.transform.SetParent( activeVessel.vesselmovement.wakeObject.transform );
									gameObject5.transform.localPosition = localPosition;
									gameObject5.transform.localRotation = Quaternion.identity;
									activeVessel.vesselmovement.propwash = gameObject5.GetComponent<ParticleSystem>();
									gameObject5.layer = 28;
								}
								catch {
									Debug.LogError( "\tCould not create BowWaveParticle" );
								}
								break;
							}
						case "FunnelSmokeParticle": {
								try {
									GameObject gameObject4 = UnityEngine.Object.Instantiate( (GameObject) Resources.Load( array2[1].Trim() ), localPosition, Quaternion.identity ) as GameObject;
									gameObject4.transform.SetParent( meshHolder.transform );
									gameObject4.transform.localPosition = Vector3.zero;
									gameObject4.transform.localRotation = Quaternion.identity;
									activeVessel.damagesystem.funnelSmoke = gameObject4.GetComponent<ParticleSystem>();
								}
								catch {
									Debug.LogError( "\tCould not create BowWaveParticle" );
								}
								break;
							}
						case "EmergencyBlowParticle": {
								try {
									GameObject gameObject3 = UnityEngine.Object.Instantiate( (GameObject) Resources.Load( array2[1].Trim() ), localPosition, Quaternion.identity ) as GameObject;
									gameObject3.transform.SetParent( meshHolder.transform );
									gameObject3.transform.localPosition = Vector3.zero;
									gameObject3.transform.localRotation = Quaternion.identity;
									activeVessel.damagesystem.emergencyBlow = gameObject3.GetComponent<ParticleSystem>();
									gameObject3.GetComponent<AudioSource>().playOnAwake = false;
								}
								catch {
									Debug.LogError( "\tCould not create BowWaveParticle" );
								}
								break;
							}
						case "CavitationParticle": {
								try {
									GameObject gameObject2 = UnityEngine.Object.Instantiate( (GameObject) Resources.Load( array2[1].Trim() ), localPosition, Quaternion.identity ) as GameObject;
									gameObject2.transform.SetParent( meshHolder.transform );
									gameObject2.transform.localPosition = Vector3.zero;
									gameObject2.transform.localRotation = Quaternion.identity;
									activeVessel.vesselmovement.cavBubbles = gameObject2.GetComponent<ParticleSystem>();
								}
								catch {
									Debug.LogError( "\tCould not create BowWaveParticle" );
								}
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
								Vector3 audioPosition = activeVessel.vesselmovement.bowwaveHolder.transform.localPosition;
								transform2.localPosition = new Vector3( 0f, 0f, audioPosition.z );
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
						default:
							break;
					}
				}
				//Debug.Log( 1066 );
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
				//Debug.Log( 1087 );
				if( activeVessel.databaseshipdata.shipType != "BIOLOGIC" && activeVessel.databaseshipdata.shipType != "OILRIG" ) {
					activeVessel.damagesystem.hullDamageMeshes = new Mesh[10];
					activeVessel.damagesystem.hullDamageMeshes[0] = GetMesh( __instance, str + "_damage_11", modelPath, assetBundle, ref bin );
					activeVessel.damagesystem.hullDamageMeshes[1] = GetMesh( __instance, str + "_damage_12", modelPath, assetBundle, ref bin );
					activeVessel.damagesystem.hullDamageMeshes[2] = GetMesh( __instance, str + "_damage_21", modelPath, assetBundle, ref bin );
					activeVessel.damagesystem.hullDamageMeshes[3] = GetMesh( __instance, str + "_damage_22", modelPath, assetBundle, ref bin );
					activeVessel.damagesystem.hullDamageMeshes[4] = GetMesh( __instance, str + "_damage_31", modelPath, assetBundle, ref bin );
					activeVessel.damagesystem.hullDamageMeshes[5] = GetMesh( __instance, str + "_damage_32", modelPath, assetBundle, ref bin );
					activeVessel.damagesystem.hullDamageMeshes[6] = GetMesh( __instance, str + "_damage_41", modelPath, assetBundle, ref bin );
					activeVessel.damagesystem.hullDamageMeshes[7] = GetMesh( __instance, str + "_damage_42", modelPath, assetBundle, ref bin );
					activeVessel.damagesystem.hullDamageMeshes[8] = GetMesh( __instance, str + "_damage_51", modelPath, assetBundle, ref bin );
					activeVessel.damagesystem.hullDamageMeshes[9] = GetMesh( __instance, str + "_damage_52", modelPath, assetBundle, ref bin );
				}
				//Debug.Log( 1101 );
				activeVessel.damagesystem.damageMeshFilters = list5.ToArray();
				activeVessel.damagesystem.damageMeshes = list4.ToArray();
				activeVessel.damagesystem.objectMeshesToHide = list6.ToArray();
				activeVessel.damagesystem.radars = list3.ToArray();
				if( list2.Count > 0 ) {
					activeVessel.superstructureColliders = list2.ToArray();
				}
				//Debug.Log( 1109 );
				meshHolder = null;
				material = null;
				gameObject = null;
				if( assetBundle != null ) {
					assetBundle.Unload( false );
				}
				//Debug.Log( 1116 );
				return false;
			}
		}

		[HarmonyPatch( typeof( VesselBuilder ), "CreateAndPlaceWeaponMeshes" )]
		public class VesselBuilder_CreateAndPlaceWeaponMeshes_Patch
		{
			[HarmonyPrefix]
			public static bool Prefix( VesselBuilder __instance, GameObject weaponTemplate, int weaponID, string weaponPrefabRef ) {
				Debug.Log( "Building Weapon: " + weaponPrefabRef );
				Torpedo component = UIFunctions.globaluifunctions.database.databaseweapondata[weaponID].weaponObject.GetComponent<Torpedo>();
				Vector3 localPosition = Vector3.zero;
				Vector3 localRotation = Vector3.zero;
				Vector3 localScale = Vector3.one;
				Texture texture = null;
				Material[] material = null;
				AudioSource audioSource = null;
				Transform transform = weaponTemplate.transform;
				__instance.currentMesh = null;
				GameObject gameObject = null;
				string assetBundlePath = null;
				string modelPath = null;
				AssetBundle assetBundle = null;
				float speed = 0f;
				int countProps = 0;
				if( UIFunctions.globaluifunctions.database.databaseweapondata[weaponID].weaponType == "MISSILE" ) {
					component.boxcollider.size = new Vector3( 0.02f, 0.02f, 0.5f );
				}
				bool flag = false;
				bool flag2 = false;
				string[] array = UIFunctions.globaluifunctions.textparser.OpenTextDataFile( "weapons" );
				//bool isCustom = false;
				for( int i = 0; i < array.Length; i++ ) {
					string[] array2 = array[i].Split( '=' );
					if( array2[0].Trim() == "WeaponObjectReference" ) {
						if( array2[1].Trim() == weaponPrefabRef ) {
							flag = true;
						}
						assetBundlePath = null;
						modelPath = null;
						material = null;
					}
					else if( array2[0].Trim() == "[Model]" ) {
						flag2 = true;
					}
					else if( array2[0].Trim() == "[/Model]" && flag ) {
						if( assetBundle != null ) {
							assetBundle.Unload( false );
						}
						break;
					}
					if( !flag2 || !flag ) {
						continue;
					}
					switch( array2[0] ) {
						case "AssetBundle":
							if( assetBundlePath != null ) {
								assetBundle.Unload( false );
							}
							assetBundlePath = array2[1].Trim();
							assetBundle = AssetBundle.LoadFromFile( Application.streamingAssetsPath + "/override/" + assetBundlePath );
							break;
						case "ModelFile":
							if( assetBundlePath != null ) {
								modelPath = array2[1].Trim();
							}
							else if( array2[1].Trim().Contains( ".gltf" ) ) {
								Debug.Log( "\tGetModel (GLTF): " + array2[1] );
								__instance.allMeshes = glTFImporter.GetMeshes( Application.streamingAssetsPath + "/override/" + array2[1].Trim() );
							}
							else if( array2[1].Trim().Contains( ".glb" ) ) {
								Debug.LogError( "\tGLTF meshes must be in embedded format not binary format!" );
							}
							else {
								__instance.allMeshes = Resources.LoadAll<Mesh>( array2[1].Trim() );
							}
							break;
						case "MeshPosition":
							localPosition = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
							break;
						case "MeshRotation":
							localRotation = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
							break;
						case "MeshScale":
							localScale = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
							break;
						case "Material":
							material = GetMaterial( __instance, array2[1].Trim(), modelPath, assetBundle );
							break;
						case "MaterialTextures":
							if( material != null ) {
								string[] array4 = array2[1].Trim().Split( ',' );
								if( array4.Length > 0 ) {
									material[0].SetTexture( "_MainTex", UIFunctions.globaluifunctions.textparser.GetTexture( array4[0] ) );
								}
								if( array4.Length > 1 ) {
									material[0].SetTexture( "_SpecTex", UIFunctions.globaluifunctions.textparser.GetTexture( array4[1] ) );
								}
								if( array4.Length > 2 ) {
									material[0].SetTexture( "_BumpMap", UIFunctions.globaluifunctions.textparser.GetTexture( array4[2] ) );
								}
							}
							break;
						case "MeshWeapon":
							component.torpedoMeshes[0].GetComponent<MeshFilter>().mesh = GetMesh( __instance, array2[1].Trim(), modelPath, assetBundle, ref material );
                            if( material != null ) {
								component.torpedoMeshes[0].GetComponent<MeshRenderer>().sharedMaterials = material;
							}
							component.torpedoMeshes[0].transform.localPosition = localPosition;
							component.torpedoMeshes[0].transform.localEulerAngles = localRotation;
							component.torpedoMeshes[0].transform.localScale = localScale;
							break;
						case "MeshWeaponCanister":
							component.torpedoMeshes[1].GetComponent<MeshFilter>().mesh = GetMesh( __instance, array2[1].Trim(), modelPath, assetBundle, ref material );
							if( material != null ) {
								component.torpedoMeshes[1].GetComponent<MeshRenderer>().sharedMaterials = material;
							}
							component.torpedoMeshes[1].transform.localPosition = localPosition;
							component.torpedoMeshes[1].transform.localEulerAngles = localRotation;
							component.torpedoMeshes[1].transform.localScale = localScale;
							component.torpedoMeshes[1].gameObject.SetActive( value: true );
							component.torpedoMeshes[0].layer = 17;
							break;
						case "MeshWeaponPropRotation":
							speed = float.Parse( array2[1].Trim() );
							break;
						case "MeshWeaponProp":
							component.torpedoPropMeshes[countProps].GetComponent<MeshFilter>().mesh = GetMesh( __instance, array2[1].Trim(), modelPath, assetBundle, ref material );
							if( material != null ) {
								component.torpedoPropMeshes[countProps].GetComponent<MeshRenderer>().sharedMaterials = material;
							}
							component.torpedoPropMeshes[countProps].transform.localPosition = localPosition;
							component.torpedoPropMeshes[countProps].transform.localEulerAngles = localRotation;
							component.torpedoPropMeshes[countProps].transform.localScale = localScale;
							component.torpedoPropMeshes[countProps].transform.localRotation = Quaternion.Slerp( Quaternion.identity, Quaternion.Euler( -90f, 0f, 0f ), 1f );
							component.propRotations[countProps].speed = speed;
							countProps++;
							break;
						case "MeshMissileBooster":
							component.boosterMesh.GetComponent<MeshFilter>().mesh = GetMesh( __instance, array2[1].Trim(), modelPath, assetBundle, ref material );
							if( material != null ) {
								component.boosterMesh.GetComponent<MeshRenderer>().sharedMaterials = material;
							}
							component.boosterMesh.transform.localPosition = localPosition;
							component.boosterMesh.transform.localEulerAngles = localRotation;
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
							if( assetBundlePath != null && modelPath != null && array2[1].Contains( ".prefab" ) ) {
								//AssetBundle assetBundle = AssetBundle.LoadFromFile( Application.streamingAssetsPath + "/override/" + assetBundlePath );
								GameObject particleSystem = assetBundle.LoadAsset<GameObject>( array2[1].Trim() );
								if( particleSystem != null ) {
									particleSystem.transform.SetParent( component.cavitationTransform.parent );
									Destroy( component.cavitationTransform.gameObject );
									component.cavitationTransform = particleSystem.transform;
									component.torpedoTrail = particleSystem.GetComponent<ParticleSystem>();
									Debug.Log( "\tCustom Particle: " + assetBundlePath + " " + array2[1].Trim() );
								}
								//assetBundle.Unload( false );
							}
							component.cavitationTransform.localPosition = localPosition;
							break;
						case "MissileTrailParticle":
							if( assetBundlePath != null && modelPath != null && array2[1].Contains( ".prefab" ) ) {
								//AssetBundle assetBundle = AssetBundle.LoadFromFile( Application.streamingAssetsPath + "/override/" + assetBundlePath );
								GameObject particleSystem = assetBundle.LoadAsset<GameObject>( array2[1].Trim() );
								if( particleSystem != null ) {
									particleSystem.transform.SetParent( component.boosterParticleTransform.parent );
									Destroy( component.boosterParticleTransform.gameObject );
									component.boosterParticleTransform = particleSystem.transform;
									component.boosterRelease = particleSystem.GetComponent<ParticleSystem>();
									Debug.Log( "\tCustom Particle: " + assetBundlePath + " " + array2[1].Trim() );
								}
								//assetBundle.Unload( false );
							}
							component.missileTrailTransform.localPosition = localPosition;
							break;
						case "BoosterParticle":
							if( assetBundlePath != null && modelPath != null && array2[1].Contains( ".prefab" ) ) {
								//AssetBundle assetBundle = AssetBundle.LoadFromFile( Application.streamingAssetsPath + "/override/" + assetBundlePath );
								GameObject particleSystem = assetBundle.LoadAsset<GameObject>( array2[1].Trim() );
								if( particleSystem != null ) {
									particleSystem.transform.SetParent( component.boosterParticleTransform.parent );
									Destroy( component.boosterParticleTransform.gameObject );
									component.boosterParticleTransform = particleSystem.transform;
									component.boosterRelease = particleSystem.GetComponent<ParticleSystem>();
									Debug.Log( "\tCustom Particle: " + assetBundlePath + " " + array2[1].Trim() );
								}
								//assetBundle.Unload( false );
							}
							component.boosterParticleTransform.localPosition = localPosition;
							break;
						case "ParachuteParticle":
							if( assetBundlePath != null && modelPath != null && array2[1].Contains( ".prefab" ) ) {
								//AssetBundle assetBundle = AssetBundle.LoadFromFile( Application.streamingAssetsPath + "/override/" + assetBundlePath );
								GameObject particleSystem = assetBundle.LoadAsset<GameObject>( array2[1].Trim() );
								if( particleSystem != null ) {
									particleSystem.transform.SetParent( component.boosterParticleTransform.parent );
									Destroy( component.boosterParticleTransform.gameObject );
									component.boosterParticleTransform = particleSystem.transform;
									component.boosterRelease = particleSystem.GetComponent<ParticleSystem>();
									Debug.Log( "\tCustom Particle: " + assetBundlePath + " " + array2[1].Trim() );
								}
								//assetBundle.Unload( false );
							}
							component.parachuteTransform.localPosition = localPosition;
							break;
						default:
							break;
					}
				}
				if( assetBundle != null ) {
					assetBundle.Unload( false );
				}
				return false;
			}
		}

		[HarmonyPatch( typeof( VesselBuilder ), "CreateAndPlaceAircraftMeshes" )]
		public class VesselBuilder_CreateAndPlaceAircraftMeshes_Patch
		{
			[HarmonyPrefix]
			public static bool Prefix( VesselBuilder __instance, GameObject aircraftTemplate, int aircraftID, bool isHelicopter, string aircraftPrefabRef ) {
				Debug.Log( "Building Aircraft: " + aircraftPrefabRef );
				Aircraft aircraft = null;
				Helicopter helicopter = null;
				if( isHelicopter ) {
					helicopter = aircraftTemplate.GetComponent<Helicopter>();
					helicopter.databaseaircraftdata = UIFunctions.globaluifunctions.database.databaseaircraftdata[aircraftID];
					if( helicopter.databaseaircraftdata.passiveSonarID >= 0 || helicopter.databaseaircraftdata.passiveSonarID >= 0 ) {
						helicopter.sonarLine.gameObject.SetActive( true );
					}
				}
				else {
					aircraft = aircraftTemplate.GetComponent<Aircraft>();
					aircraft.databaseaircraftdata = UIFunctions.globaluifunctions.database.databaseaircraftdata[aircraftID];
				}
				Transform transform = null;
				transform = ( ( !isHelicopter ) ? aircraft.meshHolder : aircraftTemplate.transform );
				List<GameObject> list = new List<GameObject>();
				Vector3 meshPosition = Vector3.zero;
				Vector3 meshRotation = Vector3.zero;
				Vector3 localScale = Vector3.one;
				Material[] material = null;
				__instance.currentMesh = null;
				GameObject gameObject = null;
				string assetBundlePath = null;
				string modelPath = null;
				AssetBundle assetBundle = null;
				float speed = 0f;
				bool flag = false;
				bool flag2 = false;
				string[] array = UIFunctions.globaluifunctions.textparser.OpenTextDataFile( "aircraft" );
				for( int i = 0; i < array.Length; i++ ) {
					string[] array2 = array[i].Split( '=' );
					if( array2[0].Trim() == "AircraftObjectReference" ) {
						if( array2[1].Trim() == aircraftPrefabRef ) {
							flag = true;
						}
						assetBundlePath = null;
						modelPath = null;
					}
					else if( array2[0].Trim() == "[Model]" ) {
						flag2 = true;
					}
					else if( array2[0].Trim() == "[/Model]" && flag ) {
						if( assetBundlePath != null ) {
							assetBundle.Unload( false );
						}
						break;
					}
					if( !flag2 || !flag ) {
						continue;
					}
					switch( array2[0] ) {
						case "AssetBundle":
							if( assetBundlePath != null ) {
								assetBundle.Unload( false );
							}
							assetBundlePath = array2[1].Trim();
							assetBundle = AssetBundle.LoadFromFile( Application.streamingAssetsPath + "/override/" + assetBundlePath );
							break;
						case "ModelFile":
							if( assetBundlePath != null ) {
								modelPath = array2[1].Trim();
							}
							else if( array2[1].Trim().Contains( ".gltf" ) ) {
								Debug.Log( "\tGetModel (GLTF): " + array2[1] );
								__instance.allMeshes = glTFImporter.GetMeshes( Application.streamingAssetsPath + "/override/" + array2[1].Trim() );
								//foreach( var mesh in __instance.allMeshes ) {
								//	Debug.Log( "\t\t" + mesh.name );
								//}
							}
							else if( array2[1].Trim().Contains( ".glb" ) ) {
								Debug.LogError( "\tGLTF meshes must be in embedded format not binary format!" );
							}
							else {
								__instance.allMeshes = Resources.LoadAll<Mesh>( array2[1].Trim() );
							}
							break;
						case "MeshPosition":
							meshPosition = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
							break;
						case "MeshRotation":
							meshRotation = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
							break;
						case "MeshScale":
							localScale = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
							break;
						case "Material":
							material = GetMaterial( __instance, array2[1].Trim(), modelPath, assetBundle );
							break;
						case "MaterialTextures":
							if( material != null ) {
								string[] array3 = array2[1].Trim().Split( ',' );
								material[0].SetTexture( "_MainTex", UIFunctions.globaluifunctions.textparser.GetTexture( array3[0] ) );
								if( array3.Length > 1 ) {
									material[0].SetTexture( "_SpecTex", UIFunctions.globaluifunctions.textparser.GetTexture( array3[1] ) );
								}
								if( array3.Length > 2 ) {
									material[0].SetTexture( "_BumpMap", UIFunctions.globaluifunctions.textparser.GetTexture( array3[2] ) );
								}
							}
							break;
						case "MeshAircraftBody":
							gameObject = SetupMesh( __instance, transform, meshPosition, meshRotation, material, array2[1].Trim(), modelPath, assetBundle );
							if( isHelicopter ) {
								helicopter.helibody = gameObject.transform;
								helicopter.sonarLine.transform.SetParent( gameObject.transform );
								helicopter.raycastPosition.SetParent( gameObject.transform );
								helicopter.raycastPosition.localPosition = Vector3.zero;
								helicopter.raycastPosition.localRotation = Quaternion.Euler( -5f, 0f, 0f );
							}
							break;
						case "DippingSonarPosition":
							if( isHelicopter ) {
								helicopter.sonarLine.transform.localRotation = Quaternion.identity;
								helicopter.sonarLine.transform.localPosition = UIFunctions.globaluifunctions.textparser.PopulateVector3( array2[1].Trim() );
							}
							break;
						case "HoverParticle":
							if( isHelicopter ) {
								GameObject particleSystem;
								if( assetBundlePath != null && modelPath != null && array2[1].Contains( ".prefab" ) ) {
									particleSystem = UnityEngine.Object.Instantiate( assetBundle.LoadAsset<GameObject>( array2[1].Trim() ) );
								}
                                else {
									particleSystem = UnityEngine.Object.Instantiate( Resources.Load( array2[1].Trim() ), helicopter.transform.position, helicopter.transform.rotation ) as GameObject;
								}
								particleSystem.transform.SetParent( helicopter.transform );
								particleSystem.transform.localPosition = Vector3.zero;
								particleSystem.transform.localRotation = Quaternion.identity;
								helicopter.hoverparticle = particleSystem.GetComponent<ParticleSystem>();
								helicopter.hoverparticle.Stop();
							}
							break;
						case "MeshSpeed":
							speed = float.Parse( array2[1].Trim() );
							break;
						case "MeshAircraftProp":
							gameObject = SetupMesh( __instance, transform, meshPosition, meshRotation, material, array2[1].Trim(), modelPath, assetBundle );
							Radar radar = gameObject.AddComponent<Radar>();
							radar.speed = speed;
							if( isHelicopter ) {
								gameObject.transform.SetParent( helicopter.helibody );
							}
							break;
						case "AudioClip":
							if( helicopter != null ) {
								helicopter.audiosource.clip = UIFunctions.globaluifunctions.textparser.GetAudioClip( array2[1].Trim() );
							}
							else {
								aircraft.audiosource.clip = UIFunctions.globaluifunctions.textparser.GetAudioClip( array2[1].Trim() );
							}
							break;
						case "AudioRollOff":
							AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
							if( array2[1].Trim() != "LOGARITHMIC" ) {
								rolloffMode = AudioRolloffMode.Linear;
							}
							if( helicopter != null ) {
								helicopter.audiosource.rolloffMode = rolloffMode;
							}
							else {
								aircraft.audiosource.rolloffMode = rolloffMode;
							}
							break;
						case "AudioDistance":
							string[] array4 = array2[1].Trim().Split( ',' );
							if( helicopter != null ) {
								helicopter.audiosource.minDistance = float.Parse( array4[0] );
								helicopter.audiosource.maxDistance = float.Parse( array4[1] );
							}
							else {
								aircraft.audiosource.minDistance = float.Parse( array4[0] );
								aircraft.audiosource.maxDistance = float.Parse( array4[1] );
							}
							break;
						case "AudioPitch":
							if( helicopter != null ) {
								helicopter.audiosource.pitch = float.Parse( array2[1].Trim() );
							}
							else {
								aircraft.audiosource.pitch = float.Parse( array2[1].Trim() );
							}
							break;
						case "MeshHardpoint":
							gameObject = SetupMesh( __instance, transform, meshPosition, meshRotation, material, array2[1].Trim(), modelPath, assetBundle );
							list.Add( gameObject );
							if( isHelicopter ) {
								helicopter.torpedoHardpoints = list.ToArray();
								gameObject.transform.SetParent( helicopter.helibody );
							}
							break;
						default:
							break;
					}
				}
				if( assetBundle != null ) {
					assetBundle.Unload( false );
				}
				return false;
			}

		}

		//[HarmonyPatch( typeof( Enemy_AntiMissileGuns ), "InitialiseEnemyMissileDefense" )]
		//public class Enemy_AntiMissileGuns_InitialiseEnemyMissileDefense_Patch
		//{
		//	[HarmonyPrefix]
		//	public static bool Prefix( Enemy_AntiMissileGuns __instance ) {
		//		__instance.gunRangeCollider.radius = __instance.parentVessel.databaseshipdata.gunRange * GameDataManager.inverseYardsScale;
		//		__instance.tracers = new ParticleSystem[__instance.turrets.Length];
		//		__instance.tracerPointLights = new PointLight[__instance.turrets.Length];
		//		__instance.tracerAudios = new AudioSource[__instance.turrets.Length];
		//		string ciwsParticle = __instance.parentVessel.databaseshipdata.ciwsParticle;
		//		for( int i = 0; i < __instance.barrels.Length; i++ ) {
		//			GameObject gameObject = UnityEngine.Object.Instantiate( (GameObject) Resources.Load( ciwsParticle ), __instance.barrels[i].position, __instance.barrels[i].rotation ) as GameObject;
		//			gameObject.transform.SetParent( __instance.barrels[i] );
		//			if( patcher.customVessels.Contains( __instance.parentVessel ) ) {
		//				gameObject.transform.localPosition = Vector3.zero;
		//				foreach( Transform transform in gameObject.transform ) {
		//					transform.localPosition = Vector3.zero;
		//				}
		//			}
		//			else {
		//				gameObject.transform.localPosition = new Vector3( 0f, 0.0046f, 0.0234f );
		//			}
		//			gameObject.transform.localRotation = Quaternion.Slerp( Quaternion.identity, Quaternion.Euler( 90f, 0f, 0f ), 1f );
		//			__instance.tracers[i] = gameObject.GetComponent<ParticleSystem>();
		//			__instance.tracerPointLights[i] = gameObject.GetComponentInChildren<PointLight>();
		//			__instance.tracerAudios[i] = gameObject.GetComponent<AudioSource>();
		//		}
		//		GameObject gameObject2 = UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.blankTransform, __instance.gameObject.transform.position, Quaternion.identity ) as GameObject;
		//		gameObject2.transform.SetParent( __instance.parentVessel.transform, worldPositionStays: true );
		//		gameObject2.transform.localRotation = Quaternion.identity;
		//		gameObject2.transform.localPosition = Vector3.zero;
		//		__instance.directionFinder = gameObject2.transform;
		//		gameObject2.name = "Anti-Missile Direction Finder";
		//		return false;
		//	}
		//}

		[HarmonyPatch(typeof( TextParser ), "OpenTextDataFile")]
		public class OpenTextDataFile_Patch
        {
			[HarmonyPrefix]
			public static bool Prefix ( TextParser __instance, ref string[] __result, string filename ) {
                if( filename == "weapons" && patcher.weaponList != null) {
					__result = patcher.weaponList;
					return false;
                }
				else if( filename == "aircraft" && patcher.aircraftList != null ) {
					__result = patcher.aircraftList;
					return false;
				}
				//Debug.Log( "ImporterPatch.cs - OpenTextDataFile_Patch - Prefix() - " + filename + " read." );
				if( File.Exists( Application.streamingAssetsPath + "/override/" + filename + ".txt" ) ) {
					//Debug.Log( "Found in override." );
					__result = File.ReadAllLines( Application.streamingAssetsPath + "/override/" + filename + ".txt" );
                }
                else if( File.Exists( Application.streamingAssetsPath + "/default/" + filename + ".txt" ) ) {
					//Debug.Log( "Found in default." );
					__result = File.ReadAllLines( Application.streamingAssetsPath + "/default/" + filename + ".txt" );
				}
                else {
					Debug.LogError( "Not Found - " + filename );
					__result = null;
					UIFunctions.globaluifunctions.SetPlayerErrorMessage( "ERROR:  \"" + Application.streamingAssetsPath + "/default/" + filename + ".txt" + "\"  not found" );
				}
				List<string> returnList = __result.ToList<string>();
				if( filename.Contains( "missions_single" ) ) {
					Debug.Log( "Single Mission List Extension" );
					foreach( string filePath in Directory.GetFiles( Application.streamingAssetsPath + "/override", "single*.txt" ) ) {
                        foreach( string line in File.ReadAllLines(filePath) ) {
                            if( line.Contains(filename.Split('\\')[0]) && !returnList.Contains(line.Split('=')[1].Trim()) ) {
								Debug.Log( '\t' + line.Split( '=' )[1].Trim() + " added to single missions in " + filename.Split( '\\' )[0] );
								returnList.Add( line.Split( '=' )[1].Trim() );
								break;
                            }
                            else if( line.Contains( filename.Split( '\\' )[0] ) && returnList.Contains( line.Split( '=' )[1].Trim() ) ) {
								Debug.LogWarning( "\tTried to add single mission that already exists: " + line.Split( '=' )[1].Trim() );
								break;
							}
                        }
                    }
					__result = returnList.ToArray();
				}
                else if( filename.Contains( "sensor_display_names" ) ) {
					if( Directory.Exists( Application.streamingAssetsPath + "/override/sensors" ) ) {
						Debug.Log( "Sensor Display Name Extension" );
						// Break the file up into weapons, depth weapons, and countermeasures
						List<string> sonarData = new List<string>();
						List<string> radarData = new List<string>();
						// List of the reference to check for duplication
						List<string> sonar = new List<string>();
						List<string> radar = new List<string>();
						int flag = 0;
						foreach( string line in returnList ) {
							switch( line ) {
								case "[SONAR]":
									flag = 1;
									sonarData.Add( line );
									break;
								case "[RADAR]":
									flag = 2;
									radarData.Add( line );
									break;
								default:									
									switch( flag ) {
										case 1:
											sonarData.Add( line );
											if( line.Contains( '=' ) ) {
												sonar.Add( line.Split( '=' )[0] );
											}
											break;
										case 2:
											radarData.Add( line );
											if( line.Contains( '=' ) ) {
												radar.Add( line.Split( '=' )[0] );
											}
											break;
										default:
											break;
									}
									break;
							}
						}
						foreach( string filePath in Directory.GetFiles( Application.streamingAssetsPath + "/override/sensors", "sonar_*.txt" ) ) {
							string[] sonarFile = File.ReadAllLines( filePath );
							string sonarModel = "";
							foreach( string item in sonarFile ) {
								if( item.Contains( filename.Split( '\\' )[0] ) && !sonar.Contains( item.Split( '=' )[1] ) ) {
									Debug.Log( '\t' + item.Split( '=' )[1] + " added to sensor descriptions" );
									List<string> description = item.Split( '=' ).ToList();
									description.RemoveAt( 0 );
									sonar.Add( item.Split( '=' )[1] );
									sonarData.Add( description.Join( delimiter: "=" ) );
									break;
								}
								else if( item.Contains( filename.Split( '\\' )[0] ) && sonar.Contains( item.Split( '=' )[1] ) ) {
									Debug.LogWarning( "\tTried to description that already exists from: override\\sensors\\" + Path.GetFileName( filePath ) );
								}
                                if( item.Contains( "SonarModel" ) ) {
									sonarModel = item.Split( '=' )[1];

								}
							}
							if( sonarModel!= "" && !sonar.Contains( sonarModel ) ) {
								Debug.LogError( "\tNo description available for: override\\sensors\\" + Path.GetFileName( filePath ) );
								sonarData.Add( sonarModel + "=Error! No Proper Name=Error! No Proper Description" );
							}
						}
						foreach( string filePath in Directory.GetFiles( Application.streamingAssetsPath + "\\override\\sensors", "radar_*.txt" ) ) {
							string[] radarFile = File.ReadAllLines( filePath );
							string radarModel = "";
							foreach( string item in radarFile ) {
								if( item.Contains( filename.Split( '\\' )[0] ) && !radar.Contains( item.Split( '=' )[1] ) ) {
									Debug.Log( '\t' + item.Split( '=' )[1] + " added to sensor descriptions" );
									List<string> description = item.Split( '=' ).ToList();
									description.RemoveAt( 0 );
									radar.Add( item.Split( '=' )[1] );
									radarData.Add( description.Join( delimiter: "=" ) );
									break;
								}
								else if( item.Contains( filename.Split( '\\' )[0] ) && radar.Contains( item.Split( '=' )[1] ) ) {
									Debug.LogWarning( "\tTried to description that already exists from:  override\\sensors\\" + Path.GetFileName( filePath ) );
								}
								if( item.Contains( "SonarModel" ) ) {
									radarModel = item.Split( '=' )[1];

								}
							}
							if( radarModel != "" && !sonar.Contains( radarModel ) ) {
								Debug.LogError( "\tNo description available for: override\\sensors\\" + Path.GetFileName( filePath ) );
								sonarData.Add( radarModel + "=Error! No Proper Name=Error! No Proper Description" );
							}
						}
						returnList = new List<string>();
						returnList.AddRange( sonarData );
						returnList.AddRange( radarData );
						__result = returnList.ToArray();
					}
				}
				else if( filename.Contains( "depth_weapon_display_names" ) ) {
					if( Directory.Exists( Application.streamingAssetsPath + "/override/weapons" ) ) {
						Debug.Log( "Depth Weapon Display Name Extension" );
						// Break the file up into weapons, depth weapons, and countermeasures
						List<string> mortarData = new List<string>();
						List<string> gunData = new List<string>();
						// List of the reference to check for duplication
						List<string> mortar = new List<string>();
						List<string> gun = new List<string>();
						int flag = 0;
						foreach( string line in returnList ) {
							switch( line ) {
								case "[MORTARS]":
									flag = 1;
									mortarData.Add( line );
									break;
								case "[GUNS]":
									flag = 2;
									gunData.Add( line );
									break;
								default:
									switch( flag ) {
										case 1:
											mortarData.Add( line );
											if( line.Contains( '=' ) ) {
												mortar.Add( line.Split( '=' )[0] );
											}
											break;
										case 2:
											gunData.Add( line );
											if( line.Contains( '=' ) ) {
												gun.Add( line.Split( '=' )[0] );
											}
											break;
										default:
											break;
									}
									break;
							}
						}
						foreach( string filePath in Directory.GetFiles( Application.streamingAssetsPath + "\\override\\weapons", "mortar_*.txt" ) ) {
							string[] mortarFile = File.ReadAllLines( filePath );
							string mortarModel = "";
							foreach( string item in mortarFile ) {
								if( item.Contains( filename.Split( '\\' )[0] ) && !mortar.Contains( item.Split( '=' )[1] ) ) {
									Debug.Log( '\t' + item.Split( '=' )[1] + " added to weapon descriptions" );
									List<string> description = item.Split( '=' ).ToList();
									description.RemoveAt( 0 );
									mortar.Add( item.Split( '=' )[1] );
									mortarData.Add( description.Join( delimiter: "=" ) );
									break;
								}
								else if( item.Contains( filename.Split( '\\' )[0] ) && mortar.Contains( item.Split( '=' )[1] ) ) {
									Debug.LogWarning( "\tTried to description that already exists from: override\\weapons\\" + Path.GetFileName( filePath ) );
								}
								if( item.Contains( "DepthWeaponObjectReference" ) ) {
									mortarModel = item.Split( '=' )[1];
								}
							}
							if( mortarModel != "" && !mortar.Contains( mortarModel ) ) {
								Debug.LogError( "\tNo description available for: override\\weapons\\" + Path.GetFileName( filePath ) );
								mortarData.Add( mortarModel + "=Error! No Proper Name=Error! No Proper Description" );
							}
						}
						foreach( string filePath in Directory.GetFiles( Application.streamingAssetsPath + "\\override\\weapons", "gun_*.txt" ) ) {
							string[] gunFile = File.ReadAllLines( filePath );
							string gunModel = "";
							foreach( string item in gunFile ) {
								if( item.Contains( filename.Split( '\\' )[0] ) && !gun.Contains( item.Split( '=' )[1] ) ) {
									Debug.Log( '\t' + item.Split( '=' )[1] + " added to weapon descriptions" );
									List<string> description = item.Split( '=' ).ToList();
									description.RemoveAt( 0 );
									gun.Add( item.Split( '=' )[1] );
									gunData.Add( description.Join( delimiter: "=" ) );
									continue;
								}
								else if( item.Contains( filename.Split( '\\' )[0] ) && gun.Contains( item.Split( '=' )[1] ) ) {
									Debug.LogWarning( "\tTried to description that already exists from:  override\\weapons\\" + Path.GetFileName( filePath ) );
								}
								if( item.Contains( "DepthWeaponObjectReference" ) ) {
									gunModel = item.Split( '=' )[1];
								}
							}
							if( gunModel != "" && !gun.Contains( gunModel ) ) {
								Debug.LogError( "\tNo description available for: override\\weapons\\" + Path.GetFileName( filePath ) );
								gunData.Add( gunModel + "=Error! No Proper Name=Error! No Proper Description" );
							}
						}
						returnList = new List<string>();
						returnList.AddRange( mortarData );
						returnList.AddRange( gunData );
						__result = returnList.ToArray();
					}
				}
				else {
					switch( filename ) {
						case "sensors":
							Debug.Log( "Sensor List Extension" );
							if( Directory.Exists( Application.streamingAssetsPath + "/override/sensors" ) ) {
								// Break the file up into weapons, depth weapons, and countermeasures
								List<string> sonarData = new List<string>();
								List<string> radarData = new List<string>();
								// List of the reference to check for duplication
								List<string> sonar = new List<string>();
								List<string> radar = new List<string>();
								int flag = 0;
								foreach( string line in returnList ) {
									switch( line ) {
										case "[Sonar]":
											flag = 1;
											sonarData.Add( line );
											break;
										case "[RADAR]":
											flag = 2;
											radarData.Add( line );
											break;
										default:
											if( line.Contains( "SonarModel" ) ) {
												sonar.Add( line.Split( '=' )[1] );
											}
											if( line.Contains( "RADARModel" ) ) {
												radar.Add( line.Split( '=' )[1] );
											}
											switch( flag ) {
												case 1:
													sonarData.Add( line );
													break;
												case 2:
													radarData.Add( line );
													break;
												default:
													break;
											}
											break;
									}
								}
								foreach( string filePath in Directory.GetFiles( Application.streamingAssetsPath + "/override/sensors", "sonar_*.txt" ) ) {
									string[] sonarFile = File.ReadAllLines( filePath );
									foreach( string item in sonarFile ) {
										if( item.Contains( "SonarModel" ) ) {
											string newName = item.Split( '=' )[1];
											if( sonar.Contains( newName ) ) {
												Debug.LogWarning( "\tTried to add sonar that already exists: " + filePath );
											}
											else {
												Debug.Log( '\t' + Path.GetFileNameWithoutExtension( filePath ) + " added to sonar" );
												sonarData.AddRange( sonarFile );
											}
											continue;
										}
									}
								}
								foreach( string filePath in Directory.GetFiles( Application.streamingAssetsPath + "/override/sensors", "radar_*.txt" ) ) {
									string[] radarFile = File.ReadAllLines( filePath );
									foreach( string item in radarFile ) {
										if( item.Contains( "RADARModel" ) ) {
											string newName = item.Split( '=' )[1];
											if( radar.Contains( newName ) ) {
												Debug.LogWarning( "\tTried to add radar that already exists: " + filePath );
											}
											else {
												Debug.Log( '\t' + Path.GetFileNameWithoutExtension( filePath ) + " added to radar" );
												radarData.AddRange( radarFile );
											}
											continue;
										}
									}
								}
								returnList = new List<string>();
								returnList.AddRange( sonarData );
								returnList.AddRange( radarData );
								__result = returnList.ToArray();
							}
							break;
						case "weapons":
							Debug.Log( "Weapon List Extension" );
							if( Directory.Exists( Application.streamingAssetsPath + "/override/weapons" ) ) {
								// Break the file up into weapons, depth weapons, and countermeasures
								List<string> weaponData = new List<string>();
								List<string> depthWeaponData = new List<string>();
								List<string> countermeasureData = new List<string>();
								List<string> weapon = new List<string>();
								List<string> depthWeapon = new List<string>();
								List<string> countermeasure = new List<string>();
								int flag = 0;
								foreach( string line in returnList ) {
									switch( line ) {
										case "[Torpedoes and Missiles]":
											flag = 1;
											weaponData.Add( line );
											break;
										case "[Depth Charges, Mortars and Shells]":
											flag = 2;
											depthWeaponData.Add( line );
											break;
										case "[Countermeasures]":
											flag = 3;
											countermeasureData.Add( line );
											break;
										default:
											if( line.Contains( "WeaponObjectReference" ) ) {
												weapon.Add( line.Split( '=' )[1] );
											}
											if( line.Contains( "DepthWeaponObjectReference" ) ) {
												depthWeapon.Add( line.Split( '=' )[1] );
											}
											if( line.Contains( "CountermeasureObjectReference" ) ) {
												countermeasure.Add( line.Split( '=' )[1] );
											}
											switch( flag ) {
												case 1:
													weaponData.Add( line );
													break;
												case 2:
													depthWeaponData.Add( line );
													break;
												case 3:
													countermeasureData.Add( line );
													break;
												default:
													break;
											}
											break;
									}
								}
								foreach( string filePath in Directory.GetFiles( Application.streamingAssetsPath + "/override/weapons", "weapon_*.txt" ) ) {
									string[] weaponFile = File.ReadAllLines( filePath );
									foreach( string item in weaponFile ) {
										if( item.Contains( "WeaponObjectReference" ) ) {
											string newName = item.Split( '=' )[1];
											if( weapon.Contains( newName ) ) {
												Debug.LogWarning( "\tTried to add weapon that already exists: " + filePath );
											}
											else {
												Debug.Log( '\t' + Path.GetFileNameWithoutExtension( filePath ) + " added to weapons" );
												weaponData.AddRange( weaponFile );
											}
											continue;
										}
									}
								}
								foreach( string filePath in Directory.GetFiles( Application.streamingAssetsPath + "/override/weapons", "mortar_*.txt" ) ) {
									string[] depthWeaponFile = File.ReadAllLines( filePath );
									foreach( string item in depthWeaponFile ) {
										if( item.Contains( "DepthWeaponObjectReference" ) ) {
											string newName = item.Split( '=' )[1];
											if( depthWeapon.Contains( newName ) ) {
												Debug.LogWarning( "\tTried to add depth weapon that already exists: " + filePath );
											}
											else {
												Debug.Log( '\t' + Path.GetFileNameWithoutExtension( filePath ) + " added to depth weapons" );
												depthWeaponData.AddRange( depthWeaponFile );
											}
											continue;
										}
									}
								}
								foreach( string filePath in Directory.GetFiles( Application.streamingAssetsPath + "/override/weapons", "gun_*.txt" ) ) {
									string[] depthWeaponFile = File.ReadAllLines( filePath );
									foreach( string item in depthWeaponFile ) {
										if( item.Contains( "DepthWeaponObjectReference" ) ) {
											string newName = item.Split( '=' )[1];
											if( depthWeapon.Contains( newName ) ) {
												Debug.LogWarning( "\tTried to add depth weapon that already exists: " + filePath );
											}
											else {
												Debug.Log( '\t' + Path.GetFileNameWithoutExtension( filePath ) + " added to depth weapons" );
												depthWeaponData.AddRange( depthWeaponFile );
											}
											continue;
										}
									}
								}
								foreach( string filePath in Directory.GetFiles( Application.streamingAssetsPath + "/override/weapons", "countermeasure_*.txt" ) ) {
									string[] countermeasureFile = File.ReadAllLines( filePath );
									foreach( string item in countermeasureFile ) {
										if( item.Contains( "CountermeasureObjectReference" ) ) {
											string newName = item.Split( '=' )[1];
											if( countermeasure.Contains( newName ) ) {
												Debug.LogWarning( "\tTried to add countermeasure that already exists: " + filePath );
											}
											else {
												Debug.Log( '\t' + Path.GetFileNameWithoutExtension( filePath ) + " added to countermeasure" );
												countermeasureData.AddRange( countermeasureFile );
											}
											continue;
										}
									}
								}
								returnList = new List<string>();
								returnList.AddRange( weaponData );
								returnList.AddRange( depthWeaponData );
								returnList.AddRange( countermeasureData );
								__result = returnList.ToArray();
								patcher.weaponList = returnList.ToArray();
							}
							break;
						case "aircraft":
							Debug.Log( "Aircraft List Extension" );
							if( Directory.Exists( Application.streamingAssetsPath + "/override/aircraft" ) ) {
								List<string> aircraft = new List<string>();
								foreach( string line in returnList ) {
									if( line.Contains( "AircraftObjectReference" ) ) {
										aircraft.Add( line.Split( '=' )[1] );
									}
								}
								foreach( string filePath in Directory.GetFiles( Application.streamingAssetsPath + "/override/aircraft", "aircraft_*.txt" ) ) {
									string[] aircraftData = File.ReadAllLines( filePath );
									foreach( string item in aircraftData ) {
										if( item.Contains( "AircraftObjectReference" ) ) {
											string newName = item.Split( '=' )[1];
											if( aircraft.Contains( newName ) ) {
												Debug.LogWarning( "\tTried to add aircraft that already exists: " + filePath );
											}
											else {
												Debug.Log( '\t' + Path.GetFileNameWithoutExtension( filePath ) + " added to aircraft" );
												returnList.AddRange( aircraftData );
											}
											continue;
										}
									}
								}
								__result = returnList.ToArray();
								patcher.aircraftList = returnList.ToArray();
							}
							break;
						case "vessels\\_vessel_list":
							if( Directory.Exists( Application.streamingAssetsPath + "/override/vessels" ) ) {
								Debug.Log( "Vessel List Extension" );
								Dictionary<string, Dictionary<string, List<string>>> vesselDict = new Dictionary<string, Dictionary<string, List<string>>>();
                                
								foreach( string vesselFileName in Directory.GetFiles( Application.streamingAssetsPath + "/override/vessels", "*.txt" ) ) {
									if( Path.GetFileNameWithoutExtension( vesselFileName ) != "_vessel_list" && !returnList.Contains( Path.GetFileNameWithoutExtension( vesselFileName ) ) ) {
										returnList.Add( Path.GetFileNameWithoutExtension( vesselFileName ) );
										Debug.Log( '\t' + Path.GetFileNameWithoutExtension( vesselFileName ) + " added to _vessel_list" );
										__result = returnList.ToArray();
									}
									else if( Path.GetFileNameWithoutExtension( vesselFileName ) != "_vessel_list" && returnList.Contains( Path.GetFileNameWithoutExtension( vesselFileName ) ) ) {
										Debug.LogWarning( '\t' + "Tried to add vessel that already exists: " + Path.GetFileNameWithoutExtension( vesselFileName ) );
									}
								}

								foreach( string vesselName in __result ) {
									if( !vesselDict.ContainsKey( vesselName.Split( '_' )[0] ) ) {
										vesselDict.Add( vesselName.Split( '_' )[0], new Dictionary<string, List<string>>() );
									}
									if( !vesselDict[vesselName.Split( '_' )[0]].ContainsKey( vesselName.Split( '_' )[1] ) ) {
										vesselDict[vesselName.Split( '_' )[0]].Add( vesselName.Split( '_' )[1], new List<string>() );
									}
									vesselDict[vesselName.Split( '_' )[0]][vesselName.Split( '_' )[1]].Add( vesselName );
								}

								//Debug.Log( "\tStructured Vessel List" );

								//foreach( KeyValuePair<string, Dictionary<string, List<string>>> nationDictionary in vesselDict ) {
								//	Debug.Log( "\tNation Name: " + nationDictionary.Key );
								//	foreach( KeyValuePair<string, List<string>> vesselTypeDictionary in nationDictionary.Value ) {
								//		Debug.Log( "\t\tVessel Type: " + vesselTypeDictionary.Key );
								//	}
								//}

								Debug.Log( "\tReordered Vessel List" );

								returnList = new List<string>();
								
								foreach( KeyValuePair<string, Dictionary<string, List<string>>> nationDictionary in vesselDict.Where( kvp => kvp.Key != "civ" && kvp.Key != "biologic" ) ) {
									Debug.Log( "\t\tNation Name: " + nationDictionary.Key );
									foreach( KeyValuePair<string, List<string>> vesselTypeDictionary in nationDictionary.Value.Where( kvp => kvp.Key.Contains("ss") ) ) {
										Debug.Log( "\t\t\tVessel Type: " + vesselTypeDictionary.Key );
                                        foreach( string vesselID in vesselTypeDictionary.Value ) {
											returnList.Add( vesselID );
                                        }
									}
									foreach( KeyValuePair<string, List<string>> vesselTypeDictionary in nationDictionary.Value.Where( kvp => !kvp.Key.Contains( "ss" ) ) ) {
										Debug.Log( "\t\t\tVessel Type: " + vesselTypeDictionary.Key );
										foreach( string vesselID in vesselTypeDictionary.Value ) {
											returnList.Add( vesselID );
										}
									}
								}
								Debug.Log( "\t\tNation Name: civ" );
								foreach( KeyValuePair<string, List<string>> vesselTypeDictionary in vesselDict["civ"] ) {
									Debug.Log( "\t\t\tVessel Type: " + vesselTypeDictionary.Key );
									foreach( string vesselID in vesselTypeDictionary.Value ) {
										returnList.Add( vesselID );
									}
								}
								Debug.Log( "\t\tNation Name: biologic" );
								foreach( KeyValuePair<string, List<string>> vesselTypeDictionary in vesselDict["biologic"] ) {
									Debug.Log( "\t\t\tVessel Type: " + vesselTypeDictionary.Key );
									foreach( string vesselID in vesselTypeDictionary.Value ) {
										returnList.Add( vesselID );
									}
								}

								__result = returnList.ToArray();
							}
							break;
						default:
							break;
					}
				}
                return false;
			}
		}
	}
}