using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_KeyboardSymbol : MonoBehaviour
{
    [SerializeField] private UI_KeyboardOutput keyboard;
    [SerializeField] private string letter;
    [SerializeField] private bool backspace = false;
    private InterfaceButton button;
    // Start is called before the first frame update
    void Start()
    {
        button = GetComponent<InterfaceButton>();
    }

    // Update is called once per frame
    void Update()
    {
        if(button.Selected && !backspace)
        {
            keyboard.AddString(letter);
        }
        else if(button.Selected && backspace)
        {
            keyboard.Backspace();
        }
    }
}
