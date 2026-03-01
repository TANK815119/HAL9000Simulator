using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HALSheetListener : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (HALSingleton.Instance.light4Active) return;

        if (other.attachedRigidbody.gameObject.name.Contains("Sheet"))
        {
            HALSingleton.Instance.ActivateLight(4);
            other.attachedRigidbody.drag = 50f; // Simulate sheet being "stuck" to HAL
            other.attachedRigidbody.angularDrag = 20f; // Simulate sheet being "stuck" to HAL
        }
    }
}
