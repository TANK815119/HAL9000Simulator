using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.XR;

namespace Rekabsen
{
    [RequireComponent(typeof(Collider))]
    public class GrabSurface : MonoBehaviour
    {
        [field: SerializeField] public bool SoftGrip { get; private set; } // not implemented yet

        [field: SerializeField] public SurfaceType Surface { get; set; }
        [field: SerializeField] public GrabPose Pose { get; set; }
        [field: SerializeField] public float Priority { get; set; } = 1.0f; // grip prioirty in range from 0.5(least) to 2(most)
        [field: SerializeField] public Handedness Handedness { get; set; } = Handedness.Both;
        [field: SerializeField] public bool MonoDirectional { get; set; } = false; //if true, can only be grabbed from one direction
        [field: SerializeField, HideInInspector] public Transform HandOffset { get; set; } //the offset transform for the hand when grabbed
        [field: SerializeField, HideInInspector] public  Vector3 LocalHandOffset { get; set; } //the local offset vector for the hand when grabbed
        [field: SerializeField, HideInInspector] public GameObject MarkerPrefab { get; set; }
        [field: SerializeField, HideInInspector] public bool ShowGizmo { get; set; }  //only functions for planes and corners
        [field: SerializeField, HideInInspector] public float GizmoScale { get; set; } = 0.1f; //only functions for planes and corners

        //binds for the main grab point that is currently grabbing this surface
        public GrabPoint MainGrabPoint { get; private set; }
        public Rigidbody ParentBody => MainGrabPoint.ParentBody;
        public Transform ParentTrans => MainGrabPoint.ParentTrans;
        public GameObject Hand => MainGrabPoint.Hand;
        public ConfigurableJoint Joint => MainGrabPoint.Joint;

        //public event OnGrabbed;
        public delegate void OnGrabbedEvent();
        public OnGrabbedEvent OnGrabbed;

        //public event OnDropped;
        public delegate void OnUnGrabbedEvent();
        public OnUnGrabbedEvent OnUngrabbed;

        public bool RightHandGrabbed { get; set; }
        public bool LeftHandGrabbed { get; set; }

        private Collider trigCol;
        private List<GrabPoint> grabList; //list of nearby grabpoints
        private List<Transform> followList;  //list of nearby hands

        // Start is called before the first frame update
        void Start()
        {
            RightHandGrabbed = false;
            LeftHandGrabbed = false;

            trigCol = gameObject.GetComponent<Collider>();
            grabList = new List<GrabPoint>(); //grab point list
            followList = new List<Transform>(); //following grab points
        }

        // Update is called once per frame
        void Update()
        {
            UpdateHandGrabbedness();
            if (grabList.Count != 0)
            {
                for (int i = 0; i < followList.Count; i++)
                {
                    switch (Surface)
                    {
                        case SurfaceType.Plane: UpdatePlaneFollow(grabList[i].gameObject, followList[i], 0.1f); break;//hard coded hand length
                        case SurfaceType.Corner: UpdateCornerFollow(grabList[i].gameObject, followList[i], 0.1f); break;//hard coded hand length
                        case SurfaceType.Sphere: UpdateSphereFollow(grabList[i].gameObject, followList[i], 0.1f); break;//hard coded hand length
                        case SurfaceType.Cylinder: UpdateCylinderFollow(grabList[i].gameObject, followList[i], 0.1f); break;//hard coded hand length
                        case SurfaceType.Line: UpdateLineFollow(grabList[i].gameObject, followList[i], 0.1f); break;//hard coded hand length
                        case SurfaceType.Point: UpdatePointFollow(grabList[i].gameObject, followList[i], 0.1f); break;//hard coded hand length
                        default: UpdatePlaneFollow(grabList[i].gameObject, followList[i], 0.1f); break;//hard coded hand length
                    }
                }
            }
        }

