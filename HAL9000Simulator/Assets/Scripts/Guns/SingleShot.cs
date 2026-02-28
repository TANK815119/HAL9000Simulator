using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class SingleShot : Firearm
{
    [SerializeField] private Transform anteriorPart;
    [SerializeField] private Transform posteriorPart;
    [SerializeField] private Transform barrelEnd;
    [SerializeField] private AmmoSlot ammoSlot;
    //[SerializeField] private Transform roundSlot;
    [SerializeField] private AudioClip barrelPrimeLock;
    [SerializeField] private AudioClip barrelLock;
    [SerializeField] private AudioClip hammerRelease;
    [SerializeField] private AudioClip buckshotFire;
    [SerializeField] private AudioClip loadAmmo;
    [SerializeField] private AudioClip ejectCasing;
    [SerializeField] private float lockTolerance = 1f; //degrees
    [SerializeField] private float primeLockTolerance = 15f; //degrees
    [SerializeField] private float lockBreakTorque = 20f;
    [SerializeField] private int pelletCount = 41; //12 gauge, #4 buckshot, 3 inch bore
    [SerializeField] private float pelletSpread = 2.2f; //degrees
    [SerializeField] private float barrelChoke = 0.0092075f; //radius of bore in meters
    [SerializeField] private float ejectionVelocity = 3.5f;
    [SerializeField] private bool debugFire = false;

    private ConfigurableJoint lockJoint;
    private bool lockPrimed = false;

    private bool loaded = false;
    private Ammo loadedAmmo;

    private Rigidbody anteriorBody;
    private Collider anteriorCollider;
    private Collider gunCollider;

    void Start()
    {
        inputData = gameObject.GetComponent<InputData>();

        ammoSlot.SetFirearm(this);

        hammerBack = false;
        //hammerReleased = false;

        //fetch a bunch of basic components from the gun
        if(anteriorPart.TryGetComponent(out Rigidbody rigidbody))
        {
            anteriorBody = rigidbody;
        }

        if(gameObject.TryGetComponent(out Rigidbody rigidbody2))
        {
            gunBody = rigidbody2;
        }
    }

    // Update is called once per frame
    void Update()
    {
        TryPrimeLock(); //prime the lock if away from in position

        TryLock(); //try to lock the barrel if in position, drawing the hammer back too

        triggerPresssed = TriggerSqueezed();
        if(debugFire)
        {
            triggerPresssed = true;
        }

        TryReleaseHammer(); //try to fire the gun if loaded and triggerpressed

        AnimateHammer(); //make hammer proportional to lock proximity for realism

        TryBreakLock(); 
    }

    private void TryPrimeLock()
    {
        if(lockPrimed)
        {
            return;
        }

        //prime the lock if over double twice the lock threshold
        float alignmentDiff = Quaternion.Angle(anteriorPart.rotation, posteriorPart.rotation);
        if (alignmentDiff >= primeLockTolerance)
        {
            //audio
            audioSource.PlayOneShot(barrelPrimeLock);

            lockPrimed = true;
        }
        else
        {
            return;
        }

        //spent casing ejection
        if (loadedAmmo == null)
        {
            return;
        }
        GameObject casing = Instantiate(loadedAmmo.SpentCasing, loadedAmmo.transform.position, loadedAmmo.transform.rotation);
        if (casing.TryGetComponent(out Rigidbody casingBody))
        {
            casingBody.velocity = gunBody.velocity;
            casingBody.AddForce(casingBody.transform.up * -ejectionVelocity, ForceMode.VelocityChange);
        }

        audioSource.PlayOneShot(ejectCasing, 0.33f);

        Destroy(loadedAmmo.gameObject);
    }

    private void TryLock()
    {
        if(!lockPrimed || !(lockJoint == null))
        {
            return;
        }

        //check if hammer is fallen -> return
        //check if the relative angle of the anterior is
        //within some threshold of the posterior, else return
        float alignmentDiff = Quaternion.Angle(anteriorPart.rotation, posteriorPart.rotation);
        if(alignmentDiff > lockTolerance)
        {
            return;
        }

        //set the relative transform of the anterior(which is always (0, 0, 0) how I set it up)
        anteriorPart.localPosition = Vector3.zero;
        anteriorPart.localRotation = Quaternion.identity;

        //create a non-breakable physics joint(make breakable after shot)
        lockJoint = gameObject.AddComponent<ConfigurableJoint>();
        lockJoint.connectedBody = anteriorBody;
        lockJoint.breakTorque = Mathf.Infinity;
        lockJoint.angularXMotion = ConfigurableJointMotion.Free;
        lockJoint.angularYMotion = ConfigurableJointMotion.Locked;
        lockJoint.angularZMotion = ConfigurableJointMotion.Locked;
        JointDrive lockDrive = new JointDrive();
        lockDrive.positionSpring = Mathf.Infinity;
        lockJoint.angularXDrive = lockDrive;

        //audio
        audioSource.PlayOneShot(barrelLock);

        //draw the hammer back
        hammerBack = true;
        lockPrimed = false;

        //animate
        animator.SetFloat("HammerCock", 1f);
    }

    private void TryReleaseHammer()
    {
        //check that the hammer is back
        if(!hammerBack || !triggerPresssed)
        {
            return;
        }

        //try to fire
        if(loaded)
        {
            //fire
            FireBuckshot();
        }
        else
        {
            DryFire();
        }

        //make lock breakable
        JointDrive lockDrive = new JointDrive();
        lockDrive.positionSpring = lockBreakTorque;
        lockJoint.angularXDrive = lockDrive;
        hammerBack = false;

        //animate
        animator.SetFloat("HammerCock", 0f);
    }

    private void FireBuckshot()
    {
        //audio
        audioSource.PlayOneShot(hammerRelease);
        audioSource.PlayOneShot(buckshotFire);

        //fire hitscan
        for (int i = 0; i < pelletCount; i++)
        {
            RaycastHit hit;
            //get a random position in a circle the size of the bore relative to the barrel end
            //randomPosition = barrelEnd.position + ( (random rotation pointing to the right of the forward axis (relative to the barrelEnd rotation)) in barrel choke distance)
            Vector3 chokeCircle = barrelEnd.rotation * Quaternion.Euler(Vector3.right * Random.Range(-180f, 180f)) * Vector3.forward;
            Vector3 randomPositon = barrelEnd.position + chokeCircle.normalized * Random.Range(0f, barrelChoke);
            Vector3 randomRotation = new Vector3(Random.Range(-pelletSpread, pelletSpread), Random.Range(-pelletSpread, pelletSpread), Random.Range(-pelletSpread, pelletSpread));
            Vector3 rayDir = Quaternion.Euler(randomRotation) * -barrelEnd.right.normalized; //Annoying I can't do forward, but the model natrually looks on left(negative x) direction
            if (Physics.Raycast(randomPositon, rayDir, out hit))
            {
                if (hit.rigidbody != null)
                {
                    hit.rigidbody.AddForceAtPosition(rayDir * ((recoil * bulletMassScale) / pelletCount), hit.point, ForceMode.Impulse);
                }

                Debug.DrawRay(randomPositon, rayDir * Vector3.Distance(randomPositon, hit.point), Color.red, 3f);
            }

        }

        //recoil
        //Vector3 force = (recoilOrigin.position - gunBody.transform.position).normalized * -recoil;
        float totoalMass = gunBody.mass + anteriorBody.mass;
        Vector3 posteriorForce = anteriorBody.transform.right * recoil * (gunBody.mass / totoalMass); //the left direction is correct due to the mannor in  which the gun was modled
        Vector3 anteriorForce = anteriorBody.transform.right * recoil * (anteriorBody.mass / totoalMass);
        Vector3 position = recoilOrigin.position;
        gunBody.AddForceAtPosition(posteriorForce, position, ForceMode.Impulse);
        anteriorBody.AddForceAtPosition(anteriorForce, position, ForceMode.Impulse);

        //ammo change
        loaded = false;
    }

    private void DryFire()
    {
        //audio
        audioSource.PlayOneShot(hammerRelease);
    }

    private void AnimateHammer()
    {
        //boot out if lock isnt primed
        if(lockPrimed == false)
        {
            return;
        }

        //proportional hammer anim
        float alignmentDiff = Quaternion.Angle(anteriorPart.rotation, posteriorPart.rotation);
        if (alignmentDiff < primeLockTolerance)
        {
            float cockProp = (primeLockTolerance - alignmentDiff) / primeLockTolerance; //0-1 where its 0 at 15 degrees and 1 at zero
            animator.SetFloat("HammerCock", cockProp);
        }
        else
        {
            animator.SetFloat("HammerCock", 0f); //>15 degrees away
        }
        
    }

    private void TryBreakLock()
    {
        //only attempt break if there is a lock and the hammer isnt back
        if(lockJoint == null || hammerBack == true)
        {
            return;
        }

        //check if the angDiff is greater than 1 degree to destroy the joint
        //this is a distance check as opposed to the puref force of the previous solution
        float alignmentDiff = Quaternion.Angle(anteriorPart.rotation, posteriorPart.rotation);
        if(alignmentDiff > lockTolerance)
        {
            //break the lock
            Destroy(lockJoint);

            //audio
            audioSource.PlayOneShot(barrelPrimeLock);
        }
    }

    public override void TryLoadAmmo(Ammo ammo)
    {
        //check the ammo is buckshot
        if (loaded || !lockPrimed || !(lockJoint == null) || !ammo.AmmoType.Equals("buckshot"))
        {
            return;
        }

        //parent to the ammo slot
        ammo.transform.parent = ammoSlot.transform;

        //move the round to the ammo slot
        ammo.transform.position = ammoSlot.transform.position;
        ammo.transform.rotation = ammoSlot.transform.rotation;

        //get rid of any connected joint(especially grip joint which is kinda shit fix but works)
        if(ammo.TryGetComponent(out Joint joint))
        {
            Destroy(joint);
        }

        //disable ammo collision with gun
        if (ammo.TryGetComponent(out Collider ammoCollider))//it definetly has collider
        {
            Destroy(ammoCollider);
        }

        //disable ammo rigidobody
        if (ammo.TryGetComponent(out Rigidbody ammoBody))
        {
            Destroy(ammoBody);
        }

        loaded = true;

        //make the loadedAmmo accesible elswhere
        loadedAmmo = ammo;

        //audio
        audioSource.PlayOneShot(loadAmmo);
    }
}
