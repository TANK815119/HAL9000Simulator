using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Rekabsen;
using Unity.VisualScripting;

public class Pistol : Firearm
{
    [SerializeField] private PistolSlide pistolSlide;
    [SerializeField] private Magwell magwell;
    [SerializeField] private AudioClip pistolFire;
    [SerializeField] private AudioClip dryFire;
    [SerializeField] private AudioClip slideBack;
    [SerializeField] private AudioClip slideForward;
    [SerializeField] private Transform barrelEnd;
    [SerializeField] public AnimationClip slideClip;
    //[SerializeField] private string slideClipName = "ANI_Gun_1911";
    [SerializeField] private float roundsPerMinute = 1200f;
    [SerializeField] private float slideTheta = 0.01f;
    [SerializeField] private float damge = 33f;

    //private AnimationClip slideClip;
    private float slideClipSpeed = 1f;
    private bool chambered = false;
    private bool firing = false;
    private bool backwarded = false;
    private bool triggerReleased = true;

    // Start is called before the first frame update
    void Start()
    {
        gunBody = GetComponent<Rigidbody>();
        inputData = gameObject.GetComponent<InputData>();

        //slideClip = slideClip = animator.runtimeAnimatorController.animationClips
        //    .FirstOrDefault(clip => clip.name == slideClipName);
        //Debug.Assert(slideClip != null, "Animation clip name does not match any animation on animator");

    }

    // Update is called once per frame
    void Update()
    {
        float roundsPerSecond = 60f / roundsPerMinute;
        slideClipSpeed = slideClip.length / roundsPerSecond;
        animator.speed = slideClipSpeed;

        if (TriggerSqueezed() && chambered && hammerBack && !firing && !pistolSlide.IsFollowingHand() && triggerReleased)
        {
            triggerReleased = false;
            FireRound();
        } else if (TriggerSqueezed() && hammerBack && !firing && !pistolSlide.IsFollowingHand() && triggerReleased)
        {
            triggerReleased = false;
            DryFire();
        }

        if (!triggerReleased && TriggerReleased())
        {
            triggerReleased = true;
        }

        //check that the slide has been brought back and hasnt been cocked
        if (pistolSlide.IsFollowingHand())
        {
            animator.enabled = false;
            if (!backwarded)
            {
                float slideProximity = Mathf.Abs(Mathf.Abs(pistolSlide.transform.localPosition.z) - pistolSlide.SlideMax());
                if (slideProximity < slideTheta)
                {
                    backwarded = true;

                    AttemptClearChamber();

                    //attempt to cock the slide
                    AttemptSlideBack(true);
                }
            }

            if (backwarded && Mathf.Abs(pistolSlide.transform.localPosition.z) < slideTheta)
            {
                backwarded = false;

                AfterSlideForward();
            }
        }
        else
        {
            animator.enabled = true;
        }
    }

    public override void TryLoadAmmo(Ammo ammo)
    {
        throw new System.NotImplementedException();
    }

