using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;
using Rekabsen;

public class GrabPoint : MonoBehaviour
{
    [field: SerializeField] public GrabPose Pose { get; set; } //GrabPose enum
    public Rigidbody ParentBody { get; private set; }
    public Transform ParentTrans { get; private set; }
    public GameObject Hand { get; set; }
    public ConfigurableJoint Joint { get; set; }
    public GrabSurface GrabSurface { get; set; }

    [field: SerializeField] public bool SoftGrip { get; set; }
    public bool Grabbed { get; set; } // tracks if the player is grabbing it
    public bool IsRightController { get; set; } // tracks if the player is grabbing it
    public float Priority { get; set; } // grip prioirty in range from 0.5(least) to 2(most)
    public Handedness Handedness { get; set; } = Handedness.Both;

    public bool MonoDirectional { get; set; } //if true, can only be grabbed from one direction
    public Transform HandOffset { get; set; } //the offset transform for the hand when grabbed

    //public event OnGrabbed;
    public delegate void OnGrabbedEvent();
    public OnGrabbedEvent OnGrabbed;

    //public event OnDropped;
    public delegate void OnUnGrabbedEvent();
    public OnUnGrabbedEvent OnUngrabbed;


    // Start is called before the first frame update
    void Start()
    {
        ParentBody = FindAncestorBody(transform.parent);
        ParentTrans = ParentBody.transform;
        //ParentOffset = transform.position - ParentTrans.position;

        if(HandOffset == null)
        {
            HandOffset = new GameObject("HandOffset").transform;
            HandOffset.SetParent(transform);
            HandOffset.localPosition = Vector3.zero;
            HandOffset.localRotation = Quaternion.identity;
        }
    }

    public Vector3 GetCurrParentOffset()
    {
        return transform.position - ParentTrans.position;
    }

    public Quaternion GetCurrParentRotationOffset()
    {
        return transform.rotation * Quaternion.Inverse(ParentTrans.rotation);
    }

    public void ResetGrip()
    {
        Hand = null;
        Joint = null;
        Grabbed = false;
        IsRightController = false;
    }

    public static Rigidbody FindAncestorBody(Transform parent) //find the rigidbody in parent or grandparents
    {
        //search through parents for a rigidbody
        Rigidbody parentBody = null;
        Transform currParent = parent;

        while(parentBody == null && currParent != null)
        {
            if (currParent.TryGetComponent(out Rigidbody rigidbody))
            {
                parentBody = rigidbody;
            }
            else
            {
                currParent = currParent.parent;
            }
        }

        return parentBody;
    }
}
