using UnityEngine;

/// <summary>
/// Ultra-thin input bridge for hotbar Echo cycling.
/// Listens for keyboard and scroll-wheel input from <see cref="GameInput"/>
/// and delegates to <see cref="PlayerInventoryCore.SetActiveEchoIndex"/>.
///
/// This class intentionally holds NO inventory state.
/// All state lives in PlayerInventoryCore.
/// </summary>
public class HotbarController : MonoBehaviour
{
    private const int MAX_HOTBAR_SLOTS = RunData.MAX_SLOTS; // matches category max

    private int currentIndex = 0;

    private void OnEnable()
    {
        if (GameInput.Instance == null) return;
        GameInput.Instance.OnHotbarKeyPressed  += HandleHotbarKey;
        GameInput.Instance.OnCycleNextPressed  += CycleNext;
        GameInput.Instance.OnCyclePrevPressed  += CyclePrev;
    }

    private void OnDisable()
    {
        if (GameInput.Instance == null) return;
        GameInput.Instance.OnHotbarKeyPressed  -= HandleHotbarKey;
        GameInput.Instance.OnCycleNextPressed  -= CycleNext;
        GameInput.Instance.OnCyclePrevPressed  -= CyclePrev;
    }

    // ------------------------------------------------------------------ //
    //  Input handlers                                                      //
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Switches to the hotbar slot at the pressed key index (0-based).
    /// </summary>
    /// <param name="keyIndex">0-based index matching the key pressed (0 = key 1).</param>
    private void HandleHotbarKey(int keyIndex)
    {
        if (keyIndex < 0 || keyIndex >= MAX_HOTBAR_SLOTS) return;
        SetIndex(keyIndex);
    }

    private void CycleNext()
    {
        int limit = GetEffectiveSlotCount();
        SetIndex((currentIndex + 1) % limit);
    }

    private void CyclePrev()
    {
        int limit = GetEffectiveSlotCount();
        SetIndex((currentIndex - 1 + limit) % limit);
    }

    // ------------------------------------------------------------------ //
    //  Helpers                                                             //
    // ------------------------------------------------------------------ //

    private void SetIndex(int index)
    {
        currentIndex = index;
        PlayerInventoryCore.Instance?.SetActiveEchoIndex(currentIndex);
    }

    private int GetEffectiveSlotCount()
    {
        return PlayerInventoryCore.Instance != null
            ? Mathf.Max(1, PlayerInventoryCore.Instance.UnlockedEchoSlots)
            : MAX_HOTBAR_SLOTS;
    }
}
