using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// creates the physics for acurate movement in bodies of water

[RequireComponent(typeof(Rigidbody))]
public class Buoyancy : MonoBehaviour
{
    [SerializeField] private float staticDrag = 5f;
    [SerializeField] private float angularDragCoefficient = 5f;
    [SerializeField] private bool useHydraulicRotationalDrag = true;

    private List<Collider> waterTriggers;
    private List<ColliderData> colliderData;
    private Rigidbody rb;
    private bool floating = false;
    private float submergedCrossSection = 0f;
    private float wholeCrossSection = 0f;

    //for vfx
    [SerializeField] private GameObject splashVisual;
    [SerializeField] private AudioClip splashSound;
    private float minimumEffectVelocity = 0.25f;
    private bool impact = false;
    private bool leftWater = true;
    private float cooldown = 0f;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        waterTriggers = new List<Collider>();
        colliderData = new List<ColliderData>();

        //fetch ther colliders associated with the rigidbody
        if (TryGetComponent(out Collider thisCollider) && thisCollider.isTrigger == false) //collider on this object
        {
            colliderData.Add(new ColliderData(thisCollider, ColliderVolume(thisCollider), AproximateCrossSectionalArea(thisCollider)));
        }

        Collider[] allColliders = GetComponentsInChildren<Collider>(); //all child colliders
        for(int i = 0; i < allColliders.Length; i++)
        {
            if (allColliders[i].attachedRigidbody.Equals(rb) && allColliders[i].isTrigger == false) //cull for associoated colliders
            {
                colliderData.Add(new ColliderData(allColliders[i], ColliderVolume(allColliders[i]), AproximateCrossSectionalArea(allColliders[i])));
            }
        }

        //caclulate the wholeVolume and wholeCorssSection
        for(int i = 0; i < colliderData.Count; i++)
        {
            wholeCrossSection += colliderData[i].CrossSection;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (floating)
        {
            submergedCrossSection = 0f;
            
            for (int i = 0; i < colliderData.Count; i++)
            {
                //update the mutable values of colliderData for FractionSubmerged and Submerged
                UpdateColliderData(colliderData[i]);

                if(colliderData[i].Submerged)
                {
                    //calculate and apply the buoyancy force at the collider
                    ApplyArchemedicBouyancy(colliderData[i], 1000f); //hard coded fluid density

                    //calculate and apply the Hydraulic drag at the collider
                    //make sure to utilize both velocity and
                    //"real" angular velocty in whcihever direction is the tangent of rotation and distance from center of mass
                    ApplyHydraulicDrag(colliderData[i], staticDrag, 1000f); //hard coded fluid density

                    
                }
            }

            //apply rotation drag using air equation
            float dragCoefficient = angularDragCoefficient * (submergedCrossSection / wholeCrossSection);
            ApplyAngularNewtonianDrag(dragCoefficient);

            //VFX
            if (impact && splashVisual != null && splashSound != null && submergedCrossSection > 0f)
            {
                ImpactEffects();
                impact = false;
            }
        }
        else //essentuially, when in air
        {
            //make sure the objec does not exceed its terminal velocity
            for(int i = 0; i < colliderData.Count; i++)
            {
                //update all collider data for air
                colliderData[i].FractionSubmerged = 1f; //totally submereged in air

                //calculate and apply the buoyancy force at the collider
                ApplyArchemedicBouyancy(colliderData[i], 1.25f); //hard coded fluid density of air

                //calculate and apply the Hydraulic drag at the collider
                ApplyHydraulicDrag(colliderData[i], staticDrag, 1.25f); //hard coded fluid density

            }
        }

        if(cooldown >= 0f)
        {
            cooldown -= Time.fixedDeltaTime;
        }
    }

    //new methods-----------------------------------------------------------------

    private void UpdateColliderData(ColliderData colData)
    {
        colData.FractionSubmerged = GetFractionSubmerged(colData.Collider.bounds);
        submergedCrossSection += colData.CrossSection * colData.FractionSubmerged;

        if (colData.FractionSubmerged > 0f)
        {
            colData.Submerged = true;
        }
        else
        {
            colData.Submerged = false;
        }
    }

    private void ApplyArchemedicBouyancy(ColliderData colData, float fluidDensity)
    {
        //calculate the bouyant force
        float displacedVolume = colData.Volume * colData.FractionSubmerged;
        float force = ArchimedesBouyantForce(displacedVolume, fluidDensity);

        //calculate the world space poition of the collider center
        Vector3 colliderCenter = GetColliderWorldCenter(colData.Collider);

        //apply the force
        rb.AddForceAtPosition(Vector3.up * force, colliderCenter);
    }

