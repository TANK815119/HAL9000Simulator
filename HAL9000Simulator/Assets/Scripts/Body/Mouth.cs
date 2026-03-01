using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Rekabsen
{
    public class Mouth : NetworkBehaviour
    {
        [SerializeField] private PlayerStatsNetwork stats; 
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private float eatTime = 3f;
        private List<Edible> inMouth;
        private float eaten = 0f; //seconds eaten

        private void Start()
        {
            inMouth = new List<Edible>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out Edible edible))
            {
                inMouth.Add(edible);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out Edible edible) && inMouth.Contains(edible))
            {
                inMouth.Remove(edible);
            }
        }

        private void Update()
        {
            //try to eat the first object in the list
            if(inMouth.Count > 0 && IsOwner)
            {
                //increment eating
                eaten += Time.deltaTime;

                //sound
                if(!audioSource.isPlaying)
                {
                    audioSource.Play();
                }

                //check if eaten
                if(eaten >= eatTime && IsOwner && NetworkManager.Singleton.LocalClientId == OwnerClientId)
                {
                    eaten = 0f;
                    
                    Debug.Log("eaten >= eatTime reached");
                    Debug.Log(inMouth.Count);

                    if (!inMouth[0].Equals(null) && inMouth[0].TryGetComponent(out NetworkObject edibleNet))
                    {
                        Debug.Log("edibleNet reached");
                        AttemptConsumptionServerRpc(edibleNet.NetworkObjectId);
                    }
                    else
                    {
                        inMouth.Clear();
                    }
                }
            }
            else
            {
                eaten = 0f;

                //sound
                if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
            }
        }

        [ServerRpc]
        private void AttemptConsumptionServerRpc(ulong edibleID, ServerRpcParams rcpParams = default)
        {
            AttemptConsumptionClientRpc(edibleID);
        }

        [ClientRpc]
        private void AttemptConsumptionClientRpc(ulong edibleID, ClientRpcParams rcpParams = default)
        {
            AttemptConsumptionGatherEdible(edibleID);
        }

        private void AttemptConsumptionGatherEdible(ulong edibleID)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(edibleID, out NetworkObject ediblNet) && ediblNet.TryGetComponent(out Edible edible))
            {
                AttemptConsumption(edible);
            }
        }

        private void AttemptConsumption(Edible edible)
        {
            Debug.Log("AttemptConsumption reached");

            //increase player hunger stat
            if (IsOwner)
            {
                stats.Feed(edible.Nourishment);
            }

            if(IsServer)
            {
                //instantiate leftowvers
                GameObject leftovers = Instantiate(edible.Leftovers, transform.position, transform.rotation);

                //spawn leftovers
                if (leftovers.TryGetComponent(out NetworkObject netLeftover))
                {
                    netLeftover.Spawn();
                }

                //despawn the first object
                if (edible.gameObject.TryGetComponent(out NetworkObject netEdible))
                {
                    netEdible.Despawn(true);
                }
            }

            Debug.Log("reached end");
            inMouth.Clear();
        }

        public PlayerStatsNetwork GetStats()
        {
            return stats;
        }
    }
}