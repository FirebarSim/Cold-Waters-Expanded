using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Cold_Waters_Expanded
{
    class MeshAnimate : MonoBehaviour
    {
        public Vessel vessel;
        public string conditionString;

        bool conditionBool = false;
        Assembly assembly;
        FieldInfo fieldInfo;
        object instance;
        enum Evaluation { Equals, NotEquals, GreaterThan, LessThan }
        Evaluation evaluation;
        enum TranslationKind { Translate, Rotate, Scale}
        TranslationKind translationKind;
        float duration = 1;
        Vector3 translationAmount;
        Vector3 startPosition;
        Vector3 startRotation;
        Vector3 startScale;
        Vector3 target;
        float increment;

        void Awake() {
            assembly = Assembly.GetAssembly( typeof( Vessel ) );
        }

        void Start() {
            fieldInfo = assembly.GetType( conditionString.Split( ',' )[0].Trim() ).GetField( conditionString.Split( ',' )[1].Trim() );
            instance = conditionString.Split( ',' )[2].Trim().Length > 0 ? typeof( Vessel ).GetField( conditionString.Split( ',' )[2].Trim() ).GetValue( vessel ) : vessel;
            evaluation = (Evaluation) Enum.Parse( typeof( Evaluation ), conditionString.Split( ',' )[3].Trim() );
            translationKind = (TranslationKind) Enum.Parse( typeof( TranslationKind ), conditionString.Split( ',' )[5].Trim() );
            duration = float.Parse( conditionString.Split( ',' )[6].Trim() );
            translationAmount = new Vector3( float.Parse( conditionString.Split( ',' )[7].Trim() ), float.Parse( conditionString.Split( ',' )[8].Trim() ), float.Parse( conditionString.Split( ',' )[9].Trim() ) );
            startPosition = transform.localPosition;
            startRotation = transform.localRotation.eulerAngles;
            startScale = transform.localScale;
            target = startPosition + translationAmount;
            increment = ( target.magnitude / duration ) * Time.fixedDeltaTime; 
        }

        void Update() {
            switch( translationKind ) {
                case TranslationKind.Translate:
                    conditionBool = EvaluateCondition();
                    if( conditionBool == true ) {
                        transform.localPosition = Vector3.MoveTowards( transform.localPosition, target, increment );
                    }
                    else if( conditionBool == false ) {
                        transform.localPosition = Vector3.MoveTowards( transform.localPosition, startPosition, increment );
                    }
                    break;
                //case TranslationKind.Rotate:
                //    break;
                //case TranslationKind.Scale:
                //    break;
                default:
                    break;
            }
        }

        bool EvaluateCondition() {
            switch( evaluation ) {
                case Evaluation.Equals:
                    return (float) fieldInfo.GetValue( instance ) == float.Parse( conditionString.Split( ',' )[4].Trim() );
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
