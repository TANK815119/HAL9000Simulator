using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(InputData))]
public class InterfaceButton : MonoBehaviour
{
    [SerializeField] private Transform cursor;
    [SerializeField] private bool XROverideTouch = false;
    public bool Selected { get; set; }
    public bool Held { get; set; }

    private InputData inputData;
    private bool cursorTouching = false;
    // Start is called before the first frame update
    void Start()
    {
        inputData = GetComponent<InputData>();
    }

    // Update is called once per frame
    void Update()
    {
        inputData.rightController.TryGetFeatureValue(CommonUsages.trigger, out float trigger);

        bool triggerPressed = false;
        if (trigger > 0.5f)
        {
            triggerPressed = true;
        }

        //manipulate visuals
        if (cursorTouching && !triggerPressed)
        {
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, -0.0025f); //forward
        }
        else
        {
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0.0025f); //receded
        }

        //assign logic
        if(cursorTouching && triggerPressed || XROverideTouch)
        {
            if(!Held)
            {
                Selected = true;
            }
            else
            {
                Selected = false;
            }

            Held = true;
        }
        else
        {
            Selected = false;
            Held = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.Equals(cursor))
        {
            cursorTouching = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.Equals(cursor))
        {
            cursorTouching = false;
        }
    }

    private void OnDisable()
    {
        cursorTouching = false;
    }
}
