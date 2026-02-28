using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Unity.Netcode;
using System.Runtime.CompilerServices;

namespace Rekabsen
{
    public class HandAnimationLocal : MonoBehaviour, HandAnimationInterface
    {
        [SerializeField] bool isRightController;
        [SerializeField] Transform handRoot;
        //[SerializeField] Transform indexFinger;
        //[SerializeField] Transform middleFinger;
        //[SerializeField] Transform ringFinger;
        //[SerializeField] Transform pinkyFinger;
        //[SerializeField] Transform thumbFinger;

        private Transform[] boneArr; //actual hand state
        private Quaternion[] interArr; //interpolation between target and actual hand state

        //"ideal" rotations
        private Quaternion[] thumbArr;
        private Quaternion[] indexArr;
        private Quaternion[] middleArr;
        private Quaternion[] ringArr;
        private Quaternion[] pinkyArr;
        private Quaternion[][] gripArr;
        private Quaternion[][] handArr; //target hand state

        private InputData inputData;

        [SerializeField] [Range(0.0f, 1.0f)] private float grip;
        [SerializeField] [Range(0.0f, 1.0f)] private float trigger;
        [SerializeField] [Range(0.0f, 1.0f)] private float thumb;

        [SerializeField] public bool Gripping { get; set; }
        [field: SerializeField] public GrabPose Pose { get; set; }

        //private GameObject HND_Default_Left;
        //private GameObject HND_Default_Right;
        //private GameObject HND_Plane_Left;
        //private GameObject HND_Plane_Right;
        //private GameObject HND_Corner_Left;
        //private GameObject HND_Corner_Right;
        //private GameObject HND_Cylinder_Left;
        //private GameObject HND_Cylinder_Right;
        //private GameObject HND_Sphere_Left;
        //private GameObject HND_Sphere_Right;

        // Start is called before the first frame update
        void Start()
        {
            thumbArr = new Quaternion[3];
            indexArr = new Quaternion[3];
            middleArr = new Quaternion[3];
            ringArr = new Quaternion[3];
            pinkyArr = new Quaternion[3];
            gripArr = new Quaternion[][] { middleArr, ringArr, pinkyArr };
            handArr = new Quaternion[][] { thumbArr, indexArr, middleArr, ringArr, pinkyArr, };

            //large boneArr for every single bone Transform in the hand
            boneArr = new Transform[5 * 3]; // hard coded size of 15 joints
            for (int x = 0; x < 5; x++) //5 is hard coded for 5 digits
            {
                Transform[] fingerArr = GetChildren(handRoot.GetChild(x));
                for (int y = 0; y < 3; y++)
                {
                    boneArr[3 * x + y] = fingerArr[y];
                }
            }

            interArr = new Quaternion[3 * 5]; //hard coded for size 20;

            inputData = gameObject.GetComponent<InputData>();

            ////load all of the hand poses into memory
            //HND_Default_Left = Resources.Load<GameObject>("HandAnimation/HND_Default_Left");
            //HND_Default_Right = Resources.Load<GameObject>("HandAnimation/HND_Default_Right");
            //HND_Plane_Left = Resources.Load<GameObject>("HandAnimation/HND_Plane_Left");
            //HND_Plane_Right = Resources.Load<GameObject>("HandAnimation/HND_Plane_Right");
            //HND_Corner_Left = Resources.Load<GameObject>("HandAnimation/HND_Corner_Left");
            //HND_Corner_Right = Resources.Load<GameObject>("HandAnimation/HND_Corner_Right");
            //HND_Cylinder_Left = Resources.Load<GameObject>("HandAnimation/HND_Cylinder_Left");
            //HND_Cylinder_Right = Resources.Load<GameObject>("HandAnimation/HND_Cylinder_Right");
            //HND_Sphere_Left = Resources.Load<GameObject>("HandAnimation/HND_Sphere_Left");
            //HND_Sphere_Right = Resources.Load<GameObject>("HandAnimation/HND_Sphere_Right");
        }

