using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Rendering;

namespace Rekabsen
{
    public class RigXRTransformer : MonoBehaviour
    {
        [SerializeField] private Transform headset;
        [SerializeField] private Transform leftController;
        [SerializeField] private Transform rightController;
        [SerializeField] private Transform head;
        [SerializeField] private Transform anchor;
        [SerializeField] private float turnSpeed = 120f;

        public float Scale { get; set; }

        private InputData inputData;
        private float turn;
        private Vector3 lastHeadsetPosition;
        private Vector3 headsetStack;

        //height saving and loading stuff
        private const string PlayerHeightKey = "PlayerHeight";
        public float PlayerHeight { get; set; }
        [SerializeField] private float modelHeight = 190f;
        [SerializeField] private RagdollXRTransformer ragdollTransformer;
        [SerializeField] private Transform headCamera;


        // Start is called before the first frame update
        void OnEnable()
        {
            inputData = GetComponent<InputData>();
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            lastHeadsetPosition = new Vector3(0f, 0f, 0f);
            headsetStack = new Vector3(0f, 0f, 0f);


            Scale = 1f;
        }

        private void Start()
        {
            PlayerHeight = GetPlayerHeight();
            ConfigurePlayerHeight();
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

        // Update is called once per frame
        void Update()
        {
            //TransformDevices();
        }
        void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            TransformDevices();
        }

        void TransformDevices()
        {
            //headset
            if (inputData.headset.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 headsetPosition))
            {
                headsetPosition *= Scale;
                Vector3 deltaPosition = (headsetPosition - lastHeadsetPosition);
                headsetStack = headsetStack + Quaternion.Euler(0f, turn, 0f) * deltaPosition;
                headset.position = headsetStack + new Vector3(anchor.position.x, anchor.position.y, anchor.position.z);
                lastHeadsetPosition = headsetPosition;
            }
            if (inputData.headset.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion headsetRotation))
            {
                //headset.localRotation = Quaternion.Euler(0f, turn, 0f) * headsetRotation;
            }

            //left controller
            if (inputData.leftController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 leftPosition))
            {
                leftPosition *= Scale;
                leftController.position = (Quaternion.Euler(0f, turn, 0f) *
                    new Vector3(leftPosition.x - headsetPosition.x,
                    leftPosition.y - headsetPosition.y,
                    leftPosition.z - headsetPosition.z)) + head.position;
            }
            if (inputData.leftController.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion leftRotation))
            {
                leftController.localRotation = Quaternion.Euler(0f, turn, 0f) * leftRotation;
            }

            //rightController
            if (inputData.rightController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 rightPosition))
            {
                rightPosition *= Scale;
                rightController.position = (Quaternion.Euler(0f, turn, 0f) *
                    new Vector3(rightPosition.x - headsetPosition.x,
                    rightPosition.y - headsetPosition.y,
                    rightPosition.z - headsetPosition.z)) + head.position;
            }
            if (inputData.rightController.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rightRotation))
            {
                rightController.localRotation = Quaternion.Euler(0f, turn, 0f) * rightRotation;
            }

            if (inputData.rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 thumbstick))
            {
                if (Mathf.Abs(thumbstick.x) > 0.5f)
                {
                    turn += turnSpeed * thumbstick.x * Time.deltaTime;  //delta tiem ought to work, even here

                }
            }
        }

        private void ConfigurePlayerHeight()
        {
            //set the scale in accordance to the set height
            float scale = PlayerHeight / modelHeight;
            this.Scale = 1f / scale;
            ragdollTransformer.Scale = scale;
            headCamera.localScale = new Vector3(1f / scale, 1f / scale, 1f / scale) * 10f;

            //record the height
            PlayerPrefs.SetFloat(PlayerHeightKey, PlayerHeight);
            PlayerPrefs.Save(); // Ensure the data is written to disk
            Debug.Log("Player height set to: " + PlayerHeight);
        }

        private float GetPlayerHeight()
        {
            if (PlayerPrefs.HasKey(PlayerHeightKey))
            {
                return PlayerPrefs.GetFloat(PlayerHeightKey);
            }
            else
            {
                //record the height
                PlayerPrefs.SetFloat(PlayerHeightKey, 175f);
                PlayerPrefs.Save(); // Ensure the data is written to disk
                Debug.Log("Player height set to: " + 175f);

                return 175f; // Or any default value or error indicator
            }
        }
    }
}