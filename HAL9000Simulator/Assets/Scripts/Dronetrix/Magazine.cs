using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Rekabsen;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Magazine : MonoBehaviour
{
    [SerializeField] MeshRenderer[] bulletRenderers;
    [SerializeField] private int bulletCount = 7;
    [field: SerializeField] public string GunName { get; private set; } = "1911";
    [SerializeField] private bool endless = false;
    public GrabSurface[] GrabSurfaces { get; private set; }
    public Collider[] Colliders { get; private set; }
    public Rigidbody MagazineBody { get; private set; }
    // Start is called before the first frame update
    void Start()
    {
        //fetch all grab surfaces
        GrabSurfaces = GetComponentsInChildren<GrabSurface>();
        Debug.Assert(GrabSurfaces.Length > 0, "Magazine has no GrabSurfaces attached in self or children.");

        Colliders = GetComponentsInChildren<Collider>();
        Debug.Assert(Colliders.Length > 0, "Magazine has no Colliders attached in self or children.");

        //fetch rb
        if (this.TryGetComponent(out Rigidbody rb))
        {
            MagazineBody = rb;
        }
        Debug.Assert(rb != null);

        UpdateBulletVisibility();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Removes a bullet from the current count if available.
    /// </summary>
    /// <returns><see langword="true"/> if a bullet was successfully removed; otherwise, <see langword="false"/>.</returns>
    public bool RemoveBullet()
    {
        //base case
        if (endless) { return true; }

        if (bulletCount > 0)
        {
            bulletCount--;
            UpdateBulletVisibility();
            return true;
        }

        return false;
    }

    private void UpdateBulletVisibility()
    {
        bool visible = true;
        if (bulletCount <= 0) {  visible = false; }
        for (int i = 0; i < bulletRenderers.Length; i++)
        {
            bulletRenderers[i].enabled = visible;
        }
    }
}
