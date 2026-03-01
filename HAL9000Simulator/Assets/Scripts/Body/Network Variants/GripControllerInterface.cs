using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Unity.Netcode;

namespace Rekabsen
{
    public interface GripControllerInterface
    {
        public void MakeTwoHandedGrip(bool isTwoHanded);
        public GameObject GetGrabbedObject();
        public void DestroyGrip();
        public void AddGrabPointReference(GrabPoint grabPoint);
        public void RemoveGrabPointReference(GrabPoint grabPoint);
    }
}
