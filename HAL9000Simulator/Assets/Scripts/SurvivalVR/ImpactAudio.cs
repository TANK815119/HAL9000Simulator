using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ImpactAudio : MonoBehaviour
{
    [SerializeField] private AudioClip audioClip;
    [SerializeField] [Range(0.0f, 2.0f)] private float volume = 1.0f;
    [SerializeField] private float forceMinimum = 2f;
    [SerializeField] private float cooldown = 0.25f;

    private Rigidbody body;
    private float cooldownState = 0f;

    private void Start()
    {
        body = gameObject.GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if(cooldownState >= 0f)
        {
            cooldownState -= Time.deltaTime;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.relativeVelocity.magnitude > forceMinimum && cooldownState <= 0f)
        {
            //modulate audio based on relative force
            float collisionVolume = 1f;
            float massScaled = 20f;
            if(body.mass < 20f)
            {
                massScaled = body.mass;
            }
            if((collision.relativeVelocity.magnitude / 10f) * (massScaled / 5) < 1f)
            {
                collisionVolume = Mathf.Pow(collision.relativeVelocity.magnitude / 10f, 2f);
            }
            AudioSource.PlayClipAtPoint(audioClip, collision.contacts[0].point, collisionVolume * volume * 0.5f);

            cooldownState = cooldown;
        }
    }
}
