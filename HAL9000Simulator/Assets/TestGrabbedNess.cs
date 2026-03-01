using System.Collections;
using System.Collections.Generic;
using Rekabsen;
using UnityEngine;

public class TestGrabbedNess : MonoBehaviour
{
    [SerializeField] private GrabSurface surface;

    // Update is called once per frame
    void Update()
    {
        if (surface.RightHandGrabbed)
        {
            Debug.Log("Right Hand Grabbed");
        }
        else if (surface.LeftHandGrabbed)
        {
            Debug.Log("Left Hand Grabbed");
        }
    }
}
