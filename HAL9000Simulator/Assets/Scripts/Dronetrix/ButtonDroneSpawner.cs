using System.Collections;
using System.Collections.Generic;
using Rekabsen;
using UnityEngine;

namespace Rekabsen
{
    public class ButtonDroneSpawner : MonoBehaviour, ButtonActionInterface
    {
        [SerializeField] private GameObject dronePrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float spawnRange = 2.5f;
        [SerializeField] private Transform poi;

        public void Play()
        {
            Debug.Log("ButtonDroneSpawner: Play button pressed, spawning drone.");
            Vector3 randomOffset = new Vector3(
                Random.Range(-spawnRange, spawnRange),
                Random.Range(-spawnRange, spawnRange),
                Random.Range(-spawnRange, spawnRange)
            );
            GameObject drone = Instantiate(dronePrefab, spawnPoint.position + randomOffset, spawnPoint.rotation);
            
            if (drone.TryGetComponent<VoxelAvoidance>(out var avoidance))
            {
                avoidance.SetPOI(poi);
            }
            else
            {
                Debug.LogWarning("ButtonDroneSpawner: Spawned drone does not have a DroneController component.");
            }
        }
    }
}
