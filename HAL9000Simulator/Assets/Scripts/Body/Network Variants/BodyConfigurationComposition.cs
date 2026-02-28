//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.XR;
//using UnityEngine.Rendering;
//using Unity.Netcode;

//namespace Rekabsen
//{
//    public interface IBodyConfiguration
//    {
//        public Vector3 HipOffset(); 
//    }

//    public class SingleplayerBodyConfiguration: MonoBehaviour, IBodyConfiguration
//    {
//        [SerializeField] private Transform anchor;
//        [SerializeField] private Transform hip;
//        [SerializeField] private Transform head;
//        [SerializeField] private Transform headCamera;
//        [SerializeField] private Transform headset;
//        [SerializeField] private Transform legs;
//        [SerializeField] private float playerHeightMax = 1.90f;
//        [SerializeField] private float torsoLengthMax = 0.625f;
//        [SerializeField] private float rotoBallRadius = 0.2f;
//        [SerializeField] private float anchorOriginHeight = 0.2f;
//        [SerializeField] private float crouchSpeed = 1f;
//        [SerializeField] private float jumpDepth = 1f;
//        [SerializeField] private float turnSpeed = 120f;
//        [SerializeField] private float nerdNeckMin = 0.4f;
//        [SerializeField] private float nerdNeckMax = 0.4f;
//        [SerializeField] private Transform ragdollHead;

//        private InputData inputData;
//        private ConfigurableJoint hipJoint;
//        private ConfigurableJoint headJoint;
//        private ConfigurableJoint legJoint;

//        //"virtual" body values
//        private float torsoHeight;
//        private float playerHeight;
//        private float hipHeight;
//        private float nerdNeck;

//        private IVariable<float> virtualCrouch;
//        private IVariable<float> jumpCrouch;
//        private IVariable<float> turn;
//        private IVariable<Quaternion> headRotation;

//        // Start is called before the first frame update
//        void Start()
//        {
//            inputData = GetComponent<InputData>();
//            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;

//            hipJoint = hip.gameObject.GetComponent<ConfigurableJoint>();
//            headJoint = head.gameObject.GetComponent<ConfigurableJoint>();
//            legJoint = legs.gameObject.GetComponent<ConfigurableJoint>();

//            hip.gameObject.GetComponent<Rigidbody>().sleepThreshold = 0f;
//            hipHeight = rotoBallRadius / 4;

//            InitializeVariables();
//        }

//        private void InitializeVariables()
//        {
//            if (!NetworkActive())
//            {
//                virtualCrouch = new LocalVariable<float> { Value = 0f };
//                jumpCrouch = new LocalVariable<float> { Value = 0f };
//                turn = new LocalVariable<float> { Value = 0f };
//                headRotation = new LocalVariable<Quaternion> { Value = Quaternion.identity };
//            }
//            else
//            {
//                virtualCrouch = new NetworkVariableWrapper<float>(new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner));
//                jumpCrouch = new NetworkVariableWrapper<float>(new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner));
//                turn = new NetworkVariableWrapper<float>(new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner));
//                headRotation = new NetworkVariableWrapper<Quaternion>(new NetworkVariable<Quaternion>(Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner));
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

//        private void OnDisable()
//        {
//            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
//        }

//        // Update is called once per frame
//        void Update()
//        {
//            if (!NetworkActive() || CheckOwner()) { VirtualCrouch(); }
//            BodilyCaltulations();
//            PhysicsHead();
//            PhysicsHip();
//            PhysicsLegs();
//        }
//        void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
//        {
//            BodilyCaltulations();
//            if (!NetworkActive() || CheckOwner()) { VirtualTurn(); }
//            PhysicsHead();
//            PhysicsHip();
//            PhysicsLegs();
//        }

//        private void BodilyCaltulations()
//        {
//            //fetch player's input height using the virtualcrouch
//            playerHeight = headset.localPosition.y + virtualCrouch.Value;

