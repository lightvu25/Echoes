using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class ShrineCardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button selectButton;
    [SerializeField] private Image iconImage;

    public void Setup(BlessingData data, Action<BlessingData> onSelect)
    {
        if (titleText != null) titleText.text = data.buffName;
        if (descriptionText != null) descriptionText.text = data.description;

        if (iconImage != null)
        {
            if (data.icon != null)
            {
                iconImage.sprite = data.icon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false;
            }
        }
        
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => onSelect?.Invoke(data));
        }
    }
}
