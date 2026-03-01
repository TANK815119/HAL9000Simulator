using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rekabsen
{
    //simply stores light probe data cause unity wont
    //ill need UI buttons to actually use this in my buttonswitchlighting script
    //the data must be recorded
    [CreateAssetMenu(fileName = "LightProbeDataAsset", menuName = "Lighting/LightProbeDataAsset")]
    public class LightProbeDataAsset : ScriptableObject
    {
        //public Vector3[] Positions { get; set; } --not even mutable in LightProbes, so not necessary
        public SphericalHarmonicsL2[] coefficients;

        public void UpdateProbes()
        {
            // Get the original baked probes array
            SphericalHarmonicsL2[] originalProbes = LightmapSettings.lightProbes.bakedProbes;

            // Create a new array with the same length as the original
            coefficients = new SphericalHarmonicsL2[originalProbes.Length];

            // Copy each element from the original array to the new array
            for (int i = 0; i < originalProbes.Length; i++)
            {
                coefficients[i] = CopySphericalHarmonicsL2(originalProbes[i]);
            }

            // Mark the ScriptableObject as dirty to ensure changes are saved
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            #endif
        }

        private SphericalHarmonicsL2 CopySphericalHarmonicsL2(SphericalHarmonicsL2 original)
        {
            SphericalHarmonicsL2 copy = new SphericalHarmonicsL2();
            for (int rgb = 0; rgb < 3; rgb++)
            {
                for (int coefficient = 0; coefficient < 9; coefficient++)
                {
                    copy[rgb, coefficient] = original[rgb, coefficient];
                }
            }
            return copy;
        }
    }
}