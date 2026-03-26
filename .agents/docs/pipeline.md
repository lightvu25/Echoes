# Asset Pipeline and Workflow Rules

## 1. Pixel Art Import Settings (Aseprite to Unity)
Whenever a new sprite sheet or image is imported from Aseprite, the following Unity import settings MUST be applied to prevent blurring and artifacts:
* **Texture Type:** Sprite (2D and UI)
* **Pixels Per Unit (PPU):** Strictly set to 16 (or the established project standard).
* **Mesh Type:** Full Rect
* **Filter Mode:** Point (no filter)
* **Compression:** None

## 2. Folder Structure
The AI must strictly adhere to the following directory structure when creating or moving files:
* `Assets/Scripts/` -> Modular organization (e.g., `Core`, `Combat System`, `Player`, `Enemy`, `UI`).
* `Assets/Sprites/` -> UI and Gameplay art organized by entity or system.
* `Assets/Prefabs/` -> Reusable GameObjects, typically sub-categorized like `Player`, `Enemy`, `Pickups`.
* `Assets/Data/` -> ScriptableObject instances and data assets.
* `Assets/Materials/` -> Visual materials and shaders.
* `Assets/Audio/` & `Assets/Fonts/` -> Sound and text assets.
* **Prohibited**: Do not create or move files into Third-Party folders like `DOTween` or `AstarPathfindingProject`.

## 3. Prefab Workflow
* **Rule:** Never modify objects directly in the active scene.
* **Implementation:** All gameplay elements must be saved as Prefabs. If the AI needs to modify an enemy's behavior or components, it must modify the base Prefab, not the instance in the scene hierarchy.

## 4. Strict Agent Workflow & Execution Protocol
Whenever the user submits a new prompt, feature request, or bug report, the AI MUST strictly follow this multi-step execution order:

**Phase 1: Deep Analysis & Inquiry**
* **Read & Analyze:** Read the requirements carefully. Do not immediately start writing code. 
* **Investigate:** You MUST use the Unity MCP tool to actively scan the codebase, read the relevant file codes, and inspect the Unity hierarchy or active scene. Never guess how a system is wired together.
* **Clarify:** Before proposing any solutions, you MUST ask the user **at least 5 specific questions** regarding the project context, edge cases, component setups, or exact mechanics to ensure complete understanding.

**Phase 2: Proposal & Confirmation**
* **Outline the Plan:** Present a clear, step-by-step plan of exactly which files will be modified and what logic will be added.
* **Mandatory Stop:** You MUST explicitly ask the user for confirmation. **DO NOT** make any file changes or write final code until the user approves the proposed plan.

**Phase 3: Debugging & Self-Testing**
* **No Guesswork:** If tasked with debugging an issue, you must actively use the Unity MCP tool to check the Editor state, verify tags, components, and active UI elements.
* **Agent Testing:** Do not hand off untested, assumed fixes. Use the MCP tool to test and verify your logic first before showing the results or asking the user to test it for you.

**Phase 4: Handoff & Explanation**
* **Code Breakdown:** After executing the confirmed changes, provide a brief, easy-to-understand explanation of how the new code works.
* **Manual Action Checklist:** Clearly list any manual steps the user must take inside the Unity Editor to make the code work (e.g., "Open the `Enemy` Prefab and drag the `Damage_Text` object into the empty Inspector slot," or "Ensure the Player has the 'Player' tag applied").

## 5. Strict Token Management & Output Restrictions
To preserve API limits and maintain a highly focused context window, the AI MUST adhere to the following output constraints at all times:

* **Diff-Only Code Generation:** NEVER output an entire script unless explicitly instructed to generate a completely new file from scratch. When modifying existing scripts, only output the specific methods, properties, or lines being changed. Assume the user will integrate the snippet into the existing architecture.
* **Surgical MCP Queries:** When using the Unity MCP tool to read the scene or hierarchy, NEVER query the entire active scene or root level. You must target specific GameObjects by name, tag, or component to keep the returned JSON payload as small as possible.
* **Concise Communication:** Keep explanations brief, direct, and highly technical. Do not explain basic Unity concepts, standard C# syntax, or what a generic component does unless specifically asked. Focus purely on the specific logic being implemented for Project Echoes.
* **No Speculative Feature Creep:** Write code ONLY for the specific task, mechanic, or bug fix currently approved in the plan. Do not add unsolicited "bonus" features, extra debug logs, or speculative logic that bloats the output.