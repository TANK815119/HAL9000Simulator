using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PIDController : MonoBehaviour
{
    private float proportionalGain;
    private float derrivativeGain;
    private float integralGain;
    private bool useVelocity = true;

    private float idealRot;
    private float realRot;

    private float lastError;
    private float integrationStored;

    private float maxPower = 2000f;
    public void IdealDelta(float deltaDist, float radius)
    {
        //convert distance to portion of circumferece
        float circum = deltaDist / (2 * Mathf.PI * radius);

        //add that circumference portion to the idealRotation
        idealRot += circum;
    }
    public void RealDelta(float angleDelta, float radius)
    {
        //more transformation may be nessesary when the 180 to -180 threshold is passed as\
        //this may spit out a deltaAngle which is far too large.

        realRot += angleDelta / 360f;
    }

    public float PIDTorque(float fixedDeltaTime, float velocity)
    {
        //calculate the error
        float error = idealRot - realRot; //may swpa these to make other make more sense

        //calculate proportional
        float propotional = error * proportionalGain;

        //calculate derrivate
        //i probably dont have to worry about derrivative kick
        float derrivative = 0f;
        float errorDerrivative = 0f;
        if (useVelocity)
        {
            errorDerrivative = -velocity;
            derrivative = -velocity * derrivativeGain;
        }
        else
        {
            errorDerrivative = (error - lastError) / fixedDeltaTime;
            lastError = error;
            derrivative = errorDerrivative * derrivativeGain;
        }

        //calculate integral
        integrationStored += error * fixedDeltaTime;
        float integral = integrationStored * integralGain;

        //Debug.Log("Proportional: " + error + " | Derrivative: " + errorDerrivative + " | Integral: " + integrationStored);

        //final tuning
        float power = propotional + derrivative + integral;
        if (power > maxPower) // necessary to avoid wrong deltaAngles in RoomspaceLocomotion
        {
            power = maxPower;
        }
        if(power < -maxPower)
        {
            power = -maxPower;
        }
        return power; // likely make this absolute values as direction is already known
    }

    public void SetPIDProperties(float propGain, float derrGain, float inteGain, bool useVel)
    {
        proportionalGain = propGain;
        derrivativeGain = derrGain;
        integralGain = inteGain;
        useVelocity = useVel;
    }

    public void SetMaxPower(float power)
    {
        maxPower = power;
    }
}
