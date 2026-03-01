using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_NavButton : MonoBehaviour
{
    [SerializeField] private List<GameObject> showButtons;
    [SerializeField] private List<GameObject> hideButtons;

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
            for(int i = 0; i < showButtons.Count; i++)
            {
                showButtons[i].SetActive(true);
            }

            for (int i = 0; i < hideButtons.Count; i++)
            {
                hideButtons[i].SetActive(false);
            }
        }
    }
}
