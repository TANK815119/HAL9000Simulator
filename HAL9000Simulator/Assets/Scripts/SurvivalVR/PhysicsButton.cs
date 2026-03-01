using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rekabsen
{
    //detects when a button hits it trigger then calls a function of another script
    //must be on one of the triggers

    [RequireComponent(typeof(Collider))]
    public class PhysicsButton : MonoBehaviour
    {
        [SerializeField] Collider detectionTrigger;
        [SerializeField] Component actionScriptOfButtonActionInterface; //must be ButtonActionInterface
        [SerializeField] AudioClip buttonClick;

        private ButtonActionInterface actionScript;

        private void Start()
        {
            actionScript = (ButtonActionInterface)actionScriptOfButtonActionInterface;
        }

        private void OnTriggerEnter(Collider other)
        {
            if(other.Equals(detectionTrigger))
            {
                AudioSource.PlayClipAtPoint(buttonClick, this.transform.position);
                actionScript.Play();
            }
        }
    }
}