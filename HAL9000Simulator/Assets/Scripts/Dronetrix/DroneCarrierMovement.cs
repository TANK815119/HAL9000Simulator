using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class DroneCarrierMovement : MonoBehaviour
{
    [SerializeField] private Transform start; //creates start and endpoints 
    [SerializeField] private Transform end;
    [SerializeField] private float Speed = 1f;
    [SerializeField] private float turnRate = 1f;
    // Start is called before the first frame update
    void Start()
    {
        Vector3 startPosition = start.position; //teleports drone carrier to the starting point
        transform.position = startPosition;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 CurrentDirection = transform.forward;
        Vector3 GoalDirection = (end.position - transform.position).normalized;
        transform.position = transform.position + (transform.forward.normalized * Speed * Time.deltaTime);
        Vector3 NewDirection = Vector3.RotateTowards(transform.forward, GoalDirection, turnRate * Time.deltaTime, 0.0f);
        transform.rotation = Quaternion.LookRotation(NewDirection, Vector3.up); 
    }

    public Transform GetEnd()
    {
        return end;
    }
    
    public float GetSpeed()
    {
        return Speed;
    }
}