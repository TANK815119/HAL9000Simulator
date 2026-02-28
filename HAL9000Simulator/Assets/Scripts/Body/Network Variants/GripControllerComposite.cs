//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.XR;
//using Unity.Netcode;

//namespace Rekabsen
//{
//    public interface GripControllerInterface
//    {
//        public void MakeTwoHandedGrip(bool isTwoHanded);
//        public GameObject GetGrabbedObject();
//    }

//    [RequireComponent(typeof(InputData))]
//    public class GripControllerLocal : MonoBehaviour, GripCOntrollerInterface
//    {
//        [SerializeField] private bool isRightController = false;
//        [SerializeField] private bool falseGrip = false;
//        [SerializeField] private AudioClip gripClip;
//        [SerializeField] private AudioClip ungripClip;
//        [SerializeField] private bool NonNetworkOveride = false;
//        [SerializeField] private GripCOntrollerInterface otherHand;

//        private IVariable<float> grip;
//        private bool gripping = false;
//        private bool clenched = false;
//        private bool twoHanded = false;

//        private ConfigurableJoint joint;
//        private ConfigurableJoint wristJoint;
//        private GrabPoint grabbedPoint;
//        private List<Transform> grabList;
//        private HandAnimation handAnim;
//        private Rigidbody handBody;
//        private Collider handCollider;
//        private Collider forearmCollider;
//        private InputData inputData;
//        // Start is called before the first frame update
//        void Start()
//        {
//            grabList = new List<Transform>();
//            handAnim = gameObject.GetComponent<HandAnimation>();
//            handBody = gameObject.GetComponent<Rigidbody>();
//            handCollider = gameObject.GetComponent<Collider>();
//            forearmCollider = transform.parent.gameObject.GetComponent<Collider>();
//            inputData = gameObject.GetComponent<InputData>();
//            wristJoint = gameObject.GetComponent<ConfigurableJoint>();

//            InitializeVariables();
//        }

//        private void InitializeVariables()
//        {
//            if (!NetworkActive())
//            {
//                grip = new LocalVariable<float> { Value = 0f };
//            }
//            else
//            {
//                grip = new NetworkVariableWrapper<float>(new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner));
//            }
//        }
//        private bool CheckOwner()
//        {
//            return false;
//        }
//        private bool NetworkActive()
//        {
//            return (this is NetworkBehaviour);
//        }

//        // Update is called once per frame
//        void Update()
//        {
//            //chack for phantom grips
//            if (joint == null && gripping == true)
//            {
//                DestroyPhantomGrip();
//            }

//            //yoink controller values
//            float localGrip = (isRightController) ? GetRightGrip() : GetLeftGrip();
//            if (NonNetworkOveride || CheckOwner())
//            {
//                grip.Value = localGrip;
//            }
//            if (falseGrip == true)
//            {
//                grip.Value = 1f;
//            }

//            //try to create grips
//            if (grip.Value > 0.85f && !gripping && !clenched)
//            {
//                if (grip.Value > 0.975f)
//                {
//                    clenched = true;
//                }

//                //CullGrabList();//make sure there arent any artifacts of deleted objects
//                if (grabList.Count != 0)
//                {
//                    CreateGrip();
//                }
//            }

//            //try to detroy grips
//            if (grip.Value < 0.85f)
//            {
//                clenched = false;
//                if (gripping)
//                {
//                    DestroyGrip();
//                }
//            }

//            //if(joint != null && joint.currentForce.magnitude > 9999f) //currentFOrce only works one "limited" joints(works well now but may acause problems later)
//            //{
//            //    Debug.Log(joint.currentForce.magnitude);
//            //    DestroyGrip();
//            //}
//        }

//        private void CreateGrip()
//        {
//            Transform closestPoint = FindClosestGrabPoint(grabList, handBody.transform);

//            //make sure you arent trying to grab a deleted object
//            while (closestPoint == null)
//            {
//                if (grabList.Count <= 0)
//                {
//                    return; //all grips were deleted
//                }

//                CullGrabList();
//                closestPoint = FindClosestGrabPoint(grabList, handBody.transform);
//            }

//            GrabPoint grabPoint = closestPoint.GetComponent<GrabPoint>();
//            gripping = true;
//            grabbedPoint = grabPoint;
//            grabbedPoint.Grabbed = true;
//            grabbedPoint.IsRightController = isRightController;

