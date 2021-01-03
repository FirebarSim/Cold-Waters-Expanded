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
    [BepInPlugin( "org.cwe.plugins.ui", "Cold Waters Expanded UI Patches", "1.0.1.2" )]
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
	}
}
