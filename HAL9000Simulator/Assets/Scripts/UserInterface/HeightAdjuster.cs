using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rekabsen
{
    //height adjuster hard-coded for a 190cm player model
    public class HeightAdjuster : MonoBehaviour
    {
        [SerializeField] private float playerHeight = 190f;
        [SerializeField] private float modelHeight = 190f;
        [SerializeField] private RigXRTransformer rigTransformer;
        [SerializeField] private RagdollXRTransformer ragdollTransformer;
        [SerializeField] private Transform headCamera;
        [SerializeField] private InterfaceButton incrementButton;
        [SerializeField] private InterfaceButton decrementButton;
        [SerializeField] private TMPro.TMP_Text textMesh;

        private const string PlayerHeightKey = "PlayerHeight";

        // Start is called before the first frame update
        void Start()
        {
            playerHeight = GetPlayerHeight();
            ConfigurePlayerHeight();
        }

        // Update is called once per frame
        void FixedUpdate() //has to be fixed update for some reason
        {
            //listen for button selections
            bool changed = false;
            if (incrementButton.Selected)
            {
                playerHeight += 1f;
                changed = true;
            }
            if (decrementButton.Selected)
            {
                playerHeight -= 1f;
                changed = true;
            }

            if(changed)
            {
                ConfigurePlayerHeight();
            }
        }

        private void ConfigurePlayerHeight()
        {
            //set the scale in accordance to the set height
            float scale = playerHeight / modelHeight;
            rigTransformer.PlayerHeight = playerHeight;
            rigTransformer.Scale = 1f / scale;
            ragdollTransformer.Scale = scale;
            headCamera.localScale = new Vector3(1f / scale, 1f / scale, 1f / scale) * 10f;

            //display the height
            textMesh.text = ((int)playerHeight).ToString() + "cm" + "\n"
                + ((int)(playerHeight * 0.393701f)).ToString() + "in";

            //record the height
            PlayerPrefs.SetFloat(PlayerHeightKey, playerHeight);
            PlayerPrefs.Save(); // Ensure the data is written to disk
            Debug.Log("Player height set to: " + playerHeight);
        }

        private float GetPlayerHeight()
        {
            if (PlayerPrefs.HasKey(PlayerHeightKey))
            {
                return PlayerPrefs.GetFloat(PlayerHeightKey);
            }
            else
            {
                //record the height
                PlayerPrefs.SetFloat(PlayerHeightKey, 175f);
                PlayerPrefs.Save(); // Ensure the data is written to disk
                Debug.Log("Player height set to: " + 175f);

                return 175f; // Or any default value or error indicator
            }
        }
    }
}