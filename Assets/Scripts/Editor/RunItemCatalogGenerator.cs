using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class RunItemCatalogGenerator
{
    public const string CatalogPath = "Assets/Data/Player/Run Item Catalog.asset";

    [MenuItem("Tools/Echoes/Generate Run Item Catalog")]
    public static void Generate()
    {
        RunItemCatalog catalog = AssetDatabase.LoadAssetAtPath<RunItemCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<RunItemCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        string[] guids = AssetDatabase.FindAssets("t:ItemBaseData", new[] { "Assets/Data" });
        var items = new List<ItemBaseData>(guids.Length);
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemBaseData item = AssetDatabase.LoadAssetAtPath<ItemBaseData>(path);
            if (item == null || string.IsNullOrWhiteSpace(item.itemID)) continue;

            if (!ids.Add(item.itemID))
            {
                Debug.LogWarning($"[RunItemCatalogGenerator] Duplicate item ID '{item.itemID}' at '{path}'. Keeping the first asset.");
                continue;
            }

            items.Add(item);
        }

        items.Sort((left, right) => string.CompareOrdinal(left.itemID, right.itemID));
        catalog.EditorReplaceItems(items);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log($"[RunItemCatalogGenerator] Catalog contains {items.Count} run items.", catalog);
    }
}
