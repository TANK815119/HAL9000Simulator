using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private bool isOpen = false;

    private void Update()
    {
        if (isOpen)
        {
            transform.position = Vector3.Lerp(transform.position, target.position, Time.deltaTime);
        }
    }

    public void Open()
    {
        isOpen = true;
    }
}
