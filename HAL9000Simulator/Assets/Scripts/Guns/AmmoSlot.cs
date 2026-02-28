using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AmmoSlot : MonoBehaviour
{
    private Firearm firearm;
    private void OnTriggerEnter(Collider other)
    {
        //make sure there is a connected firearm
        if(firearm == null)
        {
            Debug.LogError("The ammo slot has no connected firearm");
            return;
        }

        if(other.TryGetComponent(out Ammo ammo))
        {
            firearm.TryLoadAmmo(ammo);
        }
    }

    public void SetFirearm(Firearm gun)
    {
        firearm = gun;
    }
}
