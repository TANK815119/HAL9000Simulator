using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Rendering;
using Unity.Netcode;

namespace Rekabsen
{
    public interface BodyConfigurationInterface
    {
        public Vector3 HipOffset();
    }
}