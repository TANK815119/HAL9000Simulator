using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Rekabsen
{
    [CustomEditor(typeof(GrabSurface))]
    public class GrabSurfaceCustomInspector : Editor
    {
        private SerializedProperty handOffsetProp;
        private SerializedProperty localHandOffsetProp;
        private SerializedProperty markerPrefabProp;
        private SerializedProperty showGizmoProp;
        private SerializedProperty gizmoScaleProp;

        private bool showHandOffset;
        private bool showOldGizmo;

        private void OnEnable()
        {
            handOffsetProp = FindAutoProp(serializedObject, "HandOffset");
            localHandOffsetProp = FindAutoProp(serializedObject, "LocalHandOffset");
            markerPrefabProp = FindAutoProp(serializedObject, "MarkerPrefab");
            showGizmoProp = FindAutoProp(serializedObject, "ShowGizmo");
            gizmoScaleProp = FindAutoProp(serializedObject, "GizmoScale");
        }

        private SerializedProperty FindAutoProp(SerializedObject obj, string propName)
        {
            // Try Unity’s generated backing field name first
            var prop = obj.FindProperty($"<{propName}>k__BackingField");
            if (prop == null)
                prop = obj.FindProperty(propName);
            return prop;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            GrabSurface grabSurface = (GrabSurface)target;


            // Generate hands for debugging hand location
            EditorGUILayout.Space();
            bool canGenerateLeftHand = grabSurface.Handedness == Handedness.Both || grabSurface.Handedness == Handedness.Left || grabSurface.Handedness == Handedness.Exclusive;
            if (canGenerateLeftHand && GUILayout.Button("Generate Left Hand Pose"))
            {
                //ReadAllResources();
                grabSurface.DisplayLeftHandPose();
            }
            bool canGenerateRightHand = grabSurface.Handedness == Handedness.Both || grabSurface.Handedness == Handedness.Right || grabSurface.Handedness == Handedness.Exclusive;
            if (canGenerateRightHand && GUILayout.Button("Generate Right Hand Pose"))
            {
                grabSurface.DisplayRightHandPose();
            }

            // Clear generated hands
            EditorGUILayout.Space();
            if (GUILayout.Button("Clear Generated Hands"))
            {
                grabSurface.ClearDisplayedHands();
            }

            // Avoid showing sections if no relevant properties can be found
            if (handOffsetProp != null || markerPrefabProp != null || showGizmoProp != null || gizmoScaleProp != null)
            {
                //Draw hand offset dropdown
                EditorGUILayout.Space();
                showHandOffset = EditorGUILayout.Foldout(showHandOffset, "Hand Offset", true);
                if (showHandOffset)
                {
                    EditorGUI.indentLevel++;
                    if (handOffsetProp != null)
                        EditorGUILayout.PropertyField(handOffsetProp);
                    if (localHandOffsetProp != null)
                        EditorGUILayout.PropertyField(localHandOffsetProp);
                    EditorGUI.indentLevel--;
                }

                //draaw old gizmo dropdown
                EditorGUILayout.Space();
                showOldGizmo = EditorGUILayout.Foldout(showOldGizmo, "Old Gizmo", true);
                if (showOldGizmo)
                {
                    EditorGUI.indentLevel++;
                    if (markerPrefabProp != null)
                        EditorGUILayout.PropertyField(markerPrefabProp);
                    if (showGizmoProp != null)
                        EditorGUILayout.PropertyField(showGizmoProp);
                    if (gizmoScaleProp != null)
                        EditorGUILayout.PropertyField(gizmoScaleProp);
                    EditorGUI.indentLevel--;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

#if UNITY_EDITOR
        public static void ReadAllResources()
        {
            GameObject[] prefabs = Resources.LoadAll<GameObject>("");
            foreach (GameObject prefab in prefabs)
            {
                Debug.Log(prefab.name + " at " + AssetDatabase.GetAssetPath(prefab));
            }
        }
#endif
    }
}