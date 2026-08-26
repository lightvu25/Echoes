using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MindNodeUI : MonoBehaviour, IUIPanel
{
    [Header("UI Elements")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI requirementsText;
    public TextMeshProUGUI unlockText; // The text indicating Locked/Unlocked state
    public Image lockIcon;
    public Image nodeIcon; // Icon representing the node type
    public Image requirementIcon; // The icon next to the requirement text
    public Sprite killSprite; // Sprite for No-Hit kills
    public Sprite timeSprite; // Sprite for Speedrun time

    [System.Serializable]
    private sealed class FeaturedItemSlot
    {
        [Tooltip("The complete UI object for this featured-item slot.")]
        [SerializeField] private GameObject slotRoot;
        [Tooltip("The Image that displays the relic, Echo, or equipment icon.")]
        [SerializeField] private Image itemIcon;
        [Tooltip("Optional item-name text. Leave empty for an icon-only design.")]
        [SerializeField] private TextMeshProUGUI itemNameText;
        [Tooltip("Optional appearance-multiplier text, such as 3x.")]
        [SerializeField] private TextMeshProUGUI appearanceWeightText;

        public void SetVisible(bool visible)
        {
            if (slotRoot != null && slotRoot.activeSelf != visible)
                slotRoot.SetActive(visible);
        }

        public void Display(ItemBaseData item, float weightMultiplier, Sprite fallbackIcon)
        {
            if (item == null) return;

            if (itemIcon != null)
            {
                itemIcon.sprite = item.itemIcon != null ? item.itemIcon : fallbackIcon;
                itemIcon.color = itemIcon.sprite != null
                    ? Color.white
                    : new Color(1f, 1f, 1f, 0.15f);
                itemIcon.preserveAspect = true;
            }

            if (itemNameText != null)
            {
                itemNameText.text = string.IsNullOrWhiteSpace(item.itemName)
                    ? item.itemID
                    : item.itemName;
            }

            if (appearanceWeightText != null)
                appearanceWeightText.text = $"{weightMultiplier:0.#}×";
        }
    }

    [Header("Featured Item Preview")]
    [Tooltip("Container for the 2-3 featured item slots. The script shows it only for configured Relic, Echo, and Equipment nodes.")]
    [SerializeField] private GameObject featuredItemPreviewRoot;
    [Tooltip("Optional legacy featured label. It is hidden at runtime because the node title already identifies the item category.")]
    [SerializeField] private TextMeshProUGUI featuredItemPreviewHeader;
    [Tooltip("Assign three manually created UI slots. Unused slots are hidden when only two items are rolled.")]
    [SerializeField] private FeaturedItemSlot[] featuredItemSlots =
    {
        new FeaturedItemSlot(),
        new FeaturedItemSlot(),
        new FeaturedItemSlot()
    };

    private MindNode _currentNode;
    private bool _canClaimCurrentNode = false;
    private int _lastHandledInputFrame = -1;

    private void Awake()
    {
        HideFeaturedItemPreview();
    }

    private void OnEnable()
    {
        if (GameInput.Instance == null) return;
        GameInput.Instance.OnConfirmPressed += HandlePrimaryAction;
        GameInput.Instance.OnInteractPressed += HandlePrimaryAction;
        GameInput.Instance.OnCancelPressed += HandleCancelAction;
    }

    private void OnDisable()
    {
        if (GameInput.Instance == null) return;
        GameInput.Instance.OnConfirmPressed -= HandlePrimaryAction;
        GameInput.Instance.OnInteractPressed -= HandlePrimaryAction;
        GameInput.Instance.OnCancelPressed -= HandleCancelAction;
    }

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

    public void DisplayNode(MindNode node, Sprite icon = null)
    {
        _currentNode = node;
        gameObject.SetActive(true);
        HideFeaturedItemPreview();
        titleText.text = node.nodeType.ToString();
        requirementsText.text = "";
        requirementsText.color = Color.white;
        lockIcon.gameObject.SetActive(false);
        if (unlockText != null) unlockText.text = "";
        _canClaimCurrentNode = false;
        string confirmLabel = GameInput.Instance != null ? GameInput.Instance.ConfirmKey.ToString() : "Confirm";

        if (nodeIcon != null)
        {
            nodeIcon.sprite = node.nodeIcon;
            nodeIcon.gameObject.SetActive(nodeIcon.sprite != null);
        }

        if (node.nodeType == NodeType.MapExit)
        {
            titleText.text = "Goal Reached!";
            string interactLabel = GameInput.Instance != null ? GameInput.Instance.InteractKey.ToString() : "Interact";
            descriptionText.text = $"Press {confirmLabel} or {interactLabel} to enter the next level.";
            return;
        }

        var run = GameSession.Instance != null ? GameSession.Instance.currentRun : null;

        if (requirementIcon != null) requirementIcon.gameObject.SetActive(false);
        bool isLocked = false;
        string statusString = "";

        if (node.nodeType == NodeType.ChallengeSpeedrun)
        {
            if (requirementIcon != null && timeSprite != null)
            {
                requirementIcon.sprite = timeSprite;
                requirementIcon.gameObject.SetActive(true);
            }

            if (run == null)
            {
                requirementsText.text = $"{node.requiredSpeedrunTime}s Limit";
                statusString = $"<color=green>Press {confirmLabel} to claim</color>";
                descriptionText.text = "Reward: 30 Shards";
                isLocked = false;
            }
            else if (run.currentLevelTime > node.requiredSpeedrunTime)
            {
                requirementsText.text = $"Took {run.currentLevelTime:F1}s / {node.requiredSpeedrunTime}s Limit";
                statusString = "<color=red>LOCKED</color>";
                descriptionText.text = "Reward: 30 Shards";
                isLocked = true;
            }
            else
            {
                requirementsText.text = $"Time: {run.currentLevelTime:F1}s / {node.requiredSpeedrunTime}s Limit";
                statusString = $"<color=green>Press {confirmLabel} to claim</color>";
                descriptionText.text = "Reward: 30 Shards";
                isLocked = false;
            }
        }
        else if (node.nodeType == NodeType.ChallengeNoHit)
        {
            if (requirementIcon != null && killSprite != null)
            {
                requirementIcon.sprite = killSprite;
                requirementIcon.gameObject.SetActive(true);
            }

            if (run == null)
            {
                requirementsText.text = $"{node.requiredNoHitKills} Kills";
                statusString = $"<color=green>Press {confirmLabel} to claim</color>";
                descriptionText.text = "Reward: 30 Shards";
                isLocked = false;
            }
            else if (run.currentLevelNoHitKills < node.requiredNoHitKills)
            {
                requirementsText.text = $"{run.currentLevelNoHitKills} / {node.requiredNoHitKills} Kills";
                statusString = "<color=red>LOCKED</color>";
                descriptionText.text = "Reward: 30 Shards";
                isLocked = true;
            }
            else
            {
                requirementsText.text = $"{run.currentLevelNoHitKills} / {node.requiredNoHitKills} Kills";
                statusString = $"<color=green>Press {confirmLabel} to claim</color>";
                descriptionText.text = "Reward: 30 Shards";
                isLocked = false;
            }
        }

        if (node.nodeType == NodeType.ChallengeNoHit || node.nodeType == NodeType.ChallengeSpeedrun)
        {
            if (node.isChallengeClaimed)
            {
                statusString = "<color=green>CLAIMED</color>";
                descriptionText.text = "Reward: 30 Shards (Claimed)";
                _canClaimCurrentNode = false;
            }
            else if (!isLocked)
            {
                _canClaimCurrentNode = true;
            }

            if (unlockText != null)
            {
                unlockText.text = statusString;
            }
            else
            {
                requirementsText.text += $"\n{statusString}";
            }

            if (isLocked)
            {
                lockIcon.gameObject.SetActive(true);
            }
            return;
        }

        // --- Standard Node Logic Below ---



        if (node.ModifierData != null)
        {
            var data = node.ModifierData;
            string desc = "";

            if (data.useFeaturedItemBoost && node.TryGetRewardCategory(out _))
            {
                var featuredItems = node.FeaturedItems;
                ShowFeaturedItemPreview(featuredItems, data.featuredItemWeightMultiplier, node.nodeIcon);

                if (featuredItems.Count == 0)
                    desc += "<color=#FFCC66>No eligible featured items configured.</color>\n";
                else
                    desc += $"<color=#00FF00>{data.featuredItemWeightMultiplier:0.#}× Appearance Chance</color>\n";
            }
            else
            {
                if (data.bonusRelicChance > 0) desc += $"<color=#00FF00>+ {data.bonusRelicChance * 100}% Relic Chance</color>\n";
                if (data.bonusEquipmentChance > 0) desc += $"<color=#00FF00>+ {data.bonusEquipmentChance * 100}% Equipment Chance</color>\n";
                if (data.bonusEchoChance > 0) desc += $"<color=#00FF00>+ {data.bonusEchoChance * 100}% Echo Chance</color>\n";
            }
            
            if (data.magicToxicityIncrease > 0) desc += $"<color=#FF0000>+ {data.magicToxicityIncrease} Magic Toxicity</color>\n";
            if (data.enemyDensityMultiplier > 1.0f) desc += $"<color=#FF0000>x {data.enemyDensityMultiplier} Enemy Density</color>\n";
            
            if (data.addedEliteEnemyTypes != null && data.addedEliteEnemyTypes.Count > 0)
            {
                desc += $"<color=#FF0000>Warning: New Elite Enemies Added</color>\n";
            }
            
            if (string.IsNullOrEmpty(desc)) desc = "A safe path with no special modifiers.";
            
            descriptionText.text = desc;
        }
        else
        {
            descriptionText.text = "A safe path with no modifiers.";
        }
    }

    private void ShowFeaturedItemPreview(IReadOnlyList<ItemBaseData> items, float weightMultiplier, Sprite fallbackIcon)
    {
        if (featuredItemPreviewRoot == null)
        {
            Debug.LogWarning("[MindNodeUI] Featured Item Preview Root is not assigned.", this);
            return;
        }

        if (featuredItemPreviewHeader != null)
            featuredItemPreviewHeader.gameObject.SetActive(false);

        int slotCount = featuredItemSlots != null ? featuredItemSlots.Length : 0;
        for (int i = 0; i < slotCount; i++)
        {
            FeaturedItemSlot slot = featuredItemSlots[i];
            bool hasItem = items != null && i < items.Count && items[i] != null;
            if (slot == null) continue;

            slot.SetVisible(hasItem);
            if (hasItem)
                slot.Display(items[i], weightMultiplier, fallbackIcon);
        }

        featuredItemPreviewRoot.SetActive(items != null && items.Count > 0);
    }

    private void HideFeaturedItemPreview()
    {
        if (featuredItemPreviewHeader != null)
            featuredItemPreviewHeader.gameObject.SetActive(false);

        if (featuredItemPreviewRoot != null)
            featuredItemPreviewRoot.SetActive(false);
    }

    private void HandlePrimaryAction()
    {
        if (!gameObject.activeSelf || _lastHandledInputFrame == Time.frameCount) return;
        _lastHandledInputFrame = Time.frameCount;

        if (_currentNode != null && _currentNode.nodeType == NodeType.MapExit)
        {
            if (_currentNode.TryGetComponent<MindExitNode>(out var exitNode))
                exitNode.Interact();
            return;
        }

        if (_canClaimCurrentNode && _currentNode != null)
        {
            _currentNode.isChallengeClaimed = true;
            _canClaimCurrentNode = false;

            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.AddAstralShards(30);
                Debug.Log("[MindNodeUI] Claimed 30 Astral Shards from Challenge Node! (Via PlayerStats)");
            }
            else if (GameSession.Instance != null && GameSession.Instance.currentRun != null)
            {
                GameSession.Instance.currentRun.currentAstralShards += 30;
                GameSession.Instance.SaveCurrentRun();
                Debug.Log("[MindNodeUI] Claimed 30 Astral Shards from Challenge Node! (Via GameSession)");
            }
        }

        ClosePanel();
    }

    private void HandleCancelAction()
    {
        if (!gameObject.activeSelf || _lastHandledInputFrame == Time.frameCount) return;
        _lastHandledInputFrame = Time.frameCount;
        ClosePanel();
    }

    private void ClosePanel()
    {
        if (UIManager.Instance != null && UIManager.Instance.CurrentActivePanel == UIPanelType.MindNode)
            UIManager.Instance.CloseCurrentPanel();
        else
            Hide();
    }
}
