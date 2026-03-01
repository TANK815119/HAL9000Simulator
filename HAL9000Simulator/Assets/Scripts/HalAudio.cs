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
    public GrabSurface cylinder;
    public GrabSurface rightTrigger;
    public GrabSurface leftTrigger;
    public bool heldCanOnce = false;

    void Update()
    {
        //if (heldCanOnce)
        //{
        //    return;
        //}
        //if (cylinder == null || rightTrigger == null || leftTrigger == null)
        //{
        //    Debug.Log("Early Return");
        //    return;
        //}
        //if (cylinder.RightHandGrabbed || cylinder.LeftHandGrabbed)
        //{
        //    Debug.Log("Started Hal Intro Sequence");
        //    heldCanOnce = true;
        //    StartCoroutine(PlayIntroSequence());

        //}
        //else if (rightTrigger.RightHandGrabbed || rightTrigger.LeftHandGrabbed)
        //{
        //    Debug.Log("Started Hal Intro Sequence");
        //    heldCanOnce = true;
        //    StartCoroutine(PlayIntroSequence());

        //}
        //else if (leftTrigger.RightHandGrabbed || leftTrigger.LeftHandGrabbed)
        //{
        //    Debug.Log("Started Hal Intro Sequence");
        //    heldCanOnce = true;
        //    StartCoroutine(PlayIntroSequence());

        //}
        if (!heldCanOnce)
        {
            foreach (GrabSurface grip in sprayCan)
            {
                if (grip.RightHandGrabbed || grip.LeftHandGrabbed)
                {
                    Debug.Log("Started Hal Intro Sequence");
                    heldCanOnce = true;
                    StartCoroutine(PlayIntroSequence());

                }
            }
            //if (cylinder.RightHandGrabbed || cylinder.LeftHandGrabbed)
            //{
            //    Debug.Log("Started Hal Intro Sequence");
            //    heldCanOnce = true;
            //    StartCoroutine(PlayIntroSequence());

            //}
            //else if (rightTrigger.RightHandGrabbed || rightTrigger.LeftHandGrabbed)
            //{
            //    Debug.Log("Started Hal Intro Sequence");
            //    heldCanOnce = true;
            //    StartCoroutine(PlayIntroSequence());

            //}
            //else if (leftTrigger.RightHandGrabbed || leftTrigger.LeftHandGrabbed)
            //{
            //    Debug.Log("Started Hal Intro Sequence");
            //    heldCanOnce = true;
            //    StartCoroutine(PlayIntroSequence());

            //}
        }
    }

    public void StartHalIntro()
    {
        if (!heldCanOnce)
        {
            Debug.Log("Started Hal Intro Sequence");
            heldCanOnce = true;
            StartCoroutine(PlayIntroSequence());
        }
    }

    IEnumerator PlayIntroSequence()
    {

        if (hal.isPlaying) yield break;
        hal.PlayOneShot(startHal);
        yield return new WaitForSeconds(startHal.length + 2f);
        hal.PlayOneShot(freedomHal);
        yield return new WaitForSeconds(freedomHal.length + 2f);
    }

    public void PlayHalAudio(AudioClip clip)
    {
        if (hal.isPlaying) return;

        hal.PlayOneShot(clip);
    }
}