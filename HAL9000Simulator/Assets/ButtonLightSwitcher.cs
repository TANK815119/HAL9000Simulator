using System.Collections;
using System.Collections.Generic;
using Rekabsen;
using UnityEngine;

public class ButtonLightSwitcher : MonoBehaviour, ButtonActionInterface
{
    [SerializeField] private bool toggleOne = false;
    [SerializeField] private bool toggleTwo = false;
    [SerializeField] private bool toggleThree = false;

    public void Play()
    {
        if (toggleOne)
        {
            HALSingleton.Instance.ActivateLight(1);
        }
        if (toggleTwo)
        {
            HALSingleton.Instance.ActivateLight(2);
        }
        if (toggleThree)
        {
            HALSingleton.Instance.ActivateLight(3);
        }
    }
}
