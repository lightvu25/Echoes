using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public int currentGold { get; private set; }
    public int currentFragments { get; private set; }
    public List<RelicData> activeRelics = new List<RelicData>();
    [SerializeField] private List<RelicRecipe> allRecipes = new List<RelicRecipe>();

    public void AddGold(int amount)
    {
        currentGold += amount;
    }

    public void AddFragments(int amount)
    {
        currentFragments += amount;
    }

    public void AddRelic(RelicData relic)
    {
        activeRelics.Add(relic);
        CheckForFusion();
    }

    public void RemoveRelic(RelicData relic)
    {
        activeRelics.Remove(relic);
    }

    private void CheckForFusion()
    {
        if (allRecipes == null) return;

        foreach (var recipe in allRecipes)
        {
            if (recipe.requiredRelics == null || recipe.requiredRelics.Count == 0) continue;

            List<RelicData> tempActive = new List<RelicData>(activeRelics);
            List<RelicData> matched = new List<RelicData>();
            bool hasAll = true;

            foreach (var req in recipe.requiredRelics)
            {
                RelicData found = tempActive.Find(r => r.itemID == req.itemID);
                if (found != null)
                {
                    tempActive.Remove(found);
                    matched.Add(found);
                }
                else
                {
                    hasAll = false;
                    break;
                }
            }

            if (hasAll)
            {
                foreach (var match in matched)
                {
                    RemoveRelic(match);
                }


                AddRelic(recipe.resultRelic);

                RelicEffectManager rem = GetComponent<RelicEffectManager>();
                if (rem != null)
                {
                    rem.RefreshEffects();
                }

                return;
            }
        }
    }

    public bool HasRelic(string id)
    {
        foreach (var relic in activeRelics)
        {
            if (relic.itemID == id) return true;
        }
        return false;
    }
}
