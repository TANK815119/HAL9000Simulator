using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationAnalyzer : MonoBehaviour
{
    public static float TimeAtLocalPosition(AnimationClip clip, GameObject targetObject, Vector3 localPosition, float minTime, float sampleStep, float theta)
    {
        //stow innitial state and set up
        Vector3 innitTargetLocalPos = targetObject.transform.localPosition;
        float closestTime = 0f;
        float minDist = float.MaxValue;

        //sample the parent(because the animator is there) at intervals of sampleStep
        for (float t = minTime; t < clip.length; t += sampleStep)
        {

            clip.SampleAnimation(targetObject.transform.parent.gameObject, t);

            float dist = Vector3.Distance(targetObject.transform.localPosition, localPosition);

            if (dist < minDist)
            {
                minDist = dist;
                closestTime = t;
            }

            if(dist < theta)
            {
                // If the distance is within the threshold, we can stop early
                break;
            }
        }

        //clean up
        targetObject.transform.localPosition = innitTargetLocalPos;
        return closestTime;
    }

    private static GameObject FindChild(GameObject parent, string childName)
    {
        foreach (Transform child in parent.transform)
        {
            if (child.name == childName)
            {
                return child.gameObject;
            }
        }
        return null;
    }

    public static float TimeAtLocalPosition(AnimationClip clip, GameObject targetObject, Vector3 localPosition)
    {
        return TimeAtLocalPosition(clip, targetObject, localPosition, 0f, 0.025f, 0.025f);
    }

    public static float TimeAtLocalPosition(AnimationClip clip, GameObject targetObject)
    {
        return TimeAtLocalPosition(clip, targetObject, targetObject.transform.localPosition);
    }
}