        // Update is called once per frame
        void Update()
        {
            if (isRightController)
            {
                UpdateRightController();
            }
            else
            {
                UpdateLeftController();
            }

            if (Gripping)
            {
                if (!isRightController) //left hand grips
                {
                    switch (Pose)
                    {
                        case GrabPose.Plane: StaticGripHND(HandLibrary.HND_Plane_Left); break;
                        case GrabPose.Corner: StaticGripHND(HandLibrary.HND_Corner_Left); break;
                        case GrabPose.Sphere: StaticGripHND(HandLibrary.HND_Sphere_Left); break;
                        case GrabPose.Cylinder: StaticGripHND(HandLibrary.HND_Cylinder_Left); break;
                        case GrabPose.Pistol: StaticGripHND(HandLibrary.HND_Pistol_Left); break;
                        case GrabPose.PistolSecondary: StaticGripHND(HandLibrary.HND_Pistol_Secondary_Left); break;
                        case GrabPose.PistolSlide: StaticGripHND(HandLibrary.HND_Pistol_Slide_Left); break;
                        default: StaticGripHND(HandLibrary.HND_Default_Left); break;
                    }
                }
                else //right hand grips
                {
                    switch (Pose)
                    {
                        case GrabPose.Plane: StaticGripHND(HandLibrary.HND_Plane_Right); break;
                        case GrabPose.Corner: StaticGripHND(HandLibrary.HND_Corner_Right); break;
                        case GrabPose.Sphere: StaticGripHND(HandLibrary.HND_Sphere_Right); break;
                        case GrabPose.Cylinder: StaticGripHND(HandLibrary.HND_Cylinder_Right); break;
                        case GrabPose.Pistol: StaticGripHND(HandLibrary.HND_Pistol_Right); break;
                        case GrabPose.PistolSecondary: StaticGripHND(HandLibrary.HND_Pistol_Secondary_Right); break;
                        case GrabPose.PistolSlide: StaticGripHND(HandLibrary.HND_Pistol_Slide_Right); break;
                        default: StaticGripHND(HandLibrary.HND_Default_Right); break;
                    }
                }
            }
            else
            {
                EmptyHandPose();
            }

            //smoothly transform the finger rotations into real Rotations
            InterpolateRotation(); //calculate transitional rotations for "smoothness"
            TransformRotation(); //using interpolated values
        }

        private void InterpolateRotation()
        {
            for (int x = 0; x < 5; x++) //hard-coded
            {
                for (int y = 0; y < 3; y++) // hard-coded
                {
                    interArr[x * 3 + y] = Quaternion.Slerp(interArr[x * 3 + y], handArr[x][y], 25f * Time.deltaTime);
                }
            }
        }

        private void TransformRotation()
        {
            for (int i = 0; i < boneArr.Length; i++)
            {
                boneArr[i].localRotation = interArr[i];
            }
        }

        private void EmptyHandPose()
        {
            //pose grip
            float gripRotation = grip * 90f;
            for (int x = 0; x < gripArr.Length; x++)
            {
                for (int y = 0; y < gripArr[x].Length; y++)
                {
                    gripArr[x][y] = Quaternion.Euler(new Vector3(gripRotation, 0f, 0f));
                }
            }

            //pose trigger
            float indexRotation = trigger * 90f;
            for (int i = 0; i < indexArr.Length; i++)
            {
                indexArr[i] = Quaternion.Euler(new Vector3(indexRotation, 0f, 0f));
            }

            //pose thumb
            float thumbRotation = thumb * -60f;
            if (isRightController)
            {
                thumbArr[0] = Quaternion.Euler(new Vector3(thumb * 20f + 20f, 15f, 25f)); //may need to be controlled by another float for grabs
                for (int i = 1; i < thumbArr.Length; i++) // note the int i = 1
                {
                    thumbArr[i] = Quaternion.Euler(new Vector3(0f, 0f, thumbRotation + 15f));
                }
            }
            else
            {
                thumbArr[0] = Quaternion.Euler(new Vector3(thumb * 20f + 20f, -15f, -25f)); //may need to be controlled by another float for grabs
                for (int i = 1; i < thumbArr.Length; i++) // note the int i = 1
                {
                    thumbArr[i] = Quaternion.Euler(new Vector3(0f, 0f, -thumbRotation - 15f));
                }
            }

        }

        private Transform[] GetChildren(Transform parent) // precondition a chain of 2 single children from parent
        {
            Transform[] childArr = { parent, parent.GetChild(0), parent.GetChild(0).GetChild(0) };
            return childArr;
        }

