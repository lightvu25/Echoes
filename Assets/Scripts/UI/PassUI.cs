using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class PassUI : MonoBehaviour, IUIPanel
{
    private void Start()
    {
        Hide();
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
