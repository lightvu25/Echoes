using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Run Item Catalog", menuName = "Echoes/Run Item Catalog")]
public sealed class RunItemCatalog : ScriptableObject
{
    [SerializeField] private List<ItemBaseData> items = new List<ItemBaseData>();

    public IReadOnlyList<ItemBaseData> Items => items;

    public bool TryGetItem(string itemId, out ItemBaseData item)
    {
        item = null;
        if (string.IsNullOrWhiteSpace(itemId)) return false;

        for (int i = 0; i < items.Count; i++)
        {
            ItemBaseData candidate = items[i];
            if (candidate != null && candidate.itemID == itemId)
            {
                item = candidate;
                return true;
            }
        }

        return false;
    }

#if UNITY_EDITOR
    public void EditorReplaceItems(List<ItemBaseData> replacement)
    {
        items = replacement ?? new List<ItemBaseData>();
    }
#endif
}
