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
        [SerializeField] float cooldown = 0.5f;

        private ButtonActionInterface actionScript;
        private float lastPressedTime = -Mathf.Infinity;

        private void Start()
        {
            actionScript = (ButtonActionInterface)actionScriptOfButtonActionInterface;
        }

        private void OnTriggerEnter(Collider other)
        {
            if(other.Equals(detectionTrigger) && Time.time - lastPressedTime >= cooldown)
            {
                lastPressedTime = Time.time;
                AudioSource.PlayClipAtPoint(buttonClick, this.transform.position);
                actionScript.Play();
            }
        }
    }
}