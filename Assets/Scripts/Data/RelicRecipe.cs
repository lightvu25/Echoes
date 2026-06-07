using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Relic Recipe", menuName = "Data/Relic Recipe")]
public class RelicRecipe : ScriptableObject
{
    public List<RelicData> requiredRelics;
    public RelicData resultRelic;
}
