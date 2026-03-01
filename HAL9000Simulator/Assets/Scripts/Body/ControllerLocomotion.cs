using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
//shouyld purely be an input into the rorstate.
//nothing more

public class ControllerLocomotion : MonoBehaviour
{
    [SerializeField] private Transform headset;
    [SerializeField] private Rigidbody leftHand;
    [SerializeField] private Rigidbody rightHand;
    [SerializeField] private GameObject rotoball;
    [SerializeField] private float radius = 0.2f;
    [SerializeField] private float walkSpeed = 1.5f; //meters per second
    [SerializeField] private float runMult = 3.5f/1.5f; //meters per second
    [SerializeField] private float runDecay = 2f;
    [SerializeField] private float runGain = 2f;
    [SerializeField] private float VelDiffTheta = 0.5f;
    [SerializeField] private bool posZ = false;
    [SerializeField] private bool posX = false;

    private InputData inputData;
    private PIDWrapper wrapper;
    private Rigidbody rotobody;

    // Run values
    private float runIntegral = 1f;
    // Start is called before the first frame update
    void Start()
    {
        inputData = rotoball.GetComponent<InputData>();
        wrapper = rotoball.GetComponent<PIDWrapper>();
        rotobody = rotoball.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        //get input values
        inputData.leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 thumbstick);
        if(posZ) { thumbstick.y = 1f; }
        if(posX) { thumbstick.x = 1f; }

        //get run vigor
        float planarHandSpeed = (leftHand.velocity.magnitude + rightHand.velocity.magnitude) / 2f;
        float rotoballSpeed = rotobody.velocity.magnitude;
        if (planarHandSpeed > (rotoballSpeed + VelDiffTheta))
        {
            if (runIntegral < runMult)
            {
                runIntegral += runGain * (planarHandSpeed - rotoballSpeed) * Time.deltaTime;
            }
            else
            {
                runIntegral = runMult;
            }
        }
        else
        {
            if(runIntegral > 1f)
            {
                //decay
                runIntegral -= runDecay * Time.deltaTime;
            }
            else
            {
                runIntegral = 1f;
            }
        }

        //apply values to the PID controllers
        wrapper.IdealRotDelta(
            Mathf.Sin(Mathf.Deg2Rad * headset.eulerAngles.y) * thumbstick.y * walkSpeed * runIntegral * Time.deltaTime +        //x-axis movement(parralel to view)
                Mathf.Cos(Mathf.Deg2Rad * headset.eulerAngles.y) * thumbstick.x * walkSpeed * runIntegral * Time.deltaTime,     //x-axis movement(perpendicular to view)
            Mathf.Cos(Mathf.Deg2Rad * headset.eulerAngles.y) * thumbstick.y * walkSpeed * runIntegral * Time.deltaTime +        //z-axis movement(parralel to view)
                Mathf.Sin(Mathf.Deg2Rad * headset.eulerAngles.y) * thumbstick.x * -walkSpeed * runIntegral * Time.deltaTime,    //z-axis movement(perpendicular to view)
            radius);
    }
}
