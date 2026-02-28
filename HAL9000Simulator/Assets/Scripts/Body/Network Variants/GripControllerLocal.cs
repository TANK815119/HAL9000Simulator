using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Unity.Netcode;
using System;

namespace Rekabsen
{
    [RequireComponent(typeof(InputData))]
    public class GripControllerLocal : MonoBehaviour, GripControllerInterface
    {
        [SerializeField] private bool isRightController = false;
        [SerializeField] private bool falseGrip = false;
        [SerializeField] private AudioClip gripClip;
        [SerializeField] private AudioClip ungripClip;
        [SerializeField] private MonoBehaviour otherGripControllerHand;
        private GripControllerInterface otherHand;

        private float grip = 0f;
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
        [SerializeField] private Transform primaryHand;
        [SerializeField] private Transform secondaryHand;
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
            grip = localGrip;
            if (falseGrip == true)
            {
                grip = 1f;
            }

            //try to create grips
            if (grip > 0.85f && !gripping && !clenched)
            {
                if (grip > 0.975f)
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
            if (grip < 0.85f)
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

            ////update drives for twohandedness
            //if (twoHanded)
            //{
            //    //Debug.Log("Two-handing");
            //    UpdateTargetRotation(joint, primaryHand, secondaryHand, isRightController, GetGrabbedObject().transform);
            //}
        }

