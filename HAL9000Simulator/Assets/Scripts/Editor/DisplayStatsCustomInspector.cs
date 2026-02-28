using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Rekabsen
{
    [CustomEditor(typeof(DisplayStatsOnTexture))]
    public class DisplayStatsCustomInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            DisplayStatsOnTexture displayHealth = (DisplayStatsOnTexture)target;
            if (GUILayout.Button("Update Texture"))
            {
                displayHealth.UpdateStatshDisplay();
            }
        }
    }
}