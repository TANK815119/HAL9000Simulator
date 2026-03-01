using System.Collections;
using System.Collections.Generic;
using Rekabsen;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Magwell : MonoBehaviour
{
    [SerializeField] private string gunName = "1911";
    [SerializeField] private bool magazineSeated = false;
    [SerializeField] private Transform seatPosition;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip magazineInsert;
    public Magazine SeatedMagazine { get; private set; }//will often be null
    private Collider trigger;
    private Rigidbody gunBody;

    // Start is called before the first frame update
    void Start()
    {
        if(this.TryGetComponent(out Collider collider))
        {
            trigger = collider;
            gunBody = trigger.attachedRigidbody;
        }
        else
        {
            Debug.LogError("Magwell requires a Collider component set as a trigger.");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        ////Debug statements
        //if (other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent(out Magazine magazineDebug))
        //{
        //    Debug.Log("Magwell: Detected magazine in magwell trigger");
        //    if(CorrectOrientation(magazineDebug))
        //    {
        //        Debug.Log("Magwell: Magazine is in correct orientation for seating");
        //    }
        //    else
        //    {
        //        Debug.Log("Magwell: Magazine is NOT in correct orientation for seating");
        //    }
        //    if(magazineDebug.GunName == gunName)
        //    {
        //        Debug.Log("Magwell: Magazine is for correct gun type");
        //    }
        //    else
        //    {
        //        Debug.Log("Magwell: Magazine is NOT for correct gun type");
        //    }
        //}

        //Magazine-based seating logic:
        if (other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent(out Magazine magazine) && CorrectOrientation(magazine) 
            && magazine.GunName == gunName && magazineSeated == false)
        {
            //attempt to eject the current seated magazine if there is one
            //if (magazineSeated)
            //{
            //    if (seatedMagazine.Equals(magazine)) { return; }
            //    EjectMagazine();
            //}
            Debug.Log("Magwell: Seating magazine in magwell");
            //seat the new magazine
            SeatedMagazine = SeatMagazine(magazine);
        }

        //Hand-based ejection logic
        //essentially pretend the hand being here is like
        //holding a gun grip
        //if(other.TryGetComponent(out GripControllerInterface gripController))
        //{
        //    if (magazineSeated)
        //    {
        //        EjectMagazine();
        //    }
        //}
    }

    private bool CorrectOrientation(Magazine magazine)
    {
        //I think I'll just take the dot product of
        //the magwell's "up" vector and the magazine's "up" vector

        float dotProduct = Vector3.Dot(this.transform.up.normalized, magazine.transform.up.normalized);
        return dotProduct > 0.8f; //arbitrary threshold for "correct" orientation
    }

    public Magazine EjectMagazine()
    {
        Debug.Assert(magazineSeated, "Attempting to eject from empty Magwell");
        return null;
    }

    private Magazine SeatMagazine(Magazine magazine)
    {
        //remove the grip on all grabsurfaces
        foreach (GrabSurface grabSurface in magazine.GrabSurfaces)
        {
            grabSurface.Handedness = Handedness.None;
            grabSurface.WipeGrabPoints();
        }

        //form a physics joint ebtween the gun and the magazine
        //that translates and rotates the magazine to the seat position
        //pretty much copies code in GripController for forming grips
        //except this grip is between the gun and the maga
        Vector3 magPosition = magazine.transform.position;
        Quaternion magRotation = magazine.transform.rotation;
        magazine.transform.position = seatPosition.position;
        magazine.transform.rotation = seatPosition.rotation;

        //form a joint with the magazine
        FixedJoint joint = gunBody.gameObject.AddComponent<FixedJoint>(); //maybe store in field

        //@TODO: Re-implememnt smoother configurable joint
        ////position
        //joint.configuredInWorldSpace = false;
        joint.connectedBody = magazine.MagazineBody;
        //joint.autoConfigureConnectedAnchor = true;
        ////joint.connectedAnchor = Vector3.zero;
        ////joint.anchor = joint.transform.InverseTransformVector(this.GetCurrParentOffset());

        //////rotation
        ////joint.targetRotation = this.GetCurrParentRotationOffset() * gunBody.transform.rotation * Quaternion.Inverse(magazine.transform.rotation);

        ////set up joint drives
        //JointDrive jointDrive = new JointDrive();
        //jointDrive.positionSpring = Mathf.Infinity;
        //jointDrive.positionDamper = 0f;
        //jointDrive.maximumForce = Mathf.Infinity;
        //joint.xDrive = jointDrive;
        //joint.yDrive = jointDrive;
        //joint.zDrive = jointDrive;
        //joint.angularXDrive = jointDrive;
        //joint.angularYZDrive = jointDrive;

        //magazine.transform.position = magPosition;
        //magazine.transform.rotation = magRotation;

        ////disable collisions between the magazine and the gun
        //for (int i = 0; i < magazine.Colliders.Length; i++)
        //{
        //    GripControllerLocal.SetAllToCollision(magazine.Colliders[i], gunBody.transform, true);
        //}

        //play audio
        audioSource.PlayOneShot(magazineInsert, 0.2f);

        magazineSeated = true;

        return magazine;
    }

    public Vector3 GetCurrParentOffset()
    {
        return transform.position - gunBody.transform.position;
    }

    public Quaternion GetCurrParentRotationOffset()
    {
        return transform.rotation * Quaternion.Inverse(gunBody.transform.rotation);
    }
}
