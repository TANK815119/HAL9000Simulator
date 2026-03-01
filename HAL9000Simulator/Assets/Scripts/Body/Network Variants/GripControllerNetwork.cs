using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Unity.Netcode;

namespace Rekabsen
{
    [RequireComponent(typeof(InputData))]
    public class GripControllerNetwork : NetworkBehaviour, GripControllerInterface
    {
        [SerializeField] private bool isRightController = false;
        [SerializeField] private bool falseGrip = false;
        [SerializeField] private AudioClip gripClip;
        [SerializeField] private AudioClip ungripClip;
        [SerializeField] private MonoBehaviour otherGripControllerHand;
        private GripControllerInterface otherHand;

        private NetworkVariable<float> grip = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private bool gripping = false;
        private bool clenched = false;
        private bool twoHanded = false;

        private ConfigurableJoint joint;
        private ConfigurableJoint wristJoint;
        private GrabPoint grabbedPoint;
        private List<GrabPoint> grabList;
        private HandAnimationInterface handAnim;
        private Rigidbody handBody;
        private Collider handCollider;
        private Collider forearmCollider;
        private InputData inputData;
        // Start is called before the first frame update
        void Start()
        {
            otherHand = (GripControllerInterface)otherGripControllerHand;
            grabList = new List<GrabPoint>();
            handAnim = gameObject.GetComponent<HandAnimationInterface>();
            handBody = gameObject.GetComponent<Rigidbody>();
            handCollider = gameObject.GetComponent<Collider>();
            forearmCollider = transform.parent.gameObject.GetComponent<Collider>();
            inputData = gameObject.GetComponent<InputData>();
            wristJoint = gameObject.GetComponent<ConfigurableJoint>();
        }

        // Update is called once per frame
        void Update()
        {
            //chack for phantom grips
            if (joint == null && gripping == true)
            {
                DestroyPhantomGrip();
            }

            //yoink controller values
            float localGrip = (isRightController) ? GetRightGrip() : GetLeftGrip();
            if (IsOwner)
            {
                grip.Value = localGrip;
            }
            if (falseGrip == true)
            {
                grip.Value = 1f;
            }

            //try to create grips
            if (grip.Value > 0.85f && !gripping && !clenched)
            {
                if (grip.Value > 0.975f)
                {
                    clenched = true;
                }

                //CullGrabList();//make sure there arent any artifacts of deleted objects
                if (grabList.Count != 0)
                {
                    CreateGrip();
                }
            }

            //try to detroy grips
            if (grip.Value < 0.85f)
            {
                clenched = false;
                if (gripping)
                {
                    DestroyGrip();
                }
            }

            //if(joint != null && joint.currentForce.magnitude > 9999f) //currentFOrce only works one "limited" joints(works well now but may acause problems later)
            //{
            //    Debug.Log(joint.currentForce.magnitude);
            //    DestroyGrip();
            //}
        }