    private void FireRound()
    {
        //play audio
        audioSource.PlayOneShot(pistolFire);

        //fire hitscan
        RaycastHit hit;
        Vector3 endPosition = barrelEnd.position;
        Vector3 rayDir = barrelEnd.forward.normalized; //Annoying I can't do forward, but the model natrually looks on left(negative x) direction
        if (Physics.Raycast(endPosition, rayDir, out hit))
        {
            if (hit.rigidbody != null)
            {
                hit.rigidbody.AddForceAtPosition(rayDir * (recoil * bulletMassScale), hit.point, ForceMode.Impulse);
            }

            Debug.DrawRay(endPosition, rayDir * Vector3.Distance(endPosition, hit.point), Color.red, 3f);
        }

        //recoil
        //Vector3 force = (recoilOrigin.position - gunBody.transform.position).normalized * -recoil;
        float totoalMass = gunBody.mass;
        Vector3 posteriorForce = -gunBody.transform.forward * recoil * (gunBody.mass / totoalMass); //the left direction is correct due to the mannor in  which the gun was modled
        Vector3 position = recoilOrigin.position;
        gunBody.AddForceAtPosition(posteriorForce, position, ForceMode.Impulse);

        //damage NPC at top of hierachy if there
        if(hit.collider.transform.root.gameObject.TryGetComponent(out NonPlayerCharacter npc))
        {
            npc.TakeDamage(damge);
        }

        //throw spent casing
        Rigidbody casingBody = Instantiate(spentCasing.gameObject, casingOrigin.position, casingOrigin.rotation).GetComponent<Rigidbody>();
        Collider caseingCollider = casingBody.GetComponent<Collider>();
        GripControllerLocal.SetAllToCollision(caseingCollider, transform, true); //disable collsion between case and gun
        casingBody.velocity = gunBody.velocity;
        casingBody.AddForce(casingBody.transform.right * 0.03f + casingBody.transform.up * 0.03f, ForceMode.Impulse);


        //animate
        animator.Play(slideClip.name, 0, 0f);
        firing = true;
        //animator.speed = slideClipSpeed;
        Invoke(nameof(AfterRoundFire), slideClip.length / slideClipSpeed);

        //update chambered state
        hammerBack = false;
        chambered = false;
        AttemptSlideBack(false);
    }

    private void AttemptClearChamber()
    {
        if (chambered)
        {
            //throw spent casing
            Rigidbody casingBody = Instantiate(spentCasing.gameObject, casingOrigin.position, casingOrigin.rotation).GetComponent<Rigidbody>();
            Collider caseingCollider = casingBody.GetComponent<Collider>();
            GripControllerLocal.SetAllToCollision(caseingCollider, transform, true); //disable collsion between case and gun
            casingBody.velocity = gunBody.velocity;
            casingBody.AddForce(casingBody.transform.right * 0.03f + casingBody.transform.up * 0.03f, ForceMode.Impulse);

            if(magwell.SeatedMagazine != null && magwell.SeatedMagazine.RemoveBullet())
            {
                chambered = true;
            }
        }
    }

    private void DryFire()
    {
        audioSource.PlayOneShot(dryFire);
        hammerBack = false;
    }

    private void AfterRoundFire()
    {
        //animator.speed = 1f;
        firing = false;

        //make a loading sound or nah dependent on if a round is available
        //audioSource.PlayOneShot(slideForward, 0.5f);
    }

    //push the slide forward and wait for it
    //used for ungripping and last bolt hold open
    public void SlideForward()
    {
        float animStartSeconds = AnimationAnalyzer.TimeAtLocalPosition(slideClip, pistolSlide.gameObject, pistolSlide.transform.localPosition, 0.5f, 0.025f, 0.0025f);
        Debug.Log("Start Seconds: " + animStartSeconds);
        animator.Play(slideClip.name, 0, animStartSeconds);
        //animator.speed = slideClipSpeed;
        float remainingSeconds = slideClip.length - animStartSeconds;
        Invoke(nameof(AfterSlideForward), remainingSeconds / slideClipSpeed);
        float normalizedRemaining = Mathf.Clamp01(2 * remainingSeconds / slideClip.length);
        cachedVolumeFactor = normalizedRemaining;
    }

    private float cachedVolumeFactor;

    private void AfterSlideForward()
    {
        //animator.speed = 1f;

        //make a loading sound or nah dependent on if a round is available
        audioSource.PlayOneShot(slideForward, 0.2f * cachedVolumeFactor);
        backwarded = false;
    }


    private void AttemptSlideBack(bool playAudio)
    {
        //make the slide back noise
        if(playAudio) { audioSource.PlayOneShot(slideBack,0.2f); }

        //attempt to eject whatever round is in the chamber

        //if the hammer hasnt been cocked, cock it
        hammerBack = true;

        //attempt chambering by removing bullet from mag in magwell
        if (magwell.SeatedMagazine != null && magwell.SeatedMagazine.RemoveBullet())
        {
            chambered = true;
        }

        //donm't bring the slide forward, thats done automatically
    }
}