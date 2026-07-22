using UnityEngine;
using UnityEngine.UI;

public class CrimsonAmberUI : MonoBehaviour
{
    [Header("Ambers (Main Charges)")]
    [SerializeField] private Image[] amberIcons;
    [SerializeField] private Sprite amberFullSprite;
    [SerializeField] private Sprite amberEmptySprite;

    [Header("Orbs (Fragments)")]
    [Tooltip("Should ideally be 3 Images")]
    [SerializeField] private Image[] orbBars;
    [SerializeField] private Sprite orbFullSprite;
    [SerializeField] private Sprite orbEmptySprite;

    public void UpdateVisuals(int currentAmbers, int maxAmbers, int currentOrbs)
    {
        // 1. Update Main Ambers
        if (amberIcons != null)
        {
            for (int i = 0; i < amberIcons.Length; i++)
            {
                if (amberIcons[i] == null) continue;

                // Only show icons up to maxAmbers
                if (i < maxAmbers)
                {
                    amberIcons[i].gameObject.SetActive(true);
                    amberIcons[i].sprite = (i < currentAmbers) ? amberFullSprite : amberEmptySprite;
                }
                else
                {
                    amberIcons[i].gameObject.SetActive(false);
                }
            }
        }

        // 2. Update Orb Bars (Usually 3)
        if (orbBars != null)
        {
            for (int i = 0; i < orbBars.Length; i++)
            {
                if (orbBars[i] == null) continue;
                
                orbBars[i].sprite = (i < currentOrbs) ? orbFullSprite : orbEmptySprite;
            }
        }
    }
}
