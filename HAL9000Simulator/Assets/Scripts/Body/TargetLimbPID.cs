using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rekabsen
{
    public class TargetLimbPID : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float proportional;
        [SerializeField] private float derivative;
        [SerializeField] private float integral;
        private float innitialDisableMoment = 1f;
        private ConfigurableJoint configurableJoint;
        private Quaternion initial;

        void Start()
        {
            this.configurableJoint = this.GetComponent<ConfigurableJoint>();
            this.initial = this.target.transform.localRotation;
        }

        private void FixedUpdate()
        {
            if (innitialDisableMoment <= 0f)
            {
                configurableJoint.targetRotation = CopyLimb();
            }
            else
            {
                innitialDisableMoment -= Time.fixedDeltaTime;
            }
        }

        private Quaternion CopyLimb()
        {
            return Quaternion.Inverse(this.target.localRotation) * this.initial;
        }

    }
}