        private void CreateGrip()
        {
            GrabPoint closestGrab = FindClosestGrabPoint(grabList, handBody.transform);
            Transform closestPoint = (closestGrab == null) ? null : closestGrab.transform;

            //make sure you arent trying to grab a deleted object
            while (closestPoint == null)
            {
                if (grabList.Count <= 0)
                {
                    return; //all grips were deleted
                }

                CullGrabList();
                closestGrab = FindClosestGrabPoint(grabList, handBody.transform);
                closestPoint = (closestGrab == null) ? null : closestGrab.transform;
            }

            GrabPoint grabPoint = closestPoint.GetComponent<GrabPoint>();

            //change hand animation state
            handAnim.Gripping = true;
            handAnim.Pose = grabPoint.Pose;

            //form a joint with that object
            joint = grabPoint.ParentTrans.gameObject.AddComponent<ConfigurableJoint>();

            //position
            joint.configuredInWorldSpace = true;
            joint.connectedBody = handBody;
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = new Vector3(0f, 0f, 0f);
            joint.connectedAnchor = new Vector3(0f, 0f, 0f);
            joint.targetPosition = grabPoint.GetCurrParentOffset() - 0.1f * grabPoint.transform.up; //hard coded for hand length

            //rotation
            joint.targetRotation = grabPoint.GetCurrParentRotationOffset() * grabPoint.ParentTrans.rotation * Quaternion.Inverse(handBody.transform.rotation);

            //drives
            if (!grabPoint.SoftGrip)
            {
                JointDrive jointDrive = new JointDrive();
                jointDrive.positionSpring = 99999f;
                jointDrive.maximumForce = Mathf.Infinity;
                joint.xDrive = jointDrive;
                joint.yDrive = jointDrive;
                joint.zDrive = jointDrive;
                joint.angularXDrive = jointDrive;
                joint.angularYZDrive = jointDrive;
            }
            else
            {
                //JointDrive jointDrive = new JointDrive();
                //jointDrive.positionSpring = 99999f;
                //jointDrive.maximumForce = Mathf.Infinity;
                //joint.xDrive = jointDrive;
                //joint.yDrive = jointDrive;
                //joint.zDrive = jointDrive;
                //joint.angularXDrive = jointDrive;
                //joint.angularYZDrive = jointDrive;

                //joint.xMotion = ConfigurableJointMotion.Limited;
                //joint.yMotion = ConfigurableJointMotion.Limited;
                //joint.zMotion = ConfigurableJointMotion.Limited;

                //joint.targetPosition = Vector3.zero;
                //joint.connectedAnchor = grabPoint.GetCurrParentOffset() + 0.1f * grabPoint.transform.up; //hard coded for hand length
            }

            ////configure bodily collisions of the object with layers
            ////bothHandObject = 10
            ////leftHandObject = 11
            ////rightHandObject = 12

            //configure bodily collisions of the object with collision matricies
            SetAllToCollision(handCollider, joint.transform, true);
            SetAllToCollision(forearmCollider, joint.transform, true);

            //confiure wrist strnegth based on whether or not the grab is two-handed
            if (this.GetGrabbedObject().Equals(otherHand.GetGrabbedObject())) //twohanded
            {
                MakeTwoHandedGrip(true);
                otherHand.MakeTwoHandedGrip(true);
            }

            //play audio
            AudioSource.PlayClipAtPoint(gripClip, transform.position, 1f);

            //update grabPoint information
            gripping = true;
            grabbedPoint = grabPoint;
            grabbedPoint.Grabbed = true;
            grabbedPoint.IsRightController = isRightController;
            grabbedPoint.Hand = gameObject;
            grabbedPoint.Joint = joint;
            grabbedPoint.OnGrabbed?.Invoke();
        }

        public void DestroyGrip()
        {
            gripping = false;
            grabbedPoint.Grabbed = false;
            grabbedPoint.Hand = null;
            grabbedPoint.OnUngrabbed?.Invoke();

            //change hand animation state
            handAnim.Gripping = false;

            //configure bodily collisions of the object with collision matricies
            SetAllToCollision(handCollider, joint.transform, false);
            SetAllToCollision(forearmCollider, joint.transform, false);

            //confiure wrist strnegth based on whether or not the grab is two-handed(should still work as the joint still exisits)
            MakeTwoHandedGrip(false);
            if (this.GetGrabbedObject().Equals(otherHand.GetGrabbedObject())) //twohanded
            {
                otherHand.MakeTwoHandedGrip(false);
            }

            //destroy the joint with the gripped object
            Destroy(joint);
            joint = null;

            //play audio
            AudioSource.PlayClipAtPoint(ungripClip, transform.position, 1f);
        }

        private void DestroyPhantomGrip()
        {
            gripping = false;
            handAnim.Gripping = false;
            AudioSource.PlayClipAtPoint(ungripClip, transform.position, 1f);
        }

        private GrabPoint FindClosestGrabPoint(List<GrabPoint> grabList, Transform hand)
        {
            //check for any null values
            for (int i = 0; i < grabList.Count; i++)
            {
                if (grabList[i].Equals(null))
                {
                    return null; //return null so the the CreateGrip while loop handles it
                }
            }

            Vector3 handPoint = hand.position + 0.1f * hand.up; //hard coded hand length
            Quaternion handRotation = hand.rotation;

            //search through the list for the closest grab point
            int bestIndex = 0;
            for (int i = 0; i < grabList.Count; i++)
            {
                float bestEvaluation = GrabPointEvaluator((grabList[bestIndex].transform.position - handPoint).magnitude,
                    Quaternion.Angle(grabList[bestIndex].transform.rotation, handRotation),
                    grabList[bestIndex].Priority);
                float thisEvaluation = GrabPointEvaluator((grabList[i].transform.position - handPoint).magnitude,
                    Quaternion.Angle(grabList[i].transform.rotation, handRotation),
                    grabList[i].Priority);
                if (thisEvaluation > bestEvaluation)
                {
                    bestIndex = i;
                }
            }
            return grabList[bestIndex];
        }

