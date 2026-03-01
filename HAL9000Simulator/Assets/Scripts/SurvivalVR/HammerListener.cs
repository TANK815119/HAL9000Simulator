using System.Collections.Generic;
using UnityEngine;

namespace Rekabsen
{
    //intended to be placed on objects withih the trigger of a hammer
    //will hold a list of object it is tocuhing that will be easily accesible
    //objects tagged with "Body" shouldnt be in this list
    public class HammerListener : MonoBehaviour
    {
        public List<Rigidbody> TouchingBodies { get; set; }
        private Rigidbody rb;
        private Rigidbody hammerBody;

        void Awake()
        {
            TouchingBodies = new List<Rigidbody>();
            if(TryGetComponent(out Rigidbody thisBody))
            {
                rb = thisBody;
            }
        }

        private void OnCollisionEnter(Collision collision) //oncollision stay for the case that object is touched before componenet added PROBALY SHOUDL REMOVE!!
        {
            if (collision.rigidbody != null && !IsExcludedTags(collision.gameObject.tag) && !TouchingBodies.Contains(collision.rigidbody) && !collision.rigidbody.Equals(rb) && !collision.rigidbody.Equals(hammerBody))
            {
                TouchingBodies.Add(collision.rigidbody);
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (collision.rigidbody != null && !IsExcludedTags(collision.gameObject.tag) && TouchingBodies.Contains(collision.rigidbody) && !collision.rigidbody.Equals(rb) && !collision.rigidbody.Equals(hammerBody))
            {
                TouchingBodies.Remove(collision.rigidbody);
            }
        }

        private bool IsExcludedTags(string tag)
        {
            return tag.Equals("Body") || tag.Equals("PlayerCamera");
        }

        public void SetHammerBody(Rigidbody hammer)
        {
            hammerBody = hammer;
        }
    }
}