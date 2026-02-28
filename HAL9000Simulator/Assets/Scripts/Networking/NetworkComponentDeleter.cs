using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Rendering.Universal;

public class NetworkComponentDeleter : MonoBehaviour
{
    [SerializeField] private NetworkObject networkObject;
    [SerializeField] private Component component;

    // Start is called before the first frame update
    void Start()
    {
        if (!networkObject.OwnerClientId.Equals(NetworkManager.Singleton.LocalClientId))
        {
            Destroy(component);
        }
    }
}
