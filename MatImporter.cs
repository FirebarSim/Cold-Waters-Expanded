using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Cold_Waters_Expanded
{
    class MatImporter
    {
        //Material material = new Material( Resources.Load( "ships/usn_ssn_skipjack/usn_ssn_skipjack_mat" ) as Material);
        Material material;

        public MatImporter(Material baseMaterial, string path) {
            material = new Material( baseMaterial );
            string[] allLines = File.ReadAllLines( path );
            //material.SetColor( "_Color", new Color( 0.8f, 0.5f, 0.2f, 1.0f ) );
            foreach( string line in allLines ) {
                switch( line.Split( ' ' )[0] ) {
                    case "newmtl":
                        material.name = line.Split( ' ' )[1];
                        break;
                    case "Ka":
                        if( line.Split( ' ' ).Length == 4 ) {
                            material.SetColor( "_Color", new Color( float.Parse( line.Split( ' ' )[1] ), float.Parse( line.Split( ' ' )[2] ), float.Parse( line.Split( ' ' )[3] ), 1.0f ) );
                        }
                        else if( line.Split( ' ' ).Length == 5 ) {
                            material.SetColor( "_Color", new Color( float.Parse( line.Split( ' ' )[1] ), float.Parse( line.Split( ' ' )[2] ), float.Parse( line.Split( ' ' )[3] ), float.Parse( line.Split( ' ' )[4] ) ) );
                        }
                        break;
                    case "Pm":
                        material.SetFloat( "_Metallic", float.Parse( line.Split( ' ' )[1] ) );
                        break;
                    case "Ps":
                        material.SetFloat( "_Glossiness", float.Parse( line.Split( ' ' )[1] ) );
                        break;
                    default:
                        break;
                }
            }
        }

        public Material GetMaterial() {
            return material;
        }
    }
}
