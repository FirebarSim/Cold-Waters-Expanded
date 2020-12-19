using System;
using System.Reflection;
using UnityEngine;


namespace Cold_Waters_Expanded
{
    class MeshVisibility : MonoBehaviour
    {
        public Vessel vessel;
        public string conditionString;

        bool conditionBool = false;
        Assembly assembly;
        FieldInfo fieldInfo;
        object instance;
        enum Evaluation { TextEquals,Equals,TextNotEquals,NotEquals,GreaterThan,LessThan }
        Evaluation evaluation;

        void Awake() {
            assembly = Assembly.GetAssembly( typeof( Vessel ) );
        }

        void Start() {
            fieldInfo = assembly.GetType( conditionString.Split( ',' )[0].Trim() ).GetField( conditionString.Split( ',' )[1].Trim() );
            instance = conditionString.Split( ',' )[2].Trim().Length > 0 ? typeof( Vessel ).GetField( conditionString.Split( ',' )[2].Trim() ).GetValue( vessel ) : vessel;
            evaluation = (Evaluation) Enum.Parse( typeof( Evaluation ), conditionString.Split( ',' )[3].Trim() );
        }

        void Update() {
            conditionBool = EvaluateCondition();
            if( conditionBool == true && gameObject.GetComponent<MeshRenderer>().enabled == false ) {
                gameObject.GetComponent<MeshRenderer>().enabled = true;
            }
            else if( conditionBool == false && gameObject.GetComponent<MeshRenderer>().enabled == true ) {
                gameObject.GetComponent<MeshRenderer>().enabled = false;
            }
        }

        bool EvaluateCondition() {
            switch( evaluation ) {
                case Evaluation.TextEquals:
                    return (string) fieldInfo.GetValue( instance ) == conditionString.Split( ',' )[4].Trim();
                case Evaluation.Equals:
                    return (float) fieldInfo.GetValue( instance ) == float.Parse( conditionString.Split( ',' )[4].Trim() );
                case Evaluation.TextNotEquals:
                    return !( (string) fieldInfo.GetValue( instance ) == conditionString.Split( ',' )[4].Trim() );
                case Evaluation.NotEquals:
                    return !( (float) fieldInfo.GetValue( instance ) == float.Parse( conditionString.Split( ',' )[4].Trim() ) );
                case Evaluation.GreaterThan:
                    return (float) fieldInfo.GetValue( instance ) > float.Parse( conditionString.Split( ',' )[4].Trim() );
                case Evaluation.LessThan:
                    return (float) fieldInfo.GetValue( instance ) < float.Parse( conditionString.Split( ',' )[4].Trim() );
                default:
                    return false;
            }
        }
    }
}