//            //change hand animation state
//            handAnim.Gripping = true;
//            handAnim.GripState = grabPoint.GrabType;

//            //form a joint with that object
//            joint = grabPoint.ParentTrans.gameObject.AddComponent<ConfigurableJoint>();

//            //position
//            joint.configuredInWorldSpace = true;
//            joint.connectedBody = handBody;
//            joint.autoConfigureConnectedAnchor = false;
//            joint.anchor = new Vector3(0f, 0f, 0f);
//            joint.connectedAnchor = new Vector3(0f, 0f, 0f);
//            joint.targetPosition = grabPoint.GetCurrParentOffset() - 0.1f * grabPoint.transform.up; //hard coded for hand length

//            //rotation
//            joint.targetRotation = grabPoint.GetCurrParentRotationOffset() * grabPoint.ParentTrans.rotation * Quaternion.Inverse(handBody.transform.rotation);

//            //drives
//            if (!grabPoint.SoftGrip)
//            {
//                JointDrive jointDrive = new JointDrive();
//                jointDrive.positionSpring = 99999f;
//                jointDrive.maximumForce = Mathf.Infinity;
//                joint.xDrive = jointDrive;
//                joint.yDrive = jointDrive;
//                joint.zDrive = jointDrive;
//                joint.angularXDrive = jointDrive;
//                joint.angularYZDrive = jointDrive;
//            }
//            else
//            {
//                //JointDrive jointDrive = new JointDrive();
//                //jointDrive.positionSpring = 99999f;
//                //jointDrive.maximumForce = Mathf.Infinity;
//                //joint.xDrive = jointDrive;
//                //joint.yDrive = jointDrive;
//                //joint.zDrive = jointDrive;
//                //joint.angularXDrive = jointDrive;
//                //joint.angularYZDrive = jointDrive;

//                //joint.xMotion = ConfigurableJointMotion.Limited;
//                //joint.yMotion = ConfigurableJointMotion.Limited;
//                //joint.zMotion = ConfigurableJointMotion.Limited;

//                //joint.targetPosition = Vector3.zero;
//                //joint.connectedAnchor = grabPoint.GetCurrParentOffset() + 0.1f * grabPoint.transform.up; //hard coded for hand length
//            }

//            ////configure bodily collisions of the object with layers
//            ////bothHandObject = 10
//            ////leftHandObject = 11
//            ////rightHandObject = 12

//            //configure bodily collisions of the object with collision matricies
//            SetAllToCollision(handCollider, joint.transform, true);
//            SetAllToCollision(forearmCollider, joint.transform, true);

//            //confiure wrist strnegth based on whether or not the grab is two-handed
//            if (this.GetGrabbedObject().Equals(otherHand.GetGrabbedObject())) //twohanded
//            {
//                MakeTwoHandedGrip(true);
//                otherHand.MakeTwoHandedGrip(true);
//            }

//            //play audio
//            AudioSource.PlayClipAtPoint(gripClip, transform.position, 1f);
//        }

//        private void DestroyGrip()
//        {
//            gripping = false;
//            grabbedPoint.Grabbed = false;

//            //change hand animation state
//            handAnim.Gripping = false;

//            //configure bodily collisions of the object with collision matricies
//            SetAllToCollision(handCollider, joint.transform, false);
//            SetAllToCollision(forearmCollider, joint.transform, false);

//            //confiure wrist strnegth based on whether or not the grab is two-handed(should still work as the joint still exisits)
//            MakeTwoHandedGrip(false);
//            if (this.GetGrabbedObject().Equals(otherHand.GetGrabbedObject())) //twohanded
//            {
//                otherHand.MakeTwoHandedGrip(false);
//            }

//            //destroy the joint with the gripped object
//            Destroy(joint);
//            joint = null;

//            //play audio
//            AudioSource.PlayClipAtPoint(ungripClip, transform.position, 1f);
//        }

//        private void DestroyPhantomGrip()
//        {
//            gripping = false;
//            handAnim.Gripping = false;
//            AudioSource.PlayClipAtPoint(ungripClip, transform.position, 1f);
//        }

//        private Transform FindClosestGrabPoint(List<Transform> grabList, Transform hand)
//        {
//            //check for any null values
//            for (int i = 0; i < grabList.Count; i++)
//            {
//                if (grabList[i].Equals(null))
//                {
//                    return null; //return null so the the CreateGrip while loop handles it
//                }
//            }

