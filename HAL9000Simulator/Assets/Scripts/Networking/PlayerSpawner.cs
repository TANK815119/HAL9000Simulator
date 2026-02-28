using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

//manages how players will be spawned
//currently spawns player at position of PlayerSpawner GameObejct

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private float range = 1f;
    [SerializeField] private Vector3 globalSpawnPosition = Vector3.zero;

    private void Awake()
    {
        if (NetworkManager.Singleton != null)
        {
            Debug.Log("Registering connection approval callback.");
            NetworkManager.Singleton.ConnectionApprovalCallback += CustomConnectionApprovalCallback;
        }
        else
        {
            Debug.LogError("NetworkManager is not set.");
        }

        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnLoadComplete;
        SceneManager.sceneLoaded += OnSceneLoaded; // Register to scene loaded event
    }

    public void OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        //TeleportPlayersServerRPC(globalSpawnPosition);
        Debug.Log("attempting teleport");
        NetworkObject[] players = FindObjectsOfType<NetworkObject>();
        foreach (NetworkObject player in players)
        {
            if (player.IsPlayerObject)
            {
                player.transform.position = globalSpawnPosition;
                for(int i = 0; i < player.transform.childCount; i++)
                {
                    player.transform.GetChild(i).localPosition = Vector3.zero;
                }
            }
        }
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //// Spawn players when scene is loaded
        //if (IsServer)
        //{
        //    Debug.Log("scene load found");
        //    SpawnPlayers();
        //}
        //else
        //{
        //    Debug.Log("No server found");
        //}
    }

    [ServerRpc]
    private void TeleportPlayersServerRPC(Vector3 position, ServerRpcParams rpcParams = default) //use on scene transition
    {
        Debug.Log("attempting teleport");
        NetworkObject[] players = FindObjectsOfType<NetworkObject>();
        foreach (NetworkObject player in players)
        {
            if (player.IsPlayerObject)
            {
                player.transform.position = position;
            }
        }
    }

    void CustomConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)    //mustnt be private
    {
        Debug.Log("connection approval called");
        response.CreatePlayerObject = true;
        response.Position = GetPlayerSpawnPosition();
        response.Rotation = Quaternion.identity;
        response.Approved = true;
    }

    public Vector3 GetPlayerSpawnPosition()
    {
        Vector3 position = globalSpawnPosition + new Vector3(Random.Range(-range, range), 0f, Random.Range(-range, range));
        Debug.Log(position);
        return position;
    }

    private void SpawnPlayers()
    {
        for(int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
        {
            SpawnPlayer(NetworkManager.Singleton.ConnectedClientsList[i].ClientId);
        }
    }

    private void SpawnPlayer(ulong clientId)
    {
        Debug.Log("attempt spawn players");
        Vector3 spawnPosition = GetPlayerSpawnPosition();
        GameObject playerInstance = Instantiate(player, spawnPosition, Quaternion.identity);
        playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    }

    public override void OnDestroy()
    {
        

        if (NetworkManager.Singleton != null)
        {
            Debug.Log("Unregistering connection approval callback.");
            NetworkManager.Singleton.ConnectionApprovalCallback -= CustomConnectionApprovalCallback;
        }

        NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnLoadComplete;
        SceneManager.sceneLoaded -= OnSceneLoaded; // Unregister scene loaded event
    }
}
