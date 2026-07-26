using UnityEngine;

[CreateAssetMenu(fileName = "New Fusion Recipe", menuName = "Data/Fusion Recipe", order = 10)]
public class FusionRecipeData : ScriptableObject
{
    [Header("Recipe")]
    public string recipeID;

    [Header("Ingredients & Result")]
    public EchoData echoA;
    public EchoData echoB;
    public EchoData resultEcho;
    
    [Header("Requirements")]
    public int recipeTier = 1;
    public string requiredConstellationNode;
}