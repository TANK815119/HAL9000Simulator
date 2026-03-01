using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AudioTrigger : MonoBehaviour
{
    public HalAudio hal;
    public AudioClip voiceLine;
    bool isPlayed = false;

    void OnTriggerEnter(Collider other)
    {
        // Debug.Log("Trigger hit by: " + other.name);
        if (!isPlayed)
        {
            isPlayed = true;
            hal.PlayHalAudio(voiceLine);
        }
        else return;
    }

}
