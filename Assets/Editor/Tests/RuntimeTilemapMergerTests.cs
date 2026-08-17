#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RuntimeTilemapMergerTests
{
    private readonly List<Object> createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null) Object.DestroyImmediate(createdObjects[i]);
        }
        createdObjects.Clear();
    }

    [Test]
    public void CopyRendererAppearance_PreservesDestinationSorting()
    {
        Tilemap source = CreateTilemap("Room Background");
        Tilemap destination = CreateTilemap("Global Background");
        TilemapRenderer sourceRenderer = source.GetComponent<TilemapRenderer>();
        TilemapRenderer destinationRenderer = destination.GetComponent<TilemapRenderer>();

        sourceRenderer.sortingLayerName = "Default";
        sourceRenderer.sortingOrder = -50;
        destinationRenderer.sortingLayerName = "Background";
        destinationRenderer.sortingOrder = -20;
        source.color = new Color(0.25f, 0.5f, 0.75f, 1f);
        source.tileAnchor = new Vector3(0.25f, 0.75f, 0f);

        MethodInfo copyAppearance = typeof(RuntimeTilemapMerger).GetMethod(
            "CopyRendererAppearance",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(copyAppearance, Is.Not.Null);
        copyAppearance.Invoke(null, new object[] { source, destination });

        Assert.That(destinationRenderer.sortingLayerName, Is.EqualTo("Background"));
        Assert.That(destinationRenderer.sortingOrder, Is.EqualTo(-20));
        Assert.That(destination.color, Is.EqualTo(source.color));
        Assert.That(destination.tileAnchor, Is.EqualTo(source.tileAnchor));
    }

    private Tilemap CreateTilemap(string name)
    {
        GameObject gameObject = new GameObject(name);
        createdObjects.Add(gameObject);
        Tilemap tilemap = gameObject.AddComponent<Tilemap>();
        gameObject.AddComponent<TilemapRenderer>();
        return tilemap;
    }
}
#endif
