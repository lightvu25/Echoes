using System.Collections.Generic;
using UnityEngine;
using System;

public class ShrineUI : MonoBehaviour, IUIPanel
{
    [SerializeField] private List<BlessingData> allBlessings = new List<BlessingData>();
    [SerializeField] private ShrineCardUI[] cardSlots = new ShrineCardUI[3];

    [Header("Panel")]
    [SerializeField] private UIPanelAnimator _panelAnimator;
    
    public event Action<BlessingData[]> OnBlessingsOffered;

    private bool isOpen = false;

    private void Awake()
    {
        
        // Hide on start if it wasn't explicitly opened via Show()
        if (!isOpen)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (GameInput.Instance != null)
            GameInput.Instance.OnCancelPressed += HandleCancelPressed;
    }

    private void OnDisable()
    {
        if (GameInput.Instance != null)
            GameInput.Instance.OnCancelPressed -= HandleCancelPressed;
    }

    private void HandleCancelPressed()
    {
        if (isOpen && UIManager.Instance != null)
            UIManager.Instance.CloseCurrentPanel();
    }

    public void DisplayRandomBlessings()
    {
        Debug.Log("[DEBUG] ShrineUI.DisplayRandomBlessings() called.");
        if (allBlessings.Count == 0)
        {
            Debug.LogError("[DEBUG] allBlessings list is empty! ShrineUI cannot display anything.");
            return;
        }

        isOpen = true;
        if (_panelAnimator != null) _panelAnimator.Show(); else gameObject.SetActive(true);

        List<BlessingData> vitalityPool = new List<BlessingData>();
        List<BlessingData> destructionPool = new List<BlessingData>();
        List<BlessingData> swiftnessPool = new List<BlessingData>();

        foreach (var b in allBlessings)
        {
            if (b.path == BlessingPath.Vitality) vitalityPool.Add(b);
            else if (b.path == BlessingPath.Sorcery) destructionPool.Add(b);
            else if (b.path == BlessingPath.Resonance) swiftnessPool.Add(b);
        }

        List<BlessingData> chosen = new List<BlessingData>();
        
        if (vitalityPool.Count > 0) chosen.Add(vitalityPool[UnityEngine.Random.Range(0, vitalityPool.Count)]);
        if (destructionPool.Count > 0) chosen.Add(destructionPool[UnityEngine.Random.Range(0, destructionPool.Count)]);
        if (swiftnessPool.Count > 0) chosen.Add(swiftnessPool[UnityEngine.Random.Range(0, swiftnessPool.Count)]);

        for (int i = 0; i < chosen.Count; i++)
        {
            int rnd = UnityEngine.Random.Range(0, chosen.Count);
            BlessingData temp = chosen[i];
            chosen[i] = chosen[rnd];
            chosen[rnd] = temp;
        }

        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (cardSlots[i] != null)
            {
                if (i < chosen.Count)
                {
                    cardSlots[i].gameObject.SetActive(true);
                    cardSlots[i].Setup(chosen[i], SelectBlessing);
                }
                else
                {
                    cardSlots[i].gameObject.SetActive(false);
                }
            }
        }

        OnBlessingsOffered?.Invoke(chosen.ToArray());
        
        if (GameManager.Instance != null) GameManager.Instance.PauseGame();
    }

    public void SelectBlessing(BlessingData blessing)
    {
        if (RunManager.Instance != null)
        {
            RunManager.Instance.GrantBlessing(blessing);
            if (UIManager.Instance != null)
                UIManager.Instance.CloseCurrentPanel();
            else
                Hide();
        }
        else
        {
            Debug.LogError("RunManager is missing in the scene!");
        }
    }

    public void Show()
    {
        Debug.Log("[DEBUG] ShrineUI.Show() called!");
        DisplayRandomBlessings();
    }

    public void Hide()
    {
        isOpen = false;
        if (_panelAnimator != null) _panelAnimator.Hide(); else gameObject.SetActive(false);
        if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
    }
}
