using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UI_RelayJoinCodeDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text displayText;

    void Start()
    {
        StartCoroutine(WaitForJoinCode());
    }

    private IEnumerator WaitForJoinCode()
    {
        while (string.IsNullOrEmpty(SceneTransitionHandler.Singleton.JoinRelayCode))
        {
            yield return null; // Wait for the next frame
        }

        displayText.text = SceneTransitionHandler.Singleton.JoinRelayCode;
    }
}
