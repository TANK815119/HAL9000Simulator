using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SurfaceType
{
    Plane,
    Corner,
    Sphere,
    Cylinder,
    Line,
    Point
}

public enum GrabPose
{
    Plane,
    Corner,
    Sphere,
    Cylinder,
    Pistol,
    PistolSecondary,
    PistolSlide
}

public enum Handedness
{
    Both,
    Left,
    Right,
    Exclusive,
    None
}