        private void UpdateHandGrabbedness() //chack if any of the GrabPoints have been grabbed
        {
            //clear if there's nopthing in the list
            if (grabList == null || grabList.Count == 0)
            {
                //Debug.Log("cleared right");
                LeftHandGrabbed = false;
                RightHandGrabbed = false;
                return;
            }

            //check for grabbed grabPoints and set grabbedness accordingly
            //RightHandGrabbed = false;
            //LeftHandGrabbed = false;
            for (int i = 0; i < grabList.Count; i++)
            {
                if (grabList[i].Grabbed)
                {
                    if (grabList[i].IsRightController)
                    {
                        //Debug.Log("grabbed right");
                        RightHandGrabbed = true;
                    }
                    else
                    {
                        LeftHandGrabbed = true;
                    }
                }
            }
        }

        public GrabPoint FollowHand(Transform handTrans)
        {
            //base cases
            if (handTrans == null)
            {
                Debug.LogError("The following hand transform in null");
                return null;
            }
            if (followList == null)
            {
                Debug.LogError("The followList is null");
                return null;
            }

            //stow the hand transform for tracking in update()
            followList.Add(handTrans);

            //make a grabPoint that will be mainpulated by the transform of the hand
            GameObject grabObject = new GameObject();
            grabObject.name = "FollowGrabPoint";
            grabObject.transform.parent = transform.parent;

            //GameObject grabMarker = Instantiate(markerPrefab, Vector3.zero, Quaternion.identity);
            //grabMarker.transform.parent = grabObject.transform;
            //grabMarker.transform.localPosition = Vector3.zero;
            //grabMarker.transform.localRotation = Quaternion.identity;

            GrabPoint grabPoint = grabObject.AddComponent<GrabPoint>();
            grabPoint.SoftGrip = SoftGrip;
            grabPoint.Pose = Pose;
            grabPoint.Priority = Priority;
            grabPoint.Handedness = Handedness;
            grabPoint.MonoDirectional = MonoDirectional;
            grabPoint.HandOffset = HandOffset;
            grabObject.transform.position = transform.position;
            grabObject.transform.rotation = transform.rotation;

            //tell the grabPoint who its manager is
            grabPoint.GrabSurface = this;

            //stow the grabPopint at the same index as the handTrans
            grabList.Add(grabPoint);

            return grabPoint;
        }

        public GrabPoint UnfollowHand(Transform handTrans) // precondition: handTrans and grabObject are in the list
        {
            //search and destroy both at index
            int index = 0;
            while (index < followList.Count && !followList[index].Equals(handTrans))
            {
                index++;
            }
            followList.RemoveAt(index);
            GrabPoint grabPoint = grabList[index];
            Destroy(grabList[index].gameObject);
            grabList.RemoveAt(index);
            return grabPoint;
        }

        public GrabPoint FetchGrab(Transform handTrans)
        {
            //search and destroy both at index
            int index = 0;
            while (index < followList.Count)
            {
                if (followList[index].Equals(handTrans))
                {
                    return grabList[index];
                }
                index++;
            }
            return null;
        }

        public void SubscribeMainGrabbedPoint(GrabPoint grabPoint)
        {
            MainGrabPoint = grabPoint;
        }

        public void UnsubScribeMainGrabPoint(GrabPoint grabPoint)
        {
            if (MainGrabPoint == grabPoint)
            {
                MainGrabPoint = null;
            }
        }

        public void WipeGrabPoints()
        {
            //attempt to destroy main grab point
            if (MainGrabPoint != null && MainGrabPoint.Grabbed)
            {
                GameObject hand = MainGrabPoint.Hand;

                //attempt to destroy the grip controller's grip
                if (hand.TryGetComponent(out GripControllerInterface gripController))
                {
                    //Debug.Log("GrabUpdater: " + this.gameObject.name + ": Destroying grip on hand " + Hand.name);
                    gripController.DestroyGrip();
                    GrabPoint grabPoint = UnfollowHand(hand.transform);
                    gripController.RemoveGrabPointReference(grabPoint);
                }
                MainGrabPoint = null;
            }

            //wipe all othe grab points
            int depth = 0;
            while (grabList.Count > 0 && depth < 99)
            {
                GrabPoint grabPoint = grabList[0];
                GameObject hand = grabPoint.Hand;
                if (hand != null && grabPoint.Hand.TryGetComponent(out GripControllerInterface gripController))
                {
                    //Debug.Log("GrabUpdater: " + this.gameObject.name + ": Destroying grip on hand " + grabPoint.Hand.name);
                    gripController.RemoveGrabPointReference(grabPoint);
                }
                grabList.RemoveAt(0);
                RightHandGrabbed = false;
                LeftHandGrabbed = false;
                Destroy(grabPoint.gameObject);
                depth++;
            }

            followList.Clear();

            //Debug.Log("GrabSurface: depth: " + depth);
        }    

