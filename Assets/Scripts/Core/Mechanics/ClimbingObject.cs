using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ClimbingObject : MonoBehaviour
{
    public enum ClimbType { Vine, Ladder, Rope }
    [Header("Settings")]
    public ClimbType type = ClimbType.Vine;
    public float climbSpeedModifier = 1f;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;

        gameObject.tag = "Climbable";
    }
}