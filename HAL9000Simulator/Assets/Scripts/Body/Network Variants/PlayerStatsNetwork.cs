using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Rekabsen
{
    public class PlayerStatsNetwork : NetworkBehaviour
    {
        [SerializeField] private NetworkVariable<float> health = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        [SerializeField] private NetworkVariable<float> hunger = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        [SerializeField] private NetworkVariable<float> temp = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);


        [SerializeField] private DisplayStatsOnTexture statDisplay;

        private float maxHealth;
        private float maxHunger;
        private float maxTemp;
        private bool dead = false;
        public bool Burning { get; set; }
        public bool NearFire { get; set; }
        public bool IsDay { get; set; }

        //health stats
        [SerializeField] private float hurtPerSecond = 100f / (6f * 60f); //lose all health every 6 minutes
        [SerializeField] private float hungerPerSecond = 100f / (12f * 60f); //lose all hunger every 12 minutes
        [SerializeField] private float tempPerSecond = 100f / (3f * 60f); //lose all temp every 3 minutes

        [SerializeField] private AudioSource hungerGrowl;
        [SerializeField] private AudioSource hurtPain;
        [SerializeField] private AudioSource teethChatter;
        [SerializeField] private List<ConfigurableJoint> playerJoints;
        [SerializeField] private RoomspaceLocomotion roomLoco;
        [SerializeField] private ControllerLocomotion controlLoco;
        [SerializeField] private Buoyancy noseProxy;
        private Buoyancy[] buoyants;

        // Start is called before the first frame update
        void Start()
        {
            maxHealth = health.Value;
            maxHunger = hunger.Value;
            maxTemp = temp.Value;
            Burning = false;
            NearFire = false;
            IsDay = true;

            buoyants = GatherBuoyants(playerJoints);
        }

        // Update is called once per frame
        void Update()
        {
            if(IsOwner)
            {
                bool starving = HungerUpdate();
                bool cold = TempUpdate();
                HealthUpdate(starving, cold);
            }
            
            CheckDisplay();
        }

        private bool HungerUpdate()
        {
            bool starving = false;

            if (hunger.Value > 0f)
            {
                hunger.Value -= hungerPerSecond * Time.deltaTime;
                starving = false;
            }
            else
            {
                starving = true;
            }

            return starving;
        }

        private bool TempUpdate()
        {
            //go through all the conditions for temperature loss
            bool lostTemp = false;
            if(temp.Value > -1f)
            {
                if (noseProxy.IsSubmerged()) //if under water
                {
                    temp.Value -= tempPerSecond * Time.deltaTime * 2f;
                    lostTemp = true;
                }
                if (IsTouchingWater(buoyants)) //if touching water
                {
                    temp.Value -= tempPerSecond * Time.deltaTime * 8f;
                    lostTemp = true;
                }
                if (!IsDay && !NearFire) // if night and not near fire
                {
                    temp.Value -= tempPerSecond * Time.deltaTime * 2f;
                    lostTemp = true;
                }
            }
            
            if (!lostTemp && temp.Value < 100f) //regenerate
            {
                temp.Value += tempPerSecond * 8f * Time.deltaTime;
            }

            if (temp.Value > 0f)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private void HealthUpdate(bool starving, bool cold)
        {
            if (health.Value > 0f)
            {
                float totalHurt = 0f;
                if (starving)
                {
                    totalHurt += hurtPerSecond * Time.deltaTime;
                }
                if (cold)
                {
                    totalHurt += hurtPerSecond * 8f * Time.deltaTime;
                }
                if(Burning)
                {
                    totalHurt += hurtPerSecond * 8f * Time.deltaTime;
                }

                health.Value -= totalHurt;

                if (dead)
                {
                    Revive();
                }
            }
            else if (!dead)
            {
                Die();
            }
        }

        private void Die()
        {
            //make player go completely limp and disable y-axis spring(important for leg joint)
            for (int i = 0; i < playerJoints.Count; i++)
            {
                JointDrive rotation = playerJoints[i].slerpDrive;
                rotation.positionSpring = rotation.positionSpring / 100000f;
                playerJoints[i].slerpDrive = rotation;

                JointDrive positional = playerJoints[i].yDrive;
                positional.positionSpring = positional.positionSpring / 40f;
                playerJoints[i].yDrive = positional;
            }

            //disable locomotion
            roomLoco.enabled = false;
            controlLoco.enabled = false;

            dead = true;
        }

        private void Revive()
        {
            //make player go completely STRONG and disable y-axis spring(important for leg joint)
            for (int i = 0; i < playerJoints.Count; i++)
            {
                JointDrive rotation = playerJoints[i].slerpDrive;
                rotation.positionSpring = rotation.positionSpring * 100000f;
                playerJoints[i].slerpDrive = rotation;

                JointDrive positional = playerJoints[i].yDrive;
                positional.positionSpring = positional.positionSpring * 40;
                playerJoints[i].yDrive = positional;
            }

            //disable locomotion
            roomLoco.enabled = true;
            controlLoco.enabled = true;

            dead = false;
        }

        private int lastHealthSegments = 0;
        private int lastHungerSegments = 0;
        private int lastTempSegments = 0;
        private void CheckDisplay() //uodate display when a segment is lost
        {
            //calculate segments
            int healthSegments = (int)(health.Value / (maxHealth / 8f));
            int hungerSegments = (int)(hunger.Value / (maxHunger / 8f));
            int tempSegments = (int)(temp.Value / (maxTemp / 8f));

            //check for change
            bool segmentChange = false;
            if (healthSegments != lastHealthSegments) 
            { 
                segmentChange = true;
                if (healthSegments < 7f)
                {
                    hurtPain.Play();
                }
            }
            if (hungerSegments != lastHungerSegments) 
            { 
                segmentChange = true; 
                if(hungerSegments < 7f)
                {
                    hungerGrowl.Play();
                }
            }
            if (tempSegments != lastTempSegments) 
            { 
                segmentChange = true;
                if(tempSegments < 7f)
                {
                    teethChatter.Play();
                }
            }

            //update stats if changed
            if (segmentChange)
            {
                statDisplay.UpdateStatsSegments(healthSegments, hungerSegments, tempSegments);
                statDisplay.UpdateStatshDisplay();
            }

            //update last values
            lastHealthSegments = healthSegments;
            lastHungerSegments = hungerSegments;
            lastTempSegments = tempSegments;
        }

        public void Feed(float noursishment)
        {
            if (hunger.Value + noursishment <= maxHunger)
            {
                hunger.Value += noursishment;
            }
            else //extra goes to health
            {
                float healthIncrease = hunger.Value + noursishment - 100f;
                hunger.Value = 100f; //top off

                //icrease health
                if (health.Value + healthIncrease <= maxHealth)
                {
                    health.Value += healthIncrease;
                }
                else
                {
                    health.Value = 100f;
                }
            }
        }

        private Buoyancy[] GatherBuoyants(List<ConfigurableJoint> joints) //essentially converts joints list to Bouyancy array
        {
            List<Buoyancy> buoyantList = new List<Buoyancy>();
            for(int i = 0; i < joints.Count; i++)
            {
                if(joints[i].TryGetComponent(out Buoyancy bouyancy))
                {
                    buoyantList.Add(bouyancy);
                }
            }

            return buoyantList.ToArray();
        }

        private bool IsTouchingWater(Buoyancy[] buoyantList) //checks if any bouyant components are touching water
        {
            for(int i = 0; i < buoyantList.Length; i++)
            {
                if(buoyantList[i].IsSubmerged())
                {
                    return true;
                }
            }

            return false;
        }
    }
}