        private void UpdateRightController()
        {
            //grip values
            if (inputData.rightController.TryGetFeatureValue(CommonUsages.grip, out float controllerGrip))
            {
                if (controllerGrip > 0f)
                {
                    if (grip >= 0.333f)
                    {
                        grip = controllerGrip * (1 - 0.333f) + 0.333f; //smooshed range
                    }
                    else if (grip < 0.333f)
                    {
                        grip += 5f * Time.deltaTime; //get into range
                    }
                }
                if (controllerGrip == 0f && grip > 0f) //gert out of range
                {
                    grip += -5f * Time.deltaTime;
                }
            }

            //trigger values
            if (inputData.rightController.TryGetFeatureValue(CommonUsages.trigger, out float controllerTrigger))
            {
                if (controllerTrigger > 0f)
                {
                    if (trigger >= 0.333f)
                    {
                        trigger = controllerTrigger * (1 - 0.333f) + 0.333f; //smooshed range
                    }
                    else if (trigger < 0.333f)
                    {
                        trigger += 5f * Time.deltaTime; //get into range
                    }
                }
                if (controllerTrigger == 0f && trigger > 0f) //gert out of range
                {
                    trigger += -5f * Time.deltaTime;
                }
            }

            //thumb values
            if (inputData.rightController.TryGetFeatureValue(CommonUsages.primary2DAxisTouch, out bool controllerThumbTouch))
            {
                if (controllerThumbTouch && thumb < 1f)
                {
                    thumb += 10f * Time.deltaTime;
                }
                if (!controllerThumbTouch && thumb > 0f)
                {
                    thumb += -10f * Time.deltaTime;
                }
            }
        }

        private void UpdateLeftController()
        {
            //grip values
            if (inputData.leftController.TryGetFeatureValue(CommonUsages.grip, out float controllerGrip))
            {
                if (controllerGrip > 0f)
                {
                    if (grip >= 0.333f)
                    {
                        grip = controllerGrip * (1 - 0.333f) + 0.333f; //smooshed range
                    }
                    else if (grip < 0.333f)
                    {
                        grip += 5f * Time.deltaTime; //get into range
                    }
                }
                if (controllerGrip == 0f && grip > 0f) //gert out of range
                {
                    grip += -5f * Time.deltaTime;
                }
            }

            //trigger values
            if (inputData.leftController.TryGetFeatureValue(CommonUsages.trigger, out float controllerTrigger))
            {
                if (controllerTrigger > 0f)
                {
                    if (trigger >= 0.333f)
                    {
                        trigger = controllerTrigger * (1 - 0.333f) + 0.333f; //smooshed range
                    }
                    else if (trigger < 0.333f)
                    {
                        trigger += 5f * Time.deltaTime; //get into range
                    }
                }
                if (controllerTrigger == 0f && trigger > 0f) //gert out of range
                {
                    trigger += -5f * Time.deltaTime;
                }
            }

            //thumb values
            if (inputData.leftController.TryGetFeatureValue(CommonUsages.primary2DAxisTouch, out bool controllerThumbTouch))
            {
                if (controllerThumbTouch && thumb < 1f)
                {
                    thumb += 10f * Time.deltaTime;
                }
                if (!controllerThumbTouch && thumb > 0f)
                {
                    thumb += -10f * Time.deltaTime;
                }
            }
        }

        /*
        Every method past this point is essentially a custom hand animation,
        each one haveing a parameter indicating handdedness
        */

        private void StaticGripHND(GameObject handTemplate)
        {
            //fetch hand HND prefab we're reading grip data from
            Transform HNDRoot = handTemplate.transform.GetChild(0); //hard coded to assume the first child is the root
            if (isRightController)
            {
                HNDRoot = handTemplate.transform.GetChild(0);
            }
            for (int x = 0; x < 5; x++) //5 is hard coded for 5 digits
            {
                Transform[] fingerArr = GetChildren(HNDRoot.GetChild(x));
                for (int y = 0; y < 3; y++)
                {
                    handArr[x][y] = fingerArr[y].localRotation;
                }
            }
        }

        //-------------------------------------------------------------old grip methods below

