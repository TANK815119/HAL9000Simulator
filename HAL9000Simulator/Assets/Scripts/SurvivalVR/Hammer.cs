using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Netcode;

namespace Rekabsen
{
    //joints two objects if both hit by hammer
    //creates a fixed joint
    //requires a trigger collider
    public class Hammer : NetworkBehaviour
    {
        [SerializeField] float minForce = 5f;
        [SerializeField] AudioClip suctionClip;
        [SerializeField] AudioClip hitClip;
        private Rigidbody rb;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (IsOwner)
            {
                AttemptListenerAdd(collision);
                AttemptFusionRegister(collision);
            }
        }

        private void AttemptListenerAdd(Collision collision)
        {
            if (collision.rigidbody != null && !IsExcludedTags(collision.gameObject.tag))
            {
                //attach an listening compoonent to the object
                if (!collision.gameObject.TryGetComponent(out HammerListener listnerLess)) //check the object doesnt have the component
                {
                    HammerListener listener = collision.gameObject.AddComponent<HammerListener>();
                    listener.SetHammerBody(rb);
                }
            }
        }


        private void AttemptFusionRegister(Collision collision)
        {
            if (IsOwner && collision.rigidbody != null && !IsExcludedTags(collision.gameObject.tag) && collision.relativeVelocity.magnitude > minForce && collision.gameObject.TryGetComponent(out HammerListener listener))
            {
                //audio for a speedy strike
                AudioSource.PlayClipAtPoint(hitClip, this.transform.position, 0.175f); //hard coded volume

                //attempt to fuse the object touching the listner
                if (listener.TouchingBodies.Count > 0)
                {
                    ulong mainID = 0;
                    List<ulong> touchingIDs = new List<ulong>();

                    //synchronize the main object ID
                    if (collision.rigidbody.gameObject.TryGetComponent(out NetworkObject mainNet)) { mainID = mainNet.NetworkObjectId; }

                    //synchronize the list of touchingbodies
                    for (int i = 0; i < listener.TouchingBodies.Count; i++)
                    {
                        if (listener.TouchingBodies[i].gameObject.TryGetComponent(out NetworkObject touchingNet)) { touchingIDs.Add(touchingNet.NetworkObjectId); }
                    }

                    listener.TouchingBodies.Clear();

                    //attempt fusion
                    FuseBodiesServerRPC(mainID, touchingIDs.ToArray());
                }
            }
        }

        [ServerRpc] //first server
        private void FuseBodiesServerRPC(ulong mainID, ulong[] touchingIDs, ServerRpcParams rpcParams = default)
        {
            FuseBodiesClientRPC(mainID, touchingIDs);
        }

        [ClientRpc] //then clients
        private void FuseBodiesClientRPC(ulong mainID, ulong[] touchingIDs, ClientRpcParams rpcParams = default)
        {
            FuseBodiesGatherRigidbodies(mainID, touchingIDs);
        }

        private void FuseBodiesGatherRigidbodies(ulong mainID, ulong[] touchingIDs)
        {
            //read mainID and touchingIDs
            Rigidbody mainBody = null;
            List<Rigidbody> touchingBodies = new List<Rigidbody>();

            Debug.Log("mainID " + mainID);
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(mainID, out NetworkObject mainNet) && mainNet.TryGetComponent(out Rigidbody mainNetBody))
            {
                mainBody = mainNetBody;
                Debug.Log("mainID object " + mainBody.name);
            }

            for (int i = 0; i < touchingIDs.Length; i++)
            {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(touchingIDs[i], out NetworkObject touchingNet) && touchingNet.TryGetComponent(out Rigidbody touchingNetBody))
                {
                    touchingBodies.Add(touchingNetBody);
                }
            }

            FuseBodies(mainBody, touchingBodies);
        }

        private void FuseBodies(Rigidbody mainBody,  List<Rigidbody> touchingBodies)
        {
            Debug.Log("fuse bodies calleed");

            if(mainBody == null)
            {
                Debug.Log("Main body is null");
            }

            if (touchingBodies.Count == 0)
            {
                Debug.Log("touchingBodies is empty");
            }

            if (mainBody == null) return;

            Debug.Log("mainBoy nullcheck passed");

            //make a bunch of fixed joints connecting to the mainBody
            //the fixedJoint should be on the mainBody
            for (int i = 0; i < touchingBodies.Count; i++)
            {
                Debug.Log("for loop entered");

                if (!IsExcludedTags(touchingBodies[i].gameObject.tag))
                {
                    Debug.Log("added fixed joint");
                    FixedJoint joint = mainBody.gameObject.AddComponent<FixedJoint>();
                    joint.connectedBody = touchingBodies[i];

                    //audio
                    AudioSource.PlayClipAtPoint(suctionClip, touchingBodies[i].position, 0.5f);
                }
            }
        }

        private bool IsExcludedTags(string tag)
        {
            return tag.Equals("Body") || tag.Equals("PlayerCamera");
        }

        private void PrintList(List<Rigidbody> list)
        {
            for(int i = 0; i < list.Count; i++)
            {
                Debug.Log(list[i].gameObject.name);
            }
        }
    }
}