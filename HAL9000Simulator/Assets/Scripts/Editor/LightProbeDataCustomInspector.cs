using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Rekabsen
{
    [CustomEditor(typeof(LightProbeDataAsset))]
    public class LightProbeDataCustomInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            LightProbeDataAsset lightProbeData = (LightProbeDataAsset)target;
            if (GUILayout.Button("Update LightProbeDataAsset"))
            {
                lightProbeData.UpdateProbes();
            }
        }
    }
}