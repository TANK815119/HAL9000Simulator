using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Rekabsen
{
    //an item that breaks into two
    //and has health
    //is an object with the triboluminesence variable hits it, it losses health
    public class SplitItem : NetworkBehaviour
    {
        [SerializeField] private float minForce = 10f;
        [SerializeField] private NetworkVariable<float> health = new NetworkVariable<float>(50f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        [SerializeField] private GameObject emptyParent; //empty parent with children at desired positions
        [SerializeField] private List<GameObject> spawnObjects; //spawned in empty children of parent
        [SerializeField] private AudioClip damage;

        [ServerRpc]
        private void AttemptSplitServerRPC(ServerRpcParams prcParams = default)
        {
            AttemptSplitClientRPC();
        }

        [ClientRpc]
        private void AttemptSplitClientRPC(ClientRpcParams rpcParams = default)
        {
            AttemptSplit();
        }

        private void AttemptSplit()
        {
            //spawn the empty parent containing position and rotation of spawns in children
            GameObject parent = Instantiate(emptyParent, transform.position, transform.rotation);

            if(IsServer)
            {
                if(spawnObjects.Count == 1) //single object mode
                {
                    for (int i = 0; i < parent.transform.childCount; i++)
                    {
                        //instantiate real child
                        GameObject child = Instantiate(spawnObjects[0], parent.transform.GetChild(i).position, parent.transform.GetChild(i).rotation);

                        //spawn
                        if (child.TryGetComponent(out NetworkObject netObj))
                        {
                            netObj.Spawn();
                        }
                    }
                }
                else //multi-object mode
                {
                    for (int i = 0; i < parent.transform.childCount; i++)
                    {
                        //instantiate real child
                        GameObject child = Instantiate(spawnObjects[i], parent.transform.GetChild(i).position, parent.transform.GetChild(i).rotation);

                        //spawn
                        if (child.TryGetComponent(out NetworkObject netObj))
                        {
                            netObj.Spawn();
                        }
                    }
                }

                //delete the mother
                if (this.TryGetComponent(out NetworkObject motherNetObject))
                {
                    motherNetObject.Despawn(true);
                }
            }

            Destroy(parent); //clean up the emptyParent
            //Destroy(this);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (IsOwner && collision.relativeVelocity.magnitude > minForce)//check the collsion is sufficently high velocity
            {
                if (collision.gameObject.TryGetComponent(out Triboluminescence tribo))//check for triboluminescence
                {
                    health.Value -= collision.relativeVelocity.magnitude;

                    AudioSource.PlayClipAtPoint(damage, this.transform.position, 0.175f); //hard coded volume

                    if (health.Value < 0f)
                    {
                        AttemptSplitServerRPC();
                    }
                }
            }
        }
    }
}