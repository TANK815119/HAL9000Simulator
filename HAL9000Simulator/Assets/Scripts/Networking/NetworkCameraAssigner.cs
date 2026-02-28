using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Rendering.Universal;

// this script simply destroys the camera and audiolistener if the player isnt the main player of the client
// which should make sure the only view is from the player's body

public class NetworkCameraAssigner : MonoBehaviour
{
    [SerializeField] private NetworkObject networkObject;

    // Start is called before the first frame update
    void Start()
    {
        Camera playerCamera = GetComponent<Camera>();
        UniversalAdditionalCameraData cameraData = playerCamera.GetUniversalAdditionalCameraData();
        AudioListener playerListner = GetComponent<AudioListener>();
        AudioLowPassFilter playerFilter = GetComponent<AudioLowPassFilter>();

        //assign the recently-spawned player NetworkObject to the most recent playerID
        if (NetworkManager.Singleton.IsServer)
        {
            networkObject.ChangeOwnership(NetworkManager.Singleton.ConnectedClientsIds[NetworkManager.Singleton.ConnectedClientsIds.Count - 1]);
        }

        Debug.Log("Owner Client ID: " + networkObject.OwnerClientId);


        if (!networkObject.OwnerClientId.Equals(NetworkManager.Singleton.LocalClientId))
        {
            this.transform.parent.tag = "Body"; //for fog
            Destroy(cameraData);
            Destroy(playerCamera);
            Destroy(playerFilter);
            Destroy(playerListner);
        }

        Debug.Log("Owner: " + networkObject.OwnerClientId + "Local Client: " + NetworkManager.Singleton.LocalClientId);
    }

    void Update()
    {
        //Debug.Log("Owner: " + networkObject.OwnerClientId + "Local Client: " + NetworkManager.Singleton.LocalClientId);
    }
}

//for(int i = 0; i < NetworkManager.Singleton.ConnectedClientsIds.Count; i++)
//{
//    Debug.Log("Client ID: " + NetworkManager.Singleton.ConnectedClientsIds[i] + " Index " + i);
//}