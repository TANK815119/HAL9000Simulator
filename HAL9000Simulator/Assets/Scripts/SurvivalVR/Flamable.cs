using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Rekabsen
{
    [RequireComponent(typeof(Collider))] //so that it may be detected by any Triboluminescent objects
    public class Flamable : NetworkBehaviour
    {
        [SerializeField] private GameObject flame;
        [SerializeField] private List<Transform> burnPoints;
        [SerializeField] private AudioSource fireCrackle;
        [SerializeField] private AudioClip fireLite;
        [SerializeField] private float burnDuration = 8f * 60f; //burn duration
        private List<Transform> flames;
        private Buoyancy buoyancy;

        private bool burning = false;
        private NetworkVariable<bool> lit = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<bool> spent = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private List<PlayerStatsNetwork> burntPlayerStats = new List<PlayerStatsNetwork>();

        // Start is called before the first frame update
        void Start()
        {
            flames = new List<Transform>();

            buoyancy = GetComponent<Buoyancy>();

            //check the object has a collider
            GetComponent<Collider>();
        }

        // Update is called once per frame
        void Update()
        {
            if (lit.Value && !burning)
            {
                Combust();
            }

            if (burning)
            {
                //"parent" burning stuff
                for (int i = 0; i < burnPoints.Count; i++)
                {
                    flames[i].position = burnPoints[i].position;

                    //always point up
                    flames[i].rotation = Quaternion.identity;
                }
            }

            //essentially run down burn timer
            if (lit.Value && IsOwner)
            {
                if (burnDuration > 0f)
                {
                    burnDuration -= Time.deltaTime;
                }
                else
                {
                    spent.Value = true;
                }
            }

            //destroy the object if run out of burn timer
            if (spent.Value && IsHost)
            {
                if (this.TryGetComponent(out NetworkObject netObject))
                {
                    netObject.Despawn(true);
                    Destroy(this);
                }
            }

            //check if flame has been doused
            if (buoyancy.IsSubmerged() && burning && IsOwner)
            {
                DouseServerRpc(); //dous on all clients
            }
        }

        [ServerRpc]
        private void DouseServerRpc(ServerRpcParams rpcParams = default)
        {
            DouseClientRpc();
        }

        [ClientRpc]
        private void DouseClientRpc(ClientRpcParams rpcParams = default)
        {
            Douse();
        }

        private void Douse() //gets rid of flames and the such
        {
            //destroy the flames
            while(flames.Count > 0)
            {
                Destroy(flames[0].gameObject);
                flames.Remove(flames[0]);
            }

            //stop players from burning
            StopBurningPlayers();

            //stop fire crackle audio
            fireCrackle.Stop();

            //reset bools
            lit.Value = false;
            burning = false;
        }

        private void Combust()
        {
            AudioSource.PlayClipAtPoint(fireLite, transform.position, 1f);
            fireCrackle.Play();
            for (int i = 0; i < burnPoints.Count; i++)
            {
                GameObject fire = Instantiate(flame);
                fire.transform.parent = this.transform;
                flames.Add(fire.transform);
                burning = true;
            }
        }

        public void Light()
        {
            lit.Value = true;
        }

        public bool IsBurning()
        {
            return burning;
        }

        private void OnCollisionEnter(Collision collision)
        {
            //light other objects
            Flamable flamable = collision.gameObject.GetComponent<Flamable>();
            if (flamable != null && !flamable.IsBurning() && burning)
            {
                flamable.Light();
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.gameObject.TryGetComponent(out Mouth mouth))
            {
                mouth.GetStats().NearFire = true;
            }
        }
        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.TryGetComponent(out Mouth mouth))
            {
                mouth.GetStats().NearFire = false;
            }
        }


        private void OnCollisionStay(Collision collision)
        {
            //burn player
            if(collision.gameObject.tag.Equals("Body") && burning)
            {
                PlayerStatsNetwork playerStats = SearchForStats(collision.gameObject.transform);
                playerStats.Burning = true;

                //add to list of burnt playerStats
                if(!burntPlayerStats.Contains(playerStats))
                {
                    burntPlayerStats.Add(playerStats);
                }
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            //un-burn player
            if (collision.gameObject.tag.Equals("Body") && burning)
            {
                PlayerStatsNetwork playerStats = SearchForStats(collision.gameObject.transform);
                playerStats.Burning = false;

                //remove from list of burnt playerStats
                if (!burntPlayerStats.Contains(playerStats))
                {
                    burntPlayerStats.Remove(playerStats);
                }
            }
        }

        private PlayerStatsNetwork SearchForStats(Transform bodyPart) //manually aquires player stats through address, essentially -- must be changed if stats architecture/names changed!
        {
            Transform root = bodyPart.transform.root;

            ///search for StatTracker object
            Transform statTracker = null;
            for(int i = 0; i < root.childCount; i++)
            {
                if(root.GetChild(i).name.Equals("SurvivalComponents"))
                {
                    Transform survivalComponents = root.GetChild(i);
                    for (int j = 0; j < survivalComponents.childCount; j++)
                    {
                        if (survivalComponents.GetChild(j).name.Equals("StatTracker"))
                        {
                            statTracker = survivalComponents.GetChild(j);
                            break;
                        }
                    }
                }
            }

            if(statTracker != null && statTracker.gameObject.TryGetComponent(out PlayerStatsNetwork stats))
            {
                return stats;
            }
            else
            {
                Debug.LogError("PlayerStats could not be found");
                return null;
            }
        }

        private void StopBurningPlayers()
        {
            for (int i = 0; i < burntPlayerStats.Count; i++)
            {
                burntPlayerStats[i].Burning = false;
            }
        }

        public override void OnDestroy()
        {
            StopBurningPlayers();
        }
    }
}