        /*
        Every method past this point is essentially a different surface,
        each one having slightly different mathemeatical calculations to place and rotate the FollowGripPoint
        */

        private void UpdatePlaneFollow(GameObject grabPoint, Transform handTrans, float handLength) //box collider
        {
            //calculate the offset handPosition and rotation
            Vector3 handPosition = handTrans.position + handLength * handTrans.up;
            Quaternion handRotation = handTrans.rotation;

            //find the position
            Vector3 planeNormal = trigCol.transform.up; // Get the normal of the plane
            Vector3 planePoint = trigCol.transform.position; // Get any point on the plane

            Vector3 directionToObject = handPosition - planePoint; // Vector from any point on the plane to the object
            float distance = Vector3.Dot(directionToObject, planeNormal); // Project the vector onto the plane's normal

            Vector3 closestPoint = handPosition - distance * planeNormal; // Subtract the projected distance from the object's position
            closestPoint = trigCol.ClosestPoint(closestPoint); // constrain the position to the collider
            Debug.DrawLine(handPosition, closestPoint, Color.red); // Draw a line from the object to the closest point on the plane

            grabPoint.transform.position = closestPoint;

            //find the rotation
            grabPoint.transform.rotation = handRotation;
            grabPoint.transform.parent = trigCol.transform;
            grabPoint.transform.localRotation = Quaternion.Euler(90f, grabPoint.transform.localRotation.eulerAngles.y, grabPoint.transform.localRotation.eulerAngles.z);

            //reset hierarchy
            grabPoint.transform.parent = trigCol.transform.transform.parent;

            //include hand offset
            if (HandOffset != null)
            {
                grabPoint.transform.position += HandOffset.position - this.transform.position;
                grabPoint.transform.rotation *= Quaternion.Inverse(this.transform.rotation) * HandOffset.rotation;
            }
        }

        private void UpdateCornerFollow(GameObject grabPoint, Transform handTrans, float handLength) //cylinder collider
        {
            //calculate the offset handPosition and rotation
            Vector3 handPosition = handTrans.position + handLength * handTrans.up;
            Quaternion handRotation = handTrans.rotation;

            //find position
            CapsuleCollider capCol = (CapsuleCollider)(trigCol);
            Vector3 lineEnd = capCol.transform.position + capCol.transform.up * capCol.height / 2f;
            Vector3 lineStart = capCol.transform.position - capCol.transform.up * capCol.height / 2f;
            Vector3 lineDirection = (lineEnd - lineStart).normalized; // Direction of the line

            Vector3 pointToLineStart = lineStart - handPosition;
            float distanceAlongLine = Vector3.Dot(pointToLineStart, lineDirection); // Project the vector onto the line's direction

            Vector3 closestPointOnLine = lineStart - distanceAlongLine * lineDirection; // Calculate the closest point on the line
            closestPointOnLine = trigCol.ClosestPoint(closestPointOnLine); // constrain the position to the collider
            grabPoint.transform.position = closestPointOnLine;
            Debug.DrawLine(handPosition, grabPoint.transform.position, Color.red); // Draw a line from the object to the closest point on the plane

            //find the rotation
            grabPoint.transform.rotation = handRotation;
            grabPoint.transform.parent = trigCol.transform; //parent to collider

            //float xCompress = grabPoint.transform.localRotation.eulerAngles.x;
            //float xdiff = Quaternion.Angle(Quaternion.Euler(xCompress, 0f, 0f), Quaternion.Euler(45f, 0f, 0f));
            //if(xdiff > 45f)
            //{
            //    xCompress = 45f;
            //}
            //Quaternion anteriorQuat = Quaternion.Euler(xCompress, capCol.transform.localRotation.eulerAngles.y, capCol.transform.localRotation.eulerAngles.z + 90f);

            //gather the relative rotations of either angle possibiltiy
            Quaternion anteriorQuat = Quaternion.Euler(0f, -45f, 90f);
            Quaternion posteriorQuat = Quaternion.Euler(0f, -45f, -90f);

            grabPoint.transform.localRotation = anteriorQuat;
            float anteriorAngle = Quaternion.Angle(grabPoint.transform.rotation, handRotation);

            grabPoint.transform.localRotation = posteriorQuat;
            float posteriorAngle = Quaternion.Angle(grabPoint.transform.rotation, handRotation);

            if (anteriorAngle < //used to be mind boggling line of code
                posteriorAngle)  //compares the global rotations of the hand the the (theorhetical)grabpoints
            {
                grabPoint.transform.localRotation = anteriorQuat;
            }
            else
            {
                grabPoint.transform.localRotation = posteriorQuat;
            }

            //reset hierarchy
            grabPoint.transform.parent = trigCol.transform.transform.parent;

            //include hand offset
            if (HandOffset != null)
            {
                grabPoint.transform.position += HandOffset.position - this.transform.position;
                grabPoint.transform.rotation *= Quaternion.Inverse(this.transform.rotation) * HandOffset.rotation;
            }
        }