//            //torso/head offset calculations
//            torsoHeight = torsoLengthMax; //for now
//            nerdNeck = nerdNeckMin; //for now


//            ////use height(actual) to manipulate nerneck and torso length proportionatly @TODO major adjustments with if statements
//            nerdNeck = Mathf.Clamp(nerdNeckMin + nerdNeckMax * (1f - playerHeight / playerHeightMax) * 1f, nerdNeckMin, nerdNeckMax); //needs tweeking
//                                                                                                                                      //torsoHeight = Mathf.Clamp(torsoHeight - torsoHeight * (playerHeightMax - playerHeight / playerHeightMax) * 0.3333f, torsoLengthMax * 0.333f, torsoLengthMax); //needs tweeking
//            torsoHeight = Mathf.Sqrt(torsoLengthMax * torsoLengthMax - nerdNeck * nerdNeck); // pythagorean therom;
//                                                                                             ////Debug.Log("nerd neck: " + nerdNeck + " Torso Height: " + torsoHeight + " TorsoLength: " + Mathf.Sqrt(torsoHeight * torsoHeight + nerdNeck * nerdNeck));

//            //use head rotation to manipulate hip/head(leg/torso) height ratio height and nerneck
//            float headRoll = headset.localEulerAngles.x;
//            if (headRoll >= 0f && headRoll <= 90f)
//            {
//                torsoHeight = torsoHeight - (Mathf.Abs(headRoll / 90f) * torsoHeight / 3f); //needs tweeking
//                nerdNeck = nerdNeck - (Mathf.Abs(headRoll / 90f) * nerdNeck); //needs tweeking
//            }
//            //Debug.Log("Head Roll: " + headRoll + "Herd Neck: " + nerdNeck);

//            //keep the hipHeight in a range beteen the ground and max height possible(-torso)
//            if (playerHeight <= playerHeightMax && playerHeight - torsoHeight - anchorOriginHeight >= 0f) //remove "rotoball radius/4" if want even lower crouch
//            {
//                hipHeight = playerHeight - torsoHeight;
//            }
//        }
//        private void PhysicsHead()
//        {
//            //update the head's rotation to be same as headset
//            if (!NetworkActive() || CheckOwner())
//            {
//                headRotation.Value = Quaternion.Euler(0f, turn.Value, 0f) *
//                    ((inputData.headset.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion headsetRotation)) ? headsetRotation : headCamera.rotation);
//            }
//            headCamera.rotation = headRotation.Value;

//            //float xNerdNeck = Mathf.Sin(Mathf.Deg2Rad * headCamera.eulerAngles.y) * nerdNeck; // using the transform rotation may be unwise as it may be "slow"/behind
//            //float zNerdNeck = Mathf.Cos(Mathf.Deg2Rad * headCamera.eulerAngles.y) * nerdNeck; // using the transform rotation may be unwise as it may be "slow"/behind
//            headJoint.connectedAnchor = new Vector3(0f * 10f, torsoHeight * 10f, nerdNeck * 10f);
//        }
//        private void PhysicsHip()
//        {
//            hipJoint.connectedAnchor = new Vector3(0f, (hipHeight - rotoBallRadius) * 10f, 0f);

//            //calculate hip y rotation
//            if (!(ragdollHead.eulerAngles.x > 45f && ragdollHead.eulerAngles.x <= 90f) || !(ragdollHead.eulerAngles.z > 45f && ragdollHead.eulerAngles.z <= 90f) || true)  // make sure not looking down too much, else must lerp
//            {
//                //Debug.Log("problem 1");
//                //hip.rotation = Quaternion.Euler
//                //(0f,
//                //(Quaternion.Euler(0f, turn, 0f) * ((inputData.headset.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion headsetRotation)) ? headsetRotation : headCamera.rotation)).eulerAngles.y,
//                //0f);
//                hip.rotation = Quaternion.Euler
//                (0f,
//                (Quaternion.Euler(0f, turn.Value, 0f) * ragdollHead.rotation).eulerAngles.y,
//                0f);
//            }
//            else
//            {
//                //do nothing
//            }
//            //else 
//            //{
//            //    //Quaternion newQuaternion = Quaternion.Euler
//            //    //(0f,
//            //    //(Quaternion.Euler(0f, turn, 0f) * ((inputData.headset.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion headsetRotation)) ? headsetRotation : headCamera.rotation)).eulerAngles.y,
//            //    //0f);

