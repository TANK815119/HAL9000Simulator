using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrecludeBodyCollision : MonoBehaviour
{
    [SerializeField] private List<Collider> colliderList;
    void Start()
    {
        for(int x = 0; x < colliderList.Count; x++)
        {
            for(int y = 0; y < colliderList.Count; y++)
            {
                Physics.IgnoreCollision(colliderList[x], colliderList[y], true);
            }
        }
    }
}