//            Vector3 handPoint = hand.position + 0.1f * hand.up; //hard coded hand length
//            Quaternion handRotation = hand.rotation;

//            //search through the list for the closest grab point
//            int bestIndex = 0;
//            for (int i = 0; i < grabList.Count; i++)
//            {
//                float bestEvaluation = GrabPointEvaluator((grabList[bestIndex].position - handPoint).magnitude, Quaternion.Angle(grabList[bestIndex].rotation, handRotation));
//                float thisEvaluation = GrabPointEvaluator((grabList[i].position - handPoint).magnitude, Quaternion.Angle(grabList[i].rotation, handRotation));
//                if (thisEvaluation > bestEvaluation)
//                {
//                    bestIndex = i;
//                }
//            }
//            return grabList[bestIndex];
//        }

//        private void CullGrabList()
//        {
//            //remove null grabPoints
//            for (int i = 0; i < grabList.Count; i++)
//            {
//                if (grabList[i].Equals(null))
//                {
//                    grabList.RemoveAt(i);
//                }
//            }
//        }

//        private float GrabPointEvaluator(float distance, float angleDistance) //returns valuer between 0 and 1 that dictates point fitness
//        {
//            //evalute distance
//            //Debug.Log("distance: " + distance);
//            float distanceFitness = (1f / 0.1f) * (0.1f - distance); //the 0.1f is hard coded as the max distance from hand to grabPoint

//            //evaluate rotation
//            //Debug.Log("angle: " + angleDistance);
//            float angleFitness = 1f - (angleDistance / 180f); //compress to between zero and one

//            //synthesize the distance and rotation values into one value between zero and one
//            float synthesisFitness = (distanceFitness * 1f + angleFitness * 1f) / 2f;
//            //Debug.Log("distance: " + distanceFitness + " + angle: " + angleFitness + " = fitness: " + synthesisFitness);

//            return synthesisFitness;
//        }

//        private void OnTriggerEnter(Collider other)
//        {
//            if (other.GetComponent<GrabPoint>() != null)
//            {
//                grabList.Add(other.transform);
//            }
//            if (other.GetComponent<GrabPlane>() != null)
//            {
//                grabList.Add(other.GetComponent<GrabPlane>().FollowHand(transform));
//            }
//        }
//        private void OnTriggerExit(Collider other)
//        {
//            if (other.GetComponent<GrabPoint>() != null)
//            {
//                grabList.Remove(other.transform);
//            }
//            if (other.GetComponent<GrabPlane>() != null)
//            {
//                grabList.Remove(other.GetComponent<GrabPlane>().UnfollowHand(transform));
//            }
//        }

//        private float GetRightGrip()
//        {
//            if (inputData.rightController.TryGetFeatureValue(CommonUsages.grip, out float controllerGrip))
//            {
//                return controllerGrip;
//            }
//            return 0f;
//        }

//        private float GetLeftGrip()
//        {
//            if (inputData.leftController.TryGetFeatureValue(CommonUsages.grip, out float controllerGrip))
//            {
//                return controllerGrip;
//            }
//            return 0f;
//        }

//        private void SetAllToLayer(Transform parent, int layer)
//        {
//            parent.gameObject.layer = layer;
//            for (int i = 0; i < parent.childCount; i++)
//            {
//                SetAllToLayer(parent.GetChild(i), layer);
//            }
//        }

//        private void SetAllToCollision(Collider grabberCollider, Transform parent, bool collisionState)
//        {
//            if (parent.gameObject.TryGetComponent(out Collider objectCollider))
//            {
//                Physics.IgnoreCollision(grabberCollider, objectCollider, collisionState);
//            }
//            for (int i = 0; i < parent.childCount; i++)
//            {
//                SetAllToCollision(grabberCollider, parent.GetChild(i), collisionState);
//            }
//        }

//        public void MakeTwoHandedGrip(bool isTwoHanded) //makes the joint much weaker if twohanded
//        {
//            JointDrive jointDrive = wristJoint.slerpDrive;
//            if (isTwoHanded && !twoHanded)
//            {
//                jointDrive.positionSpring = jointDrive.positionSpring / 100f;
//                twoHanded = true;
//            }
//            else if (twoHanded)
//            {
//                jointDrive.positionSpring = jointDrive.positionSpring * 100f;
//                twoHanded = false;
//            }

//            wristJoint.angularXDrive = jointDrive;
//        }