        private void CullGrabList()
        {
            //remove null grabPoints
            for (int i = 0; i < grabList.Count; i++)
            {
                if (grabList[i].Equals(null))
                {
                    grabList.RemoveAt(i);
                }
            }
        }

        private float GrabPointEvaluator(float distance, float angleDistance, float priority) //returns valuer between 0 and 1 that dictates point fitness
        {
            //evalute distance
            //Debug.Log("distance: " + distance);
            float distanceFitness = (1f / 0.1f) * (0.1f - distance); //the 0.1f is hard coded as the max distance from hand to grabPoint

            //evaluate rotation
            //Debug.Log("angle: " + angleDistance);
            float angleFitness = 1f - (angleDistance / 180f); //compress to between zero and one

            //synthesize the distance and rotation values into one value between zero and one
            float synthesisFitness = (distanceFitness * 1f + angleFitness * 1f + priority) / 4f; //priority is between 0.5 and 2
            //Debug.Log("distance: " + distanceFitness + " + angle: " + angleFitness + " = fitness: " + synthesisFitness);

            return synthesisFitness;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out GrabPoint grabPoint))
            {
                grabList.Add(grabPoint);
            }
            if (other.TryGetComponent(out GrabSurface grabSurface))
            {
                grabList.Add(grabSurface.FollowHand(transform));
            }
        }
        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out GrabPoint grabPoint))
            {
                grabList.Remove(grabPoint);
            }
            if (other.TryGetComponent(out GrabSurface grabSurface))
            {
                grabList.Remove(grabSurface.UnfollowHand(transform));
            }
        }

        public void AddGrabPointReference(GrabPoint grabPoint)
        {
            if (!grabList.Contains(grabPoint))
            {
                grabList.Add(grabPoint);
            }
        }

        public void RemoveGrabPointReference(GrabPoint grabPoint)
        {
            if (grabList.Contains(grabPoint))
            {
                grabList.Remove(grabPoint);
            }
        }

        private float GetRightGrip()
        {
            if (inputData.rightController.TryGetFeatureValue(CommonUsages.grip, out float controllerGrip))
            {
                return controllerGrip;
            }
            return 0f;
        }

        private float GetLeftGrip()
        {
            if (inputData.leftController.TryGetFeatureValue(CommonUsages.grip, out float controllerGrip))
            {
                return controllerGrip;
            }
            return 0f;
        }

        private void SetAllToLayer(Transform parent, int layer)
        {
            parent.gameObject.layer = layer;
            for (int i = 0; i < parent.childCount; i++)
            {
                SetAllToLayer(parent.GetChild(i), layer);
            }
        }

        private void SetAllToCollision(Collider grabberCollider, Transform parent, bool collisionState)
        {
            if (parent.gameObject.TryGetComponent(out Collider objectCollider))
            {
                Physics.IgnoreCollision(grabberCollider, objectCollider, collisionState);
            }
            for (int i = 0; i < parent.childCount; i++)
            {
                SetAllToCollision(grabberCollider, parent.GetChild(i), collisionState);
            }
        }

        public void MakeTwoHandedGrip(bool isTwoHanded) //makes the joint much weaker if twohanded
        {
            JointDrive jointDrive = wristJoint.slerpDrive;
            if (isTwoHanded && !twoHanded)
            {
                jointDrive.positionSpring = jointDrive.positionSpring / 100f;
                twoHanded = true;
            }
            else if (twoHanded)
            {
                jointDrive.positionSpring = jointDrive.positionSpring * 100f;
                twoHanded = false;
            }

            wristJoint.angularXDrive = jointDrive;
        }

        public GameObject GetGrabbedObject()
        {
            if (joint != null)
            {
                return joint.gameObject;
            }
            else
            {
                return null; //hopefully shouldnt create error
            }
        }
    }
}