        private void UpdateSphereFollow(GameObject grabPoint, Transform handTrans, float handLength)
        {
            //calculate the offset handPosition and rotation
            Vector3 handPosition = handTrans.position + handLength * handTrans.up;
            Quaternion handRotation = handTrans.rotation;

            //find position
            //essentially, just use closest point for the easiest result(it is likely just the math I would do)
            SphereCollider sphererCol = (SphereCollider)(trigCol);
            Vector3 directionToTarget = handPosition - sphererCol.transform.position; // Vector from sphere center to target point
            Vector3 closestPoint = sphererCol.transform.position + directionToTarget.normalized * (sphererCol.radius * sphererCol.transform.lossyScale.x); // Closest point on sphere's surface
            grabPoint.transform.position = closestPoint;
            grabPoint.transform.position = grabPoint.transform.position + directionToTarget.normalized * Mathf.Clamp(0.01f * trigCol.transform.lossyScale.x, 0f, 0.02f); //offset out a bit

            //find rotation
            //find whatever the quaternion is for pointing the bottom of the hand twards the center of the sphereCollider;
            //or maybe the vector of the middle to the surface, then rotate the resulting rotation 90 on the x(global or local doesnt matter, just dont make it child of the collider
            //grabPoint.transform.rotation *= Quaternion.LookRotation(handPosition, grabPoint.transform.position);
            grabPoint.transform.parent = trigCol.transform;

            //make virtual plane at tangent and put grabpoint in it
            Vector3 surfaceVector = (grabPoint.transform.position - trigCol.transform.position).normalized;
            GameObject planeObject = new GameObject();
            planeObject.name = "FollowGrabPlane";
            planeObject.transform.parent = transform.parent;
            planeObject.transform.up = surfaceVector;

            //set the rotation in alignment to the virtual plane
            grabPoint.transform.parent = planeObject.transform;
            grabPoint.transform.rotation = handRotation;
            grabPoint.transform.localRotation = Quaternion.Euler(90f, grabPoint.transform.localEulerAngles.y, grabPoint.transform.localEulerAngles.z);
            //grabPoint.transform.rotation = handRotation;

            //grabPoint.transform.localRotation *= Quaternion.Euler(90f, 0f, 0f);
            //grabPoint.transform.localRotation = Quaternion.Euler(grabPoint.transform.localEulerAngles.x, 0f, grabPoint.transform.localEulerAngles.z);
            //grabPoint.transform.rotation *= Quaternion.Euler(0f, handRotation.y, 0f);

            //reset hierarchy
            grabPoint.transform.parent = trigCol.transform.transform.parent;
            DestroyImmediate(planeObject);

            //include hand offset
            if (HandOffset != null)
            {
                grabPoint.transform.position += HandOffset.position - this.transform.position;
                grabPoint.transform.rotation *= Quaternion.Inverse(this.transform.rotation) * HandOffset.rotation;
            }
        }

