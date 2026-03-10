---
description: How to add a new UI panel to the Echoes project
---

## Steps

1. **Create the Script**
   - Create new MonoBehaviour in `Assets/Scripts/UI/` (e.g., `InventoryUI.cs`)
   - Add show/hide methods with DOTween fade animations
   - Use `CanvasGroup` for alpha, interactable, and blocksRaycasts control

2. **Create the UI Prefab**
   - Create Canvas or panel in Unity Editor
   - Use TextMesh Pro for text elements
   - Design responsive layout with anchors and layout groups
   - Save as prefab in `Assets/Prefabs/`

3. **Wire Up with GameManager**
   - Add show/hide triggers in `GameManager` or relevant manager
   - Handle input (e.g., pressing a key to toggle the panel)
   - Manage game pause state if the panel should pause gameplay

4. **Add Transitions**
   - Use DOTween for smooth open/close animations
   - Consider slide-in, fade, or scale effects

5. **Test**
   - Verify at different screen resolutions
   - Test navigation and interaction
   - Ensure proper pause/unpause behavior
