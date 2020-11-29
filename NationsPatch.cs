using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace Cold_Waters_Expanded
{
    [BepInPlugin( "org.cwe.plugins.nation", "Cold Waters Expanded Nation Patches", "1.0.0.0" )]
    class NationsPatch : BaseUnityPlugin
    {
        static NationsPatch nationsPatch;
        List<string> nations;
        List<Sprite> nationSprites;

        void Awake() {
            nationsPatch = this;
        }

        public void LoadNewFlags() {
            foreach( string path in Directory.GetFiles( Application.streamingAssetsPath + "/override/hud/flags/", "flag_*.png" ) ) {
                //Debug.Log( Path.GetFileNameWithoutExtension(path).Replace("flag_","" ));
                nations.Add( Path.GetFileNameWithoutExtension( path ).Replace( "flag_", "" ) );
                nationSprites.Add( CreateSprite(path) );
            }
        }

        public Sprite CreateSprite(string path ) {
            byte[] fileData = File.ReadAllBytes( path );
            Texture2D texture2D = new Texture2D( 2, 2 );
            texture2D.LoadImage( fileData );
            Sprite sprite = Sprite.Create( texture2D, new Rect( 0, 0, texture2D.width, texture2D.height ), new Vector2( 0.5f, 0.5f ) );
            return sprite;
        }

        [HarmonyPatch( typeof( UIFunctions ), "Awake" )]
        public class UIFunctions_Awake_Patch
        {
            [HarmonyPostfix]
            public static void Postfix( UIFunctions __instance ) {
                Debug.Log( "UIFunctions_Awake" );
                if( __instance.levelloadmanager.nationSprites.Length == 4 ) {
                    nationsPatch.nations = Traverse.Create( __instance.levelloadmanager ).Field( "nations" ).GetValue<string[]>().ToList<string>();
                    nationsPatch.nationSprites = Traverse.Create( __instance.levelloadmanager ).Field( "nationSprites" ).GetValue<Sprite[]>().ToList<Sprite>();
                    nationsPatch.LoadNewFlags();
                    Traverse.Create( UIFunctions.globaluifunctions.levelloadmanager ).Field( "nations" ).SetValue( nationsPatch.nations.ToArray() );
                    Traverse.Create( UIFunctions.globaluifunctions.levelloadmanager ).Field( "nationSprites" ).SetValue( nationsPatch.nationSprites.ToArray() );
                    Debug.Log( "Nations Patched" );
                }
            }
        }
    }
}