        private void UpdateCylinderFollow(GameObject grabPoint, Transform handTrans, float handLength)
        {
            //allot like the sphereFollow, just locked on one axis for rotation.
            //calculate the offset handPosition and rotation
            Vector3 handPosition = handTrans.position + handLength * handTrans.up;
            Quaternion handRotation = handTrans.rotation;

            //position
            CapsuleCollider capCol = (CapsuleCollider)(trigCol);
            Vector3 axisDirection = capCol.transform.up;
            Vector3 pointOnAxis = capCol.transform.position + axisDirection * Vector3.Dot(handPosition - capCol.transform.position, axisDirection);


            Vector3 direction = (handPosition - pointOnAxis).normalized; //the if statement can destroy the direction

            //limit to the height of the cylinder
            float maxDistance = capCol.height * capCol.transform.lossyScale.y * 0.5f;
            if (Vector3.Distance(pointOnAxis, capCol.transform.position) > maxDistance)
            {
                pointOnAxis = capCol.transform.position + (axisDirection * Vector3.Dot(handPosition - capCol.transform.position, axisDirection)).normalized * maxDistance;
            }

            Vector3 closestPoint = pointOnAxis + direction * (capCol.radius * capCol.transform.lossyScale.x);
            grabPoint.transform.position = closestPoint;
            grabPoint.transform.position = grabPoint.transform.position + direction.normalized * Mathf.Clamp(0.01f * trigCol.transform.lossyScale.x, 0f, 0.02f); //offset out a bit

            //rotation
            grabPoint.transform.parent = trigCol.transform;

            //make virtual plane at tangent and put grabpoint in it
            Vector3 surfaceVector = (grabPoint.transform.position - pointOnAxis).normalized;
            GameObject planeObject = new GameObject();
            planeObject.name = "FollowGrabPlane";
            planeObject.transform.parent = transform.parent;
            planeObject.transform.up = surfaceVector;

            //set the rotation in alignment to the virtual plane
            grabPoint.transform.parent = planeObject.transform;
            grabPoint.transform.rotation = capCol.transform.rotation;
            //grabPoint.transform.localRotation = Quaternion.Euler(90f, grabPoint.transform.localEulerAngles.y, grabPoint.transform.localEulerAngles.z);

            //lock to clock or counter hand position akin to corner grip
            //gather the relative rotations of either angle possibiltiy
            Quaternion clockQuat = Quaternion.Euler(90f, grabPoint.transform.localEulerAngles.y - 90f, grabPoint.transform.localEulerAngles.z);
            Quaternion counterQuat = Quaternion.Euler(90f, grabPoint.transform.localEulerAngles.y + 90f, grabPoint.transform.localEulerAngles.z);

            grabPoint.transform.localRotation = clockQuat;
            float clockAngle = Quaternion.Angle(grabPoint.transform.rotation, handRotation);

            grabPoint.transform.localRotation = counterQuat;
            float counterrAngle = Quaternion.Angle(grabPoint.transform.rotation, handRotation);

            if (clockAngle < //used to be mind boggling line of code
                counterrAngle)  //compares the global rotations of the hand the the (theorhetical)grabpoints
            {
                grabPoint.transform.localRotation = clockQuat;
            }
            else
            {
                grabPoint.transform.localRotation = counterQuat;
            }

            //reset hierarchy
            grabPoint.transform.parent = trigCol.transform.transform.parent;
            DestroyImmediate(planeObject);

            //include hand offset
            if (HandOffset != null)
            {
                grabPoint.transform.position += HandOffset.position - this.transform.position;
                
                grabPoint.transform.rotation *= Quaternion.Inverse(this.transform.rotation) * HandOffset.rotation;
            }

            grabPoint.transform.position += grabPoint.transform.TransformDirection(LocalHandOffset);
        }

