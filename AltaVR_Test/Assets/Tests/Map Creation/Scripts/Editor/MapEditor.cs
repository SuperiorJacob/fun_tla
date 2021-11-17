using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AltaVR.MapCreation
{
    [CustomEditor(typeof(Map)), CanEditMultipleObjects]
    public class MapEditor : Editor
    {
        private SerializedProperty _mapData;
        private SerializedProperty _mapInfo;

        private bool _editMode;
        private TileData _selectedTile;

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;

            _mapData = serializedObject.FindProperty("mapData");
            _mapInfo = serializedObject.FindProperty("mapInfo");
        }

        private void OnSceneGUI(SceneView a_sceneView)
        {
            if (!_editMode)
                return;

            SceneView.RepaintAll();

            var map = (Map)target;

            Vector2 position = Camera.current.ScreenToWorldPoint(Event.current.mousePosition);

            TileData tile = map.GetTileByClosestPosition(position);

            if (Event.current.type == EventType.MouseUp)
            {
                _selectedTile = tile;

                map.DrawHovered(_selectedTile);
            }

            if (Event.current.type == EventType.KeyUp)
                if (_selectedTile.prefabIndex > -1)
                {

                }
                else if (_selectedTile.prefabIndex == -2)
                {
                    if (Event.current.keyCode == KeyCode.Space)
                    {
                        _selectedTile.prefabIndex = 0;
                        map.CreateTile(_selectedTile, true);
                    }
                }
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        public override void OnInspectorGUI()
        {
            if (_mapData.objectReferenceValue != null)
            {
                base.OnInspectorGUI();

                var data = _mapData.objectReferenceValue as MapData;

                if (data == null)
                    return;

                GUILayout.Space(10f);

                serializedObject.Update();

                EditorGUILayout.LabelField("Edit mode allows you to edit the tile map in the editor.");
                _editMode = GUILayout.Toggle(_editMode, "Edit Mode");

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Save"))
                {
                    data.mapInfo = (MapInfo)target.GetType().GetField("mapInfo").GetValue(target);

                    EditorUtility.SetDirty(target);
                }

                if (GUILayout.Button("Load"))
                {
                    target.GetType().GetField("mapInfo").SetValue(target, data.mapInfo);

                    var map = (Map)target;
                    map.DrawHovered(new TileData { prefabIndex = -1 });
                    map.LoadSavedTiles();

                    EditorUtility.SetDirty(target);
                }

                GUILayout.EndHorizontal();

                serializedObject.ApplyModifiedProperties();

                return;
            }

            serializedObject.Update();

            EditorGUILayout.LabelField("Generate map data or load one here.");
            EditorGUILayout.ObjectField(_mapData);

            if (GUILayout.Button("Generate"))
            {
                MapData data = ScriptableObject.CreateInstance<MapData>();

                AssetDatabase.CreateAsset(data, $"Assets/Tests/Map Creation/Data/MapData_{target.GetInstanceID()}.asset");
                AssetDatabase.SaveAssets();

                _mapData.objectReferenceValue = data;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
