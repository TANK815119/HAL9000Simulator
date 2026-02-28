using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rekabsen
{
    public class TargetLimb : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float muscleSpring = 10000f;
        [SerializeField] private float muscleDamper = 50f;
        [SerializeField] private float muscleMaxForce = 625f;
        [SerializeField] private float eccentricSpringRatio = 2f;
        [SerializeField] private float eccentricDamperRatio = 4f;
        //[SerializeField] private float eccentricAlpha = 0.5f;
        [SerializeField] private Vector3 limbDirection = new Vector3(0f, 1f, 0f);
        [SerializeField] private Vector3 parentLimbDirection = new Vector3(0f, 1f, 0f);
        private float innitialDisableMoment = 1f;
        private ConfigurableJoint configurableJoint;
        private Quaternion initial;

        private float lastTheta;

        void Start()
        {
            this.configurableJoint = this.GetComponent<ConfigurableJoint>();
            this.initial = this.target.transform.localRotation;
            lastTheta = 0f;

            //copy the bounds of the target joint into a new joint on the limb
            //because constraints dont work with slerp(and slerp is better)
            if (target.TryGetComponent(out ConfigurableJoint targetJoint))
            {
                ConfigurableJoint newJoint = gameObject.AddComponent<ConfigurableJoint>();
                newJoint.connectedBody = configurableJoint.connectedBody;
                newJoint.anchor = targetJoint.anchor;
                newJoint.axis = targetJoint.axis;
                newJoint.autoConfigureConnectedAnchor = targetJoint.autoConfigureConnectedAnchor;
                newJoint.connectedAnchor = targetJoint.connectedAnchor;
                newJoint.secondaryAxis = targetJoint.secondaryAxis;
                newJoint.xMotion = targetJoint.xMotion;
                newJoint.yMotion = targetJoint.yMotion;
                newJoint.zMotion = targetJoint.zMotion;
                newJoint.angularXMotion = targetJoint.angularXMotion;
                newJoint.angularYMotion = targetJoint.angularYMotion;
                newJoint.angularZMotion = targetJoint.angularZMotion;
                newJoint.lowAngularXLimit = targetJoint.lowAngularXLimit;
                newJoint.highAngularXLimit = targetJoint.highAngularXLimit;
                newJoint.angularYLimit = targetJoint.angularYLimit;
                newJoint.angularZLimit = targetJoint.angularZLimit; //y no copy unity????
            }
        }

        private void FixedUpdate()
        {
            if (innitialDisableMoment <= 0f)
            {
                configurableJoint.targetRotation = CopyLimb();
                CheckEccentricity(); //scales forces to be stronger on eccentric(resisting)
            }
            else
            {
                innitialDisableMoment -= Time.fixedDeltaTime;
            }
        }

        private void CheckEccentricity()
        {
            //find the angle between the limb and its parent
            Vector3 parentLimbDir = configurableJoint.connectedBody.transform.InverseTransformDirection(parentLimbDirection.normalized);
            Vector3 limbDir = this.transform.InverseTransformDirection(limbDirection.normalized);
            float theta = Vector3.Angle(parentLimbDir, limbDir);

            //check to see if theta has increased(eccentric) or decreases (concentric)
            if (theta - lastTheta > 0f)
            {
                SetJointStrength(muscleSpring * eccentricSpringRatio, muscleDamper * eccentricDamperRatio, muscleMaxForce * eccentricSpringRatio);
            }
            else if(theta - lastTheta <= 0f)
            {
                SetJointStrength(muscleSpring, muscleDamper, muscleMaxForce);
            }

            lastTheta = theta;
        }

        private void SetJointStrength(float spring, float damper, float maxForce)
        {
            JointDrive drive = new JointDrive();
            drive.positionSpring = spring;
            drive.positionDamper = damper;
            drive.maximumForce = maxForce;
            configurableJoint.slerpDrive = drive;
            //configurableJoint.angularXDrive = drive;
            //configurableJoint.angularYZDrive = drive;
        }

        private Quaternion CopyLimb()
        {
            return Quaternion.Inverse(this.target.localRotation) * this.initial; //(newTargetRot) - innitial
        }

    }
}