    private void ApplyHydraulicDrag(ColliderData colData, float statDrag, float fluidDensity)
    {
        //caclulate positional vector from purely rigidbody
        Vector3 positionalVelocity = rb.velocity;

        //calculate tangential velocity at the center of the collider
        Vector3 relativePosition = Vector3.zero;
        Vector3 rotationalVelocity = Vector3.zero;
        if(useHydraulicRotationalDrag)
        {
            relativePosition = GetColliderWorldCenter(colData.Collider) - (transform.position + rb.centerOfMass);
            rotationalVelocity = Vector3.Cross(rb.angularVelocity, relativePosition);
        }

        //add the two forces together
        Vector3 totalVelocity = positionalVelocity + rotationalVelocity;

        //calculate drag
        float crossSectionArea = colData.CrossSection * colData.FractionSubmerged;
        Vector3 staticDragForce = CalculateDragForce(fluidDensity, statDrag, crossSectionArea, totalVelocity);

        //make sure the force is physically possible
        float linearKineticEnergy = 0.5f * rb.mass * rb.velocity.sqrMagnitude; // F = 0.5mv^2

        float rotationalInertia = rb.mass * relativePosition.sqrMagnitude; // Simplified moment of inertia approximation that only works when useHydraulicRotationalDrag
        float rotationKineticEnergy = 0.5f * rotationalInertia * rb.angularVelocity.sqrMagnitude; //F = 0.5IW^2

        float kineticEnergyParcel = (linearKineticEnergy + rotationKineticEnergy); // calculates how much KE is in this part of the object, assuming uniform denstity

        float reactiveKineticEnergy = staticDragForce.magnitude * totalVelocity.magnitude; // Force times distance gives energy

        if (reactiveKineticEnergy > kineticEnergyParcel*10f)
        {
            staticDragForce *= (kineticEnergyParcel*10f / reactiveKineticEnergy); // Scale down to physical limit
            //must mean there is some physical impossibility happening
        }

        //addfore in the opposite direction
        rb.AddForceAtPosition(staticDragForce, GetColliderWorldCenter(colData.Collider));
    }

    private void ApplyAngularNewtonianDrag(float dragCoefficient)
    {
        rb.angularVelocity = rb.angularVelocity * (1f - dragCoefficient * Time.fixedDeltaTime);
    }

    private void ImpactEffects() // (should) only be called after the object has been in the water for one physics update
    {
        Vector3 impactPoisition = EstimateImpactPosition();

        //visual
        Instantiate(splashVisual, impactPoisition, Quaternion.identity);

        //audio
        float volume = Mathf.Min(rb.velocity.magnitude / 20f, 1f);
        
        AudioSource.PlayClipAtPoint(splashSound, impactPoisition, volume); ; //no volume set

        cooldown = 0.25f; //hard coded cooldown length
    }


    //new helper methods-----------------------------------------------------------

    private float GetFractionSubmerged(Bounds colliderBounds)
    {
        float boundVolume = colliderBounds.extents.x * colliderBounds.extents.y * colliderBounds.extents.z;
        float boundVolumeSubmerged = 0f;

        for(int i = 0; i < waterTriggers.Count; i++)
        { 
            if(waterTriggers[i].bounds.Intersects(colliderBounds))
            {
                Bounds intersectionBounds = GetIntersection(waterTriggers[i].bounds, colliderBounds);
                boundVolumeSubmerged += intersectionBounds.extents.x * intersectionBounds.extents.y * intersectionBounds.extents.z;
            }
        }

        return boundVolumeSubmerged / boundVolume;
    }

    private Vector3 GetColliderWorldCenter(Collider collider)
    {
        return collider switch
        {
            BoxCollider boxCollider => boxCollider.transform.TransformPoint(boxCollider.center),
            SphereCollider sphereCollider => sphereCollider.transform.TransformPoint(sphereCollider.center),
            CapsuleCollider capsuleCollider => capsuleCollider.transform.TransformPoint(capsuleCollider.center),
            MeshCollider meshCollider => meshCollider.bounds.center,
            _ => collider.transform.position
        };
    }

    private float AproximateCrossSectionalArea(Collider collider) //onjly needs to be calleed once cause it doesnt include roation
    {
        Vector3 localScale = collider.transform.lossyScale;

        switch (collider)
        {
            case BoxCollider boxCollider: return Mathf.Max(boxCollider.size.x * localScale.x, boxCollider.size.z * localScale.z) * boxCollider.size.y * localScale.y;
            case SphereCollider sphereCollider: return Mathf.PI * Mathf.Pow(sphereCollider.radius * localScale.x, 2f);
            case CapsuleCollider capsuleCollider: return Mathf.PI * Mathf.Pow(capsuleCollider.radius * localScale.x, 2f) + 2f * capsuleCollider.radius * localScale.x * capsuleCollider.height * localScale.y;
            default: Debug.LogError("Collider not supported for cross-sectional calculations");
                return 0f;
        }
    }

