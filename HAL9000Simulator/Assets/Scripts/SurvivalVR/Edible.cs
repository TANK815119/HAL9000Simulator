using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rekabsen
{
    [RequireComponent(typeof(Collider))]
    public class Edible : MonoBehaviour
    {
        [field: SerializeField] public float Nourishment { get; private set; }
        [field: SerializeField] public GameObject Leftovers { get; private set; }
    }
}