        private void UpdateLineFollow(GameObject grabPoint, Transform handTrans, float handLength)
        {
            //allot like the sphereFollow, just locked on one axis for rotation.
            //calculate the offset handPosition and rotation
            Vector3 handPosition = handTrans.position + handLength * handTrans.up;
            Quaternion handRotation = handTrans.rotation;

            //position
            CapsuleCollider capCol = (CapsuleCollider)(trigCol);
            Vector3 axisDirection = capCol.transform.up;
            Vector3 pointOnAxis = capCol.transform.position + axisDirection * Vector3.Dot(handPosition - capCol.transform.position, axisDirection);

            Vector3 direction = (handPosition - pointOnAxis).normalized; //the if statement can destroy the direction

            //limit to the height of the cylinder
            float maxDistance = capCol.height * capCol.transform.lossyScale.y * 0.5f;
            if (Vector3.Distance(pointOnAxis, capCol.transform.position) > maxDistance)
            {
                pointOnAxis = capCol.transform.position + (axisDirection * Vector3.Dot(handPosition - capCol.transform.position, axisDirection)).normalized * maxDistance;
            }

            Vector3 closestPoint = pointOnAxis + direction * (0.025f);
            grabPoint.transform.position = closestPoint;

            //rotation
            grabPoint.transform.parent = trigCol.transform;

            //make virtual plane at tangent and put grabpoint in it
            Vector3 surfaceVector = (grabPoint.transform.position - pointOnAxis).normalized;
            GameObject planeObject = new GameObject();
            planeObject.name = "FollowGrabPlane";
            planeObject.transform.parent = transform.parent;
            planeObject.transform.up = surfaceVector;

            //set the rotation in alignment to the virtual plane
            grabPoint.transform.parent = planeObject.transform;
            grabPoint.transform.rotation = capCol.transform.rotation;
            //grabPoint.transform.localRotation = Quaternion.Euler(90f, grabPoint.transform.localEulerAngles.y, grabPoint.transform.localEulerAngles.z);

            //lock to clock or counter hand position akin to corner grip
            //gather the relative rotations of either angle possibiltiy
            Quaternion clockQuat = Quaternion.Euler(90f, grabPoint.transform.localEulerAngles.y - 90f, grabPoint.transform.localEulerAngles.z);
            Quaternion counterQuat = Quaternion.Euler(90f, grabPoint.transform.localEulerAngles.y + 90f, grabPoint.transform.localEulerAngles.z);

            grabPoint.transform.localRotation = clockQuat;
            float clockAngle = Quaternion.Angle(grabPoint.transform.rotation, handRotation);

            grabPoint.transform.localRotation = counterQuat;
            float counterrAngle = Quaternion.Angle(grabPoint.transform.rotation, handRotation);

            if (clockAngle < //used to be mind boggling line of code
                counterrAngle)  //compares the global rotations of the hand the the (theorhetical)grabpoints
            {
                grabPoint.transform.localRotation = clockQuat;
            }
            else
            {
                grabPoint.transform.localRotation = counterQuat;
            }

            //reset hierarchy
            grabPoint.transform.parent = trigCol.transform.transform.parent;
            DestroyImmediate(planeObject);

            //include hand offset
            if (HandOffset != null)
            {
                grabPoint.transform.position += HandOffset.position - this.transform.position;
                grabPoint.transform.rotation *= Quaternion.Inverse(this.transform.rotation) * HandOffset.rotation;
            }
        }

        private void UpdatePointFollow(GameObject grabPoint, Transform handTrans, float handLength)
        {
            grabPoint.transform.position = trigCol.transform.position;
            grabPoint.transform.rotation = trigCol.transform.rotation;

            ////reset hierarchy
            //grabPoint.transform.parent = trigCol.transform.transform.parent; //not necessary

            //include hand offset
            if (HandOffset != null)
            {
                grabPoint.transform.position += HandOffset.position - this.transform.position;
                grabPoint.transform.rotation *= Quaternion.Inverse(this.transform.rotation) * HandOffset.rotation;
            }
        }


        /*
        Every method in this section is essentially a different gizmo drawing,
        each one having slightly different calculations to represent the orientation of their respective grips
        */

        private void OnDrawGizmos()
        {
            if (ShowGizmo)
            {
                switch (Surface)
                {
                    case SurfaceType.Plane: PlaneGizmo(); break;//hard coded hand length
                    case SurfaceType.Corner: CornerGizmo(); break;//hard coded hand length
                    case SurfaceType.Point: PointGizmo(); break;//hard coded hand length
                    default: PlaneGizmo(); break;//hard coded hand length
                }
            }
        }

