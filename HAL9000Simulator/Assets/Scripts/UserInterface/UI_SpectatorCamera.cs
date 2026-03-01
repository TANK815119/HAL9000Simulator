using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Rekabsen
{
    [RequireComponent(typeof(InterfaceButton))]
    public class UI_SpectatorCamera : MonoBehaviour
    {
        private InterfaceButton button;
        [SerializeField] private GameObject spectatorCamera;
        [SerializeField] private GameObject playerCamera;
        [SerializeField] private TMP_Text displayText;
        private bool on = false;

        // Start is called before the first frame update
        void Start()
        {
            button = GetComponent<InterfaceButton>();
        }

        // Update is called once per frame
        void Update()
        {
            if (button.Selected)
            {
                ChangeCamera();
            }
        }

        private void ChangeCamera()
        {
            if(!on)//turn on
            {
                spectatorCamera.transform.rotation = playerCamera.transform.rotation;
                spectatorCamera.SetActive(true);
                displayText.text = "\n Spectator       on";
                on = true;
            }
            else //turn off
            {
                spectatorCamera.SetActive(false);
                displayText.text = "\n Spectator       off";
                on = false;
            }
        }
    }
}