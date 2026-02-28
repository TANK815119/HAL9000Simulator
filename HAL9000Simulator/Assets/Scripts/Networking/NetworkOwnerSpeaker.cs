using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkOwnerSpeaker : MonoBehaviour
{
    private NetworkObject networkObject;
    // Update is called once per frame
    private void Start()
    {
        networkObject = GetComponent<NetworkObject>();
    }
    void Update()
    {
        Debug.Log(gameObject.name + " is owned by client " + networkObject.OwnerClientId);
    }
}
