using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Unity.Netcode;

namespace Rekabsen
{
    public interface HandAnimationInterface
    {
        public bool Gripping { get; set; }
        public GrabPose Pose { get; set; }
    }
}