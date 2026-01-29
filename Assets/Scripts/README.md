# Advanced Movement 2D

A feature-rich 2D platformer game developed in Unity, featuring advanced player movement mechanics, time manipulation, and dynamic camera systems.

## 🎮 Features

### Advanced Player Movement
- **Running & Air Control**: Smooth acceleration/deceleration with customizable physics
- **Jumping**: Variable jump height with coyote time and input buffering
- **Wall Jumping**: Wall slide and wall jump mechanics with directional control
- **Dashing**: Multi-directional dash ability with cooldown and refill system
- **Gravity Modifiers**: Dynamic gravity scaling for jump hang time, fast falls, and wall slides

### Time Manipulation
- **Time Rewind**: Rewind player position and game state by holding Enter key
- **Time Management**: Collectible time pickups to extend gameplay duration
- **Record System**: Records up to 5 seconds of player movement history

### Game Systems
- **Level Progression**: Multi-level system with scene management
- **Score System**: Collect coins to increase score
- **Goal System**: Success/Fail conditions based on reaching the goal
- **Pause/Resume**: Pause functionality with menu system

### Camera & Visual Effects
- **Cinemachine Integration**: Smooth camera following and transitions
- **Dynamic Camera Zoom**: Adaptive camera zoom based on game state
- **Camera Shake**: Screen shake effects on pickups and events
- **Parallax Background**: Multi-layer parallax scrolling backgrounds

### Audio System
- **Sound Manager**: Handles all sound effects (coin pickup, time pickup, success/fail)
- **Music Manager**: Background music with volume control
- **Player Audio**: Movement-based audio feedback (footsteps, jumps, landings, dash)

### UI System
- Main Menu
- Pause Menu
- Game Over Screen
- Pass/Level Complete Screen
- Stats Display (score, time)

## 🛠️ Technical Architecture

### Core Components

#### Player System (`/Player`)
- `PlayerMovement.cs` - Advanced physics-based 2D movement controller
- `PlayerInteract.cs` - Handles pickups, goals, and game state
- `PlayerData.cs` - ScriptableObject for configurable player parameters
- `PlayerVisual.cs` - Visual feedback and animations
- `PlayerAudio.cs` - Audio feedback for player actions

#### Game Management
- `GameManager.cs` - Core game loop, level loading, scoring, and state management
- `GameManagerVisual.cs` - Visual effects and feedback
- `GameInput.cs` - Unified input handling system
- `GameLevel.cs` - Level configuration and setup
- `SceneLoader.cs` - Scene transition management

#### Camera System
- `CinemachineCameraShake2D.cs` - Impulse-based camera shake effects
- `CinemachineCameraZoom2D.cs` - Smooth camera zoom transitions

#### Pickup System (`/PickUp`)
- `CoinPickup.cs` - Collectible coins for scoring
- `TimePickup.cs` - Time extension pickups
- `PickupVisual.cs` - Visual effects for pickups

#### Time System
- `TimeRewind.cs` - Time rewind mechanics
- `PointInTime.cs` - Data structure for time snapshots

#### UI Components (`/UI`)
- `MainMenuUI.cs` - Main menu interface
- `PausedUI.cs` - Pause menu and controls
- `PassUI.cs` - Level completion screen
- `GameOverUI.cs` - Game over screen
- `StatsUI.cs` - Score and time display

#### Audio
- `SoundManager.cs` - Sound effects manager
- `MusicManager.cs` - Background music controller

#### Other
- `ParallaxBackground.cs` - Scrolling background effect
- `Goal.cs` - Level goal/finish line

## 🎯 Controls

- **Arrow Keys / WASD**: Movement
- **Space**: Jump
- **Left Shift**: Dash
- **Enter**: Time Rewind (Hold)
- **Escape**: Pause Menu

## 📋 Requirements

- Unity 2022.3 LTS or higher
- Cinemachine package
- Unity Input System package

## 🚀 Setup

1. Clone this repository
2. Open the project in Unity
3. Ensure all required packages are installed (Cinemachine, Input System)
4. Open the main scene and press Play

## 🎨 Player Data Configuration

The player movement is highly customizable through the `PlayerData` ScriptableObject. Key parameters include:

- **Gravity Settings**: Gravity strength, fall multipliers, max fall speeds
- **Run Parameters**: Max speed, acceleration, deceleration, air control
- **Jump Settings**: Jump height, time to apex, hang time effects
- **Wall Jump**: Force, lerp amount, duration
- **Dash Settings**: Speed, attack time, end time, refill time
- **Assists**: Coyote time, input buffer time

## 📝 Game Flow

1. **Waiting to Start**: Game begins in idle state
2. **Normal Play**: Timer counts down as player navigates level
3. **Collecting Items**: Coins increase score, time pickups extend duration
4. **Win Condition**: Reach the goal before time runs out
5. **Fail Condition**: Time expires before reaching goal
6. **Level Progression**: Advance through multiple levels

## 🔧 Development Notes

- Uses Unity's new Input System for cross-platform input handling
- Implements event-driven architecture for loose coupling
- Singleton pattern used for manager classes
- ScriptableObject-based data for easy balancing
- Physics-based movement with Force mode for realistic feel

## 📦 Project Structure

```
Assets/Script/
├── Player/                 # Player-related scripts
│   ├── PlayerMovement.cs
│   ├── PlayerInteract.cs
│   ├── PlayerData.cs
│   ├── PlayerVisual.cs
│   └── PlayerAudio.cs
├── PickUp/                 # Pickup items
│   ├── CoinPickup.cs
│   ├── TimePickup.cs
│   └── PickupVisual.cs
├── UI/                     # User interface
│   ├── MainMenuUI.cs
│   ├── PausedUI.cs
│   ├── PassUI.cs
│   ├── GameOverUI.cs
│   └── StatsUI.cs
├── GameManager.cs          # Core game management
├── GameManagerVisual.cs
├── GameInput.cs
├── GameLevel.cs
├── TimeRewind.cs           # Time manipulation
├── PointInTime.cs
├── SoundManager.cs         # Audio management
├── MusicManager.cs
├── CinemachineCameraShake2D.cs
├── CinemachineCameraZoom2D.cs
├── ParallaxBackground.cs
├── SceneLoader.cs
└── Goal.cs
```

## 👤 Author

QuangZu/Beyond-the-Garden

## 📄 License

This project is part of a Unity learning exercise.

---

**Note**: This is an educational project demonstrating advanced 2D platformer mechanics in Unity.
