using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aspose.ThreeD;
using Aspose.ThreeD.Formats;
using Aspose.ThreeD.Entities;
using UnityEngine;

namespace Cold_Waters_Expanded
{
    static class glTFImporter
    {
        static float conversionFactor = 3.2808f / 225.39f;

        public static UnityEngine.Mesh[] GetMeshes( string path ) {
            List<UnityEngine.Mesh> meshes = new List<UnityEngine.Mesh>();
            // Initialize Scene class object
            Scene scene = new Scene();
            // Set load options
            GLTFLoadOptions loadOpt = new GLTFLoadOptions();
            // The default value is true, usually we don't need to change it. Aspose.3D will automatically flip the V/T texture coordinate during load and save.       
            loadOpt.FlipTexCoordV = true;
            scene.Open( path, loadOpt );
            foreach( Node node in scene.RootNode.ChildNodes ) {
                List<Vector3> verts = new List<Vector3>();
                List<Vector3> norms = new List<Vector3>();
                List<Vector2> uvws = new List<Vector2>();
                List<int> tris = new List<int>();
                //Debug.Log( node.Name );
                UnityEngine.Mesh outputMesh = new UnityEngine.Mesh();
                outputMesh.name = node.Name;
                // only convert meshes, lights/camera and other stuff will be ignored
                foreach( Entity entity in node.Entities ) {
                    if( entity is IMeshConvertible ) {
                        //Debug.Log( "MESH = True" );
                        Aspose.ThreeD.Entities.Mesh mesh = ( (IMeshConvertible) entity ).ToMesh();
                        // triangulate the mesh, so triFaces will only store triangle indices
                        var controlPoints = mesh.ControlPoints;
                        var normalPoints = (VertexElementNormal) mesh.GetElement( VertexElementType.Normal );
                        var uvwPoints = (VertexElementUV) mesh.GetElement( VertexElementType.UV );
                        //Debug.Log( node.Name );
                        //Debug.Log( mesh.ControlPoints.Count );
                        //Debug.Log( normalPoints.Data.Count );
                        //Debug.Log( uvwPoints.Data.Count );
                        //int[][] triFaces = PolygonModifier.Triangulate( controlPoints, mesh.Polygons );
                        // gets the global transform matrix
                        Aspose.ThreeD.Utilities.Matrix4 transformGlobal = node.GlobalTransform.TransformMatrix;
                        // write control points
                        for( int i = 0; i < controlPoints.Count; i++ ) {
                            // calculate the control points in world space and save them to file
                            var cp = transformGlobal * controlPoints[i];
                            verts.Add( conversionFactor * ( new Vector3( (float) cp.x, (float) cp.y, (float) cp.z ) ) );
                            var np = transformGlobal * normalPoints.Data[i];
                            norms.Add( new Vector3( (float) np.x, (float) np.y, (float) np.z ) );
                            uvws.Add( new Vector2( (float) uvwPoints.Data[i].x, (float) uvwPoints.Data[i].y ) );
                            //Debug.Log( normalPoints.Data[i].ToString() );
                        }
                        // write triangle indices
                        foreach( int[] polygon in mesh.Polygons ) {
                            foreach( int index in polygon ) {
                                tris.Add( index );
                            }
                        }
                    }
                }
                foreach( Node childNode in node.ChildNodes ) {
                    foreach( Entity entity in childNode.Entities ) {
                        int offset = verts.Count;
                        if( entity is IMeshConvertible ) {
                            //Debug.Log( "   MESH = True" );
                            Aspose.ThreeD.Entities.Mesh mesh = ( (IMeshConvertible) entity ).ToMesh();
                            // gets the global transform matrix
                            Aspose.ThreeD.Utilities.Matrix4 transformGlobal = childNode.GlobalTransform.TransformMatrix;
                            // triangulate the mesh, so triFaces will only store triangle indices
                            var controlPoints = mesh.ControlPoints;
                            var normalPoints = (VertexElementNormal) mesh.GetElement( VertexElementType.Normal );
                            var uvwPoints = (VertexElementUV) mesh.GetElement( VertexElementType.UV );
                            //Debug.Log( childNode.Name );
                            //Debug.Log( mesh.ControlPoints.Count );
                            //Debug.Log( normalPoints.Data.Count );
                            //Debug.Log( uvwPoints.Data.Count );
                            //int[][] triFaces = PolygonModifier.Triangulate( controlPoints, mesh.Polygons );

                            for( int i = 0; i < controlPoints.Count; i++ ) {
                                // calculate the control points in world space and save them to file
                                var cp = transformGlobal * controlPoints[i];
                                verts.Add( conversionFactor * ( new Vector3( (float) cp.x, (float) cp.y, (float) cp.z ) ) );
                                var np = transformGlobal * normalPoints.Data[i];
                                norms.Add( new Vector3( (float) np.x, (float) np.y, (float) np.z ) );
                                uvws.Add( new Vector2( (float) uvwPoints.Data[i].x, (float) uvwPoints.Data[i].y ) );
                            }
                            // write polygon indices
                            foreach( int[] polygon in mesh.Polygons ) {
                                foreach( int index in polygon ) {
                                    tris.Add( index + offset );
                                }
                            }
                        }
                    }
                }
                //Debug.Log( outputMesh.name );
                //Debug.Log( verts.Count + " " + norms.Count + " " + uvws.Count );
                outputMesh.vertices = verts.ToArray();
                outputMesh.normals = norms.ToArray();
                outputMesh.uv = uvws.ToArray();
                outputMesh.triangles = tris.ToArray();
                //outputMesh.RecalculateNormals();
                outputMesh.RecalculateBounds();
                //Debug.Log( "Output MESH " + outputMesh.vertices.Length + " " + outputMesh.normals.Length + " " + outputMesh.triangles.Length );
                meshes.Add( outputMesh );
            }
            //Debug.Log( "Output " + meshes.Count );
            return meshes.ToArray();
        }
    }
}
