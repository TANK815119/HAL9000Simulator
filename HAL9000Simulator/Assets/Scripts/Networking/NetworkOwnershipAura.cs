using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkOwnershipAura : NetworkBehaviour
{
    [SerializeField] private NetworkObject networkObject;

    private void OnTriggerStay(Collider other)
    {
        if (other.attachedRigidbody == null)
        {
            return;
        }

        if (other.attachedRigidbody.gameObject.TryGetComponent(out NetworkObject otherNetObject) && NetworkManager.Singleton.IsHost)
        {
            //avoid ownership conflicts
            NetworkObject otherPlayerNetObject = NetworkManager.Singleton.ConnectedClients[otherNetObject.OwnerClientId].PlayerObject;
            if (!otherPlayerNetObject.OwnerClientId.Equals(networkObject.OwnerClientId))
            {
                //make sure the object isnt grabbed
                if (GrabbedByOtherPlayer(other.attachedRigidbody.transform))
                {
                    return; //dont try to change ownership
                }

                //collect all the auras of the other player
                List<NetworkOwnershipAura>  auraList = new List<NetworkOwnershipAura>();
                FindAuras(otherPlayerNetObject.transform, auraList);

                //check if this NetworkOwnershipAura is closest, and assign if is
                bool closestAura = true;
                float thisAuraDist = Vector3.Distance(this.transform.position, other.attachedRigidbody.transform.position);
                for (int i = 0; i < auraList.Count; i++)
                {
                    float outherAuraDist = Vector3.Distance(auraList[i].transform.position, other.attachedRigidbody.transform.position);
                    if(outherAuraDist < thisAuraDist)
                    {
                        closestAura = false;
                    }
                }

                if(closestAura)
                {
                    //assign ownership
                    otherNetObject.ChangeOwnership(networkObject.OwnerClientId);
                    Debug.Log("attempted change " + other.gameObject.name + " to client " + networkObject.OwnerClientId);
                }
            }
        }
    }

    //alternative to this method, I could look for all the joints connected to any given component of the gameobject
    //and then check if it is either tagged or layered as the body
    //then I can get acces to the players who grabbed the object
    //but that appears not to be neccessay right now
    //private bool FindIfGrabbed(Transform parent) //checks if the object is grabbed
    //{
    //    for (int i = 0; i < parent.childCount; i++)
    //    {
    //        if(FindIfGrabbed(parent.GetChild(i)))
    //        {
    //            return true;
    //        }
    //    }

    //    if (parent.gameObject.TryGetComponent(out GrabPoint grabPoint))
    //    {
    //        if (grabPoint.Grabbed)
    //        {
    //            return true;
    //        }
    //        else
    //        {
    //            return false;
    //        }
    //    }
    //    else
    //    {
    //        return false;
    //    }
    //}
    private bool GrabbedByOtherPlayer(Transform parent) //checks if the object is grabbed
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            if (GrabbedByOtherPlayer(parent.GetChild(i)))
            {
                return true;
            }
        }

        if (parent.gameObject.TryGetComponent(out ConfigurableJoint joint))
        {
            Debug.Log("has attached joint");
            NetworkObject jointedNetObject = GetParentNetworkObject(joint.connectedBody.gameObject.transform);
            if (jointedNetObject != null)
            {
                Debug.Log("has network");
                if (!jointedNetObject.OwnerClientId.Equals(networkObject.OwnerClientId))
                {
                    Debug.Log("heterogenous");
                    return true;
                }
            }
        }
        return false;
    }

    private void FindAuras(Transform parent, List<NetworkOwnershipAura> auraList)
    {
        if (parent.gameObject.TryGetComponent(out NetworkOwnershipAura parentNetObject))
        {
            auraList.Add(parentNetObject);
        }

        for(int i = 0; i < parent.childCount; i++)
        {
            FindAuras(parent.GetChild(i), auraList);
        }
    }

    private NetworkObject GetParentNetworkObject(Transform childTransform)
    {
        Transform currentTransform = childTransform;

        while (currentTransform != null)
        {
            if (currentTransform.gameObject.TryGetComponent(out NetworkObject netObject))
            {
                return netObject;
            }

            currentTransform = currentTransform.parent;
        }

        return null; // No NetworkObject found in the parent hierarchy
    }
}