        private void CreateGrip()
        {
            GrabPoint closestGrab = FindClosestGrabPoint(NonOccupiedGrabs(grabList), handBody.transform);
            Transform closestPoint = (closestGrab == null) ? null : closestGrab.transform;

            //make sure you arent trying to grab a deleted object
            while (closestPoint == null)
            {
                if (NonOccupiedGrabs(grabList).Count <= 0)
                {
                    return; //all grips were deleted
                }

                CullGrabList();
                closestGrab = FindClosestGrabPoint(NonOccupiedGrabs(grabList), handBody.transform);
                closestPoint = (closestGrab == null) ? null : closestGrab.transform;
            }

            GrabPoint grabPoint = closestPoint.GetComponent<GrabPoint>();

            //change hand animation state
            handAnim.Gripping = true;
            handAnim.Pose = grabPoint.Pose;

            //store and temporarily change positon of hand transform to the grabPoint
            Transform oldHandTrans = handBody.transform;
            handBody.transform.position = grabPoint.transform.position - 0.1f * grabPoint.transform.up; //hard coded for hand length
            handBody.transform.rotation = grabPoint.transform.rotation;


            //form a joint with that object
            joint = grabPoint.ParentTrans.gameObject.AddComponent<ConfigurableJoint>();

            //position
            joint.configuredInWorldSpace = false;
            joint.connectedBody = handBody;
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = new Vector3(0.0f, 0.1f, 0.0f); //hard coded hand length, 0.1f is not 10cm but instead a value that will scale with the size of the transform of the hand
            joint.anchor = joint.transform.InverseTransformVector(grabPoint.GetCurrParentOffset());

            //rotation
            joint.targetRotation = grabPoint.GetCurrParentRotationOffset() * grabPoint.ParentTrans.rotation * Quaternion.Inverse(handBody.transform.rotation);

            //drives
            if (!grabPoint.SoftGrip)
            {
                JointDrive jointDrive = new JointDrive();
                JointDrive angleDrive = new JointDrive();
                jointDrive.positionSpring = 99999f; //experiment with different procedural ways of setting this force for smoother grabs
                angleDrive.positionSpring = 999f;
                jointDrive.maximumForce = Mathf.Infinity;
                angleDrive.maximumForce = Mathf.Infinity;

                joint.xDrive = jointDrive;
                joint.yDrive = jointDrive;
                joint.zDrive = jointDrive;
                joint.angularXDrive = angleDrive;
                joint.angularYZDrive = angleDrive;

                joint.xMotion = ConfigurableJointMotion.Free;
                joint.yMotion = ConfigurableJointMotion.Free;
                joint.zMotion = ConfigurableJointMotion.Free;

                joint.angularXMotion = ConfigurableJointMotion.Free;
                joint.angularYMotion = ConfigurableJointMotion.Free;
                joint.angularZMotion = ConfigurableJointMotion.Free;
            }
            else
            {
                JointDrive jointDrive = new JointDrive();
                JointDrive angleDrive = new JointDrive();
                jointDrive.positionSpring = 99999f;
                angleDrive.positionSpring = 15f;
                jointDrive.maximumForce = Mathf.Infinity;
                angleDrive.maximumForce = Mathf.Infinity;
                joint.xDrive = jointDrive;
                joint.yDrive = jointDrive;
                joint.zDrive = jointDrive;
                joint.angularXDrive = angleDrive;
                joint.angularYZDrive = angleDrive;

                joint.angularXMotion = ConfigurableJointMotion.Free;
                joint.angularYMotion = ConfigurableJointMotion.Free;
                joint.angularZMotion = ConfigurableJointMotion.Free;

                joint.angularXMotion = ConfigurableJointMotion.Free;
                joint.angularYMotion = ConfigurableJointMotion.Free;
                joint.angularZMotion = ConfigurableJointMotion.Free;

                //joint.connectedMassScale = 1f / joint.GetComponent<Rigidbody>().mass;

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
            if (IsOtherHandGrabbingSameObject()) //twohanded
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
            if (grabbedPoint.GrabSurface != null)
            {
                grabbedPoint.GrabSurface.SubscribeMainGrabbedPoint(grabbedPoint);
                grabbedPoint.GrabSurface.OnGrabbed?.Invoke();
            }
            grabbedPoint.OnGrabbed?.Invoke();

            //check how the autoconnectedanchor actually works
            //Debug.Log("connected anchor:" + joint.connectedAnchor);
            //Debug.Log("the positon of the anchor in the local space of the hand(connected body):" + gameObject.transform.InverseTransformPoint(joint.transform.TransformPoint(joint.anchor)));

            //Debug.Log("offset from hand to grab point in joint space:" + joint.transform.InverseTransformVector(grabPoint.transform.position - gameObject.transform.position));
            //Debug.Log("offset from hand to grab point in world space:" + (grabPoint.transform.position - gameObject.transform.position));
            //Debug.Log("offset from grab point to hand in joint space:" + joint.transform.InverseTransformVector(gameObject.transform.position - grabPoint.transform.position));
            //Debug.Log("offset from grab point to hand in world space:" + (gameObject.transform.position - grabPoint.transform.position));
            //Debug.Log("offset from hand to anchor in joint space:" + joint.transform.InverseTransformVector(joint.transform.TransformPoint(joint.anchor) - gameObject.transform.position));
            //Debug.Log("offset from hand to anchor in world space:" + (joint.transform.TransformPoint(joint.anchor) - gameObject.transform.position));
            //Debug.Log("offset from anchor to hand in joint space:" + joint.transform.InverseTransformVector(gameObject.transform.position - joint.transform.TransformPoint(joint.anchor)));
            //Debug.Log("offset from anchor to hand in world space:" + (gameObject.transform.position - joint.transform.TransformPoint(joint.anchor)));
        }

        public void DestroyGrip()
        {
            Debug.Assert(gripping == true, "GripControllerLocal: DestroyGrip called when not gripping");
            gripping = false;
            grabbedPoint.Grabbed = false;
            grabbedPoint.Hand = null;
            if (grabbedPoint.GrabSurface != null)
            {
                grabbedPoint.GrabSurface.UnsubScribeMainGrabPoint(grabbedPoint);
                grabbedPoint.GrabSurface.OnUngrabbed?.Invoke();
            }
            grabbedPoint.OnUngrabbed?.Invoke();

            //change hand animation state
            handAnim.Gripping = false;

            //configure bodily collisions of the object with collision matricies
            SetAllToCollision(handCollider, joint.transform, false);
            SetAllToCollision(forearmCollider, joint.transform, false);

            //confiure wrist strnegth based on whether or not the grab is two-handed(should still work as the joint still exisits)
            MakeTwoHandedGrip(false);
            if (IsOtherHandGrabbingSameObject()) //twohanded
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
            if (grabList.Count == 0)
            {
                return null;
            }
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

        private List<GrabPoint> NonOccupiedGrabs(List<GrabPoint> grabList)
        {
            List<GrabPoint> culledList = new List<GrabPoint>();
            for (int i = 0; i < grabList.Count; i++)
            {
                //handle exclusive grabs
                GrabSurface grab = grabList[i].GrabSurface;
                //bool occupied = (grab.Handedness == Handedness.Exclusive && grab.MainGrabPoint.Grabbed && grab.MainGrabPoint.IsRightController != isRightController);
                if (grab.MainGrabPoint == null || 
                    !(grab.Handedness == Handedness.Exclusive && grab.MainGrabPoint.Grabbed && grab.MainGrabPoint.IsRightController != isRightController))
                {
                    culledList.Add(grabList[i]);
                }
            }
            return culledList;
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
            if (other.TryGetComponent(out GrabPoint grabPoint)) //elim duplicates
            {
                bool handednessMatch = (grabPoint.Handedness == Handedness.Both) ||
                    (grabPoint.Handedness == Handedness.Right && isRightController) ||
                    (grabPoint.Handedness == Handedness.Left && !isRightController) ||
                    (grabPoint.Handedness == Handedness.Exclusive);
                if (!handednessMatch || grabList.Contains(grabPoint)) //dont add if already added
                {
                    return;
                }
                AddGrabPointReference(grabPoint);
                return;
            }
            if (other.TryGetComponent(out GrabSurface grabSurface))
            {
                bool handednessMatch = (grabSurface.Handedness == Handedness.Both) ||
                    (grabSurface.Handedness == Handedness.Right && isRightController) ||
                    (grabSurface.Handedness == Handedness.Left && !isRightController) ||
                    (grabSurface.Handedness == Handedness.Exclusive);
                if (!handednessMatch || grabSurface.FetchGrab(transform) != null) //dont follow if already added
                {
                    return;
                }
                AddGrabPointReference(grabSurface.FollowHand(transform));
                return;
            }
        }
        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out GrabPoint grabPoint))
            {
                if(grabPoint.Grabbed && grabPoint.IsRightController == isRightController) //dont elim grabbed
                {
                    return;
                }
                RemoveGrabPointReference(grabPoint);
                return;
            }
            if (other.TryGetComponent(out GrabSurface grabSurface))
            {
                GrabPoint grab = grabSurface.FetchGrab(transform);
                if (grab == null || (grab.Grabbed && grab.IsRightController == isRightController)) //dont elim grabbed
                {
                    //in theory there is still and edge case where the player lets go of the grab after it leaves the trigger and never gerts removed i think
                    return;
                }
                RemoveGrabPointReference(grabSurface.UnfollowHand(transform));
                return;
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

        public static void SetAllToCollision(Collider grabberCollider, Transform parent, bool ignore)
        {
            if (parent.gameObject.TryGetComponent(out Collider objectCollider))
            {
                Physics.IgnoreCollision(grabberCollider, objectCollider, ignore);
            }
            for (int i = 0; i < parent.childCount; i++)
            {
                SetAllToCollision(grabberCollider, parent.GetChild(i), ignore);
            }
        }

        public void MakeTwoHandedGrip(bool isTwoHanded) //makes the joint much weaker if twohanded
        {
            //JointDrive jointDrive = wristJoint.slerpDrive;
            if (isTwoHanded && !twoHanded)
            {
                //jointDrive.positionSpring = jointDrive.positionSpring / 100f;
                twoHanded = true;
            }
            else if (twoHanded)
            {
                //jointDrive.positionSpring = jointDrive.positionSpring * 100f;
                twoHanded = false;
                joint.targetRotation = Quaternion.identity;
            }

            //wristJoint.angularXDrive = jointDrive;
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

        private void UpdateTargetRotation(ConfigurableJoint joint, Transform primaryHand, Transform secondaryHand, bool rightHand, Transform heldObject)
        {
            //calculate the ideal object direction from kinematics(global)
            Vector3 idealGlobalObjectDirection = (secondaryHand.position - primaryHand.position).normalized;

            //calculate the ideal object rotation from kinematics(global)
            Vector3 currentObjectDirection;
            if (rightHand)
            {
                currentObjectDirection = -heldObject.right; //may vary between different objects; where object 
            }
            else
            {
                currentObjectDirection = heldObject.right; //may vary between different objects; where object 
            }
            Quaternion idealGlobalObjectRotation = Quaternion.FromToRotation(currentObjectDirection, idealGlobalObjectDirection) * heldObject.transform.rotation;

            //put that rotation in terms of the held object's target rotation
            Quaternion connectedBodyRotation = joint.connectedBody.transform.rotation;
            Quaternion targetLocalObjectRotation = Quaternion.Inverse(connectedBodyRotation) * idealGlobalObjectRotation;

            //set the local rotation as the joints target rotation
            joint.configuredInWorldSpace = false; //I don't think this does shit for rotations; always local I think
            joint.targetRotation = targetLocalObjectRotation;
        }

        private bool IsOtherHandGrabbingSameObject()
        {
            //other hand not grabbing anything base case
            if(otherHand.GetGrabbedObject() == null)
            {
                return false;
            }

            //compare grabbed object to the whole strata of objects grabbed by the other hand
            //I include multiple strata for hierarchical obejcts like the shotgun
            Transform grabbedObject = this.GetGrabbedObject().transform;
            Transform grabbedParent = this.GetGrabbedObject().transform.parent;
            Transform otherObject = otherHand.GetGrabbedObject().transform;
            Transform otherParent = otherHand.GetGrabbedObject().transform.parent;

            if (grabbedObject.Equals(otherObject) ||
                (otherParent != null && grabbedObject.Equals(otherParent)) ||
                (grabbedParent != null && grabbedParent.Equals(otherObject))) //twohanded
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}