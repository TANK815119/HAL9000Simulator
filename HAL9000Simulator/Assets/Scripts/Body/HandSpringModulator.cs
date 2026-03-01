using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandSpringModulator : MonoBehaviour
{
    [SerializeField] private Transform hand;
    [SerializeField] private Transform controller;
    [SerializeField] private Transform shoulder;
    [SerializeField] private float springMax = 99999f;
    [SerializeField] private float springCeiling = 5000f;
    [SerializeField] private float springMin = 500f; // 5000
    [SerializeField] private float angularSpringMax = 9999f;
    [SerializeField] private float angularSpringMin = 50f;
    [SerializeField] private float funcHeight = 1f; //increasing this will "heighten" the reduction(lower=less falloff)
    [SerializeField] private float funcWidth = 5f; //increasing this will "widen" until 0
    [SerializeField] private float buffer = 0.2f;
    [SerializeField] private bool useShoulderDistance = true;
    [SerializeField] private HandSpringModulator otherMod;

    private ConfigurableJoint joint;
    private Vector3 offset;
    private float maxArmDistance;
    // Start is called before the first frame update
    void Start()
    {
        joint = hand.gameObject.GetComponent<ConfigurableJoint>();
        joint.rotationDriveMode = RotationDriveMode.Slerp; //force slerp
        offset = joint.connectedAnchor;
        maxArmDistance = Vector3.Distance(controller.position, shoulder.position) + buffer;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
       if(useShoulderDistance)
       {
            ShoulderControllerModulator();
       }
       else
       {
            HandCOntrollerModulator();
       }
    }

    private void ShoulderControllerModulator()
    {
        //fetch the drives
        JointDrive drive = joint.xDrive;
        JointDrive angularDrive = joint.slerpDrive;

        //find relativeDistance
        float relativeDistance = this.GetDistance();
        if(otherMod.GetDistance() > relativeDistance)
        {
            relativeDistance = otherMod.GetDistance();
        }

        //calculate joint drive from relativeDistance
        if(relativeDistance == 0)
        {
            drive.positionSpring = springMax;
            angularDrive.positionSpring = angularSpringMax;
        }
        else
        {
            float modulator = funcHeight * (float)System.Math.Tanh(relativeDistance * funcWidth);
            drive.positionSpring = springCeiling - springCeiling * modulator;
            angularDrive.positionSpring = angularSpringMax - angularSpringMax * modulator;
        }

        //stop drive from goinf below minimum(will eventualy be 0?)
        if (drive.positionSpring < springMin)
        {
            drive.positionSpring = springMin;
            angularDrive.positionSpring = angularSpringMin;
        }

        //apply to the joint
        joint.xDrive = drive;
        joint.yDrive = drive;
        joint.zDrive = drive;
        joint.slerpDrive = angularDrive; //angular drive not recorded and compared
    }

    public float GetDistance()
    {
        float relativeDistance = Vector3.Distance(controller.position, shoulder.position) - maxArmDistance;
        if (relativeDistance < 0)
        {
            relativeDistance = 0f;
        }
        return relativeDistance;
    }

    private void HandCOntrollerModulator()
    {
        //fetch the drives
        JointDrive drive = joint.xDrive;
        JointDrive angularDrive = joint.slerpDrive;

        //manipulate the spring force based on distance
        float modulator = funcHeight * (float)System.Math.Tanh(Vector3.Distance(controller.position, hand.position + offset));
        drive.positionSpring = springMax - springMax * modulator;
        angularDrive.positionSpring = angularSpringMax - angularSpringMax * modulator;

        if (drive.positionSpring < springMin)
        {
            drive.positionSpring = springMin;
            angularDrive.positionSpring = angularSpringMin;
        }

        //apply to the joint
        joint.xDrive = drive;
        joint.yDrive = drive;
        joint.zDrive = drive;
        joint.slerpDrive = angularDrive;
    }
}
