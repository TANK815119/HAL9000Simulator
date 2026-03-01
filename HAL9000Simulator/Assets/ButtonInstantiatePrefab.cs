using System.Collections;
using System.Collections.Generic;
using Rekabsen;
using UnityEngine;

public class ButtonInstantiatePrefab : MonoBehaviour, ButtonActionInterface
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject HAL90001;
    [SerializeField] private GameObject HAL90002;

    public void Play()
    {
        Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        Destroy(HAL90001);
        Destroy(HAL90002);
        Destroy(this.gameObject);
    }
}
