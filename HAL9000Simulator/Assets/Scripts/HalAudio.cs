using Rekabsen;
using System.Collections;
using System.Collections.Generic;
using Unity.Content;
using Unity.VisualScripting;
using UnityEngine;

public class HalAudio : MonoBehaviour
{
    // audio sources
    public AudioSource hal;
    public AudioClip startHal;
    public AudioClip freedomHal;


    // can variables
    public GrabSurface[] sprayCan;
    public bool heldCanOnce = false;

    void Update()
    {
        if (!heldCanOnce)
        {
            foreach (GrabSurface grip in sprayCan)
            {
                if (grip.RightHandGrabbed || grip.LeftHandGrabbed)
                {
                    heldCanOnce = true;
                    StartCoroutine(PlayIntroSequence());
                }
            }
        }
    }

    IEnumerator PlayIntroSequence()
    {
        hal.PlayOneShot(startHal);
        yield return new WaitForSeconds(startHal.length + 2f);
        hal.PlayOneShot(freedomHal);
    }

    public void PlayHalAudio(AudioClip clip)
    {
        hal.PlayOneShot(clip);
    }
}