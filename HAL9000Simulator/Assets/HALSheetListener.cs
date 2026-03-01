using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HALSheetListener : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody.gameObject.name.Contains("Sheet"))
        {
            //Do something
        }
    }
}