        private void FlatGrip()
        {
            //pose flat grip
            for (int x = 0; x < gripArr.Length; x++)
            {
                for (int y = 0; y < gripArr[x].Length; y++)
                {
                    if (y == 2)
                    {
                        gripArr[x][y] = Quaternion.Euler(new Vector3(-30f, 0f, 0f));
                    }
                    else
                    {
                        gripArr[x][y] = Quaternion.Euler(new Vector3(15f, 0f, 0f));
                    }
                }
            }

            //pose flat trigger
            for (int i = 0; i < indexArr.Length; i++)
            {
                if (i == 2)
                {
                    indexArr[i] = Quaternion.Euler(new Vector3(-30f, 0f, 0f));
                }
                else
                {
                    indexArr[i] = Quaternion.Euler(new Vector3(15f, 0f, 0f));
                }
            }

            //pose flat thumb
            if (isRightController)
            {
                thumbArr[0] = Quaternion.Euler(new Vector3(5f, 15f, 25f)); //may need to be controlled by another float for grabs
                for (int i = 1; i < thumbArr.Length; i++) // note the int i = 1
                {
                    thumbArr[i] = Quaternion.Euler(new Vector3(0f, 0f, 15f));
                }
            }
            else
            {
                thumbArr[0] = Quaternion.Euler(new Vector3(5f, -15f, -25f)); //may need to be controlled by another float for grabs
                for (int i = 1; i < thumbArr.Length; i++) // note the int i = 1
                {
                    thumbArr[i] = Quaternion.Euler(new Vector3(0f, 0f, -15f));
                }
            }
        }

        private void CornerGrip()
        {
            //pose corner grip
            for (int x = 0; x < gripArr.Length; x++)
            {
                for (int y = 0; y < gripArr[x].Length; y++)
                {
                    gripArr[x][y] = Quaternion.Euler(new Vector3(45f, 0f, 0f));
                }
            }

            //pose corner trigger
            for (int i = 0; i < indexArr.Length; i++)
            {
                indexArr[i] = Quaternion.Euler(new Vector3(45f, 0f, 0f));
            }

            //pose corner thumb
            if (isRightController)
            {
                thumbArr[0] = Quaternion.Euler(new Vector3(5f, -25f, 15f)); //may need to be controlled by another float for grabs
                for (int i = 1; i < thumbArr.Length; i++) // note the int i = 1
                {
                    thumbArr[i] = Quaternion.Euler(new Vector3(0f, 0f, 0));
                }
            }
            else
            {
                thumbArr[0] = Quaternion.Euler(new Vector3(5f, 25f, -15f)); //may need to be controlled by another float for grabs
                for (int i = 1; i < thumbArr.Length; i++) // note the int i = 1
                {
                    thumbArr[i] = Quaternion.Euler(new Vector3(0f, 0f, 0f));
                }
            }
        }

        private void CylinderGrip()
        {
            //pose cylinder grip
            for (int x = 0; x < gripArr.Length; x++)
            {
                for (int y = 0; y < gripArr[x].Length; y++)
                {
                    gripArr[x][y] = Quaternion.Euler(new Vector3(60f, 0f, 0f));
                }
            }

            //pose cylinder trigger
            for (int i = 0; i < indexArr.Length; i++)
            {
                indexArr[i] = Quaternion.Euler(new Vector3(60f, 0f, 0f));
            }

            //pose cylinder thumb
            if (isRightController)
            {
                thumbArr[0] = Quaternion.Euler(new Vector3(45f, -45f, -15f));
                thumbArr[1] = Quaternion.Euler(new Vector3(0f, 45f, 0f));
                thumbArr[2] = Quaternion.Euler(new Vector3(15f, 0f, -45f));
            }
            else
            {
                thumbArr[0] = Quaternion.Euler(new Vector3(45f, 45f, 15f));
                thumbArr[1] = Quaternion.Euler(new Vector3(0f, -45f, 0f));
                thumbArr[2] = Quaternion.Euler(new Vector3(15f, 0f, 45f));
            }
        }

        private void SphereGrip()
        {
            //pose sphere grip
            for (int x = 0; x < gripArr.Length; x++)
            {
                for (int y = 0; y < gripArr[x].Length; y++)
                {
                    gripArr[x][y] = Quaternion.Euler(new Vector3(15f, 0f, 0f));
                }
            }

            //pose sphere trigger
            for (int i = 0; i < indexArr.Length; i++)
            {
                indexArr[i] = Quaternion.Euler(new Vector3(15f, 0f, 0f));
            }

            //pose sphere thumb
            if (isRightController)
            {
                thumbArr[0] = Quaternion.Euler(new Vector3(45f, -90f, -15f));
                thumbArr[1] = Quaternion.Euler(new Vector3(0f, 45f, -7.5f));
                thumbArr[2] = Quaternion.Euler(new Vector3(0f, 0f, -7.5f));
            }
            else
            {
                thumbArr[0] = Quaternion.Euler(new Vector3(45f, 90f, 15f));
                thumbArr[1] = Quaternion.Euler(new Vector3(0f, -45f, 7.5f));
                thumbArr[2] = Quaternion.Euler(new Vector3(0f, 0f, 7.5f));
            }
        }
    }
}