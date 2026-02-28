using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

//changes the skybox every 12 minutes
//and dims the sun
namespace Rekabsen
{
    public class DayNightCycle : NetworkBehaviour
    {
        [SerializeField] private List<MeshRenderer> holoWalls;
        [SerializeField] private Material dayMaterial;
        [SerializeField] private Material nightMaterial;

        [SerializeField] private Light sun;

        private NetworkVariable<bool> daytime = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private bool localDaytime = true;

        [SerializeField] private float dayLength = 60f * 8f; //12 minutes
        [SerializeField] private float nightLength = 60f * 6f; //12 minutes
        private float timeCounter;

        [SerializeField] AudioSource switchSound;
        [SerializeField] private Component buttonLightSwitch;
        private ButtonActionInterface lightSwitch;

        // Start is called before the first frame update
        void Start()
        {
            timeCounter = dayLength;
            lightSwitch = (ButtonActionInterface)buttonLightSwitch;
        }

        // Update is called once per frame
        void Update()
        {
            //keep track of day or night on network
            if(IsServer)
            {
                timeCounter -= Time.deltaTime;

                if (timeCounter <= 0f)
                {
                    daytime.Value = !daytime.Value;
                }
            }

            //update clients based on server daytime.Value
            if(daytime.Value != localDaytime)
            {
                if(daytime.Value)
                {
                    MakeDayTime();
                }
                else
                {
                    MakeNightTime();
                }

                //updated local read state
                localDaytime = daytime.Value;

                //swap lighting
                lightSwitch.Play();

                //audio
                switchSound.Play();
            }
        }

        private void MakeDayTime()
        {
            for(int i = 0; i < holoWalls.Count; i++)
            {
                holoWalls[i].material = dayMaterial;
            }

            sun.intensity = 1f;
            SetAllPlayersDay(true);

            timeCounter = dayLength;
        }

        private void MakeNightTime()
        {
            for (int i = 0; i < holoWalls.Count; i++)
            {
                holoWalls[i].material = nightMaterial;
            }

            sun.intensity = 0.05f;
            SetAllPlayersDay(false);

            timeCounter = nightLength;
        }

        private void SetAllPlayersDay(bool day)
        {
            PlayerStatsNetwork[] playerStats = Object.FindObjectsByType<PlayerStatsNetwork>(FindObjectsSortMode.None);
            for(int i = 0; i < playerStats.Length; i++)
            {
                playerStats[i].IsDay = day;
            }
        }
    }
}