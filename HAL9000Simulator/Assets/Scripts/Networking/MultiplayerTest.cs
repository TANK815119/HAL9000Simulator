using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class MultiplayerTest : MonoBehaviour
{
    private bool isHost = true;
    private bool isClient = false;

    // Start is called before the first frame update
    void Awake()
    {
        if (isHost)
        {
            StartPlayerServer();
        }
        if(isClient)
        {
            StartPlayerClient();
        }
    }

    public void StartPlayerServer()
    {
        NetworkManager.Singleton.StartServer();
        isHost = true;
    }

    public void StartPlayerClient()
    {
        NetworkManager.Singleton.Shutdown();
        NetworkManager.Singleton.StartClient();
        isClient = true;
    }
}