    private Vector3 CalculateDragForce(float fluidDensity, float dragCoefficient, float crossSection, Vector3 velocity)
    {
        return -0.5f * fluidDensity * dragCoefficient * crossSection * Mathf.Pow(velocity.magnitude, 2f) * velocity.normalized;

    }

    private Vector3 EstimateImpactPosition() // can only be called after the object has been in the water for one physics update
    {
        Vector3 impactPoint = Vector3.zero;

        //position of first underwater collider
        for(int i = 0; i < colliderData.Count; i++)
        {
            if(colliderData[i].Submerged)
            {
                impactPoint = GetColliderWorldCenter(colliderData[i].Collider);
            }
        }

        //bring y value to surface of first waterTrigger
        impactPoint.y = waterTriggers[0].bounds.extents.y  + GetColliderWorldCenter(waterTriggers[0]).y;

        return impactPoint;
    }

    private void CullWaterTriggers() //removes watertriggers that arent intersected with at all
    {
        for(int i = 0; i < waterTriggers.Count; i++)
        {
            bool usefuleTrigger = false;
            for (int j = 0; j < colliderData.Count; j++)
            {
                if (waterTriggers[i].bounds.Intersects(colliderData[j].Collider.bounds))
                {
                    usefuleTrigger = true;
                }
            }

            if(!usefuleTrigger)
            {
                waterTriggers.RemoveAt(i);
            }
        }
    }

    //old methods-------------------------------------------------------------------

    private float ArchimedesBouyantForce(float displacedVolume, float fluidDensity)
    {
        return -1f * fluidDensity * Physics.gravity.y * displacedVolume;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer == 8 && !waterTriggers.Contains(other)) //8 is water layer
        {
            waterTriggers.Add(other);
            floating = true;
        }

        //effects
        if(cooldown <= 0f && rb.velocity.magnitude > minimumEffectVelocity && leftWater == true) //could probably set more condisitons here
        {
            impact = true;
        }
        leftWater = false;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == 8 && waterTriggers.Contains(other)) //8 is water layer
        {
            CullWaterTriggers(); //essentially a fancy(and expensive) remove)

            if (waterTriggers.Count == 0)
            {
                floating = false;
                leftWater = true;
            }
        }
    }

    private float ColliderVolume(Collider collider)
    {
        Vector3 localScale = collider.transform.lossyScale;

        if (collider is BoxCollider)
        {
            BoxCollider box = (BoxCollider)collider;
            Vector3 size = box.size;
            return size.x * localScale.x * size.y * localScale.y * size.z * localScale.z;
        }
        else if (collider is SphereCollider)
        {
            SphereCollider sphere = (SphereCollider)collider;
            float radius = sphere.radius;
            return (4f / 3f) * Mathf.PI * Mathf.Pow(radius * localScale.x, 3); // Assuming uniform scale
        }
        else if (collider is CapsuleCollider)
        {
            CapsuleCollider capsule = (CapsuleCollider)collider;
            float radius = capsule.radius;
            float height = capsule.height - 2 * radius;

            float scaledRadius = radius * Mathf.Max(localScale.x, localScale.z); // Assuming uniform scale for radius
            float scaledHeight = height * localScale.y;

            float volumeCaps = Mathf.PI * Mathf.Pow(scaledRadius, 2) * scaledHeight;
            float volumeSpheres = (4f / 3f) * Mathf.PI * Mathf.Pow(scaledRadius, 3);
            return volumeCaps + volumeSpheres;
        }
        else
        {
            Debug.LogError("Collider type not supported for volume calculation.");
            return 0;
        }
    }

    private Bounds GetIntersection(Bounds a, Bounds b)
    {
        Vector3 min = Vector3.Max(a.min, b.min);
        Vector3 max = Vector3.Min(a.max, b.max);

        if (min.x < max.x && min.y < max.y && min.z < max.z)
        {
            return new Bounds((min + max) / 2, max - min);
        }
        else
        {
            // Return an empty bounds if there is no intersection
            return new Bounds();
        }
    }

    private class ColliderData
    {
        public Collider Collider { get; }
        public float Volume { get; }
        public float CrossSection { get; }
        public float FractionSubmerged { get; set; } //value between 0 and 1
        public bool Submerged { get; set; }

        public ColliderData(Collider collider, float volume, float crossSection)
        {
            Collider = collider;
            Volume = volume;
            CrossSection = crossSection;
            FractionSubmerged = 0f;
            Submerged = false;
        }
    }

    public bool IsSubmerged()
    {
        return (floating && submergedCrossSection / wholeCrossSection > 0.5f);
    }
}
