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
        static Dictionary<string, UnityEngine.Mesh[]> preLoadedMesh = new Dictionary<string, UnityEngine.Mesh[]>();
        static float conversionFactor = 3.2808f / 225.39f;

        public static UnityEngine.Mesh[] GetMeshes( string path ) {
            if( preLoadedMesh.ContainsKey(path) ) {
                return preLoadedMesh[path];
            }
            List<UnityEngine.Mesh> meshes = new List<UnityEngine.Mesh>();
            // Initialize Scene class object
            Scene scene = new Scene();
            // Set load options
            GLTFLoadOptions loadOpt = new GLTFLoadOptions();
            // The default value is true, usually we don't need to change it. Aspose.3D will automatically flip the V/T texture coordinate during load and save.
            //Debug.Log( scene.RootNode.ChildNodes.Count );
            loadOpt.FlipTexCoordV = true;
            scene.Open( path, loadOpt );
            foreach( Node node in scene.RootNode.ChildNodes ) {
                List<Vector3> verts = new List<Vector3>();
                List<Vector3> norms = new List<Vector3>();
                List<Vector2> uvws = new List<Vector2>();
                List<int> tris = new List<int>();
                UnityEngine.Mesh outputMesh = new UnityEngine.Mesh();
                outputMesh.name = node.Name;
                // only convert meshes, lights/camera and other stuff will be ignored
                //Debug.Log( node.Name );
                //Debug.Log( node.ChildNodes.Count );
                //Debug.Log( node.Transform.TransformMatrix.ToString() );
                Aspose.ThreeD.Utilities.Matrix4 nodeInverseTransform = node.Transform.TransformMatrix.Inverse();
                foreach( Entity entity in node.Entities ) {
                    if( entity is IMeshConvertible ) {
                        //Debug.Log( "MESH = True" );
                        Aspose.ThreeD.Entities.Mesh mesh = ( (IMeshConvertible) entity ).ToMesh();
                        // gets the global transform matrix
                        Aspose.ThreeD.Utilities.Matrix4 transformGlobal = node.GlobalTransform.TransformMatrix;
                        // gets the points of interest from the mesh
                        var controlPoints = mesh.ControlPoints;
                        var normalPoints = (VertexElementNormal) mesh.GetElement( VertexElementType.Normal );
                        var uvwPoints = (VertexElementUV) mesh.GetElement( VertexElementType.UV );
                        // write control points
                        for( int i = 0; i < controlPoints.Count; i++ ) {
                            // calculate the control points in world space and save them to file
                            var cp = transformGlobal * nodeInverseTransform * controlPoints[i];
                            //verts.Add( conversionFactor * ( new Vector3( (float) cp.x, (float) cp.y * -1f, (float) cp.z ) ) );
                            verts.Add( conversionFactor * ( new Vector3( (float) cp.x * -1f, (float) cp.y, (float) cp.z ) ) );
                            var np = transformGlobal * nodeInverseTransform * normalPoints.Data[i];
                            norms.Add( conversionFactor * ( new Vector3( (float) np.x * -1f, (float) np.y, (float) np.z ) ) );
                            uvws.Add( new Vector2( (float) uvwPoints.Data[i].x, (float) uvwPoints.Data[i].y ) );
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
                    //Debug.Log( "   " + childNode.Name );
                    foreach( Entity entity in childNode.Entities ) {
                        int offset = verts.Count;
                        if( entity is IMeshConvertible ) {
                            Aspose.ThreeD.Entities.Mesh mesh = ( (IMeshConvertible) entity ).ToMesh();
                            // gets the global transform matrix
                            Aspose.ThreeD.Utilities.Matrix4 transformGlobal = childNode.GlobalTransform.TransformMatrix;
                            // gets the points of interest from the mesh0
                            var controlPoints = mesh.ControlPoints;
                            var normalPoints = (VertexElementNormal) mesh.GetElement( VertexElementType.Normal );
                            var uvwPoints = (VertexElementUV) mesh.GetElement( VertexElementType.UV );
                            for( int i = 0; i < controlPoints.Count; i++ ) {
                                // calculate the control points in world space and save them to file
                                var cp = transformGlobal * nodeInverseTransform * controlPoints[i];
                                //verts.Add( conversionFactor * ( new Vector3( (float) cp.x, (float) cp.y, (float) cp.z ) ) );
                                verts.Add( conversionFactor * ( new Vector3( (float) cp.x * -1f, (float) cp.y, (float) cp.z ) ) );
                                var np = transformGlobal * nodeInverseTransform * normalPoints.Data[i];
                                norms.Add( new Vector3( (float) np.x * -1f, (float) np.y, (float) np.z ) );
                                uvws.Add( conversionFactor * ( new Vector2( (float) uvwPoints.Data[i].x, (float) uvwPoints.Data[i].y ) ) );
                            }
                            // write triangle indices
                            foreach( int[] polygon in mesh.Polygons ) {
                                foreach( int index in polygon ) {
                                    tris.Add( index + offset );
                                }
                            }
                        }
                    }
                }
                outputMesh.vertices = verts.ToArray();
                outputMesh.normals = norms.ToArray();
                outputMesh.uv = uvws.ToArray();
                outputMesh.triangles = tris.ToArray().Reverse().ToArray();
                outputMesh.RecalculateBounds();
                meshes.Add( outputMesh );
            }
            preLoadedMesh.Add( path, meshes.ToArray() );
            return meshes.ToArray();
        }
    }
}
