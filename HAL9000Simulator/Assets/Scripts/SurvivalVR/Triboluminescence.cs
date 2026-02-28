using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rekabsen
{
    public class Triboluminescence : MonoBehaviour
    {
        [SerializeField] private float minForce = 1f;
        [SerializeField] private float combustRadius = 0.5f;
        [SerializeField] private GameObject sparks;
        [SerializeField] private AudioClip sizzle;

        private List<Transform> flamableList; //list of flamable objects in range(sticks)

        private void Start()
        {
            flamableList = new List<Transform>();

            //object detecting trigger
            SphereCollider sphereTrigger = gameObject.AddComponent<SphereCollider>();
            sphereTrigger.isTrigger = true;
            sphereTrigger.radius = combustRadius * 1f * (1 / transform.lossyScale.x);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.GetComponent<Flamable>() != null) //MonoBehavior stand-in
            {
                flamableList.Add(other.transform);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<MonoBehaviour>() != null) //MonoBehavior stand-in
            {
                flamableList.Remove(other.transform);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.magnitude > minForce)//check the collsion is sufficently high velocity
            {
                if (collision.gameObject.GetComponent<Triboluminescence>() != null)//check for triboluminescence
                {
                    for (int i = 0; i < collision.contactCount * 0f + 1f; i++) //for each collsion point
                    {
                        //create sparks
                        //this method of Instantiation is super expensive, but easy
                        GameObject sparkInstance = Instantiate(sparks, collision.contacts[i].point, Quaternion.identity);
                        sparkInstance.transform.forward = collision.contacts[i].normal;

                        //sizzle sound
                        AudioSource.PlayClipAtPoint(sizzle, collision.contacts[i].point);

                        //look for neary flamable objects to light
                        AttemptCombust(collision.contacts[i]);
                    }
                }
            }
        }

        private void AttemptCombust(ContactPoint contactPoint)
        {
            for (int i = 0; i < flamableList.Count; i++)
            {
                Flamable flamable = flamableList[i].gameObject.GetComponent<Flamable>();
                if (flamableList.Count > 5 && Vector3.Distance(contactPoint.point, flamableList[i].position) < combustRadius && flamable != null && !flamable.IsBurning())
                {
                    //combuts the flamable object
                    flamable.Light();
                    //GameObject fire = Instantiate(flame);
                    //fire.transform.parent = flamableList[i].transform;
                    //fire.transform.localPosition = Vector3.zero;
                    //fire.transform.localRotation = Quaternion.identity;
                }
            }
        }
    }
}