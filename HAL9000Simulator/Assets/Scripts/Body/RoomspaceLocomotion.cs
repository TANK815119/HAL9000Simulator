using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomspaceLocomotion : MonoBehaviour
{
    [SerializeField] private Transform headset;
    [SerializeField] private Transform physicsHead;
    [SerializeField] private GameObject rotoball;
    [SerializeField] private float radius = 0.2f;

    private Rigidbody rotoBody;
    private Transform rotoTrans;
    private PIDWrapper wrapper;

    private Vector3 lastHeadsetPosition = Vector3.zero;
    private Quaternion lastRotoballRotation = Quaternion.identity;
    // Start is called before the first frame update
    void Start()
    {
        rotoBody = rotoball.GetComponent<Rigidbody>();
        rotoTrans = rotoball.transform;

        wrapper = rotoball.GetComponent<PIDWrapper>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        rotoBody.angularVelocity = Vector3.zero;

        //update headset deltas
        Vector3 headsetPosition = headset.position - physicsHead.position;
        Vector3 deltaHeadsetPosition = headsetPosition - lastHeadsetPosition;
        lastHeadsetPosition = headsetPosition;

        //update rotoball deltas
        Quaternion rotoballRotation = rotoTrans.rotation;
        Quaternion deltaRotoballRotation = rotoballRotation * Quaternion.Inverse(lastRotoballRotation); //this - last
        lastRotoballRotation = rotoballRotation;

        //convert to usable values
        float xDeltaRot = deltaRotoballRotation.eulerAngles.x;
        float zDeltaRot = deltaRotoballRotation.eulerAngles.z;
        if(xDeltaRot > 180f)
        {
            xDeltaRot = xDeltaRot - 360f;
        }
        if(zDeltaRot > 180f)
        {
            zDeltaRot = zDeltaRot - 360f;
        }

        //write and read to the PID controllers
        wrapper.IdealRotDelta(deltaHeadsetPosition.x, deltaHeadsetPosition.z, radius);
        wrapper.RealRotDelta(xDeltaRot, zDeltaRot, radius); // should only appear in one place
        rotoBody.AddTorque(wrapper.PIDRotTorque(Time.fixedDeltaTime, rotoBody.velocity.x, rotoBody.velocity.z)); // should only appear in one place
    }
}
