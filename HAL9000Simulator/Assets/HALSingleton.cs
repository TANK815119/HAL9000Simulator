using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HALSingleton : MonoBehaviour
{
    public static HALSingleton Instance { get; private set; }

    [Header("Light GameObjects")]
    [SerializeField] private GameObject light1;
    [SerializeField] private GameObject light2;
    [SerializeField] private GameObject light3;
    [SerializeField] private GameObject light4;

    [Header("Prefabs")]
    [SerializeField] private GameObject lightOffPrefab;
    [SerializeField] private GameObject lightOnPrefab;

    [Header("Doors Stuff")]
    [SerializeField] private Door leftDoor;
    [SerializeField] private Door rightDoor;
    [SerializeField] private AudioSource doorOpenSound;

    [Header("Light States")]
    public bool light1Active = false;
    public bool light2Active = false;
    public bool light3Active = false;
    public bool light4Active = false;

    [Header("HAL Perception")]
    [SerializeField] GameObject HALLens;
    [SerializeField] GameObject sprayedHALLens;

    private bool halPerceptive = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start() { }
    void Update() { }

    public void ActivateLight(int n)
    {
        //if (n != 4 && halPerceptive)
        //{
        //    Debug.Log("HAL is currently disabling lights. Cannot activate light " + n);
        //    return;
        //}

        switch (n)
        {
            case 1:
                light1Active = !light1Active;
                SwapLight(ref light1, light1Active);
                break;
            case 2:
                light2Active = !light2Active;
                SwapLight(ref light2, light2Active);
                break;
            case 3:
                light3Active = !light3Active;
                SwapLight(ref light3, light3Active);
                break;
            case 4:
                light4Active = !light4Active;
                SwapLight(ref light4, light4Active);
                halPerceptive = false; // Once light 4 is toggled, HAL enables further toggling of lights 1-3
                break;
            default:
                Debug.LogWarning($"ActivateLight: No light found for index {n}");
                break;
        }

        if (light1Active && light2Active && light3Active && light4Active)
        {
            Debug.Log("All lights are active! Puzzle solved.");
            leftDoor.Open();
            rightDoor.Open();
            doorOpenSound.Play();
        }
    }

    private void SwapLight(ref GameObject lightObj, bool isActive)
    {
        if (lightObj == null)
        {
            Debug.LogWarning("SwapLight: Light reference is null.");
            return;
        }

        GameObject prefabToSpawn = isActive ? lightOnPrefab : lightOffPrefab;

        Vector3 position = lightObj.transform.position;
        Quaternion rotation = lightObj.transform.rotation;
        Transform parent = lightObj.transform.parent;

        Destroy(lightObj);
        lightObj = Instantiate(prefabToSpawn, position, rotation, parent);
    }

    public void SprayHALLens()
    {
        Vector3 position = HALLens.transform.position;
        Quaternion rotation = HALLens.transform.rotation;
        Transform parent = HALLens.transform.parent;

        Destroy(HALLens);
        HALLens = Instantiate(sprayedHALLens, position, rotation, parent);
    }
}