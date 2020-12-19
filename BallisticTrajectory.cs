using UnityEngine;

namespace Cold_Waters_Expanded
{
    class BallisticTrajectory
    {
        float runDistance;
        float maxAltitude;

        public BallisticTrajectory( float maxAltitude, float runDistance ) {
            this.runDistance = runDistance;
            this.maxAltitude = maxAltitude;
        }

        public float GetAltitude( float distanceToRun ) {
            Debug.Log( "GetAltitude" );
            Debug.Log( "   runDistance: " + runDistance );
            Debug.Log( "   distanceToRun: " + distanceToRun );
            float alpha = Mathf.PI * ( ( runDistance - distanceToRun ) / runDistance );
            Debug.Log( "   alpha: " + alpha );
            Debug.Log( "   alt: " + ( maxAltitude * Mathf.Sin( alpha ) ) );
            return maxAltitude * Mathf.Sin( alpha );
        }

        public float GetPitch( float distanceToRun ) {
            float alpha = Mathf.PI * ( ( runDistance - distanceToRun ) / runDistance );
            float gradient = maxAltitude * Mathf.Cos( alpha );
            return Mathf.Rad2Deg * Mathf.Atan( gradient );
        }
    }
}
