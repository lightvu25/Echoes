using UnityEngine;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [System.Serializable]
    public class UIPanelMapping
    {
        public UIPanelType panelType;
        public GameObject panelObject;
        [Tooltip("If true, time will freeze (timeScale=0) when this panel opens.")]
        public bool freezeTime = true;
    }

    [Header("UI Panels Registration")]
    [SerializeField] private List<UIPanelMapping> panelMappings;

    private Dictionary<UIPanelType, IUIPanel> panelsDict = new Dictionary<UIPanelType, IUIPanel>();
    private UIPanelType currentActivePanel = UIPanelType.None;
    public UIPanelType CurrentActivePanel => currentActivePanel;

    // Deprecated HashSet, use the bool in UIPanelMapping instead.
    // Keeping for backwards compatibility if needed, but not actively used in OpenPanel anymore.
    private static readonly HashSet<UIPanelType> noFreezePanels = new HashSet<UIPanelType>();

    private int lastPanelCloseFrame = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        for (int i = 0; i < panelMappings.Count; i++)
        {
            var mapping = panelMappings[i];
            if (mapping.panelObject == null) continue;

            GameObject panelGo = mapping.panelObject;
            
            // Check if it's a prefab (not in the scene hierarchy)
            if (!panelGo.scene.IsValid())
            {
                panelGo = Instantiate(panelGo, transform);
                mapping.panelObject = panelGo; // Update the mapping to point to the instance
            }

            IUIPanel panel = panelGo.GetComponent<IUIPanel>();
            if (panel != null)
            {
                panelsDict[mapping.panelType] = panel;
                // We no longer call panel.Hide() here because doing so in Awake
                // disables the GameObject before Start() can run, breaking event subscriptions.
                // The panels will call Hide() themselves in their own Start/Awake.
            }
            else
            {
                Debug.LogWarning($"[UIManager] {panelGo.name} does not implement IUIPanel.");
            }
        }
    }

    public void OpenPanel(UIPanelType type)
    {
        Debug.Log($"[DEBUG] UIManager.OpenPanel({type}) called.");
        if (type == UIPanelType.None)
        {
            Debug.LogWarning("[DEBUG] Panel type is None. Returning.");
            return;
        }
        if (!panelsDict.ContainsKey(type))
        {
            Debug.LogError($"[DEBUG] UIManager does NOT contain panel of type {type} in panelsDict! Did you forget to add it to the mappings list?");
            return;
        }

        Debug.Log($"[DEBUG] Found {type} in panelsDict. Calling Show().");

        if (currentActivePanel != UIPanelType.None && panelsDict.ContainsKey(currentActivePanel))
        {
            panelsDict[currentActivePanel].Hide();
        }

        panelsDict[type].Show();
        currentActivePanel = type;

        var mapping = panelMappings.Find(m => m.panelType == type);
        if (mapping != null && mapping.freezeTime)
        {
            Time.timeScale = 0f;
        }
    }

    public void CloseCurrentPanel()
    {
        if (currentActivePanel != UIPanelType.None && panelsDict.ContainsKey(currentActivePanel))
        {
            panelsDict[currentActivePanel].Hide();
            currentActivePanel = UIPanelType.None;
            Time.timeScale = 1f;
            lastPanelCloseFrame = Time.frameCount;
        }
    }

    public void ClosePanelIfOpen(UIPanelType type)
    {
        if (currentActivePanel == type)
        {
            CloseCurrentPanel();
        }
    }

    public T GetPanel<T>(UIPanelType type) where T : class, IUIPanel
    {
        if (panelsDict.TryGetValue(type, out IUIPanel panel))
        {
            return panel as T;
        }
        return null;
    }

    public bool IsAnyPanelOpen => currentActivePanel != UIPanelType.None;

    public bool IsTimeFrozenByPanel
    {
        get
        {
            if (currentActivePanel == UIPanelType.None) return false;
            var mapping = panelMappings.Find(m => m.panelType == currentActivePanel);
            return mapping != null && mapping.freezeTime;
        }
    }

    public bool WasPanelClosedThisFrame => Time.frameCount == lastPanelCloseFrame;
}
