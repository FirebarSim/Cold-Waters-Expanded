using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Cold_Waters_Expanded
{
    [Serializable]
    class SerialiseMaterial
    {
        [SerializeField] string shaderReference;
        [SerializeField] List<float> ambientColour;
        [SerializeField] List<float> diffuseColour;
        [SerializeField] float specularExponent;
        [SerializeField] List<float> specularColour;
        [SerializeField] float opaqueness;
        [SerializeField] float opticalDensity;
        [SerializeField] float roughness;
        [SerializeField] float metallic;
        [SerializeField] float sheen;

        public Material GetMaterial() {
            Material material = new Material( Resources.Load( "ships/usn_ssn_skipjack/usn_ssn_skipjack_mat" ) as Material );
            Color diffuse;
            if( diffuseColour.Count == 3 ) {
                diffuse = new Color( diffuseColour[0], diffuseColour[1], diffuseColour[2], 1.0f );
            }
            else {
                diffuse = new Color( diffuseColour[0], diffuseColour[1], diffuseColour[2], diffuseColour[3] );
            }
            material.SetColor( "_Color", diffuse );
            Color specular;
            if( specularColour.Count == 3 ) {
                specular = new Color( specularColour[0], specularColour[1], specularColour[2], 1.0f );
            }
            else {
                specular = new Color( specularColour[0], specularColour[1], specularColour[2], specularColour[3] );
            }
            material.SetColor( "_SpecColor", specular );
            material.SetFloat( "_Shininess", sheen );
            material.SetFloat( "_Metallic", metallic );
            material.SetFloat( "_SpecInt", specularExponent );
            material.SetTexture( "_MainTex", null );
            material.SetTexture( "_SpecTex", null );
            material.SetTexture( "_BumpMap", null );
            return material;
        }
    }
}
