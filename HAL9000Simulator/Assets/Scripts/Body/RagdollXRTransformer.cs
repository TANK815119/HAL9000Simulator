using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Rendering;

namespace Rekabsen
{
    public class RagdollXRTransformer : MonoBehaviour
    {
        [SerializeField] private Transform headset;
        [SerializeField] private Transform leftController;
        [SerializeField] private Transform rightController;
        [SerializeField] private Transform hipTarget;
        [SerializeField] private MonoBehaviour bodyConfiguration;
        private BodyConfigurationInterface bodyConfig;
        [SerializeField] private ConfigurableJoint joint;

        public float Scale { get; set; }

        private InputData inputData;
        // Start is called before the first frame update
        void OnEnable()
        {
            bodyConfig = (BodyConfigurationInterface)bodyConfiguration;
            inputData = GetComponent<InputData>();
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;

            Scale = 1f;
        }
        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

        void Update()
        {
            UpdateHipTarget();
            //ragdollCalculations.localScale = new Vector3(Scale, Scale, Scale);
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
                headset.localPosition = headsetPosition * (1f / Scale);
                //hipTarget.position = headsetPosition + headset.parent.position + bodyConfig.HipOffset();

            }
            if (inputData.headset.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion headsetRotation))
            {
                bool angleLimited = false;
                float angle = headsetRotation.eulerAngles.x;
                if (headsetRotation.eulerAngles.x > 45f && headsetRotation.eulerAngles.x <= 90f)
                {
                    headsetRotation = Quaternion.Euler(0f, headsetRotation.eulerAngles.y, headsetRotation.eulerAngles.z);
                    angleLimited = true;
                }

                headsetRotation = Quaternion.Euler(headsetRotation.eulerAngles.x, headsetRotation.eulerAngles.y, 0f);
                //if (headsetRotation.eulerAngles.z > 45f && headsetRotation.eulerAngles.z <= 90f)
                //{
                //    //Debug.Log("limiting 2");
                //    headsetRotation = Quaternion.Euler(headsetRotation.eulerAngles.x, headsetRotation.eulerAngles.y, 0f);
                //    angleLimited = true;
                //}

                if (angleLimited || true) //true to avoid snapping
                {
                    //float yDifference = Quaternion.Angle(Quaternion.Euler(0f, headset.eulerAngles.y, 0f), Quaternion.Euler(0f, headsetRotation.eulerAngles.y, 0f));
                    //headset.localRotation = Quaternion.Euler(new Vector3(headsetRotation.eulerAngles.x,
                    //    Mathf.Lerp(headset.eulerAngles.y, headset.eulerAngles.y + yDifference, yDifference / 90f * Time.deltaTime),
                    //    headsetRotation.eulerAngles.z));
                    //headset.localRotation = Quaternion.Euler(headsetRotation.eulerAngles.x, headsetRotation.eulerAngles.y, headsetRotation.eulerAngles.z);
                    headset.localRotation = Quaternion.Slerp(headset.transform.localRotation, headsetRotation, Mathf.Pow(2f * ((90f - angle) / 45f), 2f) * Time.deltaTime);
                    //Debug.Log(headset.eulerAngles);
                }
                else
                {
                    //Debug.Log("problem 2");
                    headset.localRotation = headsetRotation;
                }

                //headset.localRotation = headsetRotation;
                //hipTarget.rotation = Quaternion.Euler(0f, headset.eulerAngles.y, 0f);
            }
            //make sure the head doesnt go over the rotation limit
            //if (headset.localRotation.eulerAngles.x >= headRotationLimit)
            //{
            //    Debug.Log("limiting");
            //    headset.localRotation = Quaternion.Euler(75f, headset.localRotation.eulerAngles.y, headset.localRotation.eulerAngles.z);
            //}

            //left controller
            if (inputData.leftController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 leftPosition))
            {
                leftController.localPosition = leftPosition * (1f / Scale);
            }
            if (inputData.leftController.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion leftRotation))
            {
                leftController.localRotation = leftRotation;
            }

            //rightController
            if (inputData.rightController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 rightPosition))
            {
                rightController.localPosition = rightPosition * (1f / Scale);
            }
            if (inputData.rightController.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rightRotation))
            {
                rightController.localRotation = rightRotation;
            }
        }
        private void UpdateHipTarget()
        {
            //hip position
            Vector3 hipOffset = bodyConfig.HipOffset();
            //hipTarget.position = Vector3.Lerp(hipTarget.position, headset.position + hipOffset, 0.01f * Time.deltaTime);

            if (!(headset.eulerAngles.x > 45f && headset.eulerAngles.x <= 90f) && !(headset.eulerAngles.z > 45f && headset.eulerAngles.z <= 90f) || true)  // make sure not looking down too much, else must lerp
            {
                //Debug.Log("problem 3");
                hipTarget.position = headset.position + hipOffset;
                hipTarget.rotation = Quaternion.Euler(0f, headset.eulerAngles.y, 0f);
            }
            //else
            //{
            //    Debug.Log("detect 2");
            //    //hipTarget.position = Vector3.Lerp(hipTarget.position, headset.position + hipOffset, 0.01f * Time.deltaTime);
            //}

            //hip rotation
            //float anteriorPelvicTilt = Mathf.Rad2Deg * Mathf.Atan2(hipOffset.y, hipOffset.z);
            //hipTarget.rotation = Quaternion.Euler(0f, headset.eulerAngles.y, 0f);
        }
    }
}