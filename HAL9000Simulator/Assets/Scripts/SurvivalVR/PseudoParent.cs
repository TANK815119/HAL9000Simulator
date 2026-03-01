using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//postionaly and rotationally attatches an object to another
//with an offset that is calculated on start

namespace Rekabsen
{
    public class PseudoParent : MonoBehaviour
    {
        [SerializeField] private Transform followTransform;
        //[SerializeField] private bool followPosition = true;
        //[SerializeField] private bool followRotation = true;
        //[SerializeField] private Vector3 positionOffsetAdditional = Vector3.zero;
        //[SerializeField] private Vector3 eulerOffsetAdditional = Vector3.zero;

        private Vector3 positionOffsetInnitial;
        private Quaternion rotationOffsetInnitial;
        private Quaternion followRotationInnitial;

        // Start is called before the first frame update
        void Awake()
        {
            positionOffsetInnitial = transform.position - followTransform.position;
            rotationOffsetInnitial = transform.rotation * Quaternion.Inverse(followTransform.rotation);
            followRotationInnitial = followTransform.rotation;
        }

        // Update is called once per frame
        void Update()
        {
            transform.position = followTransform.position + followTransform.rotation * Quaternion.Inverse(followRotationInnitial) * positionOffsetInnitial;
            transform.rotation = followTransform.rotation * rotationOffsetInnitial;
        }
    }
}