using System.Collections;
using System.Collections.Generic;
using Rekabsen;
using UnityEngine;

[RequireComponent(typeof(GrabSurface))]
public class GrabUpdater : MonoBehaviour
{
    [SerializeField] private GrabSurface[] enableOnGrab;
    [SerializeField] private GrabSurface[] disableOnGrab;
    private Handedness[] enableOnGrabHandedness;
    private Handedness[] disableOnGrabHandedness;
    private GrabSurface grabSurface;

    // Start is called before the first frame update
    void Start()
    {
        if(TryGetComponent<GrabSurface>(out GrabSurface gs))
        {
            grabSurface = gs;
            grabSurface.OnGrabbed += Grabbed;
            grabSurface.OnUngrabbed += UnGrabbed;
        }
        
        //stow OG handedness
        //assumes static arrays
        enableOnGrabHandedness = new Handedness[enableOnGrab.Length];
        for (int i = 0; i < enableOnGrab.Length; i++)
        {
            //Debug.Log("GrabUpdater: Stowing enableOnGrabHandedness for " + enableOnGrab[i].gameObject.name + " as OG handedness");
            Debug.Assert(enableOnGrab[i] != null, "GrabUpdater: enableOnGrab[" + i + "] is null");
            enableOnGrabHandedness[i] = enableOnGrab[i].Handedness;
            enableOnGrab[i].Handedness = Handedness.None; //disable at start
        }

        disableOnGrabHandedness = new Handedness[disableOnGrab.Length];
        for (int i = 0; i < disableOnGrab.Length; i++)
        {
            Debug.Assert(disableOnGrab[i] != null, "GrabUpdater: ableOnGrab[" + i + "] is null");
            disableOnGrabHandedness[i] = disableOnGrab[i].Handedness;
        }
    }

    private void Grabbed()
    {
        Debug.Assert(enableOnGrabHandedness.Length == enableOnGrab.Length, "GrabUpdater: enableOnGrabHandedness length mismatch");
        Debug.Assert(disableOnGrabHandedness.Length == disableOnGrab.Length, "GrabUpdater: disableOnGrabHandedness length mismatch");

        //enable OnEnable, disable OnDisable
        for (int i = 0; i < enableOnGrab.Length; i++)
        {
            enableOnGrab[i].gameObject.SetActive(true);
            enableOnGrab[i].Handedness = enableOnGrabHandedness[i];
        }
        for (int i = 0; i < disableOnGrab.Length; i++)
        {
            disableOnGrab[i].Handedness = Handedness.None;
            disableOnGrab[i].WipeGrabPoints();
            disableOnGrab[i].gameObject.SetActive(false);
        }
    }

    private void UnGrabbed()
    {
        Debug.Assert(enableOnGrabHandedness.Length == enableOnGrab.Length, "GrabUpdater: enableOnGrabHandedness length mismatch");
        Debug.Assert(disableOnGrabHandedness.Length == disableOnGrab.Length, "GrabUpdater: disableOnGrabHandedness length mismatch");

        //enable OnDisable, disable onEnable
        for (int i = 0; i < enableOnGrab.Length; i++)
        {
            enableOnGrab[i].Handedness = Handedness.None;
            enableOnGrab[i].WipeGrabPoints();
            enableOnGrab[i].gameObject.SetActive(false);
        }
        for (int i = 0; i < disableOnGrab.Length; i++)
        {
            disableOnGrab[i].gameObject.SetActive(true);
            disableOnGrab[i].Handedness = disableOnGrabHandedness[i];
        }
    }
}
