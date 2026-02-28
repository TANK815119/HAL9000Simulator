using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Rekabsen;

public class PistolSlide : MonoBehaviour
{
    [SerializeField] private Pistol pistol;
    [SerializeField] private Vector3 originOffset; //the localpositioon origin of the slide
    [SerializeField] private float slideMax = 0.04f; //slideMax assumed to be zero
    [SerializeField] private float slideSpring = 999f;
    [SerializeField] private GrabSurface[] slideGrabs;
    [SerializeField] private GameObject originalAnchor;
    [SerializeField] private GameObject shiftedAnchor;
    [SerializeField] private GameObject targetAnchor;

    private Transform hand;
    private ConfigurableJoint joint;
    private Vector3 handOffset; //the offset of hand from transform of slide upon grabbing on the "sliding" z-axis

    public bool followHand = false;

    // Start is called before the first frame update
    void Start()
    {
        followHand = false;
        transform.localPosition = originOffset;
    }

    // Update is called once per frame
    void Update()
    {
        if(followHand)
        {
            //follow the hand
            transform.position = hand.TransformPoint(handOffset);
        }
    }

    private void SlideGripped()
    {
        //figure out which slideGrab has been grabbed
        GrabSurface slideGrab = null;
        for(int i = 0; i < slideGrabs.Length; i++)
        {
            if(slideGrabs[i].MainGrabPoint != null && slideGrabs[i].MainGrabPoint.Grabbed)
            {
                slideGrab = slideGrabs[i];
                break;
            }
        }


        //assign whatever hand grabbed, get the offset, and tell the slide to follow the hand
        Debug.Assert(slideGrab != null && slideGrab.Hand != null);
        hand = slideGrab.Hand.transform;
        //handOffset = transform.localPosition - pistol.transform.InverseTransformPoint(hand.position); //innitial grabbed offset from hand to slide
        handOffset = hand.transform.InverseTransformPoint(transform.position); //innitial grabbed offset from hand to slide
        followHand = true;

        //create a nice physics joint with the parameters specified in the serialized field
        //the joint is on the rigidbody of the gun connected to the hand
        //this MUST be done by hijacking and manipulating the joint
        Debug.Assert(slideGrab.Joint != null);
        joint = slideGrab.Joint;

        //update joint to be floppy
        JointDrive jointDrive = new JointDrive();
        JointDrive angleDrive = new JointDrive();
        jointDrive.positionSpring = slideSpring; //experiment with different procedural ways of setting this force for smoother grabs
        angleDrive.positionSpring = 0f;
        jointDrive.maximumForce = Mathf.Infinity;
        angleDrive.maximumForce = Mathf.Infinity;

        joint.xDrive = jointDrive;
        joint.yDrive = jointDrive;
        joint.zDrive = jointDrive;
        joint.angularXDrive = angleDrive;
        joint.angularYZDrive = angleDrive;

        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Limited;

        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;

        //set slide limits
        SoftJointLimit jointLimit = new SoftJointLimit();
        jointLimit.limit = slideMax;
        joint.linearLimit = jointLimit;

        //store and configure auto anchor
        joint.autoConfigureConnectedAnchor = true;
        Vector3 tempAutoConAnchor = joint.connectedAnchor;
        joint.autoConfigureConnectedAnchor = false;

        Vector3 worldAnchor = joint.transform.TransformPoint(joint.anchor);
        originalAnchor.transform.position = worldAnchor;
        Vector3 newWorldAnchor = worldAnchor - pistol.transform.forward.normalized * slideMax;
        shiftedAnchor.transform.position = newWorldAnchor;
        joint.anchor = joint.transform.InverseTransformPoint(newWorldAnchor); //since limits are around joints, I must move the joint origin bakc half the limit so it only moves back

        //joint.configuredInWorldSpace = true;
        //Vector3 targetPositionAnchor = worldAnchor + pistol.transform.forward.normalized * slideMax;
        //targetAnchor.transform.position = targetPositionAnchor;
        //joint.targetPosition = joint.transform.InverseTransformPoint(targetPositionAnchor);
        joint.targetPosition = Vector3.forward * slideMax;
        //joint.configuredInWorldSpace = false;

        //put auto anchor baack in with new offset
        joint.connectedAnchor = tempAutoConAnchor;
    }

    private void SlideUngripped()
    {
        //unnasign the hand and tell the slide to stop following the hand as well as resetting following
        hand = null;
        followHand = false;
        pistol.SlideForward();

        //destroy the joint connecting the rigidbody to the hand
    }

    private void OnEnable()
    {
        for(int i = 0; i < slideGrabs.Length; i++)
        {
            if(slideGrabs[i] == null)
            {
                Debug.LogError("PistolSlide: OnEnable: slideGrabs[" + i + "] is null!");
            }
            slideGrabs[i].OnGrabbed += SlideGripped;
            slideGrabs[i].OnUngrabbed += SlideUngripped;
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < slideGrabs.Length; i++)
        {
            slideGrabs[i].OnGrabbed -= SlideGripped;
            slideGrabs[i].OnUngrabbed -= SlideUngripped;
        }
    }

    public bool IsFollowingHand()
    {
        return followHand;
    }

    public float SlideMax()
    {
        return slideMax;
    }
}