//        public GameObject GetGrabbedObject()
//        {
//            if (joint != null)
//            {
//                return joint.gameObject;
//            }
//            else
//            {
//                return null; //hopefully shouldnt create error
//            }
//        }
//    }

//    [RequireComponent(typeof(InputData))]
//    public class GripControllerNetwork : NetworkBehaviour, GripControllerInterface
//    {
//        [SerializeField] private bool isRightController = false;
//        [SerializeField] private bool falseGrip = false;
//        [SerializeField] private AudioClip gripClip;
//        [SerializeField] private AudioClip ungripClip;
//        [SerializeField] private bool NonNetworkOveride = false;
//        [SerializeField] private GripControllerInterface otherHand;

//        private IVariable<float> grip;
//        private bool gripping = false;
//        private bool clenched = false;
//        private bool twoHanded = false;

//        private ConfigurableJoint joint;
//        private ConfigurableJoint wristJoint;
//        private GrabPoint grabbedPoint;
//        private List<Transform> grabList;
//        private HandAnimation handAnim;
//        private Rigidbody handBody;
//        private Collider handCollider;
//        private Collider forearmCollider;
//        private InputData inputData;
//        // Start is called before the first frame update
//        void Start()
//        {
//            grabList = new List<Transform>();
//            handAnim = gameObject.GetComponent<HandAnimation>();
//            handBody = gameObject.GetComponent<Rigidbody>();
//            handCollider = gameObject.GetComponent<Collider>();
//            forearmCollider = transform.parent.gameObject.GetComponent<Collider>();
//            inputData = gameObject.GetComponent<InputData>();
//            wristJoint = gameObject.GetComponent<ConfigurableJoint>();

//            InitializeVariables();
//        }

//        private void InitializeVariables()
//        {
//            if (!NetworkActive())
//            {
//                grip = new LocalVariable<float> { Value = 0f };
//            }
//            else
//            {
//                grip = new NetworkVariableWrapper<float>(new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner));
//            }
//        }
//        private bool CheckOwner()
//        {
//            return IsOwner;
//        }
//        private bool NetworkActive()
//        {
//            return (this is NetworkBehaviour);
//        }

//        // Update is called once per frame
//        void Update()
//        {
//            //chack for phantom grips
//            if (joint == null && gripping == true)
//            {
//                DestroyPhantomGrip();
//            }

//            //yoink controller values
//            float localGrip = (isRightController) ? GetRightGrip() : GetLeftGrip();
//            if (NonNetworkOveride || CheckOwner())
//            {
//                grip.Value = localGrip;
//            }
//            if (falseGrip == true)
//            {
//                grip.Value = 1f;
//            }

//            //try to create grips
//            if (grip.Value > 0.85f && !gripping && !clenched)
//            {
//                if (grip.Value > 0.975f)
//                {
//                    clenched = true;
//                }

//                //CullGrabList();//make sure there arent any artifacts of deleted objects
//                if (grabList.Count != 0)
//                {
//                    CreateGrip();
//                }
//            }

//            //try to detroy grips
//            if (grip.Value < 0.85f)
//            {
//                clenched = false;
//                if (gripping)
//                {
//                    DestroyGrip();
//                }
//            }

//            //if(joint != null && joint.currentForce.magnitude > 9999f) //currentFOrce only works one "limited" joints(works well now but may acause problems later)
//            //{
//            //    Debug.Log(joint.currentForce.magnitude);
//            //    DestroyGrip();
//            //}
//        }

//        private void CreateGrip()
//        {
//            Transform closestPoint = FindClosestGrabPoint(grabList, handBody.transform);

//            //make sure you arent trying to grab a deleted object
//            while (closestPoint == null)
//            {
//                if (grabList.Count <= 0)
//                {
//                    return; //all grips were deleted
//                }

//                CullGrabList();
//                closestPoint = FindClosestGrabPoint(grabList, handBody.transform);
//            }

//            GrabPoint grabPoint = closestPoint.GetComponent<GrabPoint>();
//            gripping = true;
//            grabbedPoint = grabPoint;
//            grabbedPoint.Grabbed = true;
//            grabbedPoint.IsRightController = isRightController;

//            //change hand animation state
//            handAnim.Gripping = true;
//            handAnim.GripState = grabPoint.GrabType;

//            //form a joint with that object
//            joint = grabPoint.ParentTrans.gameObject.AddComponent<ConfigurableJoint>();

