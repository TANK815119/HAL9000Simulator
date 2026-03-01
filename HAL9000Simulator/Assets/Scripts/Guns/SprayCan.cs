using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class SprayCan : Firearm
{
    [SerializeField] private VisualEffect sprayEffect;
    private bool spraying = false;

    // Start is called before the first frame update
    void Start()
    {
        gunBody = GetComponent<Rigidbody>();
        inputData = gameObject.GetComponent<InputData>();
        sprayEffect.Stop();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (TriggerSqueezed())
        {
            Spray();
        }
        if (TriggerReleased())
        {
            spraying = false;
            audioSource.Stop();
            sprayEffect.Stop();
        }
    }

    private void Spray()
    {
        //recoil
        //Vector3 force = (recoilOrigin.position - gunBody.transform.position).normalized * -recoil;
        float totoalMass = gunBody.mass;
        Vector3 posteriorForce = -gunBody.transform.right * recoil * (gunBody.mass / totoalMass); //the right direction is correct due to the mannor in  which the gun was modled
        Vector3 position = recoilOrigin.position;
        gunBody.AddForceAtPosition(posteriorForce * Time.deltaTime, position, ForceMode.Impulse);

        //play audio
        if (!spraying)
        {
            sprayEffect.Play();
            audioSource.Play();
            spraying = true;
        }
    }

    public override void TryLoadAmmo(Ammo ammo)
    {
        throw new System.NotImplementedException();
    }
}