//            //    //Debug.Log("detect 1");
//            //    //slow rotation at low angles
//            //    //hip.rotation = Quaternion.Slerp(hip.rotation, newQuaternion, 0.1f * Time.deltaTime);
//            //}
//        }

//        private void PhysicsLegs()
//        {
//            //find the distance between the hip and anchor
//            float legLength = hip.position.y - anchor.position.y;
//            if (legLength > 0f)
//            {
//                //manipulate the leg transform with this information
//                legs.localScale = new Vector3(0.2f, Mathf.Abs(legLength / 2f), 0.2f);
//                legJoint.anchor = new Vector3(0f, (legLength / 2f) * 1f, 0f);
//            }
//            else
//            {
//                legs.localScale = new Vector3(0.0f, 0f, 0.0f);
//                legJoint.connectedAnchor = new Vector3(0f, 0f, 0f);
//            }
//        }

//        private void VirtualTurn()
//        {
//            if (inputData.rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 thumbstick))
//            {
//                if (Mathf.Abs(thumbstick.x) > 0.5f)
//                {
//                    turn.Value += turnSpeed * thumbstick.x * Time.deltaTime;  //delta tiem ought to work, even here

//                }
//            }
//        }

//        private void VirtualCrouch()
//        {
//            inputData.rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 thumbstick);
//            inputData.rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool squating);

//            if (virtualCrouch.Value < 0f)
//            {
//                if (thumbstick.y > 0.1f)
//                {
//                    virtualCrouch.Value += thumbstick.y * crouchSpeed * Time.deltaTime;
//                }
//                if (!squating && jumpCrouch.Value <= 0) //instant
//                {
//                    virtualCrouch.Value -= jumpCrouch.Value;
//                    jumpCrouch.Value = 0f;
//                }
//            }
//            else
//            {
//                virtualCrouch.Value = 0f;
//            }

//            if (playerHeight - torsoHeight >= 0f)
//            {
//                if (thumbstick.y < -0.4f)
//                {
//                    virtualCrouch.Value += thumbstick.y * crouchSpeed * Time.deltaTime;
//                }
//                if (squating && jumpCrouch.Value > jumpDepth)
//                {
//                    virtualCrouch.Value += -crouchSpeed * Time.deltaTime;
//                    jumpCrouch.Value += -crouchSpeed * Time.deltaTime;
//                }
//            }
//            else
//            {
//                if (virtualCrouch.Value - (playerHeight - torsoHeight) < 0f && playerHeight - torsoHeight < 0f)
//                {
//                    virtualCrouch.Value -= (playerHeight - torsoHeight); //in event "mathematical" hip goes too low
//                }

//                //the pplayer is prepped to jump if "bottoms out"
//                if ((playerHeight - virtualCrouch.Value) < Mathf.Abs(jumpDepth) && squating)
//                {
//                    jumpCrouch.Value = jumpDepth;
//                }
//                else if (squating)
//                {
//                    jumpCrouch.Value = -(playerHeight - virtualCrouch.Value);
//                }
//            }
//        }

//        public Vector3 HipOffset() // get the positional difference between the head and hip  
//        {
//            //Quaternion headRotation = ((inputData.headset.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion headsetRotation)) ? headsetRotation : headCamera.rotation);
//            //return hip.position - head.position;
//            return Quaternion.Euler(0f, ragdollHead.eulerAngles.y, 0f) * new Vector3(0f, -torsoHeight, -nerdNeck);
//        }
//    }

