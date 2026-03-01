using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ammo : MonoBehaviour
{
    [field: SerializeField] public string AmmoType { get; set; }
    [field: SerializeField] public GameObject SpentCasing { get; set; }
}
