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
    [BepInPlugin( "org.cwe.plugins.ui", "Cold Waters Expanded UI Patches", "1.0.1.0" )]
    class UIPatch : BaseUnityPlugin {
        static UIPatch uiPatch;
        void Awake() {
            uiPatch = this;
        }

        [HarmonyPatch( typeof( PlayerFunctions ), "InitialiseWeapons" )]
        public class PlayerFunctions_InitialiseWeapons_Patch {
            [HarmonyPrefix]
            public static bool Prefix( PlayerFunctions __instance ) {
                __instance.mastThresholdDepth = __instance.playerVessel.databaseshipdata.periscopeDepthInFeet + 5;
                __instance.fullMessageLog = new List<string>();
                __instance.fullMessageLogColors = new List<Color32>();
                __instance.numberOfLogEntries = 0;
                if( __instance.currentFullLogParentObject != null ) {
                    UnityEngine.Object.Destroy( __instance.currentFullLogParentObject );
                }
                __instance.currentFullLogParentObject = UnityEngine.Object.Instantiate( __instance.fullLogParentObject );
                __instance.currentFullLogParentObject.transform.SetParent( __instance.fullLogParentObject.transform, false );
                __instance.currentFullLogParentObject.GetComponent<UnityEngine.UI.Image>().enabled = true;
                __instance.fullLogScrollRect.content = __instance.currentFullLogParentObject.GetComponent<RectTransform>();
                __instance.fullLogObject.SetActive( false );
                __instance.fullLogToggleButton.SetActive( __instance.generateFullLog );
                __instance.playerSunkBy = string.Empty;
                __instance.hudHidden = false;
                __instance.eventcamera.eventCameraOn = false;
                __instance.SetEventCameraMode();
                __instance.ballastRechargeTimer = 0f;
                __instance.ballastRechargeTime = 120f;
                __instance.landAttackNumber = 0;
                __instance.ClearStatusIcons();
                __instance.firstDepthCheckDone = false;
                PlayerFunctions.draggingWaypoint = false;
                for( int i = 0; i < __instance.torpedoTubesGUIs.Length; i++ ) {
                    UnityEngine.Object.Destroy( __instance.torpedoTubesGUIs[i].gameObject );
                }
                __instance.weaponSprites = new Sprite[__instance.playerVessel.databaseshipdata.torpedoIDs.Length];
                for( int j = 0; j < __instance.playerVessel.databaseshipdata.torpedoIDs.Length; j++ ) {
                    __instance.weaponSprites[j] = UIFunctions.globaluifunctions.database.databaseweapondata[__instance.playerVessel.databaseshipdata.torpedoIDs[j]].weaponImage;
                }
                Vector2 vector = new Vector2( -260f, 36f );
                int num = Mathf.CeilToInt( __instance.playerVessel.databaseshipdata.torpedotubes / 2f );
                if( __instance.playerVessel.vesselmovement.weaponSource.hasVLS ) {
                    num = Mathf.FloorToInt( __instance.playerVessel.databaseshipdata.torpedotubes / 2f );
                }
                //Debug.Log( "num=" + num );
                int num2 = 1;
                float num3 = 0f;
                if( __instance.playerVessel.databaseshipdata.vlsTorpedoIDs != null ) {
                    num2 = 0;
                    num3 = 36f;
                }
                float x = vector.x;
                float num4 = vector.y * ( (float) num - (float) num2 );
                __instance.torpedoTubesGUIs = new TorpedoTubeGUI[__instance.playerVessel.databaseshipdata.torpedotubes];
                __instance.torpedoTubeImages = new UnityEngine.UI.Image[__instance.playerVessel.databaseshipdata.torpedotubes];
                for( int k = 0; k < __instance.playerVessel.databaseshipdata.torpedotubes; k++ ) {
                    GameObject torpTube = UnityEngine.Object.Instantiate( __instance.torpedoTubeGUIObject, __instance.hudTransfrom.position, Quaternion.identity ) as GameObject;
                    torpTube.SetActive( true );
                    torpTube.transform.SetParent( __instance.menuPanel.transform, true );
                    RectTransform component = torpTube.GetComponent<RectTransform>();
                    component.localScale = Vector3.one;
                    torpTube.transform.localPosition = new Vector2( x, num4 );
                    //Debug.Log( "Tube " + k + "pos=" + torpTube.transform.localPosition );
                    torpTube.name = k.ToString();
                    num4 -= vector.y;
                    if( k == num - 1 ) {
                        x = 0f;
                        num4 = vector.y * ( (float) num - (float) num2 );
                    }
                    torpTube.transform.SetParent( __instance.menuPanel.transform, true );
                    __instance.torpedoTubesGUIs[k] = torpTube.GetComponent<TorpedoTubeGUI>();
                    __instance.torpedoTubeImages[k] = __instance.torpedoTubesGUIs[k].weaponInTube;
                    __instance.torpedoTubesGUIs[k].maskSprite.gameObject.GetComponent<UnityEngine.UI.Button>().onClick.AddListener( delegate {
                        __instance.ClickOnTube( int.Parse( torpTube.name ) );
                    } );
                    UnityEngine.UI.ColorBlock colors = __instance.torpedoTubesGUIs[k].attackSettingButton.colors;
                    colors.normalColor = __instance.helmmanager.buttonColors[1];
                    colors.highlightedColor = __instance.helmmanager.buttonColors[1];
                    colors.pressedColor = __instance.helmmanager.buttonColors[1];
                    colors.disabledColor = __instance.helmmanager.buttonColors[0];
                    __instance.torpedoTubesGUIs[k].attackSettingButton.colors = colors;
                    __instance.torpedoTubesGUIs[k].homeSettingButton.colors = colors;
                    __instance.torpedoTubesGUIs[k].depthSettingButton.colors = colors;
                }
                if( !GameDataManager.trainingMode && !GameDataManager.missionMode ) {
                    UIFunctions.globaluifunctions.campaignmanager.GetPlayerCampaignData();
                }
                for( int l = 0; l < __instance.playerVessel.databaseshipdata.torpedotubes; l++ ) {
                    if( !GameDataManager.trainingMode && !GameDataManager.missionMode && UIFunctions.globaluifunctions.campaignmanager.playercampaigndata.playerTubeStatus[l] == -200 ) {
                        __instance.torpedoTubeImages[l].sprite = UIFunctions.globaluifunctions.playerfunctions.tubeDestroyedSprite;
                        __instance.ClearTubeSettingButtons( l );
                        __instance.playerVessel.vesselmovement.weaponSource.tubeStatus[l] = -200;
                        __instance.playerVessel.vesselmovement.weaponSource.weaponInTube[l] = -200;
                        continue;
                    }
                    int playerTorpedoIDInTubeOnInit = Traverse.Create( __instance ).Method( "GetPlayerTorpedoIDInTubeOnInit", new object[] { 1 } ).GetValue<int>();
                    //int playerTorpedoIDInTubeOnInit = __instance.GetPlayerTorpedoIDInTubeOnInit( l );
                    bool flag = false;
                    int[] torpedoIDs = __instance.playerVessel.databaseshipdata.torpedoIDs;
                    foreach( int num5 in torpedoIDs ) {
                        if( playerTorpedoIDInTubeOnInit == num5 ) {
                            flag = true;
                        }
                    }
                    if( !flag ) {
                        __instance.playerVessel.vesselmovement.weaponSource.tubeStatus[l] = -10;
                        __instance.playerVessel.vesselmovement.weaponSource.weaponInTube[l] = -10;
                        __instance.torpedoTubeImages[l].gameObject.SetActive( false );
                        __instance.ClearTubeSettingButtons( l );
                    }
                    else {
                        __instance.playerVessel.vesselmovement.weaponSource.torpedoSearchPattern[l] = __instance.GetSettingIndex( UIFunctions.globaluifunctions.database.databaseweapondata[playerTorpedoIDInTubeOnInit].searchSettings[0], __instance.attackSettingDefinitions );
                        __instance.playerVessel.vesselmovement.weaponSource.torpedoDepthPattern[l] = __instance.GetSettingIndex( UIFunctions.globaluifunctions.database.databaseweapondata[playerTorpedoIDInTubeOnInit].heightSettings[0], __instance.depthSettingDefinitions );
                        __instance.playerVessel.vesselmovement.weaponSource.torpedoHomingPattern[l] = __instance.GetSettingIndex( UIFunctions.globaluifunctions.database.databaseweapondata[playerTorpedoIDInTubeOnInit].homeSettings[0], __instance.homeSettingDefinitions );
                        __instance.SetTubeSettingButtons( l );
                    }
                }
                Traverse.Create( __instance ).Method( "HighlightActiveTube" ).GetValue();
                //__instance.HighlightActiveTube();
                Vector2 v = new Vector2( 0f, vector.y * (float) num + vector.y - 36f + num3 );
                //Debug.Log( "v=" + v );
                __instance.signaturePanel.transform.localPosition = v;
                __instance.conditionsPanel.transform.localPosition = v;
                __instance.damagePanel.transform.localPosition = v;
                __instance.storesPanel.transform.localPosition = v;
                __instance.messageLogPanel.transform.localPosition = new Vector2( 0f, 36f * (float) num + 28f + num3 );
                __instance.messageLogPositions = new Vector2( 36f * (float) num + 28f + num3, 36f * (float) num + 275f + num3 );
                if( __instance.currentOpenPanel != -1 ) {
                    __instance.OpenContextualPanel( __instance.currentOpenPanel );
                }
                __instance.currentSignatureIndex = 0;
                __instance.sensormanager.SetSonarSignatureLabelData( __instance.playerVessel.databaseshipdata.shipID, 2 );
                __instance.DisableESMMeter();
                __instance.storesPanel.SetActive( false );
                __instance.wireData[0].text = string.Empty;
                __instance.wireData[1].text = string.Empty;
                return false;
            }
        }

		//[HarmonyPatch( typeof( LevelLoadManager ), "BackgroundMuseumRender" )]
		//public class LevelLoadManager_BackgroundMuseumRender_Patch
		//{
		//	[HarmonyPrefix]
		//	public static bool Prefix( LevelLoadManager __instance, bool resetCamera ) {
		//		UIFunctions.globaluifunctions.cameraMount.gameObject.SetActive( true );
		//		UIFunctions.globaluifunctions.MainCamera.GetComponent<ManualCameraZoom>().enabled = true;
		//		UIFunctions.globaluifunctions.missionmanager.assignedShip = false;
		//		Rect rect = new Rect( 0f, 0f, 1f, 1f );
		//		UIFunctions.globaluifunctions.GUICameraObject.rect = rect;
		//		if( !KeybindManagerMuseum.selectionScreen ) {
		//			UIFunctions.globaluifunctions.keybindManagerMuseum.selectShipButton.gameObject.SetActive( false );
		//		}
		//		LevelLoadManager.inMuseum = true;
		//		ManualCameraZoom.museumThreshold = UIFunctions.globaluifunctions.GUICameraObject.WorldToScreenPoint( __instance.museumThreshold.position );
		//		UIFunctions.globaluifunctions.keybindManagerMenu.enabled = false;
		//		__instance.amplifycoloreffect.enabled = false;
		//		for( int i = 0; i < GameDataManager.enemyvesselsonlevel.Length; i++ ) {
		//			UnityEngine.Object.Destroy( GameDataManager.enemyvesselsonlevel[i].gameObject );
		//		}
		//		if( __instance.currentMuseumInstantiatedObject != null ) {
		//			UnityEngine.Object.Destroy( __instance.currentMuseumInstantiatedObject );
		//		}
		//		Resources.UnloadUnusedAssets();
		//		Time.timeScale = 1f;
		//		int num = 0;
		//		int num2 = 0;
		//		Traverse.Create( __instance ).Method( "CreateEnvironment", new object[] { 0 } ).GetValue();
		//		__instance.skyboxobject.SetActive( false );
		//		__instance.environment.directionalLights[0].transform.localRotation = Quaternion.Euler( UIFunctions.globaluifunctions.levelloadmanager.museumLightingAngle );
		//		float y = 0f;
		//		ManualCameraZoom.sensitivity = GameDataManager.camerasensitivity;
		//		if( ManualCameraZoom.sensitivity <= 0f ) {
		//			ManualCameraZoom.sensitivity = 0.05f;
		//		}
		//		GameDataManager.cameraTimeScale = 1f;
		//		int num3 = __instance.currentMuseumItem;
		//		string text = "vessel";
		//		if( !KeybindManagerMuseum.selectionScreen ) {
		//			if( num3 < __instance.currentFilteredVessels.Count ) {
		//				__instance.museumObjectNumber = __instance.currentFilteredVessels[num3];
		//			}
		//			else {
		//				__instance.museumObjectNumber = 0;
		//				num3 -= __instance.currentFilteredVessels.Count;
		//				if( num3 < __instance.currentFilteredAircraft.Count ) {
		//					text = "aircraft";
		//					num3 = __instance.currentFilteredAircraft[num3];
		//				}
		//				else {
		//					num3 -= __instance.currentFilteredAircraft.Count;
		//					text = "torpedo";
		//					num3 = __instance.currentFilteredWeapons[num3];
		//				}
		//			}
		//		}
		//		else {
		//			__instance.museumObjectNumber = __instance.currentMuseumItem;
		//		}
		//		GameObject gameObject = __instance.uifunctions.vesselbuilder.CreateVessel( __instance.uifunctions.database.databaseshipdata[__instance.museumObjectNumber].shipID, false, new Vector3( 1f, 1100f, 2f ), Quaternion.identity );
		//		Vessel component = gameObject.GetComponent<Vessel>();
		//		component.databaseshipdata = __instance.uifunctions.database.databaseshipdata[__instance.museumObjectNumber];
		//		gameObject.transform.localRotation = Quaternion.Slerp( gameObject.transform.rotation, Quaternion.Euler( 0f, y, 0f ), 1f );
		//		GameDataManager.enemyvesselsonlevel = new Vessel[1];
		//		GameDataManager.enemyvesselsonlevel[0] = gameObject.GetComponent<Vessel>();
		//		if( GameDataManager.enemyvesselsonlevel[0].vesselai != null ) {
		//			GameDataManager.enemyvesselsonlevel[0].vesselai.enabled = false;
		//		}
		//		if( GameDataManager.enemyvesselsonlevel[0].vesselmovement.weaponSource != null ) {
		//			GameDataManager.enemyvesselsonlevel[0].vesselmovement.weaponSource.enabled = false;
		//		}
		//		switch( text ) {
		//			case "vessel":
		//				__instance.PopulateMuseumData( "vessel", __instance.museumObjectNumber );
		//				ManualCameraZoom.minDistance = component.databaseshipdata.minCameraDistance;
		//				ManualCameraZoom.distance = ManualCameraZoom.minDistance * 2f;
		//				if( component.databaseshipdata.shipType == "OILRIG" ) {
		//					ManualCameraZoom.distance *= 1.2f;
		//				}
		//				if( component.databaseshipdata.shipType == "BIOLOGIC" ) {
		//					ManualCameraZoom.distance = component.databaseshipdata.minCameraDistance;
		//				}
		//				__instance.currentMuseumInstantiatedObject = gameObject;
		//				break;
		//			case "aircraft": {
		//					__instance.PopulateMuseumData( "aircraft", num3 );
		//					GameObject gameObject3 = __instance.uifunctions.vesselbuilder.CreateAircraft( num3, new Vector3( 1f, 1100f, 2f ), Quaternion.identity, true );
		//					Helicopter component3 = gameObject3.GetComponent<Helicopter>();
		//					if( component3 != null ) {
		//						component3.enabled = false;
		//						component3.helicopterrigidbody.useGravity = false;
		//						gameObject3.transform.Translate( Vector3.up * 0.025f );
		//					}
		//					else {
		//						Aircraft component4 = gameObject3.GetComponent<Aircraft>();
		//						component4.enabled = false;
		//					}
		//					__instance.currentMuseumInstantiatedObject = gameObject3;
		//					GameDataManager.enemyvesselsonlevel[0].gameObject.SetActive( false );
		//					ManualCameraZoom.minDistance = __instance.uifunctions.database.databaseaircraftdata[num3].minCameraDistance;
		//					ManualCameraZoom.distance = ManualCameraZoom.minDistance;
		//					break;
		//				}
		//			case "torpedo": {
		//					__instance.PopulateMuseumData( "torpedo", num3 );
		//					GameObject gameObject2 = UnityEngine.Object.Instantiate( __instance.uifunctions.database.databaseweapondata[num3].weaponObject, new Vector3( 1f, 1100f, 2f ), Quaternion.identity ) as GameObject;
		//					gameObject2.SetActive( true );
		//					Torpedo component2 = gameObject2.GetComponent<Torpedo>();
		//					component2.enabled = false;
		//					__instance.currentMuseumInstantiatedObject = gameObject2;
		//					GameDataManager.enemyvesselsonlevel[0].gameObject.SetActive( false );
		//					ManualCameraZoom.minDistance = __instance.uifunctions.database.databaseweapondata[num3].minCameraDistance;
		//					ManualCameraZoom.distance = ManualCameraZoom.minDistance;
		//					break;
		//				}
		//		}
		//		__instance.cetoOcean.gameObject.SetActive( false );
		//		Camera component5 = __instance.MainCamera.GetComponent<Camera>();
		//		component5.depth = 2f;
		//		component5.clearFlags = CameraClearFlags.Nothing;
		//		component5.rect = new Rect( 0.3865f, 0.157f, 1f, 0.701f );
		//		__instance.MainCamera.SetActive( true );
		//		ManualCameraZoom.oceanShadowPlane.SetActive( false );
		//		__instance.uifunctions.SetMenuSystem( "MUSEUM" );
		//		GameDataManager.enemyNumberofShips = 1;
		//		GameDataManager.playerNumberofShips = 0;
		//		__instance.uifunctions.skyobjectcenterer.ForceSkyboxPosition( GameDataManager.enemyvesselsonlevel[0].transform );
		//		ManualCameraZoom.yMinLimit = -89;
		//		__instance.uifunctions.backgroundImage.gameObject.SetActive( false );
		//		GC.Collect();
		//		__instance.skyboxobject.SetActive( false );
		//		if( resetCamera ) {
		//			ManualCameraZoom.x = 135f;
		//			ManualCameraZoom.y = 22.5f;
		//		}
		//		return false;
		//	}
		//}

		[HarmonyPatch( typeof( LevelLoadManager ), "PopulateMuseumData" )]
		public class LevelLoadManager_PopulateMuseumData_Patch
		{
			[HarmonyPrefix]
			public static bool Prefix( LevelLoadManager __instance, string museumObjectType, int index ) {
				__instance.uifunctions.scrollbarDefault.value = 1f;
				__instance.uifunctions.mainTitle.text = LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "UnitReferenceHeader" );
				if( KeybindManagerMuseum.selectionScreen ) {
					__instance.uifunctions.mainTitle.text = LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "SelectVesselHeader" );
				}
				__instance.uifunctions.mainColumn.text = string.Empty;
				__instance.uifunctions.secondColumm.text = string.Empty;
				if( museumObjectType == "vessel" ) {
					//Debug.Log( 28 );
					__instance.currentShipDebugIndex = 0;
					DatabaseShipData databaseShipData = UIFunctions.globaluifunctions.database.databaseshipdata[index];
					__instance.uifunctions.mainColumn.text = "\n<size=22><b>" + databaseShipData.description + "</b></size>\n\n";
					Text secondColumm = __instance.uifunctions.secondColumm;
					secondColumm.text = secondColumm.text + "\n<size=22> </size>\n\n<b>" + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceDimensions" ) + ":</b>\n";
					Text secondColumm2 = __instance.uifunctions.secondColumm;
					string text = secondColumm2.text;
					secondColumm2.text = text + string.Format( "{0:#,0}", databaseShipData.displacement ) + " " + LanguageManager.interfaceDictionary["ReferenceTons"] + "\n";
					if( databaseShipData.displayLength == 0f ) {
						Text secondColumm3 = __instance.uifunctions.secondColumm;
						secondColumm3.text = secondColumm3.text + databaseShipData.length + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceMetre" ) + " x ";
					}
					else {
						Text secondColumm4 = __instance.uifunctions.secondColumm;
						secondColumm4.text = secondColumm4.text + databaseShipData.displayLength + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceMetre" ) + " x ";
					}
					if( databaseShipData.displayBeam == 0f ) {
						Text secondColumm5 = __instance.uifunctions.secondColumm;
						secondColumm5.text = secondColumm5.text + databaseShipData.beam + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceMetre" ) + "\n";
					}
					else {
						Text secondColumm6 = __instance.uifunctions.secondColumm;
						secondColumm6.text = secondColumm6.text + databaseShipData.displayBeam + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceMetre" ) + "\n";
					}
					float num = databaseShipData.surfacespeed;
					if( databaseShipData.shipType == "SUBMARINE" ) {
						num = databaseShipData.submergedspeed;
					}
					if( !( databaseShipData.shipType != "BIOLOGIC" ) ) {
						Text secondColumm7 = __instance.uifunctions.secondColumm;
						secondColumm7.text = secondColumm7.text + num + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceKnot" );
						Text mainColumn = __instance.uifunctions.mainColumn;
						mainColumn.text = mainColumn.text + "\n\n\n\n<b>" + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceNotes" ) + ":</b>\n";
						for( int i = 0; i < databaseShipData.history.Length; i++ ) {
							Text mainColumn2 = __instance.uifunctions.mainColumn;
							mainColumn2.text = mainColumn2.text + databaseShipData.history[i] + "\n";
						}
						return false;
					}
					Text secondColumm8 = __instance.uifunctions.secondColumm;
					text = secondColumm8.text;
					secondColumm8.text = text + num + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceKnot" ) + ", " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceCrew" ) + " " + databaseShipData.crew;
					if( databaseShipData.shipType == "SUBMARINE" ) {
						Text secondColumm9 = __instance.uifunctions.secondColumm;
						text = secondColumm9.text;
						secondColumm9.text = text + "\n" + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceTestDepth" ) + " " + databaseShipData.testDepth + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceFeet" );
						if( databaseShipData.numberOfWires > 0 ) {
							Text secondColumm10 = __instance.uifunctions.secondColumm;
							text = secondColumm10.text;
							secondColumm10.text = text + "\n" + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceWires" ) + " " + databaseShipData.numberOfWires;
						}
					}
					int num2 = 0;
					Text mainColumn3 = __instance.uifunctions.mainColumn;
					mainColumn3.text = mainColumn3.text + "<b>" + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceDefensive" ) + ":</b>\n";
					if( databaseShipData.defensiveWeapons != null ) {
						for( int j = 0; j < databaseShipData.defensiveWeapons.Length; j++ ) {
							Text mainColumn4 = __instance.uifunctions.mainColumn;
							mainColumn4.text = mainColumn4.text + databaseShipData.defensiveWeapons[j] + "\n";
							num2++;
						}
					}
					if( databaseShipData.gunProbability > 0f ) {
						Text mainColumn5 = __instance.uifunctions.mainColumn;
						mainColumn5.text = mainColumn5.text + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "Reference30mmCIWS" ) + "\n";
					}
					if( num2 == 0 ) {
						Text mainColumn6 = __instance.uifunctions.mainColumn;
						mainColumn6.text = mainColumn6.text + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceNone" ) + "\n";
					}
					Text mainColumn7 = __instance.uifunctions.mainColumn;
					mainColumn7.text = mainColumn7.text + "\n<b>" + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceOffensive" ) + ":</b>\n";
					num2 = 0;
					if( databaseShipData.missileGameObject != null && databaseShipData.missilesPerLauncher.Length > 0 ) {
						Text mainColumn8 = __instance.uifunctions.mainColumn;
						text = mainColumn8.text;
						mainColumn8.text = text + __instance.uifunctions.database.databaseweapondata[databaseShipData.missileType].weaponName + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceMissiles" ) + "\n";
						num2++;
					}
					if( databaseShipData.torpedotubes > 0 ) {
						List<int> list = new List<int>();
						for( int k = 0; k < databaseShipData.torpedotypes.Length; k++ ) {
							list.Add( databaseShipData.torpedoIDs[k] );
						}
						if( databaseShipData.vlsTorpedoIDs != null && databaseShipData.vlsTorpedoIDs.Length > 0 ) {
							for( int l = 0; l < databaseShipData.vlsTorpedotypes.Length; l++ ) {
								if( !list.Contains( databaseShipData.vlsTorpedoIDs[l] ) ) {
									list.Add( databaseShipData.vlsTorpedoIDs[l] );
								}
							}
						}
						for( int m = 0; m < list.Count; m++ ) {
							__instance.uifunctions.mainColumn.text += __instance.uifunctions.database.databaseweapondata[list[m]].weaponName;
							if( __instance.uifunctions.database.databaseweapondata[list[m]].isMissile ) {
								Text mainColumn9 = __instance.uifunctions.mainColumn;
								mainColumn9.text = mainColumn9.text + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceMissiles" ) + "\n";
								num2++;
							}
							else if( __instance.uifunctions.database.databaseweapondata[list[m]].isDecoy ) {
								Text mainColumn10 = __instance.uifunctions.mainColumn;
								mainColumn10.text = mainColumn10.text + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceDecoys" ) + "\n";
								num2++;
							}
							else {
								Text mainColumn11 = __instance.uifunctions.mainColumn;
								mainColumn11.text = mainColumn11.text + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceTorpedoes" ) + "\n";
								num2++;
							}
						}
					}
					if( databaseShipData.rbuLauncherTypes != null ) {
						string text2 = string.Empty;
						for( int n = 0; n < databaseShipData.rbuLauncherTypes.Length; n++ ) {
							string depthchargeName = __instance.uifunctions.database.databasedepthchargedata[databaseShipData.rbuLauncherTypes[n]].depthchargeName;
							if( !text2.Contains( depthchargeName ) ) {
								text2 = text2 + depthchargeName + "\n";
								num2++;
							}
						}
						__instance.uifunctions.mainColumn.text += text2;
					}
					if( databaseShipData.navalGunTypes != null ) {
						string text3 = string.Empty;
						for( int num3 = 0; num3 < databaseShipData.navalGunTypes.Length; num3++ ) {
							string depthchargeName2 = __instance.uifunctions.database.databasedepthchargedata[databaseShipData.navalGunTypes[num3]].depthchargeName;
							if( !text3.Contains( depthchargeName2 ) ) {
								text3 = text3 + depthchargeName2 + "\n";
								num2++;
							}
						}
						__instance.uifunctions.mainColumn.text += text3;
					}
					if( num2 == 0 ) {
						Text mainColumn12 = __instance.uifunctions.mainColumn;
						mainColumn12.text = mainColumn12.text + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceNone" ) + "\n";
					}
					num2 = 0;
					Text mainColumn13 = __instance.uifunctions.mainColumn;
					mainColumn13.text = mainColumn13.text + "\n<b>" + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceSensors" ) + ":</b>\n";
					if( databaseShipData.activeSonarID == -1 && databaseShipData.passiveSonarID == -1 && databaseShipData.towedSonarID == -1 && databaseShipData.radarID == -1 ) {
						Text mainColumn14 = __instance.uifunctions.mainColumn;
						mainColumn14.text = mainColumn14.text + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceNavRADAR" ) + "\n";
					}
					else {
						if( databaseShipData.radarID > -1 ) {
							Text mainColumn15 = __instance.uifunctions.mainColumn;
							mainColumn15.text = mainColumn15.text + __instance.uifunctions.database.databaseradardata[databaseShipData.radarID].radarDisplayName + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceSearchRADAR" );
							if( databaseShipData.shipType == "SUBMARINE" ) {
								Text mainColumn16 = __instance.uifunctions.mainColumn;
								mainColumn16.text = mainColumn16.text + ", " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceMastMounted" ) + "\n";
							}
							else {
								__instance.uifunctions.mainColumn.text += "\n";
							}
						}
						bool flag = false;
						if( databaseShipData.activeSonarID > -1 && databaseShipData.passiveSonarID > -1 ) {
							if( __instance.uifunctions.database.databasesonardata[databaseShipData.activeSonarID].sonarActiveSensitivity > 0f && __instance.uifunctions.database.databasesonardata[databaseShipData.activeSonarID].sonarPassiveSensitivity > 0f ) {
								Text mainColumn17 = __instance.uifunctions.mainColumn;
								text = mainColumn17.text;
								mainColumn17.text = text + __instance.uifunctions.database.databasesonardata[databaseShipData.activeSonarID].sonarDisplayName + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceSonarBoth" ) + ", ";
								Text mainColumn18 = __instance.uifunctions.mainColumn;
								text = mainColumn18.text;
								mainColumn18.text = text + Traverse.Create( __instance ).Method( "GetFrequencies", new object[] { __instance.uifunctions.database.databasesonardata[databaseShipData.activeSonarID].sonarFrequencies } ).GetValue<string>() + ", " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceHullMounted" ) + "\n";
							}
							else {
								flag = true;
							}
						}
						else {
							flag = true;
						}
						if( flag ) {
							if( databaseShipData.activeSonarID > -1 ) {
								Text mainColumn19 = __instance.uifunctions.mainColumn;
								text = mainColumn19.text;
								mainColumn19.text = text + __instance.uifunctions.database.databasesonardata[databaseShipData.activeSonarID].sonarDisplayName + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceSonarActive" ) + ", ";
								Text mainColumn20 = __instance.uifunctions.mainColumn;
								text = mainColumn20.text;
								mainColumn20.text = text + Traverse.Create( __instance ).Method( "GetFrequencies", new object[] { __instance.uifunctions.database.databasesonardata[databaseShipData.activeSonarID].sonarFrequencies } ).GetValue<string>() + ", " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceHullMounted" ) + "\n";
							}
							if( databaseShipData.passiveSonarID > -1 ) {
								Text mainColumn21 = __instance.uifunctions.mainColumn;
								text = mainColumn21.text;
								mainColumn21.text = text + __instance.uifunctions.database.databasesonardata[databaseShipData.passiveSonarID].sonarDisplayName + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceSonarPassive" ) + ", ";
								Text mainColumn22 = __instance.uifunctions.mainColumn;
								text = mainColumn22.text;
								mainColumn22.text = text + Traverse.Create( __instance ).Method( "GetFrequencies", new object[] { __instance.uifunctions.database.databasesonardata[databaseShipData.passiveSonarID].sonarFrequencies } ).GetValue<string>() + ", " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceHullMounted" ) + "\n";
							}
						}
						if( databaseShipData.towedSonarID > -1 ) {
							Text mainColumn23 = __instance.uifunctions.mainColumn;
							mainColumn23.text = mainColumn23.text + __instance.uifunctions.database.databasesonardata[databaseShipData.towedSonarID].sonarDisplayName + " ";
							if( __instance.uifunctions.database.databasesonardata[databaseShipData.towedSonarID].sonarActiveSensitivity > 0f && __instance.uifunctions.database.databasesonardata[databaseShipData.towedSonarID].sonarPassiveSensitivity > 0f ) {
								Text mainColumn24 = __instance.uifunctions.mainColumn;
								mainColumn24.text = mainColumn24.text + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceSonarBoth" ) + ", ";
							}
							else if( __instance.uifunctions.database.databasesonardata[databaseShipData.towedSonarID].sonarActiveSensitivity > 0f ) {
								Text mainColumn25 = __instance.uifunctions.mainColumn;
								mainColumn25.text = mainColumn25.text + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceSoanrActive" ) + ", ";
							}
							else {
								Text mainColumn26 = __instance.uifunctions.mainColumn;
								mainColumn26.text = mainColumn26.text + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceSonarPassive" ) + ", ";
							}
							Text mainColumn27 = __instance.uifunctions.mainColumn;
							text = mainColumn27.text;
							mainColumn27.text = text + Traverse.Create( __instance ).Method( "GetFrequencies", new object[] { __instance.uifunctions.database.databasesonardata[databaseShipData.passiveSonarID].sonarFrequencies } ).GetValue<string>() + ", " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceTowedArray" ) + "\n";
						}
					}
					if( databaseShipData.aircraftOnBoard != null ) {
						Text mainColumn28 = __instance.uifunctions.mainColumn;
						mainColumn28.text = mainColumn28.text + "\n<b>" + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceAircraft" ) + ":</b>\n";
						for( int num4 = 0; num4 < databaseShipData.aircraftOnBoard.Length; num4++ ) {
							Text mainColumn29 = __instance.uifunctions.mainColumn;
							mainColumn29.text = mainColumn29.text + databaseShipData.aircraftOnBoard[num4] + "\n";
						}
					}
					Text mainColumn30 = __instance.uifunctions.mainColumn;
					mainColumn30.text = mainColumn30.text + "\n<b>" + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceNotes" ) + ":</b>\n";
					for( int num5 = 0; num5 < databaseShipData.history.Length; num5++ ) {
						Text mainColumn31 = __instance.uifunctions.mainColumn;
						mainColumn31.text = mainColumn31.text + databaseShipData.history[num5] + "\n";
					}
				}
				//Debug.Log( 254 );
				if( museumObjectType == "aircraft" ) {
					//Debug.Log( 257 );
					DatabaseAircraftData databaseAircraftData = __instance.uifunctions.database.databaseaircraftdata[index];
					__instance.uifunctions.mainColumn.text = "\n<size=22><b>" + databaseAircraftData.aircraftDescriptiveName + "</b></size>\n\n";
					Text secondColumm11 = __instance.uifunctions.secondColumm;
					secondColumm11.text = secondColumm11.text + "\n<size=22> </size>\n\n<b>" + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceDimensions" ) + ":</b>\n";
					Text secondColumm12 = __instance.uifunctions.secondColumm;
					string text = secondColumm12.text;
					secondColumm12.text = text + string.Format( "{0:#,0}", databaseAircraftData.weight ) + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceKilogram" ) + "\n";
					Text secondColumm13 = __instance.uifunctions.secondColumm;
					text = secondColumm13.text;
					secondColumm13.text = text + databaseAircraftData.length + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceMetre" ) + " x " + databaseAircraftData.height + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceMetre" ) + "\n";
					Text secondColumm14 = __instance.uifunctions.secondColumm;
					text = secondColumm14.text;
					secondColumm14.text = text + databaseAircraftData.cruiseSpeed + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceKnot" ) + " , " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceCrew" ) + ": " + databaseAircraftData.crew;
					int num6 = 0;
					Text mainColumn32 = __instance.uifunctions.mainColumn;
					mainColumn32.text = mainColumn32.text + "<b>" + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceOffensive" ) + ":</b>\n";
					//Debug.Log( 273 );
					if( databaseAircraftData.torpedotypes.Length > 0 ) {
						for( int num7 = 0; num7 < databaseAircraftData.torpedotypes.Length; num7++ ) {
							__instance.uifunctions.mainColumn.text += __instance.uifunctions.database.databaseweapondata[databaseAircraftData.torpedoIDs[num7]].weaponName;
							if( __instance.uifunctions.database.databaseweapondata[databaseAircraftData.torpedoIDs[num7]].isMissile ) {
								Text mainColumn33 = __instance.uifunctions.mainColumn;
								mainColumn33.text = mainColumn33.text + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceMissiles" ) + "\n";
								num6++;
							}
							else if( __instance.uifunctions.database.databaseweapondata[databaseAircraftData.torpedoIDs[num7]].isDecoy ) {
								Text mainColumn34 = __instance.uifunctions.mainColumn;
								mainColumn34.text = mainColumn34.text + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceDecoys" ) + "\n";
								num6++;
							}
							else {
								Text mainColumn35 = __instance.uifunctions.mainColumn;
								mainColumn35.text = mainColumn35.text + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceTorpedoes" ) + "\n";
								num6++;
							}
						}
					}
					//Debug.Log( 294 );
					if( num6 == 0 ) {
						Text mainColumn36 = __instance.uifunctions.mainColumn;
						mainColumn36.text = mainColumn36.text + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceNone" ) + "\n";
					}
					num6 = 0;
					Text mainColumn37 = __instance.uifunctions.mainColumn;
					mainColumn37.text = mainColumn37.text + "\n<b>" + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceSensors" ) + ":</b>\n";
					if( databaseAircraftData.radarID > -1 ) {
						Text mainColumn38 = __instance.uifunctions.mainColumn;
						text = mainColumn38.text;
						mainColumn38.text = text + __instance.uifunctions.database.databaseradardata[databaseAircraftData.radarID].radarDisplayName + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceSearchRADAR" ) + "\n";
					}
					//Debug.Log( 307 );
					bool flag2 = false;
					if( databaseAircraftData.activeSonarID > -1 && databaseAircraftData.passiveSonarID > -1 ) {
						if( __instance.uifunctions.database.databasesonardata[databaseAircraftData.activeSonarID].sonarActiveSensitivity > 0f && __instance.uifunctions.database.databasesonardata[databaseAircraftData.activeSonarID].sonarPassiveSensitivity > 0f ) {
							Text mainColumn39 = __instance.uifunctions.mainColumn;
							text = mainColumn39.text;
							mainColumn39.text = text + __instance.uifunctions.database.databasesonardata[databaseAircraftData.activeSonarID].sonarDisplayName + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceSonarBoth" ) + ", ";
							Text mainColumn40 = __instance.uifunctions.mainColumn;
							text = mainColumn40.text;
							mainColumn40.text = text + Traverse.Create( __instance ).Method( "GetFrequencies", new object[] { __instance.uifunctions.database.databasesonardata[databaseAircraftData.activeSonarID].sonarFrequencies } ).GetValue<string>() + ", " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceDipping" ) + "\n";
						}
						else {
							flag2 = true;
						}
					}
					else {
						flag2 = true;
					}
					if( flag2 ) {
						if( databaseAircraftData.activeSonarID > -1 ) {
							Text mainColumn41 = __instance.uifunctions.mainColumn;
							text = mainColumn41.text;
							mainColumn41.text = text + __instance.uifunctions.database.databasesonardata[databaseAircraftData.activeSonarID].sonarDisplayName + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceSonarActive" ) + " , ";
							Text mainColumn42 = __instance.uifunctions.mainColumn;
							text = mainColumn42.text;
							mainColumn42.text = text + Traverse.Create( __instance ).Method( "GetFrequencies", new object[] { __instance.uifunctions.database.databasesonardata[databaseAircraftData.activeSonarID].sonarFrequencies } ).GetValue<string>() + ", " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceDipping" ) + "\n";
						}
						if( databaseAircraftData.passiveSonarID > -1 ) {
							Text mainColumn43 = __instance.uifunctions.mainColumn;
							text = mainColumn43.text;
							mainColumn43.text = text + __instance.uifunctions.database.databasesonardata[databaseAircraftData.passiveSonarID].sonarDisplayName + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceSonarPassive" ) + ", ";
							Text mainColumn44 = __instance.uifunctions.mainColumn;
							text = mainColumn44.text;
							mainColumn44.text = text + Traverse.Create( __instance ).Method( "GetFrequencies", new object[] { __instance.uifunctions.database.databasesonardata[databaseAircraftData.passiveSonarID].sonarFrequencies } ).GetValue<string>() + ", " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceDipping" ) + "\n";
						}
					}
					//Debug.Log( 343 );
					if( databaseAircraftData.sonobuoytypes.Length > 0 ) {
						if( databaseAircraftData.sonobuoytypes[0] != "FALSE" ) {
							//Debug.Log( 346 );
							for( int num8 = 0; num8 < databaseAircraftData.sonobuoyIDs.Length; num8++ ) {
								//Debug.Log( 348 );
								Text mainColumn45 = __instance.uifunctions.mainColumn;
								mainColumn45.text = mainColumn45.text + databaseAircraftData.sonobuoyNumbers[num8] + " x ";
								//Debug.Log( 351 );
								if( __instance.uifunctions.database.databasesonardata[databaseAircraftData.sonobuoyIDs[num8]].sonarActiveSensitivity > 0f && __instance.uifunctions.database.databasesonardata[databaseAircraftData.sonobuoyIDs[num8]].sonarPassiveSensitivity > 0f ) {
									Text mainColumn46 = __instance.uifunctions.mainColumn;
									text = mainColumn46.text;
									mainColumn46.text = text + __instance.uifunctions.database.databasesonardata[databaseAircraftData.sonobuoyIDs[num8]].sonarDisplayName + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceSonarBoth" ) + ", ";
								}
								else if( __instance.uifunctions.database.databasesonardata[databaseAircraftData.sonobuoyIDs[num8]].sonarActiveSensitivity > 0f ) {
									Text mainColumn47 = __instance.uifunctions.mainColumn;
									text = mainColumn47.text;
									mainColumn47.text = text + __instance.uifunctions.database.databasesonardata[databaseAircraftData.sonobuoyIDs[num8]].sonarDisplayName + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceSonarActive" ) + ", ";
								}
								else {
									Text mainColumn48 = __instance.uifunctions.mainColumn;
									text = mainColumn48.text;
									mainColumn48.text = text + __instance.uifunctions.database.databasesonardata[databaseAircraftData.sonobuoyIDs[num8]].sonarDisplayName + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceSonarPassive" ) + ", ";
								}
								//Debug.Log( 367 );
								Text mainColumn49 = __instance.uifunctions.mainColumn;
								mainColumn49.text = mainColumn49.text + Traverse.Create( __instance ).Method( "GetFrequencies", new object[] { __instance.uifunctions.database.databasesonardata[databaseAircraftData.sonobuoyIDs[num8]].sonarFrequencies } ).GetValue<string>() + ", " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceSonobuoys" );
								__instance.uifunctions.mainColumn.text += "\n";
							}
						}
					}
					//Debug.Log( 374 );
					Text mainColumn50 = __instance.uifunctions.mainColumn;
					mainColumn50.text = mainColumn50.text + "\n<b>" + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceNotes" ) + ":</b>\n";
					for( int num9 = 0; num9 < databaseAircraftData.aircraftDescription.Length; num9++ ) {
						Text mainColumn51 = __instance.uifunctions.mainColumn;
						mainColumn51.text = mainColumn51.text + databaseAircraftData.aircraftDescription[num9] + "\n";
					}
				}
				else if( museumObjectType == "torpedo" ) {
					DatabaseWeaponData databaseWeaponData = __instance.uifunctions.database.databaseweapondata[index];
					__instance.uifunctions.mainColumn.text = "\n<size=22><b>" + databaseWeaponData.weaponDescriptiveName + "</b></size>\n\n";
					if( !databaseWeaponData.isSonobuoy ) {
						Text mainColumn52 = __instance.uifunctions.mainColumn;
						string text = mainColumn52.text;
						mainColumn52.text = text + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceRange" ) + ": " + string.Format( "{0:#,0}", databaseWeaponData.rangeInYards ) + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceYard" );
						if( databaseWeaponData.weaponType == "MISSILE" ) {
							Text mainColumn53 = __instance.uifunctions.mainColumn;
							text = mainColumn53.text;
							mainColumn53.text = text + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceAt" ) + " " + databaseWeaponData.activeRunSpeed + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceKnot" ) + "\n";
						}
						else {
							Text mainColumn54 = __instance.uifunctions.mainColumn;
							text = mainColumn54.text;
							mainColumn54.text = text + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceAt" ) + " " + databaseWeaponData.runSpeed + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceKnot" ) + "\n";
						}
						Text mainColumn55 = __instance.uifunctions.mainColumn;
						text = mainColumn55.text;
						mainColumn55.text = text + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceMaxSpeed" ) + ": " + databaseWeaponData.activeRunSpeed + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceKnot" ) + "\n";
						if( databaseWeaponData.sensorRange > 0f ) {
							Text mainColumn56 = __instance.uifunctions.mainColumn;
							text = mainColumn56.text;
							mainColumn56.text = text + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceSeekerRange" ) + ": " + string.Format( "{0:#,0}", databaseWeaponData.sensorRange ) + " " + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceYard" ) + "\n";
						}
						__instance.uifunctions.mainColumn.text += "\n";
					}
					Text mainColumn57 = __instance.uifunctions.mainColumn;
					mainColumn57.text = mainColumn57.text + "<b>" + LanguageManager.GetDictionaryString( LanguageManager.interfaceDictionary, "ReferenceNotes" ) + ":</b>\n";
					for( int num10 = 0; num10 < databaseWeaponData.weaponDescription.Length; num10++ ) {
						Text mainColumn58 = __instance.uifunctions.mainColumn;
						mainColumn58.text = mainColumn58.text + databaseWeaponData.weaponDescription[num10] + "\n";
					}
				}
				//Debug.Log( 416 );
				UIFunctions.globaluifunctions.SetMainColumnHeight( true );
				return false;
			}
		}
	}
}