//    public class MultiplayerrBodyConfiguration : NetworkBehaviour, IBodyConfiguration
//    {
//        [SerializeField] private Transform anchor;
//        [SerializeField] private Transform hip;
//        [SerializeField] private Transform head;
//        [SerializeField] private Transform headCamera;
//        [SerializeField] private Transform headset;
//        [SerializeField] private Transform legs;
//        [SerializeField] private float playerHeightMax = 1.90f;
//        [SerializeField] private float torsoLengthMax = 0.625f;
//        [SerializeField] private float rotoBallRadius = 0.2f;
//        [SerializeField] private float anchorOriginHeight = 0.2f;
//        [SerializeField] private float crouchSpeed = 1f;
//        [SerializeField] private float jumpDepth = 1f;
//        [SerializeField] private float turnSpeed = 120f;
//        [SerializeField] private float nerdNeckMin = 0.4f;
//        [SerializeField] private float nerdNeckMax = 0.4f;
//        [SerializeField] private Transform ragdollHead;

//        private InputData inputData;
//        private ConfigurableJoint hipJoint;
//        private ConfigurableJoint headJoint;
//        private ConfigurableJoint legJoint;

//        //"virtual" body values
//        private float torsoHeight;
//        private float playerHeight;
//        private float hipHeight;
//        private float nerdNeck;

//        private IVariable<float> virtualCrouch;
//        private IVariable<float> jumpCrouch;
//        private IVariable<float> turn;
//        private IVariable<Quaternion> headRotation;

//        // Start is called before the first frame update
//        void Start()
//        {
//            inputData = GetComponent<InputData>();
//            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;

//            hipJoint = hip.gameObject.GetComponent<ConfigurableJoint>();
//            headJoint = head.gameObject.GetComponent<ConfigurableJoint>();
//            legJoint = legs.gameObject.GetComponent<ConfigurableJoint>();

//            hip.gameObject.GetComponent<Rigidbody>().sleepThreshold = 0f;
//            hipHeight = rotoBallRadius / 4;

//            InitializeVariables();
//        }

//        private void InitializeVariables()
//        {
//            if (!NetworkActive())
//            {
//                virtualCrouch = new LocalVariable<float> { Value = 0f };
//                jumpCrouch = new LocalVariable<float> { Value = 0f };
//                turn = new LocalVariable<float> { Value = 0f };
//                headRotation = new LocalVariable<Quaternion> { Value = Quaternion.identity };
//            }
//            else
//            {
//                virtualCrouch = new NetworkVariableWrapper<float>(new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner));
//                jumpCrouch = new NetworkVariableWrapper<float>(new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner));
//                turn = new NetworkVariableWrapper<float>(new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner));
//                headRotation = new NetworkVariableWrapper<Quaternion>(new NetworkVariable<Quaternion>(Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner));
//            }
//        }

//        protected virtual bool CheckOwner()
//        {
//            return IsOwner;
//        }
//        protected virtual bool NetworkActive()
//        {
//            return (this is NetworkBehaviour);
//        }

//        private void OnDisable()
//        {
//            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
//        }

//        // Update is called once per frame
//        void Update()
//        {
//            if (!NetworkActive() || CheckOwner()) { VirtualCrouch(); }
//            BodilyCaltulations();
//            PhysicsHead();
//            PhysicsHip();
//            PhysicsLegs();
//        }
//        void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
//        {
//            BodilyCaltulations();
//            if (!NetworkActive() || CheckOwner()) { VirtualTurn(); }
//            PhysicsHead();
//            PhysicsHip();
//            PhysicsLegs();
//        }

//        private void BodilyCaltulations()
//        {
//            //fetch player's input height using the virtualcrouch
//            playerHeight = headset.localPosition.y + virtualCrouch.Value;

//            //torso/head offset calculations
//            torsoHeight = torsoLengthMax; //for now
//            nerdNeck = nerdNeckMin; //for now


