using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Cold_Waters_Expanded
{
    public static class ObjImporter
    {
        static float conversionFactor = 3.2808f / 225.39f;

        public static Mesh[] GetMeshes( string path ) {
            List<Mesh> meshes = new List<Mesh>();
            List<Vector3> vertsLibrary = new List<Vector3>();
            List<Vector3> normsLibrary = new List<Vector3>();
            List<Vector2> uvsLibrary = new List<Vector2>();
            string[] allLines = File.ReadAllLines( path );
            Vector3 corrOffset = new Vector3( 0, 0, 0 );
            foreach( string line in allLines ) {
                switch( line.Split( ' ' )[0] ) {
                    case "#o":
                        corrOffset = new Vector3( float.Parse( line.Split( ' ' )[1] ), float.Parse( line.Split( ' ' )[2] ), float.Parse( line.Split( ' ' )[3] ) );
                        break;
                    case "v":
                        vertsLibrary.Add( conversionFactor * ( new Vector3( float.Parse( line.Split( ' ' )[1] ), float.Parse( line.Split( ' ' )[2] ), float.Parse( line.Split( ' ' )[3] ) ) - corrOffset ) );
                        break;
                    case "vt":
                        uvsLibrary.Add( new Vector2( float.Parse( line.Split( ' ' )[1] ), float.Parse( line.Split( ' ' )[2] ) ) );
                        break;
                    case "vn":
                        normsLibrary.Add( new Vector3( float.Parse( line.Split( ' ' )[1] ), float.Parse( line.Split( ' ' )[2] ), float.Parse( line.Split( ' ' )[3] ) ) );
                        break;
                    default:
                        break;
                }
            }
            //Debug.Log( "Verts: " + vertsLibrary.Count + " UVs: " + uvsLibrary.Count + " Normals: " + normsLibrary.Count );
            int i = 0;
            foreach( string line in allLines ) {
                switch( line.Split( ' ' )[0] ) {
                    case "g":
                        Mesh mesh = new Mesh();
                        mesh.name = line.Split( ' ' )[1];
                        List<Vector3> verts = new List<Vector3>();
                        List<Vector3> norms = new List<Vector3>();
                        List<Vector2> uvws = new List<Vector2>();
                        List<int> tris = new List<int>();
                        int index = 0;
                        bool exported = false;
                        for( int j = i + 1; j < allLines.Length; j++ ) {
                            if( exported ) {
                                break;
                            }
                            switch( allLines[j].Split( ' ' )[0] ) {
                                case "f":
                                    tris.Add( index );
                                    verts.Add( vertsLibrary[int.Parse( allLines[j].Split( ' ' )[1].Split( '/' )[0] ) - 1] );
                                    norms.Add( normsLibrary[int.Parse( allLines[j].Split( ' ' )[1].Split( '/' )[2] ) - 1] );
                                    uvws.Add( uvsLibrary[int.Parse( allLines[j].Split( ' ' )[1].Split( '/' )[1] ) - 1] );
                                    index++;
                                    tris.Add( index );
                                    verts.Add( vertsLibrary[int.Parse( allLines[j].Split( ' ' )[2].Split( '/' )[0] ) - 1] );
                                    norms.Add( normsLibrary[int.Parse( allLines[j].Split( ' ' )[2].Split( '/' )[2] ) - 1] );
                                    uvws.Add( uvsLibrary[int.Parse( allLines[j].Split( ' ' )[2].Split( '/' )[1] ) - 1] );
                                    index++;
                                    tris.Add( index );
                                    verts.Add( vertsLibrary[int.Parse( allLines[j].Split( ' ' )[3].Split( '/' )[0] ) - 1] );
                                    norms.Add( normsLibrary[int.Parse( allLines[j].Split( ' ' )[3].Split( '/' )[2] ) - 1] );
                                    uvws.Add( uvsLibrary[int.Parse( allLines[j].Split( ' ' )[3].Split( '/' )[1] ) - 1] );
                                    index++;
                                    break;
                                case "g":
                                    exported = true;
                                    break;
                                default:
                                    break;
                            }
                        }
                        mesh.vertices = verts.ToArray();
                        mesh.normals = norms.ToArray();
                        mesh.uv = uvws.ToArray();
                        mesh.triangles = tris.ToArray();
                        mesh.RecalculateBounds();
                        meshes.Add( mesh );
                        exported = true;
                        //Debug.Log( mesh.name );
                        break;
                    default:
                        break;
                }
                i++;
            }
            return meshes.ToArray();
        }

    }

}
