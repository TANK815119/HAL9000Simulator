using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Leash : MonoBehaviour
{
    [SerializeField] private Transform start;
    [SerializeField] private Transform end;

    // Update is called once per frame
    void Update()
    {
        transform.position = (start.position + end.position) / 2;
        transform.up = (end.position - start.position).normalized;
        transform.localScale = new Vector3(transform.localScale.x, Vector3.Distance(start.position, end.position) / 2f, transform.localScale.z);
    }
}
