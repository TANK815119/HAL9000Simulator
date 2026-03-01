using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class M1A1 : Firearm
{
    void Start()
    {
        inputData = gameObject.GetComponent<InputData>();
        gunBody = gameObject.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        triggerPresssed = TriggerSqueezed();

        //get animation state
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        //state.IsName("m1a1_fire")
        if (triggerPresssed)
        {
            //update animations
            animator.SetBool("fire", true);
            animator.SetBool("bolt open", false);

            //firing logic
            if (state.IsName("m1a1_fire"))
            {
                FireUpdate(state.normalizedTime % 1f);
            }
        }
        else
        {
            //update animations
            animator.SetBool("bolt open", true);
            animator.SetBool("fire", false);
            hammerBack = true;
        }

    }

    private void FireUpdate(float boltDist) // bolt distance back; 0=min; 1=max
    {
        //release hammer if bolt is forward, the hammer is back, and not released
        if (hammerBack && !hammerReleased && boltDist < 0.70f && boltDist > 0.40f)
        {
            hammerReleased = true;
        }

        //fire the weapon if the hammer is released
        if (hammerReleased)
        {
            FireBullet();
            hammerBack = false;
            hammerReleased = false;
        }

        //recharge the hammer if it goes sufficiently back(automatic firing group)
        if (automatic && !hammerBack && boltDist >= 0.70f)
        {
            hammerBack = true;
        }
    }

    private void FireBullet()
    {
        //audio
        audioSource.PlayOneShot(audioSource.clip);

        //fire projectile
        Rigidbody bulletBody = Instantiate(round, recoilOrigin.position, recoilOrigin.rotation).GetComponent<Rigidbody>();
        bulletBody.velocity = gunBody.velocity;
        bulletBody.mass *= bulletMassScale;
        bulletBody.AddForce(bulletBody.transform.forward * recoil, ForceMode.Impulse);

        //recoil
        //Vector3 force = (recoilOrigin.position - gunBody.transform.position).normalized * -recoil;
        Vector3 force = gunBody.transform.forward * -recoil;
        Vector3 position = recoilOrigin.position;
        gunBody.AddForceAtPosition(force, position, ForceMode.Impulse);

        //spent casing
        Rigidbody casingBody = Instantiate(spentCasing.gameObject, casingOrigin.position, casingOrigin.rotation).GetComponent<Rigidbody>();
        casingBody.velocity = gunBody.velocity;
        casingBody.AddForce(casingBody.transform.right * 0.03f + casingBody.transform.up * 0.03f, ForceMode.Impulse);
        //casingBody.AddExplosionForce(0.03f, gunBody.position, 1f, 0f, ForceMode.Impulse);
    }

    public override void TryLoadAmmo(Ammo ammo)
    {
        throw new System.NotImplementedException();
    }
}