//            ////use height(actual) to manipulate nerneck and torso length proportionatly @TODO major adjustments with if statements
//            nerdNeck = Mathf.Clamp(nerdNeckMin + nerdNeckMax * (1f - playerHeight / playerHeightMax) * 1f, nerdNeckMin, nerdNeckMax); //needs tweeking
//                                                                                                                                      //torsoHeight = Mathf.Clamp(torsoHeight - torsoHeight * (playerHeightMax - playerHeight / playerHeightMax) * 0.3333f, torsoLengthMax * 0.333f, torsoLengthMax); //needs tweeking
//            torsoHeight = Mathf.Sqrt(torsoLengthMax * torsoLengthMax - nerdNeck * nerdNeck); // pythagorean therom;
//                                                                                             ////Debug.Log("nerd neck: " + nerdNeck + " Torso Height: " + torsoHeight + " TorsoLength: " + Mathf.Sqrt(torsoHeight * torsoHeight + nerdNeck * nerdNeck));

//            //use head rotation to manipulate hip/head(leg/torso) height ratio height and nerneck
//            float headRoll = headset.localEulerAngles.x;
//            if (headRoll >= 0f && headRoll <= 90f)
//            {
//                torsoHeight = torsoHeight - (Mathf.Abs(headRoll / 90f) * torsoHeight / 3f); //needs tweeking
//                nerdNeck = nerdNeck - (Mathf.Abs(headRoll / 90f) * nerdNeck); //needs tweeking
//            }
//            //Debug.Log("Head Roll: " + headRoll + "Herd Neck: " + nerdNeck);

//            //keep the hipHeight in a range beteen the ground and max height possible(-torso)
//            if (playerHeight <= playerHeightMax && playerHeight - torsoHeight - anchorOriginHeight >= 0f) //remove "rotoball radius/4" if want even lower crouch
//            {
//                hipHeight = playerHeight - torsoHeight;
//            }
//        }
//        private void PhysicsHead()
//        {
//            //update the head's rotation to be same as headset
//            if (!NetworkActive() || CheckOwner())
//            {
//                headRotation.Value = Quaternion.Euler(0f, turn.Value, 0f) *
//                    ((inputData.headset.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion headsetRotation)) ? headsetRotation : headCamera.rotation);
//            }
//            headCamera.rotation = headRotation.Value;

//            //float xNerdNeck = Mathf.Sin(Mathf.Deg2Rad * headCamera.eulerAngles.y) * nerdNeck; // using the transform rotation may be unwise as it may be "slow"/behind
//            //float zNerdNeck = Mathf.Cos(Mathf.Deg2Rad * headCamera.eulerAngles.y) * nerdNeck; // using the transform rotation may be unwise as it may be "slow"/behind
//            headJoint.connectedAnchor = new Vector3(0f * 10f, torsoHeight * 10f, nerdNeck * 10f);
//        }
//        private void PhysicsHip()
//        {
//            hipJoint.connectedAnchor = new Vector3(0f, (hipHeight - rotoBallRadius) * 10f, 0f);

//            //calculate hip y rotation
//            if (!(ragdollHead.eulerAngles.x > 45f && ragdollHead.eulerAngles.x <= 90f) || !(ragdollHead.eulerAngles.z > 45f && ragdollHead.eulerAngles.z <= 90f) || true)  // make sure not looking down too much, else must lerp
//            {
//                //Debug.Log("problem 1");
//                //hip.rotation = Quaternion.Euler
//                //(0f,
//                //(Quaternion.Euler(0f, turn, 0f) * ((inputData.headset.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion headsetRotation)) ? headsetRotation : headCamera.rotation)).eulerAngles.y,
//                //0f);
//                hip.rotation = Quaternion.Euler
//                (0f,
//                (Quaternion.Euler(0f, turn.Value, 0f) * ragdollHead.rotation).eulerAngles.y,
//                0f);
//            }
//            else
//            {
//                //do nothing
//            }
//            //else 
//            //{
//            //    //Quaternion newQuaternion = Quaternion.Euler
//            //    //(0f,
//            //    //(Quaternion.Euler(0f, turn, 0f) * ((inputData.headset.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion headsetRotation)) ? headsetRotation : headCamera.rotation)).eulerAngles.y,
//            //    //0f);