        private void PlaneGizmo() //up arrow in direction(normal) plane is facing
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + transform.up * 0.1f * GizmoScale, transform.position); //arrow pointing up(normal) from surface of plane
            Gizmos.DrawLine(transform.position + transform.up * 0.1f * GizmoScale, transform.position + transform.up * 0.075f * GizmoScale + transform.right * 0.025f * GizmoScale); //branch
            Gizmos.DrawLine(transform.position + transform.up * 0.1f * GizmoScale, transform.position + transform.up * 0.075f * GizmoScale - transform.right * 0.025f * GizmoScale); //branch
            Gizmos.DrawLine(transform.position + transform.up * 0.1f * GizmoScale, transform.position + transform.up * 0.075f * GizmoScale + transform.forward * 0.025f * GizmoScale); //branch
            Gizmos.DrawLine(transform.position + transform.up * 0.1f * GizmoScale, transform.position + transform.up * 0.075f * GizmoScale - transform.forward * 0.025f * GizmoScale); //branch
            Gizmos.DrawLine(transform.position, transform.position + transform.right * 0.025f * GizmoScale); //base
            Gizmos.DrawLine(transform.position, transform.position - transform.right * 0.025f * GizmoScale); //base
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.025f * GizmoScale); //base
            Gizmos.DrawLine(transform.position, transform.position - transform.forward * 0.025f * GizmoScale); //base
        }

        private void CornerGizmo() //both of the two possible orientations
        {
            Gizmos.color = Color.red;

            //negative x(tail facing) arrow
            Gizmos.DrawLine(transform.position, transform.position - transform.right * 0.1f * GizmoScale);
            Gizmos.DrawLine(transform.position, transform.position - transform.right * 0.025f * GizmoScale + transform.up * 0.025f * GizmoScale); //branch
            Gizmos.DrawLine(transform.position, transform.position - transform.right * 0.025f * GizmoScale - transform.up * 0.025f * GizmoScale); //branch

            //positive z(tail facing) arrow
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.1f * GizmoScale);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.025f * GizmoScale + transform.up * 0.025f * GizmoScale); //branch
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.025f * GizmoScale - transform.up * 0.025f * GizmoScale); //branch
        }

        private void PointGizmo()
        {
            //draw a weird low-poly hand
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position - transform.up * 0.05f * GizmoScale, transform.position - transform.forward * 0.025f * GizmoScale - transform.up * 0.05f * GizmoScale); //arrow pointing up from mid hand
            Gizmos.DrawLine(transform.position, transform.position - transform.up * 0.1f * GizmoScale); //arrow pointing forward stem
            Gizmos.DrawLine(transform.position, transform.position - transform.up * 0.05f * GizmoScale - transform.right * 0.025f * GizmoScale); //arrow pointing forward side
            Gizmos.DrawLine(transform.position, transform.position - transform.up * 0.05f * GizmoScale + transform.right * 0.025f * GizmoScale); //arrow pointing forward side
        }

        /*
        Every method in this section is essentially a different pose that generates a real hand in the 
        scene for more accurate debugging given the parameters of this component,
        each one having slightly different calculations to represent the orientation of their respective grips
        */

        public void DisplayLeftHandPose()
        {
            //HandLibrary.ReadAllResources(); //for debugging purposes, can be removed later

            GameObject fakeGrabPoint = new GameObject("fake grab point"); //will be reorientated with hand data in [GRIP]follow()
            fakeGrabPoint.transform.position = transform.position;
            fakeGrabPoint.transform.rotation = transform.rotation;

            Transform displayHand = null;
            switch (Pose)
            {
                case GrabPose.Plane: displayHand = Instantiate(HandLibrary.HND_Plane_Left).transform; break;
                case GrabPose.Corner: displayHand = Instantiate(HandLibrary.HND_Corner_Left).transform;  break;
                case GrabPose.Sphere: displayHand = Instantiate(HandLibrary.HND_Sphere_Left).transform; break;
                case GrabPose.Cylinder: displayHand = Instantiate(HandLibrary.HND_Cylinder_Left).transform; break;
                case GrabPose.Pistol: displayHand = Instantiate(HandLibrary.HND_Pistol_Left).transform; break;
                case GrabPose.PistolSecondary: displayHand = Instantiate(HandLibrary.HND_Pistol_Secondary_Left).transform; break;
                case GrabPose.PistolSlide: displayHand = Instantiate(HandLibrary.HND_Pistol_Slide_Left).transform; break;
                default: displayHand = Instantiate(HandLibrary.HND_Default_Left).transform; break;
            }

            displayHand.transform.position = transform.position;// + transform.forward * transform.lossyScale.magnitude;
            displayHand.transform.rotation = transform.rotation;

            OrientDisplayHand(fakeGrabPoint, displayHand);
        }

        public void DisplayRightHandPose()
        {
            GameObject fakeGrabPoint = new GameObject("fake grab point");

            Transform displayHand = null;
            switch (Pose)
            {
                case GrabPose.Plane: displayHand = Instantiate(HandLibrary.HND_Plane_Right).transform; break;
                case GrabPose.Corner: displayHand = Instantiate(HandLibrary.HND_Corner_Right).transform; break;
                case GrabPose.Sphere: displayHand = Instantiate(HandLibrary.HND_Sphere_Right).transform; break;
                case GrabPose.Cylinder: displayHand = Instantiate(HandLibrary.HND_Cylinder_Right).transform; break;
                case GrabPose.Pistol: displayHand = Instantiate(HandLibrary.HND_Pistol_Right).transform; break;
                case GrabPose.PistolSecondary: displayHand = Instantiate(HandLibrary.HND_Pistol_Secondary_Right).transform; break;
                case GrabPose.PistolSlide: displayHand = Instantiate(HandLibrary.HND_Pistol_Slide_Right).transform; break;
                default: displayHand = Instantiate(HandLibrary.HND_Default_Right).transform; break;
            }

            displayHand.transform.position = transform.position;// + transform.up * transform.lossyScale.magnitude;
            displayHand.transform.rotation = transform.rotation;

            OrientDisplayHand(fakeGrabPoint, displayHand);
        }

        public void ClearDisplayedHands()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.name.Contains("HND"))
                {
                    i--;
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private void OrientDisplayHand(GameObject fakeGrabPoint, Transform displayHand)
        {
            //move the grab point to the correct position given the surface type
            trigCol = gameObject.GetComponent<Collider>(); //necessary to have the collider for these functions
            switch (Surface)
            {
                case SurfaceType.Plane: 
                    UpdatePlaneFollow(fakeGrabPoint, displayHand, 0.0f); break;//hard coded hand length
                case SurfaceType.Corner:
                    displayHand.position = transform.TransformPoint(new Vector3(0f, 0f, 1f));
                    UpdateCornerFollow(fakeGrabPoint, displayHand, 0.0f); break;//hard coded hand length
                case SurfaceType.Sphere: 
                    displayHand.position = transform.TransformPoint(new Vector3(0f, 1f, 0f)); 
                    UpdateSphereFollow(fakeGrabPoint, displayHand, 0.0f); break;//hard coded hand length
                case SurfaceType.Cylinder: 
                    displayHand.position =  transform.TransformPoint(new Vector3(0f, 0f, 1f)); 
                    UpdateCylinderFollow(fakeGrabPoint, displayHand, 0.0f); break;//hard coded hand length
                case SurfaceType.Line:
                    displayHand.position = transform.TransformPoint(new Vector3(0f, 0f, 1f));
                    UpdateLineFollow(fakeGrabPoint, displayHand, 0.0f); break;//hard coded hand length
                case SurfaceType.Point: 
                    UpdatePointFollow(fakeGrabPoint, displayHand, 0.0f); break;//hard coded hand length
                default: UpdatePlaneFollow(fakeGrabPoint, displayHand, 0.0f); break;//hard coded hand length
            }

            displayHand.position = fakeGrabPoint.transform.position;
            displayHand.parent = fakeGrabPoint.transform;
            displayHand.localRotation = Quaternion.Euler(0f, 0f, 0f);
            displayHand.position = displayHand.position + displayHand.up * -0.1f * displayHand.transform.localScale.x; //this simulates the offset of the hand from the grab point
            //displayHand.localPosition = new Vector3(0f, -0.1f * displayHand.transform.localScale.x, 0f); //this simulates the offset of the hand from the grab point
            displayHand.parent = transform;
            DestroyImmediate(fakeGrabPoint); //destroy the grab point used for positioning the hand 
        }
    }
}