using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(UIPanelAnimator))]
public class MapIntroUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI mapNameText;
    [SerializeField] private float displayDuration = 3f;

    private UIPanelAnimator panelAnimator;

    private void Awake()
    {
        panelAnimator = GetComponent<UIPanelAnimator>();
    }

    private void Start()
    {
        if (mapNameText != null)
        {
            if (GameSession.Instance != null && GameSession.Instance.currentRun != null)
            {
                string mapName = GameSession.Instance.currentRun.currentLevelName; 
                
                if (string.IsNullOrEmpty(mapName)) 
                {
                    mapName = "The Abyss"; 
                }
                
                mapNameText.text = mapName;
            }
            else
            {
                mapNameText.text = "The Abyss";
            }
        }

        panelAnimator.Show();
        panelAnimator.GetComponent<CanvasGroup>().blocksRaycasts = false; // FIX: Never block clicks!
        StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(displayDuration); // FIX: Unaffected by Time.timeScale = 0
        if (panelAnimator != null && panelAnimator.IsShowing)
        {
            panelAnimator.Hide();
        }
    }
}