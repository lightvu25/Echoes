using UnityEngine;

public class EchoDebugTester : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool enableDebugKeys = true;
    public bool force100PercentProc = true;

    [Header("Current Status")]
    [SerializeField] private string currentModifier = "None";
    [SerializeField] private float currentProcChance = 0f;

    private void Update()
    {
        if (!enableDebugKeys) return;

        // Number keys 1-7 to swap modifiers
        if (Input.GetKeyDown(KeyCode.Alpha1)) ApplyModifier("IGNITION");
        if (Input.GetKeyDown(KeyCode.Alpha2)) ApplyModifier("FROSTBITE");
        if (Input.GetKeyDown(KeyCode.Alpha3)) ApplyModifier("CHAIN_ARC");
        if (Input.GetKeyDown(KeyCode.Alpha4)) ApplyModifier("KINETIC_FORCE");
        if (Input.GetKeyDown(KeyCode.Alpha5)) ApplyModifier("DISTORTION");
        if (Input.GetKeyDown(KeyCode.Alpha6)) ApplyModifier("FUS_EVENT_HORIZON"); // Black Hole / Void
        if (Input.GetKeyDown(KeyCode.Alpha7)) ApplyModifier("OBLIVION"); // Curse

        // Mind Garden Testing
        if (Input.GetKeyDown(KeyCode.F9)) GiveAstralShards(9999);
        if (Input.GetKeyDown(KeyCode.F10)) ResetMindGarden();

        UpdateStatusDisplay();
    }

    private void GiveAstralShards(int amount)
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddAstralShards(amount);
            Debug.Log($"[Debug] Added {amount} Astral Shards!");
        }
    }

    private void ResetMindGarden()
    {
        if (GameSession.Instance != null && GameSession.Instance.currentProfile != null)
        {
            GameSession.Instance.currentProfile.unlockedSkillIDs.Clear();
            SaveManager.saveProfile(GameSession.Instance.currentProfile);
            
            // Try to auto-refresh the UI if it's open
            MindGardenUI ui = FindFirstObjectByType<MindGardenUI>();
            if (ui != null && ui.IsOpen)
            {
                ui.RefreshAllNodes();
            }
            Debug.Log("[Debug] Mind Garden Skill Tree Reset!");
        }
    }

    private void ApplyModifier(string modifierID)
    {
        if (PlayerInventoryCore.Instance == null)
        {
            Debug.LogWarning("[EchoDebugTester] PlayerInventoryCore not found!");
            return;
        }

        EchoData activeEcho = PlayerInventoryCore.Instance.GetActiveEcho();
        if (activeEcho == null)
        {
            Debug.LogWarning("[EchoDebugTester] No Active Echo equipped! Please equip an Echo first.");
            return;
        }

        activeEcho.uniqueModifierID = modifierID;

        if (force100PercentProc)
        {
            activeEcho.statusProcCoefficient = 1.0f;
            activeEcho.currentStatusProc = 1.0f;
        }

        Debug.Log($"[EchoDebugTester] Swapped active modifier to: {modifierID} | Proc Chance: {activeEcho.currentStatusProc * 100}%");
        UpdateStatusDisplay();
    }

    private void UpdateStatusDisplay()
    {
        if (PlayerInventoryCore.Instance != null && PlayerInventoryCore.Instance.GetActiveEcho() != null)
        {
            EchoData activeEcho = PlayerInventoryCore.Instance.GetActiveEcho();
            currentModifier = activeEcho.uniqueModifierID;
            currentProcChance = activeEcho.currentStatusProc;
        }
    }

    private void OnGUI()
    {
        if (!enableDebugKeys) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUI.color = Color.green;
        GUILayout.Label("<b>--- ECHO DEBUG TESTER ---</b>");
        GUILayout.Label($"Active Modifier: {currentModifier}");
        GUILayout.Label($"Proc Chance: {currentProcChance * 100}%");
        GUILayout.Space(10);
        GUILayout.Label("Press 1: Blaze (IGNITION)");
        GUILayout.Label("Press 2: Frostbite (FROSTBITE)");
        GUILayout.Label("Press 3: Arc (CHAIN_ARC)");
        GUILayout.Label("Press 4: Kinetic (KINETIC_FORCE)");
        GUILayout.Label("Press 5: Anomaly (DISTORTION)");
        GUILayout.Label("Press 6: Void (FUS_EVENT_HORIZON)");
        GUILayout.Label("Press 7: Curse (OBLIVION)");
        GUILayout.Space(10);
        GUI.color = Color.cyan;
        GUILayout.Label("<b>--- MIND GARDEN ---</b>");
        GUILayout.Label("Press F9: Give 9999 Astral Shards");
        GUILayout.Label("Press F10: Reset Entire Skill Tree");
        GUILayout.EndArea();
    }
}