//            //    //Debug.Log("detect 1");
//            //    //slow rotation at low angles
//            //    //hip.rotation = Quaternion.Slerp(hip.rotation, newQuaternion, 0.1f * Time.deltaTime);
//            //}
//        }

//        private void PhysicsLegs()
//        {
//            //find the distance between the hip and anchor
//            float legLength = hip.position.y - anchor.position.y;
//            if (legLength > 0f)
//            {
//                //manipulate the leg transform with this information
//                legs.localScale = new Vector3(0.2f, Mathf.Abs(legLength / 2f), 0.2f);
//                legJoint.anchor = new Vector3(0f, (legLength / 2f) * 1f, 0f);
//            }
//            else
//            {
//                legs.localScale = new Vector3(0.0f, 0f, 0.0f);
//                legJoint.connectedAnchor = new Vector3(0f, 0f, 0f);
//            }
//        }

//        private void VirtualTurn()
//        {
//            if (inputData.rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 thumbstick))
//            {
//                if (Mathf.Abs(thumbstick.x) > 0.5f)
//                {
//                    turn.Value += turnSpeed * thumbstick.x * Time.deltaTime;  //delta tiem ought to work, even here

//                }
//            }
//        }

//        private void VirtualCrouch()
//        {
//            inputData.rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 thumbstick);
//            inputData.rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool squating);

//            if (virtualCrouch.Value < 0f)
//            {
//                if (thumbstick.y > 0.1f)
//                {
//                    virtualCrouch.Value += thumbstick.y * crouchSpeed * Time.deltaTime;
//                }
//                if (!squating && jumpCrouch.Value <= 0) //instant
//                {
//                    virtualCrouch.Value -= jumpCrouch.Value;
//                    jumpCrouch.Value = 0f;
//                }
//            }
//            else
//            {
//                virtualCrouch.Value = 0f;
//            }

//            if (playerHeight - torsoHeight >= 0f)
//            {
//                if (thumbstick.y < -0.4f)
//                {
//                    virtualCrouch.Value += thumbstick.y * crouchSpeed * Time.deltaTime;
//                }
//                if (squating && jumpCrouch.Value > jumpDepth)
//                {
//                    virtualCrouch.Value += -crouchSpeed * Time.deltaTime;
//                    jumpCrouch.Value += -crouchSpeed * Time.deltaTime;
//                }
//            }
//            else
//            {
//                if (virtualCrouch.Value - (playerHeight - torsoHeight) < 0f && playerHeight - torsoHeight < 0f)
//                {
//                    virtualCrouch.Value -= (playerHeight - torsoHeight); //in event "mathematical" hip goes too low
//                }

//                //the pplayer is prepped to jump if "bottoms out"
//                if ((playerHeight - virtualCrouch.Value) < Mathf.Abs(jumpDepth) && squating)
//                {
//                    jumpCrouch.Value = jumpDepth;
//                }
//                else if (squating)
//                {
//                    jumpCrouch.Value = -(playerHeight - virtualCrouch.Value);
//                }
//            }
//        }

//        public Vector3 HipOffset() // get the positional difference between the head and hip  
//        {
//            //Quaternion headRotation = ((inputData.headset.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion headsetRotation)) ? headsetRotation : headCamera.rotation);
//            //return hip.position - head.position;
//            return Quaternion.Euler(0f, ragdollHead.eulerAngles.y, 0f) * new Vector3(0f, -torsoHeight, -nerdNeck);
//        }
//    }
//}