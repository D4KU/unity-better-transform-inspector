using System;
using UnityEngine;
using UnityEditor;

namespace BetterTransformInspector
{
    [CustomEditor(typeof(Transform))]
    [CanEditMultipleObjects]
    public class TransformEditor : Editor
    {
        /// <summary>
        /// Unity's built-in editor this custom one wraps around
        /// </summary>
        private Editor defaultEditor;

        /// <summary>
        /// Editor to process extended expressions.
        /// Only set when in expression mode.
        /// </summary>
        private ExpressiveTransformEditor expressiveEditor;

        /// <summary>
        /// True when foldout for local space values in open
        /// </summary>
        private bool showLocalSpace = true;

        /// <summary>
        /// True when foldout for world space values in open
        /// </summary>
        private bool showWorldSpace;

        /// <summary>
        /// Last drawn editor. Hack to access members from static context menu
        /// functions.
        /// </summary>
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

        /// <summary>
        /// Activate Expressive Inspector
        /// </summary>
        [MenuItem("CONTEXT/Transform/Enable Expressions")]
        private static void EnableExpressions()
        {
            if (current && current.expressiveEditor == null)
                current.expressiveEditor = new();
        }

        /// <summary>
        /// Deactivate Expressive Inspector
        /// </summary>
        [MenuItem("CONTEXT/Transform/Disable Expressions")]
        private static void DisableExpressions()
        {
            if (current)
                current.expressiveEditor = null;
        }

        public override void OnInspectorGUI()
        {
            current = this;

            // Initialize Expressive Inspector when it is activated
            if (expressiveEditor != null)
                expressiveEditor.Initialize(targets);

            // Draw foldout for local space values
            showLocalSpace = EditorGUILayout.BeginFoldoutHeaderGroup(
                showLocalSpace, "Local Space");

            // Check if foldout is open
            if (showLocalSpace)
            {
                if (expressiveEditor == null)
                    defaultEditor.OnInspectorGUI();
                else
                    expressiveEditor.DrawLocalVectors();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            // Draw foldout for local space values
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

        /// <summary>
        /// Draw world space position, rotation, and scale vector fields
        /// </summary>
        private void DrawGlobalVectors()
        {
            int len = targets.Length;
            var localPositions = new Vector3[len];
            var localRotations = new Quaternion[len];
            var localScales    = new Vector3[len];
            var positions      = new Vector3[len];
            var rotations      = new Quaternion[len];
            var scales         = new Vector3[len];

            for (int i = 0; i < len; i++)
            {
                Transform t = (Transform)targets[i];

                // Without a parent global values equal local ones.
                // Nothing needs to be replaced.
                if (t.parent == null)
                    continue;

                // Cache original local space values
                localPositions[i] = t.localPosition;
                localRotations[i] = t.localRotation;
                localScales[i]    = t.localScale;

                // Cache original world space values
                positions[i] = t.position;
                rotations[i] = t.rotation;
                scales[i]    = t.lossyScale;

                // Set local values to global ones
                t.localPosition = positions[i];
                t.localRotation = rotations[i];
                t.localScale    = scales[i];
            }

            // Draw default inspector with world space values
            defaultEditor.OnInspectorGUI();

            for (int i = 0; i < len; i++)
            {
                Transform t = (Transform)targets[i];
                if (t.parent == null)
                    continue;

                // Restore unchanged values back to original local value or
                // convert newly set global values to new local values
                t.localPosition = t.localPosition == positions[i]
                    ? localPositions[i]
                    : t.parent.InverseTransformPoint(t.localPosition);

                t.localRotation = t.localRotation == rotations[i]
                    ? localRotations[i]
                    : Quaternion.Inverse(t.parent.rotation) * t.localRotation;

                if (t.localScale == scales[i])
                    t.localScale = localScales[i];
                else
                    SetLossyScale(t, t.localScale);
            }
        }

        /// <summary>
        /// Set world space scale values
        /// </summary>
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
