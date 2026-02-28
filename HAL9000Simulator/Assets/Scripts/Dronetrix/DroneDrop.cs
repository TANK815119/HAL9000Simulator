using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Pool;

public class DroneDrop : MonoBehaviour
{
    [SerializeField] private DroneCarrierMovement droneCarrierMovement;
    [SerializeField] private GameObject drone;
    [SerializeField] private Transform droneSpawns; //the locations of the children of this object will be used to determine drone spawns
    //[SerializeField] private VoxelAvoidance voxelAvoidance;
    [SerializeField] private Transform poi;
    private DroneInfo[] droneInfoArr;
    private Transform end;
    private float speed;
    private float timer = 0f;
    private int droneNumber;
    private float dropInterval = 0f;
    private int numDropped = 0;

    // Start is called before the first frame update
    void Start()
    {
        droneNumber = droneSpawns.childCount;
        end = droneCarrierMovement.GetEnd();
        speed = droneCarrierMovement.GetSpeed();
        //calculate the drop interval
        dropInterval = CalculateDropInterval();

        //generate the drones along the wing of the drone
        droneInfoArr = GenerateHangingDrones();
    }

    private float CalculateDropInterval()
    {
        Vector3 Distance = transform.position - end.position;
        float DistanceLength = Distance.magnitude;
        float MovementTime = DistanceLength / speed;
        return (float)(MovementTime / droneNumber);
    }

    private DroneInfo[] GenerateHangingDrones()
    {
        //loop throught the children of the droneSpawns to generate real, but disabled hanging drones
        DroneInfo[] generatedHangingDrones = new DroneInfo[droneSpawns.childCount];
        for(int i = 0; i < droneSpawns.childCount; i++)
        {
            //instantiate a reel drone at the spawn point
            Transform spawnPoint = droneSpawns.GetChild(i);
            GameObject spawnedDrone = Instantiate(drone, spawnPoint.position, spawnPoint.rotation, transform);
            DroneInfo spawnedDroneInfo = new DroneInfo(spawnedDrone);

            //disbale the spawned drone 
            spawnedDroneInfo.VoxelAvoidance.enabled = false;
            spawnedDroneInfo.Rigidbody.isKinematic = true;

            generatedHangingDrones[i] = spawnedDroneInfo;
        }
        return generatedHangingDrones;
    }

    // Update is called once per frame
    void Update()
    {
        if (timer >= dropInterval && numDropped < droneInfoArr.Length)
        {
            DropDrone();
            timer = 0f;
        }
        else
        {
            timer += Time.deltaTime;
        }
    }

    private void DropDrone()
    {
        if(numDropped < droneInfoArr.Length)
        {
            DroneInfo thisDrone = droneInfoArr[numDropped];
            thisDrone.Transform.parent = null;
            thisDrone.VoxelAvoidance.SetPOI(poi);
            thisDrone.VoxelAvoidance.enabled = true;
            thisDrone.Rigidbody.isKinematic = false;
            thisDrone.Rigidbody.velocity = transform.forward * speed; //crude way of matching drone's speed to carrier

            numDropped++;
        }
    }


    public struct DroneInfo
    {
        public readonly GameObject GameObject;
        public readonly Transform Transform;
        public readonly Rigidbody Rigidbody;
        public readonly VoxelAvoidance VoxelAvoidance;

        public DroneInfo(GameObject autoDrone)
        {
            GameObject = autoDrone;
            Transform = autoDrone.transform;
            //try get VoxelAvoidance
            VoxelAvoidance = null;
            if (autoDrone.TryGetComponent(out VoxelAvoidance voxelAvoidance))
            {
                VoxelAvoidance = voxelAvoidance;
            }

            //try ge the actual drone's rigidbbody
            Rigidbody = null;
            for(int i = 0; i < Transform.childCount; i++)
            {
                if(Transform.GetChild(i).TryGetComponent(out Rigidbody rigidbody))
                {
                    Rigidbody = rigidbody;
                }
            }

            Debug.Assert(VoxelAvoidance != null, "This dropped object does not have VoxelAvoidance attatched");
            Debug.Assert(Rigidbody != null, "Couldn't find the drone's rigidbody");
        }
    }
}