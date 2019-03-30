using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

internal sealed class LevelLoader : MonoBehaviour
{
    private readonly IFormatter formatter = new BinaryFormatter();

    [SerializeField]
    private Transform spawnParent;

    [SerializeField]
    private Level level;

    [SerializeField]
    private string levelPath;

    private void OnEnable()
    {
        this.level = this.LoadLevel();
        this.InstantiateAllObjects(this.spawnParent);
    }

    private Level LoadLevel()
    {
        var asset = Resources.Load(this.levelPath) as TextAsset;
        using (var stream = new MemoryStream(asset.bytes))
        {
            return (Level)this.formatter.Deserialize(stream);
        }
    }

    private void InstantiateAllObjects(Transform parent)
    {
        for (int i = 0; i < this.level.Objects.Length; i++)
        {
            var obj = this.level.Objects[i];
            var prefab = Resources.Load<GameObject>(obj.PrefabPath);
            LevelLoader.Instantiate(prefab, obj.Position, Quaternion.identity, parent);
        }
    }
}
