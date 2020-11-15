using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Cold_Waters_Expanded
{
    class XPlane : MonoBehaviour
    {
        public Transform sternPlaneTransform;
        public Transform rudderTransform;
        float lastAngle = 0;
        float angleDemand = 0;
        float angleDelta = 0;

        void Update() {
            if( transform.localEulerAngles.z > 0 && transform.localEulerAngles.z < 90 ) {
                angleDemand =  - rudderTransform.localEulerAngles.y + sternPlaneTransform.localEulerAngles.x;
                angleDelta = lastAngle - angleDemand;
                lastAngle = angleDemand;
            }
            else if( transform.localEulerAngles.z > 90 && transform.localEulerAngles.z < 180 ) {
                angleDemand =  rudderTransform.localEulerAngles.y + sternPlaneTransform.localEulerAngles.x;
                angleDelta = lastAngle - angleDemand;
                lastAngle = angleDemand;
            }
            else if( transform.localEulerAngles.z > 180 && transform.localEulerAngles.z < 270 ) {
                angleDemand = rudderTransform.localEulerAngles.y - sternPlaneTransform.localEulerAngles.x;
                angleDelta = lastAngle - angleDemand;
                lastAngle = angleDemand;
            }
            else if( transform.localEulerAngles.z > 270 && transform.localEulerAngles.z < 360 ) {
                angleDemand = - rudderTransform.localEulerAngles.y - sternPlaneTransform.localEulerAngles.x;
                angleDelta = lastAngle - angleDemand;
                lastAngle = angleDemand;
            }
            transform.rotation = transform.rotation * Quaternion.Euler( 0, angleDelta, 0 );
        }
    }
}
