 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(InterfaceButton))]
public class UI_ApplicationQuit : MonoBehaviour
{

    private InterfaceButton button;
    // Start is called before the first frame update
    void Start()
    {
        button = GetComponent<InterfaceButton>();
    }

    // Update is called once per frame
    void Update()
    {
        if(button.Selected)
        {
            Application.Quit();
        }
    }
}