//            //position
//            joint.configuredInWorldSpace = true;
//            joint.connectedBody = handBody;
//            joint.autoConfigureConnectedAnchor = false;
//            joint.anchor = new Vector3(0f, 0f, 0f);
//            joint.connectedAnchor = new Vector3(0f, 0f, 0f);
//            joint.targetPosition = grabPoint.GetCurrParentOffset() - 0.1f * grabPoint.transform.up; //hard coded for hand length

//            //rotation
//            joint.targetRotation = grabPoint.GetCurrParentRotationOffset() * grabPoint.ParentTrans.rotation * Quaternion.Inverse(handBody.transform.rotation);

//            //drives
//            if (!grabPoint.SoftGrip)
//            {
//                JointDrive jointDrive = new JointDrive();
//                jointDrive.positionSpring = 99999f;
//                jointDrive.maximumForce = Mathf.Infinity;
//                joint.xDrive = jointDrive;
//                joint.yDrive = jointDrive;
//                joint.zDrive = jointDrive;
//                joint.angularXDrive = jointDrive;
//                joint.angularYZDrive = jointDrive;
//            }
//            else
//            {
//                //JointDrive jointDrive = new JointDrive();
//                //jointDrive.positionSpring = 99999f;
//                //jointDrive.maximumForce = Mathf.Infinity;
//                //joint.xDrive = jointDrive;
//                //joint.yDrive = jointDrive;
//                //joint.zDrive = jointDrive;
//                //joint.angularXDrive = jointDrive;
//                //joint.angularYZDrive = jointDrive;

//                //joint.xMotion = ConfigurableJointMotion.Limited;
//                //joint.yMotion = ConfigurableJointMotion.Limited;
//                //joint.zMotion = ConfigurableJointMotion.Limited;

//                //joint.targetPosition = Vector3.zero;
//                //joint.connectedAnchor = grabPoint.GetCurrParentOffset() + 0.1f * grabPoint.transform.up; //hard coded for hand length
//            }

//            ////configure bodily collisions of the object with layers
//            ////bothHandObject = 10
//            ////leftHandObject = 11
//            ////rightHandObject = 12

//            //configure bodily collisions of the object with collision matricies
//            SetAllToCollision(handCollider, joint.transform, true);
//            SetAllToCollision(forearmCollider, joint.transform, true);

//            //confiure wrist strnegth based on whether or not the grab is two-handed
//            if (this.GetGrabbedObject().Equals(otherHand.GetGrabbedObject())) //twohanded
//            {
//                MakeTwoHandedGrip(true);
//                otherHand.MakeTwoHandedGrip(true);
//            }

//            //play audio
//            AudioSource.PlayClipAtPoint(gripClip, transform.position, 1f);
//        }

//        private void DestroyGrip()
//        {
//            gripping = false;
//            grabbedPoint.Grabbed = false;

//            //change hand animation state
//            handAnim.Gripping = false;

//            //configure bodily collisions of the object with collision matricies
//            SetAllToCollision(handCollider, joint.transform, false);
//            SetAllToCollision(forearmCollider, joint.transform, false);

//            //confiure wrist strnegth based on whether or not the grab is two-handed(should still work as the joint still exisits)
//            MakeTwoHandedGrip(false);
//            if (this.GetGrabbedObject().Equals(otherHand.GetGrabbedObject())) //twohanded
//            {
//                otherHand.MakeTwoHandedGrip(false);
//            }

//            //destroy the joint with the gripped object
//            Destroy(joint);
//            joint = null;

//            //play audio
//            AudioSource.PlayClipAtPoint(ungripClip, transform.position, 1f);
//        }

//        private void DestroyPhantomGrip()
//        {
//            gripping = false;
//            handAnim.Gripping = false;
//            AudioSource.PlayClipAtPoint(ungripClip, transform.position, 1f);
//        }

//        private Transform FindClosestGrabPoint(List<Transform> grabList, Transform hand)
//        {
//            //check for any null values
//            for (int i = 0; i < grabList.Count; i++)
//            {
//                if (grabList[i].Equals(null))
//                {
//                    return null; //return null so the the CreateGrip while loop handles it
//                }
//            }

//            Vector3 handPoint = hand.position + 0.1f * hand.up; //hard coded hand length
//            Quaternion handRotation = hand.rotation;

