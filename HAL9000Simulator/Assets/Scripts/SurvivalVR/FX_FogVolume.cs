using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FX_FogVolume : MonoBehaviour
{
    [SerializeField] private Color fogColor = new Color(0.0f, 0.5f, 0.7f, 1.0f);
    [SerializeField] private float fogDensity = 0.1f;
    [SerializeField] private float defaultCutoffAudioFrequency = 22000f;
    [SerializeField] private float volumeCutoffAudioFrequency = 500f; //muffled

    private void OnTriggerStay(Collider other) //may not be performant, was originally OnTriggerEnter, bu thtat had problems on volume seems
    {
        if(other.CompareTag("PlayerCamera"))
        {
            //visuals
            RenderSettings.fog = true;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = fogDensity;

            //audio
            if (other.gameObject.TryGetComponent(out AudioLowPassFilter filter))
            {
                filter.cutoffFrequency = volumeCutoffAudioFrequency;
            }
            else
            {
                for (int i = 0; i < other.transform.childCount; i++)
                {
                    if (other.transform.GetChild(i).gameObject.TryGetComponent(out AudioLowPassFilter childFilter))
                    {
                        childFilter.cutoffFrequency = volumeCutoffAudioFrequency;
                    }
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("PlayerCamera"))
        {
            // viual
            RenderSettings.fog = false;

            //audio
            if (other.gameObject.TryGetComponent(out AudioLowPassFilter filter))
            {
                filter.cutoffFrequency = defaultCutoffAudioFrequency;
            }
            else
            {
                for (int i = 0; i < other.transform.childCount; i++)
                {
                    if (other.transform.GetChild(i).gameObject.TryGetComponent(out AudioLowPassFilter childFilter))
                    {
                        childFilter.cutoffFrequency = defaultCutoffAudioFrequency;
                    }
                }
            }
        }
    }
}
