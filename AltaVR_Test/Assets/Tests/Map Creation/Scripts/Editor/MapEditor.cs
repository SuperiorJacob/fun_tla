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
        private SerializedProperty _editMode;

        private TileData _selectedTile;

        public override void OnInspectorGUI()
        {
            if (_mapData.objectReferenceValue != null)
            {
                base.OnInspectorGUI();

                var data = _mapData.objectReferenceValue as MapData;

                if (data == null)
                    return;

                serializedObject.Update();

                EditorGUILayout.LabelField("Edit mode allows you to edit the tile map in the editor.");

                if (_editMode.boolValue)
                {
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("To select a tile, press right click.");
                    EditorGUILayout.LabelField("To place / remove a tile, press space.");
                    EditorGUILayout.LabelField("To cycle through a tile, press a & d.");
                    GUILayout.EndVertical();
                }

                GUILayout.Space(10f);

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Save"))
                {
                    data.mapInfo = (MapInfo)target.GetType().GetField("mapInfo").GetValue(target);

                    EditorUtility.SetDirty(data);
                    AssetDatabase.SaveAssets();
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

        private void OnSceneGUI(SceneView a_sceneView)
        {
            if (!_editMode.boolValue)
                return;

            SceneView.RepaintAll();

            var map = (Map)target;

            if (map.loadedTiles == null)
                map.LoadSavedTiles();

            Camera sceneCamera = Camera.current;

            // Scene view camera mouse position inverted position solution.
            // Found here: https://forum.unity.com/threads/mouse-position-in-scene-view.250399/#post-4838108

            Vector3 mousePos = Event.current.mousePosition;
            mousePos.z = -sceneCamera.worldToCameraMatrix.MultiplyPoint(map.transform.position).z;
            mousePos.y = Screen.height - mousePos.y - 36.0f;

            Vector2 position = Camera.current.ScreenToWorldPoint(mousePos);

            //

            TileData tile = map.GetTileByClosestPosition(position);

            if (Event.current.type == EventType.MouseUp)
            {
                _selectedTile = tile;

                map.DrawHovered(_selectedTile);
            }
            else if (Event.current.type == EventType.KeyUp)
            {
                if (_selectedTile.prefabIndex > -1)
                {
                    if (Event.current.keyCode == KeyCode.Space)
                    {
                        map.RemoveTileAtPosition(_selectedTile.position);
                    }
                    else
                    {
                        _selectedTile = map.NextTile(_selectedTile, Event.current.keyCode == KeyCode.D);
                    }
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
        }

        private void OnEnable()
        {
            // Hook scene refreshing.
            SceneView.duringSceneGui += OnSceneGUI;

            _mapData = serializedObject.FindProperty("mapData");
            _editMode = serializedObject.FindProperty("_editMode");
        }

        private void OnDisable()
        {
            // Unhook scene refreshing.
            SceneView.duringSceneGui -= OnSceneGUI;
        }
    }
}