//            //search through the list for the closest grab point
//            int bestIndex = 0;
//            for (int i = 0; i < grabList.Count; i++)
//            {
//                float bestEvaluation = GrabPointEvaluator((grabList[bestIndex].position - handPoint).magnitude, Quaternion.Angle(grabList[bestIndex].rotation, handRotation));
//                float thisEvaluation = GrabPointEvaluator((grabList[i].position - handPoint).magnitude, Quaternion.Angle(grabList[i].rotation, handRotation));
//                if (thisEvaluation > bestEvaluation)
//                {
//                    bestIndex = i;
//                }
//            }
//            return grabList[bestIndex];
//        }

//        private void CullGrabList()
//        {
//            //remove null grabPoints
//            for (int i = 0; i < grabList.Count; i++)
//            {
//                if (grabList[i].Equals(null))
//                {
//                    grabList.RemoveAt(i);
//                }
//            }
//        }

//        private float GrabPointEvaluator(float distance, float angleDistance) //returns valuer between 0 and 1 that dictates point fitness
//        {
//            //evalute distance
//            //Debug.Log("distance: " + distance);
//            float distanceFitness = (1f / 0.1f) * (0.1f - distance); //the 0.1f is hard coded as the max distance from hand to grabPoint

//            //evaluate rotation
//            //Debug.Log("angle: " + angleDistance);
//            float angleFitness = 1f - (angleDistance / 180f); //compress to between zero and one

//            //synthesize the distance and rotation values into one value between zero and one
//            float synthesisFitness = (distanceFitness * 1f + angleFitness * 1f) / 2f;
//            //Debug.Log("distance: " + distanceFitness + " + angle: " + angleFitness + " = fitness: " + synthesisFitness);

//            return synthesisFitness;
//        }

//        private void OnTriggerEnter(Collider other)
//        {
//            if (other.GetComponent<GrabPoint>() != null)
//            {
//                grabList.Add(other.transform);
//            }
//            if (other.GetComponent<GrabPlane>() != null)
//            {
//                grabList.Add(other.GetComponent<GrabPlane>().FollowHand(transform));
//            }
//        }
//        private void OnTriggerExit(Collider other)
//        {
//            if (other.GetComponent<GrabPoint>() != null)
//            {
//                grabList.Remove(other.transform);
//            }
//            if (other.GetComponent<GrabPlane>() != null)
//            {
//                grabList.Remove(other.GetComponent<GrabPlane>().UnfollowHand(transform));
//            }
//        }

//        private float GetRightGrip()
//        {
//            if (inputData.rightController.TryGetFeatureValue(CommonUsages.grip, out float controllerGrip))
//            {
//                return controllerGrip;
//            }
//            return 0f;
//        }

//        private float GetLeftGrip()
//        {
//            if (inputData.leftController.TryGetFeatureValue(CommonUsages.grip, out float controllerGrip))
//            {
//                return controllerGrip;
//            }
//            return 0f;
//        }

//        private void SetAllToLayer(Transform parent, int layer)
//        {
//            parent.gameObject.layer = layer;
//            for (int i = 0; i < parent.childCount; i++)
//            {
//                SetAllToLayer(parent.GetChild(i), layer);
//            }
//        }

//        private void SetAllToCollision(Collider grabberCollider, Transform parent, bool collisionState)
//        {
//            if (parent.gameObject.TryGetComponent(out Collider objectCollider))
//            {
//                Physics.IgnoreCollision(grabberCollider, objectCollider, collisionState);
//            }
//            for (int i = 0; i < parent.childCount; i++)
//            {
//                SetAllToCollision(grabberCollider, parent.GetChild(i), collisionState);
//            }
//        }

//        public void MakeTwoHandedGrip(bool isTwoHanded) //makes the joint much weaker if twohanded
//        {
//            JointDrive jointDrive = wristJoint.slerpDrive;
//            if (isTwoHanded && !twoHanded)
//            {
//                jointDrive.positionSpring = jointDrive.positionSpring / 100f;
//                twoHanded = true;
//            }
//            else if (twoHanded)
//            {
//                jointDrive.positionSpring = jointDrive.positionSpring * 100f;
//                twoHanded = false;
//            }

//            wristJoint.angularXDrive = jointDrive;
//        }

//        public GameObject GetGrabbedObject()
//        {
//            if (joint != null)
//            {
//                return joint.gameObject;
//            }
//            else
//            {
//                return null; //hopefully shouldnt create error
//            }
//        }
//    }
//}
