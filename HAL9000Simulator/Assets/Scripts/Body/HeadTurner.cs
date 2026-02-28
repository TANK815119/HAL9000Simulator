using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadTurner : MonoBehaviour
{
    [SerializeField] private Transform playerHead;
    [SerializeField] private Transform ragdollHead;
    [SerializeField] private float speed = 3f;
    [SerializeField] private bool turn;
    [SerializeField] private float readRotation;

    public void Start()
    {
        playerHead.Rotate(75f, 0f, 0f);
        ragdollHead.Rotate(75f, 0f, 0f);
    }
    void Update()
    {
        if(turn)
        {
            playerHead.Rotate(speed * Time.deltaTime, 0f, 0f);
            if(ragdollHead.eulerAngles.x < 75f)
            {
                ragdollHead.Rotate(speed * Time.deltaTime, 0f, 0f);
            }
        }
        readRotation = playerHead.eulerAngles.x;
    }
}
