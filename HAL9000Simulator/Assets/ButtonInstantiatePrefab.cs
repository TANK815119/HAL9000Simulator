using System.Collections;
using System.Collections.Generic;
using Rekabsen;
using UnityEngine;

public class ButtonInstantiatePrefab : MonoBehaviour, ButtonActionInterface
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private Transform spawnPoint;

    public void Play()
    {
        Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
    }
}
