using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AltaVR.Pathfinding
{
    [CustomEditor(typeof(Pathfinding)), CanEditMultipleObjects]
    public class PathfindingEditor : Editor
    {
        private SerializedProperty _renderPaths;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (_renderPaths.boolValue)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Click below to render all possible paths for the user.");
                GUILayout.EndVertical();

                if (GUILayout.Button("Generate"))
                {
                    var pathFinding = (Pathfinding)target;
                    pathFinding.GenerateRenderPath();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void OnEnable()
        {
            _renderPaths = serializedObject.FindProperty("renderPaths");
        }
    }
}
