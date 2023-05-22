using System;
using UnityEngine;
using UnityEditor;

namespace BetterTransformInspector
{
    [CustomEditor(typeof(Transform))]
    [CanEditMultipleObjects]
    public class TransformEditor : Editor
    {
        // Unity's built-in editor
        private Editor defaultEditor;
        private ExpressiveTransformEditor expressiveEditor;

        private bool showLocalSpace = true;
        private bool showWorldSpace;
        private static TransformEditor current;

        private void OnEnable()
        {
            // Create the built-in inspector
            defaultEditor = Editor.CreateEditor(targets,
                Type.GetType("UnityEditor.TransformInspector, UnityEditor"));
        }

        private void OnDisable()
        {
            DestroyImmediate(defaultEditor);
        }

        [MenuItem("CONTEXT/Transform/Enable Expressions")]
        private static void EnableExpressions()
        {
            if (current && current.expressiveEditor == null)
                current.expressiveEditor = new();
        }

        [MenuItem("CONTEXT/Transform/Disable Expressions")]
        private static void DisableExpressions()
        {
            if (current)
                current.expressiveEditor = null;
        }

        public override void OnInspectorGUI()
        {
            current = this;
            if (expressiveEditor != null)
                expressiveEditor.OnInspectorGUI(targets);

            // Show local space transform
            showLocalSpace = EditorGUILayout.BeginFoldoutHeaderGroup(
                showLocalSpace, "Local Space");

            if (showLocalSpace)
            {
                if (expressiveEditor == null)
                    defaultEditor.OnInspectorGUI();
                else
                    expressiveEditor.DrawLocalVectors();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            // Show world space transform
            showWorldSpace = EditorGUILayout.BeginFoldoutHeaderGroup(
                showWorldSpace, "World Space");

            if (showWorldSpace)
            {
                if (expressiveEditor == null)
                    DrawGlobalVectors();
                else
                    expressiveEditor.DrawGlobalVectors();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawGlobalVectors()
        {
            Transform transform = (Transform)target;
            Transform parent = transform.parent;

            if (parent == null)
            {
                defaultEditor.OnInspectorGUI();
                return;
            }

            // Cache original local space values
            Vector3    localPosition = transform.localPosition;
            Quaternion localRotation = transform.localRotation;
            Vector3    localScale    = transform.localScale;

            // Cache original world space values
            Vector3    position = transform.position;
            Quaternion rotation = transform.rotation;
            Vector3    scale    = transform.lossyScale;

            // Set world space values
            transform.localPosition = position;
            transform.localRotation = rotation;
            transform.localScale    = scale;

            // Draw default inspector with world space values
            defaultEditor.OnInspectorGUI();

            // Restore unchanged values back to original local value or
            // convert newly set global values to new local values
            transform.localPosition = transform.localPosition == position
                ? localPosition
                : parent.InverseTransformPoint(transform.localPosition);

            transform.localRotation = transform.localRotation == rotation
                ? localRotation
                : Quaternion.Inverse(parent.rotation) * transform.localRotation;

            if (transform.localScale == scale)
                transform.localScale = localScale;
            else
                SetLossyScale(transform, transform.localScale);
        }

        public static void SetLossyScale(Transform transform, Vector3 scale)
        {
            transform.localScale = Vector3.one;
            transform.localScale = new Vector3(
                    scale.x / transform.lossyScale.x,
                    scale.y / transform.lossyScale.y,
                    scale.z / transform.lossyScale.z);
        }
    }
}
