using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;

internal sealed class LevelEditor : EditorWindow
{
    private const string
        SavePathKey = "LevelEditorSavePath",
        AssetsPathKey = "LevelEditorAssetsPath";
    private readonly IFormatter formatter = new BinaryFormatter();

    [SerializeField]
    private Level level;

    [SerializeField]
    private GameObject[] serializableAssets;

    private string savePath, assetsPath;
    private bool levelIsValid;

    private SerializedObject serializedObject;
    private SerializedProperty
        levelProperty, assetsProperty;

    [MenuItem("Window/Level Editor")]
    public static void ShowWindow()
    {
        var window = EditorWindow.GetWindow(typeof(LevelEditor));
    }

    private void OnEnable()
    {
        this.serializedObject = new SerializedObject(this);
        this.levelProperty = this.serializedObject.FindProperty("level");
        this.assetsProperty = this.serializedObject.FindProperty("serializableAssets");
        
        Selection.selectionChanged += this.OnSelectionChanged;

        if (!EditorPrefs.HasKey(SavePathKey))
        {
            EditorPrefs.SetString(SavePathKey, "Assets/Resources/Levels/");
        }

        if (!EditorPrefs.HasKey(AssetsPathKey))
        {
            EditorPrefs.SetString(AssetsPathKey, "LevelAssets/");
        }

        this.savePath = EditorPrefs.GetString(SavePathKey);
        this.assetsPath = EditorPrefs.GetString(AssetsPathKey);

        this.UpdateAssets();
    }

    private void OnDisable()
    {
        this.serializedObject.Dispose();
        this.levelProperty.Dispose();
        this.assetsProperty.Dispose();

        this.serializedObject = null;
        this.levelProperty = null;
        this.assetsProperty = null;

        Selection.selectionChanged -= this.OnSelectionChanged;
    }
    
    private void OnGUI()
    {
        GUILayout.Label(
            "Select GameObjects with SpriteRenderers in the scene"
            + " and press the button to create a new level", EditorStyles.helpBox);

        EditorGUI.BeginChangeCheck();

        this.DrawLevelProperty();
        this.DrawSavePathField();
        this.DrawSelectionButton();
        GUILayout.Space(8f);
        this.DrawAssetsPathField();
        this.DrawAssetsProperty();

        if (EditorGUI.EndChangeCheck())
        {
            this.serializedObject.ApplyModifiedProperties();
        }
    }

    private void DrawLevelProperty()
    {
        EditorGUILayout.PropertyField(this.levelProperty, true);
    }

    private void DrawSavePathField()
    {
        this.savePath = EditorGUILayout.DelayedTextField("Level Save Path", EditorPrefs.GetString(SavePathKey));
        EditorPrefs.SetString(SavePathKey, this.savePath);
    }

    private void DrawSelectionButton()
    {
        GUI.enabled = !this.levelIsValid;

        if (GUILayout.Button("Create level"))
        {
            this.CreateLevel();
        }

        if (GUILayout.Button("Refresh Selection"))
        {
            this.UpdateLevel();
        }

        GUI.enabled = true;
    }

    private void DrawAssetsPathField()
    {
        this.assetsPath = EditorGUILayout.DelayedTextField("Level Assets Path", EditorPrefs.GetString(AssetsPathKey));
        EditorPrefs.SetString(AssetsPathKey, this.assetsPath);

        if (GUILayout.Button("Refresh Assets"))
        {
            this.UpdateAssets();
        }
    }

    private void DrawAssetsProperty()
    {
        EditorGUILayout.PropertyField(this.assetsProperty, true);
    }

    private void UpdateAssets()
    {
        this.serializableAssets = Resources.LoadAll<GameObject>(this.assetsPath);
        this.serializedObject.Update();
    }

    private void CreateLevel()
    {
        string fullSavePath = Path.Combine(Path.GetFullPath(this.savePath), $"Level{this.level.Id}") + ".txt";
        if (File.Exists(fullSavePath))
        {
            Debug.LogError("File already exists");
            return;
        }

        using (var stream = new FileStream(fullSavePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            this.formatter.Serialize(stream, this.level);
        }
    }

    private void UpdateLevel()
    {
        var transforms = Selection.transforms;
        this.level.Objects = this.GetLevelObjects(transforms);
        this.serializedObject.Update();
        this.levelIsValid = this.level.Objects == null || this.level.Objects.Length == 0;
    }

    private LevelObject[] GetLevelObjects(Transform[] selection)
    {
        if (selection.Length == 0)
        {
            return new LevelObject[0];
        }

        return this.UpdateLevelObjects(selection);
    }

    private LevelObject[] UpdateLevelObjects(Transform[] selection)
    {
        var levelObjects = new List<LevelObject>(selection.Length);

        for (int i = 0; i < selection.Length; i++)
        {
            var transform = selection[i];
            this.TryAddLevelObject(levelObjects, transform);
        }

        return levelObjects.ToArray();
    }

    private void TryAddLevelObject(List<LevelObject> levelObjects, Transform transform)
    {
        string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(transform.gameObject);
        if (prefabPath == null || !this.TryGetSerializableAssetPath(prefabPath, out string resourcesPath))
        {
            return;
        }
        
        var levelObject = new LevelObject()
        {
            PrefabPath = resourcesPath,
            Position = (LevelPosition)transform.position
        };

        levelObjects.Add(levelObject);
    }

    private bool TryGetSerializableAssetPath(string assetPath, out string serializableAssetPath)
    {
        string match = Path.Combine("/Resources/", this.assetsPath);
        int index = assetPath.IndexOf(match);
        if (index == -1)
        {
            serializableAssetPath = null;
            return false;
        }

        serializableAssetPath = Path.ChangeExtension(assetPath.Substring(index + "/Resources/".Length), null);
        return true;
    }

    private void OnSelectionChanged()
    {
        this.UpdateLevel();
        this.Repaint();
    }
}
