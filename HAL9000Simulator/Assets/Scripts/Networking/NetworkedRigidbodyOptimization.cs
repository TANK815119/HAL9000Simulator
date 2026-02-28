using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;

public class NetworkedRigidbodyOptimization : NetworkBehaviour
{
    private Rigidbody rb;
    private ClientNetworkTransform netTransform;
    private bool isSyncing = true;
    private float stationaryThreshold = 0.1f; // Velocity threshold to consider the object stationary
    private float stationaryTime = 2.0f; // Time threshold to disable network sync
    private float stationaryTimer = 0.0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        netTransform = GetComponent<ClientNetworkTransform>();
    }

    void Update()
    {
        if (IsOwner)
        {
            if (rb.velocity.magnitude < stationaryThreshold)
            {
                stationaryTimer += Time.deltaTime;
                if (stationaryTimer >= stationaryTime && isSyncing)
                {
                    DisableNetworkSync();
                }
            }
            else
            {
                stationaryTimer = 0.0f;
                if (!isSyncing)
                {
                    EnableNetworkSync();
                }
            }
        }
    }

    void DisableNetworkSync()
    {
        isSyncing = false;

        // Disable network transform updates
        netTransform.SyncPositionX = false;
        netTransform.SyncPositionY = false;
        netTransform.SyncPositionZ = false;
        netTransform.SyncRotAngleX = false;
        netTransform.SyncRotAngleY = false;
        netTransform.SyncRotAngleZ = false;
    }

    void EnableNetworkSync()
    {
        isSyncing = true;

        // Enable network transform updates
        netTransform.SyncPositionX = true;
        netTransform.SyncPositionY = true;
        netTransform.SyncPositionZ = true;
        netTransform.SyncRotAngleX = true;
        netTransform.SyncRotAngleY = true;
        netTransform.SyncRotAngleZ = true;
    }
}