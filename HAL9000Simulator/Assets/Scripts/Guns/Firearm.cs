using System.Collections;
using System.Collections.Generic;
using Rekabsen;
using UnityEngine;
using UnityEngine.XR;

/*
 * Base script for guns
 * no longer can be used by its lonesome
 */

[RequireComponent(typeof(InputData))]
public abstract class Firearm : MonoBehaviour
{
    [SerializeField] protected Animator animator;
    [SerializeField] protected AudioSource audioSource;

    [SerializeField] protected bool automatic;
    [SerializeField] protected float recoil = 20f;
    [SerializeField] protected float bulletMassScale = 1f;
    [SerializeField] protected List<GrabSurface> pistolGrips;
    [SerializeField] protected Transform recoilOrigin;
    [SerializeField] protected Transform casingOrigin;
    [SerializeField] protected SpentCasing spentCasing;
    [SerializeField] protected GameObject round;

    protected InputData inputData;
    protected Rigidbody gunBody;

    protected bool hammerBack = true;
    protected bool hammerReleased = false;
    protected bool triggerPresssed;
    // Start is called before the first frame update
    void Start()
    {
        //inputData = gameObject.GetComponent<InputData>();
        //gunBody = gameObject.GetComponent<Rigidbody>();
    }

    protected bool TriggerSqueezed()
    {
        //check if one of the grips is doin stuff
        bool trigPres = false;
        for (int i = 0; i < pistolGrips.Count; i++)
        {
            if (pistolGrips[i].RightHandGrabbed)
            {
                if (inputData.rightController.TryGetFeatureValue(CommonUsages.trigger, out float controllerTrigger) && controllerTrigger > 0.75f)
                {
                    trigPres = true;
                }
            }
            else if (pistolGrips[i].LeftHandGrabbed)
            {
                if (inputData.leftController.TryGetFeatureValue(CommonUsages.trigger, out float controllerTrigger) && controllerTrigger > 0.75f)
                {
                    trigPres = true;
                }
            }
        }
        return trigPres;
    }

    protected bool TriggerReleased()
    {
        //check if one of the grips is doin stuff
        bool trigRel = false;
        for (int i = 0; i < pistolGrips.Count; i++)
        {
            if (pistolGrips[i].RightHandGrabbed)
            {
                if (inputData.rightController.TryGetFeatureValue(CommonUsages.trigger, out float controllerTrigger) && controllerTrigger < 0.25f && !TriggerSqueezed())
                {
                    trigRel = true;
                }
            }
            else if (pistolGrips[i].LeftHandGrabbed)
            {
                if (inputData.leftController.TryGetFeatureValue(CommonUsages.trigger, out float controllerTrigger) && controllerTrigger < 0.25f && !TriggerSqueezed())
                {
                    trigRel = true;
                }
            }
        }
        return trigRel;
    }

    public abstract void TryLoadAmmo(Ammo ammo);
}
