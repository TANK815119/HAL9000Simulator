using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rekabsen
{
    public class HandLibrary : MonoBehaviour
    {
        public static GameObject HND_Default_Left = Resources.Load<GameObject>("HandAnimation/HND_Default_Left");
        public static GameObject HND_Default_Right = Resources.Load<GameObject>("HandAnimation/HND_Default_Right");
        public static GameObject HND_Plane_Left = Resources.Load<GameObject>("HandAnimation/HND_Plane_Left");
        public static GameObject HND_Plane_Right = Resources.Load<GameObject>("HandAnimation/HND_Plane_Right");
        public static GameObject HND_Corner_Left = Resources.Load<GameObject>("HandAnimation/HND_Corner_Left");
        public static GameObject HND_Corner_Right = Resources.Load<GameObject>("HandAnimation/HND_Corner_Right");
        public static GameObject HND_Cylinder_Left = Resources.Load<GameObject>("HandAnimation/HND_Cylinder_Left");
        public static GameObject HND_Cylinder_Right = Resources.Load<GameObject>("HandAnimation/HND_Cylinder_Right");
        public static GameObject HND_Sphere_Left = Resources.Load<GameObject>("HandAnimation/HND_Sphere_Left");
        public static GameObject HND_Sphere_Right = Resources.Load<GameObject>("HandAnimation/HND_Sphere_Right");
        public static GameObject HND_Pistol_Left = Resources.Load<GameObject>("HandAnimation/HND_Pistol_Left");
        public static GameObject HND_Pistol_Right = Resources.Load<GameObject>("HandAnimation/HND_Pistol_Right");
        public static GameObject HND_Pistol_Secondary_Left = Resources.Load<GameObject>("HandAnimation/HND_Pistol_Secondary_Left");
        public static GameObject HND_Pistol_Secondary_Right = Resources.Load<GameObject>("HandAnimation/HND_Pistol_Secondary_Right");
        public static GameObject HND_Pistol_Slide_Left = Resources.Load<GameObject>("HandAnimation/HND_Pistol_Slide_Left");
        public static GameObject HND_Pistol_Slide_Right = Resources.Load<GameObject>("HandAnimation/HND_Pistol_Slide_Right");
    }

    //TODO add class that converts GrabPose and Handedness to the correct GameObject from above
}