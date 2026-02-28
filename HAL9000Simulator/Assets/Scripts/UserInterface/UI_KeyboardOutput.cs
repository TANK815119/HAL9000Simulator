using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class UI_KeyboardOutput : MonoBehaviour
{
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private int characterLimit = 64;
    private string text = "";
    
    public void AddString(string s)
    {
        if(text.Length < characterLimit)
        {
            text += s;
        }
        
        UpdateTextMesh();
    }
    public void Backspace()
    {
        if(text.Length > 0)
        {
            text = text.Substring(0, text.Length - 1);
        }

        UpdateTextMesh();
    }

    private void UpdateTextMesh()
    {
        displayText.text = this.text;
    }

    public string GetText()
    {
        return text;
    }
}
