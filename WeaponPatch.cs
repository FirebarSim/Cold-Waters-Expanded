using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace Cold_Waters_Expanded
{
    [BepInPlugin( "org.cwe.plugins.weapon", "Cold Waters Expanded Weapon Patches", "1.0.0.5" )]
    class WeaponPatch : BaseUnityPlugin
	{
		static WeaponPatch weaponPatch;
		Dictionary<DatabaseWeaponData, DatabaseWeaponDataExtension> weaponDataExtensions = new Dictionary<DatabaseWeaponData, DatabaseWeaponDataExtension>();
		Dictionary<Torpedo, TorpedoExtension> torpedoExtensions = new Dictionary<Torpedo, TorpedoExtension>();

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

		[HarmonyPatch( typeof( Torpedo ), "FixedUpdate" )]
		public class Torpedo_FixedUpdate_Patch
		{
			[HarmonyPrefix]
			public static bool Prefix( Torpedo __instance ) {
				//Debug.Log( "FixedUpdate" );
				if( __instance.destroyTimer > 0f ) {
					__instance.destroyTimer -= Time.deltaTime;
					if( __instance.destroyTimer <= 0f ) {
						__instance.destroyMe = true;
					}
				}
				if( __instance.databaseweapondata.landAttack ) {
					__instance.terrainScanTimer += Time.deltaTime;
					if( __instance.terrainScanTimer > 0.5f ) {
						Traverse.Create( __instance ).Method( "ScanForTerrain" ).GetValue();
					}
				}
				if( __instance.destroyMe ) {
					Traverse.Create( __instance ).Method( "DestroyTorpedo", true ).GetValue();
					return false;
				}
				__instance.timer += Time.deltaTime;
				if( __instance.timer > __instance.databaseweapondata.runTime ) {
					__instance.destroyMe = true;
				}
				if( !__instance.boxcollider.enabled ) {
					if( __instance.timer > 5f ) {
						__instance.boxcollider.enabled = true;
					}
					if( UIFunctions.globaluifunctions.levelloadmanager.currentMapGeneratorInstance != null ) {
						LayerMask mask = 1073741824;
						if( Physics.Raycast( __instance.gameObject.transform.position, -Vector3.up, out RaycastHit _, 0.05f, mask ) ) {
							__instance.destroyMe = true;
						}
					}
				}
				if( __instance.onWire ) {
					float num = 5f;
					__instance.wireTimer += Time.deltaTime;
					if( __instance.wireTimer > num ) {
						Traverse.Create( __instance ).Method( "CheckWire" ).GetValue();
						__instance.wireTimer -= num;
					}
				}
				if( __instance.sensorsActive ) {
					__instance.sortTargetsTimer += Time.deltaTime;
					if( __instance.sortTargetsTimer > 6f ) {
						Traverse.Create( __instance ).Method( "SortTargetsByDistance" ).GetValue();
						__instance.sortTargetsTimer = 0f;
					}
				}
				if( __instance.databaseweapondata.isMissile ) {
					//Debug.Log( "   isMissile" );
					if( __instance.shotDown ) {
						__instance.torpedoGuidance.transform.localRotation = Quaternion.Slerp( __instance.torpedoGuidance.transform.localRotation, Quaternion.Euler( __instance.shotDownAngles.x, __instance.shotDownAngles.y, 0f ), 1f );
						__instance.gameObject.transform.rotation = Quaternion.RotateTowards( __instance.gameObject.transform.rotation, __instance.torpedoGuidance.transform.rotation, __instance.databaseweapondata.turnRate * Time.deltaTime );
						__instance.gameObject.transform.rotation = Quaternion.Slerp( __instance.gameObject.transform.rotation, Quaternion.Euler( __instance.gameObject.transform.eulerAngles.x, __instance.gameObject.transform.eulerAngles.y, 0f ), 1f );
						if( __instance.gameObject.transform.position.y < 1000f ) {
							ObjectPoolManager.CreatePooled( UIFunctions.globaluifunctions.database.missileShotDown[UnityEngine.Random.Range( 0, UIFunctions.globaluifunctions.database.missileShotDown.Length )], __instance.gameObject.transform.position, __instance.gameObject.transform.rotation );
							ObjectPoolManager.CreatePooled( UIFunctions.globaluifunctions.database.surfacePlumes[UnityEngine.Random.Range( 0, UIFunctions.globaluifunctions.database.surfacePlumes.Length )], __instance.gameObject.transform.position, __instance.gameObject.transform.rotation );
							if( __instance.shotDownParticleEffect != null ) {
								__instance.shotDownParticleEffect.transform.parent = null;
								__instance.shotDownParticleEffect.GetComponent<DestroyTimer>().timer = 5f;
								__instance.shotDownParticleEffect.GetComponent<DestroyTimer>().enabled = true;
								__instance.shotDownParticleEffect.GetComponent<ParticleSystem>().Stop();
							}
							__instance.destroyMe = true;
						}
						return false;
					}
					if( !__instance.isAirborne ) {
						//Debug.Log( "   !isAirborne" );
						__instance.torpedoGuidance.transform.localRotation = Quaternion.Slerp( __instance.torpedoGuidance.transform.localRotation, Quaternion.Euler( -5f, 0f, 0f ), 1f );
						__instance.gameObject.transform.rotation = Quaternion.RotateTowards( __instance.gameObject.transform.rotation, __instance.torpedoGuidance.transform.rotation, __instance.databaseweapondata.turnRate * Time.deltaTime );
						__instance.gameObject.transform.rotation = Quaternion.Slerp( __instance.gameObject.transform.rotation, Quaternion.Euler( __instance.gameObject.transform.eulerAngles.x, __instance.gameObject.transform.eulerAngles.y, 0f ), 1f );
						if( __instance.gameObject.transform.position.y > 999.99f ) {
							__instance.isAirborne = true;
							if( __instance.missileTrail != null ) {
								__instance.missileTrail.gameObject.SetActive( value: true );
								__instance.missileTrail.Play();
							}
							if( __instance.launchAudioSource != null ) {
								__instance.launchAudioSource.enabled = true;
								__instance.launchAudioSource.Play();
							}
							if( !__instance.databaseweapondata.surfaceLaunched ) {
								ObjectPoolManager.CreatePooled( UIFunctions.globaluifunctions.database.splashes[0], new Vector3( __instance.gameObject.transform.position.x, 1000f, __instance.gameObject.transform.position.z ), Quaternion.Euler( 0f, 0f, 0f ) );
							}
							__instance.timer = 0f;
							if( ManualCameraZoom.target == __instance.gameObject.transform ) {
								UIFunctions.globaluifunctions.playerfunctions.eventcamera.CheckSeaLevelSwap( surface: true );
							}
						}
						if( __instance.gameObject.transform.eulerAngles.x > 350f ) {
							__instance.gameObject.transform.rotation = Quaternion.Slerp( __instance.gameObject.transform.rotation, Quaternion.Euler( 350f, __instance.gameObject.transform.eulerAngles.y, 0f ), 1f );
						}
						//Debug.Log( "   Exit !isAirborne" );
						return false;
					}
					if( __instance.actualCurrentSpeed < __instance.databaseweapondata.actualActiveRunSpeed ) {
						__instance.actualCurrentSpeed += 0.5f * Time.deltaTime;
					}
					__instance.actionTimer += Time.deltaTime;
					if( !__instance.boosterReleased && __instance.databaseweapondata.boosterReleasedAfterSeconds > 0f && __instance.timer > __instance.databaseweapondata.boosterReleasedAfterSeconds ) {
						Traverse.Create( __instance ).Method( "ReleaseBooster" ).GetValue();
					}
					if( __instance.guidanceActive ) {
						//Debug.Log( "   guidanceActive" );
						// Ballistic Trajectory Height command
						if( weaponPatch.weaponDataExtensions[__instance.databaseweapondata].isBallistic ) {
							if( weaponPatch.torpedoExtensions[__instance].ballisticTrajectory == null ) {
								weaponPatch.torpedoExtensions[__instance].ballisticTrajectory = new BallisticTrajectory( weaponPatch.weaponDataExtensions[__instance.databaseweapondata].ballisticCeiling, Vector3.Distance( __instance.gameObject.transform.position, __instance.initialWaypointPosition ) );
								//Debug.Log( "   new BallisticTrajectory" );
								//Debug.Log( "   launchPosition: " + __instance.gameObject.transform.position.ToString() );
								//Debug.Log( "   initialWaypointPosition: " + __instance.initialWaypointPosition.ToString() );
								//Debug.Log( "   ballisticCeiling: " + weaponPatch.weaponDataExtensions[__instance.databaseweapondata].ballisticCeiling );
							}
							//Debug.Log( "   isBallistic" );
							//Debug.Log( "   ballisticHeightTarget: " + ( 1000f + weaponPatch.torpedoExtensions[__instance].ballisticTrajectory.GetAltitude( Vector3.Distance( __instance.gameObject.transform.position, __instance.initialWaypointPosition ) ) ) );
							__instance.cruiseYValue = 1000f + weaponPatch.torpedoExtensions[__instance].ballisticTrajectory.GetAltitude( Vector3.Distance( __instance.gameObject.transform.position, __instance.initialWaypointPosition ) );
						}
						float heightError = __instance.gameObject.transform.position.y - __instance.cruiseYValue;
						if( heightError > -0.01f && heightError < 0.01f ) {
							heightError = 0f;
						}
						else {
							heightError *= 20f;
							heightError = Mathf.Clamp( heightError, 0f - __instance.databaseweapondata.maxPitchAngle, __instance.databaseweapondata.maxPitchAngle );
						}
						__instance.torpedoGuidance.transform.LookAt( __instance.initialWaypointPosition );
						float y = __instance.torpedoGuidance.transform.eulerAngles.y;
						__instance.torpedoGuidance.transform.rotation = Quaternion.Slerp( __instance.torpedoGuidance.transform.rotation, Quaternion.Euler( heightError, y, 0f ), 1f );
						__instance.gameObject.transform.rotation = Quaternion.RotateTowards( __instance.gameObject.transform.rotation, __instance.torpedoGuidance.transform.rotation, __instance.databaseweapondata.turnRate * Time.deltaTime );
						__instance.gameObject.transform.rotation = Quaternion.Slerp( __instance.gameObject.transform.rotation, Quaternion.Euler( __instance.gameObject.transform.eulerAngles.x, __instance.gameObject.transform.eulerAngles.y, 0f ), 1f );
						// Release Payload Commands
						if( Vector3.Distance( __instance.launchPosition, __instance.gameObject.transform.position ) > __instance.distanceToWaypoint ) {
							if( !__instance.databaseweapondata.hasPayload ) {
								Traverse.Create( __instance ).Method( "ActivateTorpedo" ).GetValue();
							}
							else if( !__instance.payloadDropped ) {
								if( __instance.databaseweapondata.boosterReleasedAfterSeconds < 0f ) {
									Traverse.Create( __instance ).Method( "ReleaseBooster" ).GetValue();
								}
								// Instantiate Payload
								GameObject gameObject = UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.databaseweapondata[__instance.databaseweapondata.missilePayload].weaponObject, __instance.payloadPosition.position, __instance.gameObject.transform.rotation ) as GameObject;
								// Retarget Camera
								if( ManualCameraZoom.target == __instance.gameObject.transform ) {
									ManualCameraZoom.target = gameObject.transform;
								}
								// Activate new GameObject
								gameObject.SetActive( value: true );
								// Get the Torpedo component of the Payload
								Torpedo component = gameObject.GetComponent<Torpedo>();
								component.databaseweapondata = UIFunctions.globaluifunctions.database.databaseweapondata[__instance.databaseweapondata.missilePayload];
								component.guidanceActive = false;
								component.sensorsActive = true;
								component.searching = true;
								component.vesselFiredFrom = __instance.vesselFiredFrom;
								component.whichNavy = __instance.whichNavy;
								// Player Payload
								if( component.whichNavy == 0 ) {
									int nearestVesselIndex = GetNearestVesselIndex( __instance.gameObject.transform );
									if( nearestVesselIndex != -1 ) {
										component.cruiseYValue = GameDataManager.enemyvesselsonlevel[nearestVesselIndex].transform.position.y;
										component.searchYValue = GameDataManager.enemyvesselsonlevel[nearestVesselIndex].transform.position.y;
									}
									// Randomly Set to Passive Homing if the Torpedo supports it.
									if( UnityEngine.Random.value > 0.5f ) {
										for( int i = 0; i < component.databaseweapondata.homeSettings.Length; i++ ) {
											if( component.databaseweapondata.homeSettings[i] == "PASSIVE" ) {
												component.passiveHoming = true;
											}
										}
									}
								}
								// Enemy Payload
								else {
									component.cruiseYValue = GameDataManager.playervesselsonlevel[0].transform.position.y;
									component.searchYValue = GameDataManager.playervesselsonlevel[0].transform.position.y;
								}
								component.noSurfaceTargets = UIFunctions.globaluifunctions.combatai.AreHostileShipsInArea();
								component.InitialiseTorpedo();
								UIFunctions.globaluifunctions.playerfunctions.sensormanager.AddTorpedoToArray( component );
								component.isAirborne = true;
								component.actualCurrentSpeed = __instance.actualCurrentSpeed;
								__instance.payloadDropped = true;
								__instance.onWire = false;
								__instance.destroyTimer = UnityEngine.Random.Range( 4f, 9f );
								__instance.guidanceActive = false;
							}
						}
					}
					else if( __instance.sensorsActive ) {
						Debug.Log( "   sensorsActive" );
						if( __instance.landAttackTerminal ) {
							float num3 = Vector3.Distance( __instance.gameObject.transform.position, __instance.targetTransform.position );
							if( num3 < 1f ) {
								( UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.shipFires[0], __instance.targetTransform.position + Vector3.up * 0.15f, Quaternion.identity ) as ParticleSystem ).transform.SetParent( UIFunctions.globaluifunctions.levelloadmanager.currentMapGeneratorInstance.transform );
								UnityEngine.Object.Instantiate( UIFunctions.globaluifunctions.database.magazineExplosionsLand[UnityEngine.Random.Range( 0, 2 )], __instance.targetTransform.position, Quaternion.identity );
								Traverse.Create( __instance ).Method( "DestroyTorpedo", false ).GetValue();
								return false;
							}
							if( num3 < 5f && !__instance.eventCameraSet ) {
								__instance.eventCameraSet = true;
								UIFunctions.globaluifunctions.playerfunctions.eventcamera.CheckForEventCamera( __instance.gameObject.transform, __instance.targetTransform, 10f, surface: true, fixedPosition: false, CameraReturns: false, -1f, -1f, -1f, checkDistance: false );
							}
						}
						bool flag;
						if( __instance.chaffed ) {
							__instance.torpedoGuidance.transform.LookAt( __instance.targetTransform.position );
							flag = true;
							__instance.driveAroundTimer += Time.deltaTime;
							if( __instance.driveAroundTimer > 5f ) {
								__instance.driveAroundTimer = 0f;
								__instance.chaffed = false;
							}
						}
						else if( !__instance.databaseweapondata.landAttack || __instance.targetTransform == null ) {
							if( __instance.poppingUp && !__instance.poppedUp ) {
								flag = true;
								__instance.torpedoGuidance.transform.localRotation = Quaternion.Euler( 10f, 0f, 0f );
							}
							else {
								flag = Traverse.Create( __instance ).Method( "CheckTargetInSensorCone" ).GetValue<bool>();
							}
						}
						else {
							flag = true;
							__instance.torpedoGuidance.transform.LookAt( __instance.targetTransform.position + Vector3.up * __instance.cruiseAltitudeBonus );
						}
						if( __instance.poppingUp && !__instance.chaffed ) {
							__instance.torpedoGuidance.transform.rotation = Quaternion.Euler( -10f, 0f, 0f );
							if( __instance.gameObject.transform.position.y > __instance.cruiseYValue ) {
								__instance.poppingUp = false;
								__instance.poppedUp = true;
								__instance.cruiseYValue = 1000.1f;
								__instance.targetTransform = __instance.popUpTransform;
							}
						}
						else if( flag && !__instance.poppedUp && !__instance.runLow && Vector3.Distance( __instance.gameObject.transform.position, __instance.targetTransform.position ) < 31f ) {
							__instance.poppingUp = true;
							__instance.popUpTransform = __instance.targetTransform;
							__instance.cruiseYValue = 1002f;
						}
						__instance.gameObject.transform.rotation = Quaternion.RotateTowards( __instance.gameObject.transform.rotation, __instance.torpedoGuidance.transform.rotation, __instance.databaseweapondata.turnRate * Time.deltaTime );
						__instance.gameObject.transform.rotation = Quaternion.Slerp( __instance.gameObject.transform.rotation, Quaternion.Euler( __instance.gameObject.transform.eulerAngles.x, __instance.gameObject.transform.eulerAngles.y, 0f ), 1f );
					}
					if( __instance.gameObject.transform.position.y < 1000f && __instance.timer > 2f ) {
						ObjectPoolManager.CreatePooled( UIFunctions.globaluifunctions.database.underwaterLargeExplosions[UnityEngine.Random.Range( 0, UIFunctions.globaluifunctions.database.underwaterLargeExplosions.Length )], new Vector3( __instance.gameObject.transform.position.x, 1000f, __instance.gameObject.transform.position.z ), Quaternion.identity );
						__instance.destroyMe = true;
					}
					if( __instance.gameObject.transform.position.y < __instance.cruiseYValue ) {
						Mathf.Clamp01( __instance.gameObject.transform.position.y - __instance.cruiseYValue );
					}
					else if( __instance.gameObject.transform.position.y < __instance.cruiseYValue + 0.1f && __instance.gameObject.transform.eulerAngles.x < 180f ) {
						float num4 = __instance.gameObject.transform.position.y - __instance.cruiseYValue;
						__instance.gameObject.transform.Rotate( Vector3.right * ( 0f - num4 ) * 2f );
					}
					//Debug.Log( "   Exit isMissile" );
					return false;
				}
				if( __instance.isAirborne ) {
					if( __instance.actualCurrentSpeed > __instance.databaseweapondata.actualActiveRunSpeed && __instance.actualCurrentSpeed > 0.5f ) {
						__instance.actualCurrentSpeed -= 0.5f * Time.deltaTime;
					}
					if( __instance.actualCurrentSpeed < 0.5f ) {
						__instance.gameObject.transform.Translate( Vector3.up * Time.deltaTime * -0.2f, Space.World );
					}
					__instance.gameObject.transform.Translate( Vector3.up * Time.deltaTime * -0.1f, Space.World );
					float x = __instance.gameObject.transform.eulerAngles.x;
					if( x < 88f || x > 180f ) {
						__instance.gameObject.transform.Rotate( Vector3.right * Time.deltaTime * 10f );
					}
					if( !( __instance.gameObject.transform.position.y < 1000f ) ) {
						return false;
					}
					if( !__instance.databaseweapondata.isSonobuoy ) {
						__instance.isAirborne = false;
						__instance.tacMapTorpedoIcon.gameObject.SetActive( value: true );
						ObjectPoolManager.CreatePooled( UIFunctions.globaluifunctions.database.splashes[0], new Vector3( __instance.gameObject.transform.position.x, 1000f, __instance.gameObject.transform.position.z ), Quaternion.Euler( 0f, 0f, 0f ) );
						__instance.actualCurrentSpeed = __instance.databaseweapondata.actualRunSpeed;
						if( __instance.cavitationAudioSource != null ) {
							__instance.cavitationAudioSource.Play();
						}
						if( !( __instance.parachute != null ) ) {
							return false;
						}
						__instance.parachute.Stop();
						__instance.parachute.gameObject.SetActive( value: false );
						if( __instance.whichNavy == 1 ) {
							__instance.cruiseYValue = GameDataManager.playervesselsonlevel[0].transform.position.y;
							if( Vector3.Distance( __instance.gameObject.transform.position, GameDataManager.playervesselsonlevel[0].transform.position ) * GameDataManager.inverseYardsScale < 1500f ) {
								if( UnityEngine.Random.value < 0.5f ) {
									__instance.searchLeft = true;
								}
							}
							else {
								float value = UnityEngine.Random.value;
								if( value < 0.4f ) {
									__instance.searchLeft = true;
								}
								else if( value >= 0.4f ) {
									__instance.snakeSearch = true;
								}
								__instance.torpedoGuidance.LookAt( GameDataManager.playervesselsonlevel[0].transform.position );
								if( __instance.torpedoGuidance.localEulerAngles.y > 270f && __instance.torpedoGuidance.localEulerAngles.y < 90f && value < 0.6f ) {
									__instance.searchLeft = false;
									__instance.snakeSearch = true;
								}
							}
						}
						Traverse.Create( __instance ).Method( "ActivateTorpedo" ).GetValue();
						if( ManualCameraZoom.target == __instance.gameObject.transform ) {
							UIFunctions.globaluifunctions.playerfunctions.eventcamera.CheckSeaLevelSwap( surface: false );
						}
					}
					else {
						ObjectPoolManager.CreatePooled( UIFunctions.globaluifunctions.database.splashes[0], new Vector3( __instance.gameObject.transform.position.x, 1000f, __instance.gameObject.transform.position.z ), Quaternion.Euler( 0f, 0f, 0f ) );
						Noisemaker component2 = ObjectPoolManager.CreatePooled( UIFunctions.globaluifunctions.database.sonobuoyInWaterObject, __instance.gameObject.transform.position, Quaternion.identity ).GetComponent<Noisemaker>();
						UIFunctions.globaluifunctions.playerfunctions.sensormanager.AddSonobuoyToArray( component2, __instance.sensorData );
						if( __instance.isActiveSonobuoy ) {
							UIFunctions.globaluifunctions.playerfunctions.sensormanager.AddNoiseMakerToArray( component2 );
							component2.tacMapNoisemakerIcon.shipDisplayIcon.color = UIFunctions.globaluifunctions.levelloadmanager.tacticalmap.navyColors[1];
							component2.name = "ACTIVE";
						}
						if( ManualCameraZoom.target == __instance.gameObject.transform ) {
							ManualCameraZoom.target = component2.gameObject.transform;
						}
						UIFunctions.globaluifunctions.playerfunctions.sensormanager.sonosInFlight.Remove( __instance );
						UnityEngine.Object.Destroy( __instance.gameObject.gameObject );
					}
					return false;
				}
				if( __instance.sensorsActive ) {
					Traverse.Create( __instance ).Method( "RaycastSensor" ).GetValue();
					if( __instance.actualCurrentSpeed < __instance.databaseweapondata.actualActiveRunSpeed ) {
						__instance.actualCurrentSpeed += 0.2f * Time.deltaTime;
					}
					__instance.actionTimer += Time.deltaTime;
				}
				if( __instance.gameObject.transform.position.y > 999.9f && !__instance.isAirborne ) {
					float d = __instance.gameObject.transform.position.y - 999.9f;
					__instance.gameObject.transform.Rotate( Vector3.right * d * 6f );
				}
				float num5 = __instance.gameObject.transform.position.y - __instance.cruiseYValue;
				if( num5 > -0.01f && num5 < 0.01f ) {
					num5 = 0f;
				}
				else {
					num5 *= 20f;
					num5 = Mathf.Clamp( num5, 0f - __instance.databaseweapondata.maxPitchAngle, __instance.databaseweapondata.maxPitchAngle );
				}
				if( __instance.playerControlling ) {
					__instance.playerDistToWaypoint = Vector2.Distance( new Vector2( __instance.gameObject.transform.position.x, __instance.gameObject.transform.position.z ), new Vector2( __instance.initialWaypointPosition.x, __instance.initialWaypointPosition.z ) );
					if( __instance.playerTimeToWaypoint == -100f ) {
						__instance.playerTimeToWaypoint = __instance.playerDistToWaypoint / __instance.actualCurrentSpeed;
					}
					__instance.playerTimeToWaypoint -= Time.deltaTime;
					__instance.cruiseYValue += __instance.playerDepthInput * Time.deltaTime * 0.5f;
					if( __instance.cruiseYValue > 999.98f ) {
						__instance.cruiseYValue = 999.98f;
					}
					__instance.torpedoGuidance.transform.localRotation = Quaternion.Slerp( __instance.gameObject.transform.rotation, Quaternion.Euler( 0f, 10f * __instance.playerTurnInput, 0f ), 1f );
					__instance.torpedoGuidance.transform.rotation = Quaternion.Slerp( __instance.torpedoGuidance.transform.rotation, Quaternion.Euler( num5, __instance.torpedoGuidance.transform.eulerAngles.y, 0f ), 1f );
					__instance.gameObject.transform.rotation = Quaternion.RotateTowards( __instance.gameObject.transform.rotation, __instance.torpedoGuidance.transform.rotation, __instance.databaseweapondata.turnRate * Time.deltaTime );
					__instance.gameObject.transform.rotation = Quaternion.Slerp( __instance.gameObject.transform.rotation, Quaternion.Euler( __instance.gameObject.transform.eulerAngles.x, __instance.gameObject.transform.eulerAngles.y, 0f ), 1f );
					if( __instance.playerTimeToWaypoint < 0f ) {
						Traverse.Create( __instance ).Method( "ActivateTorpedo" ).GetValue();
					}
					__instance.runStraight = true;
					if( __instance.playerDepthInput != 0f ) {
						weaponPatch.torpedoExtensions[__instance].wasPlayerDepthControlled = true;
					}
					return false;
				}
				if( weaponPatch.torpedoExtensions[__instance].wasPlayerDepthControlled ) {
					weaponPatch.torpedoExtensions[__instance].wasPlayerDepthControlled = false;
					__instance.cruiseYValue = __instance.gameObject.transform.position.y;
				}
				if( __instance.guidanceActive ) {
					__instance.torpedoGuidance.transform.LookAt( __instance.initialWaypointPosition );
					if( __instance.lockGuidance ) {
						__instance.torpedoGuidance.transform.rotation = Quaternion.Slerp( __instance.torpedoGuidance.transform.rotation, Quaternion.Euler( num5, __instance.gameObject.transform.eulerAngles.y, 0f ), 1f );
					}
					else {
						__instance.torpedoGuidance.transform.rotation = Quaternion.Slerp( __instance.torpedoGuidance.transform.rotation, Quaternion.Euler( num5, __instance.torpedoGuidance.transform.eulerAngles.y, 0f ), 1f );
					}
					__instance.gameObject.transform.rotation = Quaternion.RotateTowards( __instance.gameObject.transform.rotation, __instance.torpedoGuidance.transform.rotation, __instance.databaseweapondata.turnRate * Time.deltaTime );
					__instance.gameObject.transform.rotation = Quaternion.Slerp( __instance.gameObject.transform.rotation, Quaternion.Euler( __instance.gameObject.transform.eulerAngles.x, __instance.gameObject.transform.eulerAngles.y, 0f ), 1f );
					if( Vector2.Distance( new Vector2( __instance.gameObject.transform.position.x, __instance.gameObject.transform.position.z ), new Vector2( __instance.initialWaypointPosition.x, __instance.initialWaypointPosition.z ) ) < 1f ) {
						Traverse.Create( __instance ).Method( "ActivateTorpedo" ).GetValue();
					}
					return false;
				}
				bool flag2 = Traverse.Create( __instance ).Method( "CheckTargetInSensorCone" ).GetValue<bool>();
				if( __instance.databaseweapondata.weaponType == "TORPEDO" ) {
					__instance.pingTimer += Time.deltaTime;
					float num6 = 10f;
					if( __instance.targetTransform != null ) {
						num6 = Vector3.Distance( __instance.gameObject.transform.position, __instance.targetTransform.position ) / 2f;
						if( num6 < 1f ) {
							num6 = 1f;
						}
					}
					if( __instance.pingTimer > num6 && !__instance.jammed ) {
						Traverse.Create( __instance ).Method( "TorpedoActivePing" ).GetValue();
						__instance.pingTimer = 0f;
						if( flag2 && __instance.whichNavy == 0 && __instance.onWire && !__instance.passiveHoming ) {
							for( int j = 0; j < GameDataManager.enemyvesselsonlevel.Length; j++ ) {
								if( __instance.targetTransform == GameDataManager.enemyvesselsonlevel[j].transform ) {
									UIFunctions.globaluifunctions.playerfunctions.sensormanager.solutionQualityOfContacts[j] = UIFunctions.globaluifunctions.playerfunctions.maximumPlayerTMA;
								}
							}
						}
					}
				}
				if( __instance.tacMapTorpedoIcon.sensorConeLines[0].gameObject.activeSelf ) {
					if( __instance.jammed || __instance.driveThrough || __instance.drivingAround ) {
						Traverse.Create( __instance ).Method( "DisplaySensorConeColor", false ).GetValue();
					}
					else {
						Traverse.Create( __instance ).Method( "DisplaySensorConeColor", flag2 ).GetValue();
					}
				}
				if( flag2 ) {
					if( !__instance.jammed ) {
						__instance.gameObject.transform.rotation = Quaternion.RotateTowards( __instance.gameObject.transform.rotation, Quaternion.Euler( __instance.torpedoGuidance.transform.eulerAngles.x, __instance.torpedoGuidance.transform.eulerAngles.y, 0f ), __instance.databaseweapondata.turnRate * Time.deltaTime );
						if( !__instance.onTarget ) {
							__instance.onTarget = true;
						}
					}
					else if( __instance.driveThrough ) {
						__instance.torpedoGuidance.transform.localRotation = Quaternion.identity;
						__instance.gameObject.transform.rotation = Quaternion.RotateTowards( __instance.gameObject.transform.rotation, Quaternion.Euler( __instance.torpedoGuidance.transform.eulerAngles.x, __instance.torpedoGuidance.transform.eulerAngles.y, 0f ), __instance.databaseweapondata.turnRate * Time.deltaTime );
						if( __instance.onTarget ) {
							__instance.onTarget = false;
						}
					}
					else {
						__instance.drivingAround = true;
						__instance.driveAroundTimer = 0f;
						if( __instance.onTarget ) {
							__instance.onTarget = false;
						}
					}
				}
				else {
					if( __instance.drivingAround ) {
						__instance.driveAroundTimer += Time.deltaTime;
						if( __instance.driveAroundTimer > 8f ) {
							__instance.driveAroundTimer = 0f;
							__instance.drivingAround = false;
							__instance.targetTransform = null;
							__instance.searching = true;
						}
						if( __instance.searchLeft ) {
							__instance.gameObject.transform.Rotate( Vector3.up * 10f * Time.deltaTime );
						}
						else {
							__instance.gameObject.transform.Rotate( Vector3.up * -10f * Time.deltaTime );
						}
					}
					else if( !__instance.runStraight ) {
						if( __instance.snakeSearch ) {
							__instance.snakeTimer += Time.deltaTime;
							if( __instance.snakeTimer > __instance.snakeTime ) {
								__instance.snakeMode *= -1;
								__instance.snakeTimer = 0f;
							}
							__instance.gameObject.transform.Rotate( Vector3.up * 2f * __instance.snakeMode * Time.deltaTime );
						}
						else if( __instance.searchLeft ) {
							__instance.gameObject.transform.Rotate( Vector3.up * -10f * Time.deltaTime );
						}
						else {
							__instance.gameObject.transform.Rotate( Vector3.up * 10f * Time.deltaTime );
						}
					}
					__instance.gameObject.transform.rotation = Quaternion.RotateTowards( __instance.gameObject.transform.rotation, Quaternion.Euler( num5, __instance.gameObject.transform.eulerAngles.y, 0f ), __instance.databaseweapondata.turnRate * Time.deltaTime );
				}
				__instance.gameObject.transform.rotation = Quaternion.Slerp( __instance.gameObject.transform.rotation, Quaternion.Euler( __instance.gameObject.transform.eulerAngles.x, __instance.gameObject.transform.eulerAngles.y, 0f ), 1f );
				return false;
			}

			public static int GetNearestVesselIndex( Transform t ) {
				float num = 40000f;
				int result = -1;
				for( int i = 0; i < GameDataManager.enemyvesselsonlevel.Length; i++ ) {
					float num2 = Vector3.Distance( t.position, GameDataManager.enemyvesselsonlevel[i].transform.position );
					if( num2 < num ) {
						result = i;
						num = num2;
					}
				}
				return result;
			}

			[HarmonyPatch( typeof( Torpedo ), "InitialiseTorpedo" )]
			public class Torpedo_InitialiseTorpedo_Patch
			{
				[HarmonyPostfix]
				public static void Postfix( Torpedo __instance ) {
					TorpedoExtension torpedoExtension = new TorpedoExtension();
					//DatabaseWeaponDataExtension databaseWeaponDataExtension = weaponPatch.weaponDataExtensions[__instance.databaseweapondata];
					weaponPatch.torpedoExtensions.Add( __instance, torpedoExtension );
					//if( databaseWeaponDataExtension.isBallistic ) {
					//	torpedoExtension.ballisticTrajectory = new BallisticTrajectory( databaseWeaponDataExtension.ballisticCeiling, Vector3.Distance( __instance.launchPosition, __instance.initialWaypointPosition ) );
					//	Debug.Log( "InitialiseTorpedo" );
					//	Debug.Log( "   launchPosition: " + __instance.launchPosition.ToString() );
					//	Debug.Log( "   initialWaypointPosition: " + __instance.initialWaypointPosition.ToString() );
					//	Debug.Log( "   ballisticCeiling: " + databaseWeaponDataExtension.ballisticCeiling );
					//}
				}
			}
		}
    }
}
