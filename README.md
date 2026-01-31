# Find Me - Unity Game

A 2D platformer game featuring advanced movement mechanics, time rewind functionality, and polished gameplay systems built with Unity.

## 🎮 Core Features

### Advanced Player Movement System
The game implements a sophisticated 2D movement system with physics-based controls and multiple movement abilities:

- **Running & Walking**: Smooth acceleration and deceleration with customizable speeds
- **Jumping**: Variable jump height based on button hold duration
- **Wall Jumping**: Jump from walls with directional force
- **Wall Sliding**: Slide down walls with reduced gravity
- **Dashing**: Fast directional dash with cooldown system
- **Coyote Time**: Grace period for jumping after leaving a platform
- **Jump Buffering**: Queue jump inputs before landing

#### Movement Controls
- **Arrow Keys / WASD**: Move left and right
- **Space**: Jump
- **Left Shift**: Dash
- **Return/Enter**: Time Rewind (hold)

### Time Rewind Mechanism
Unique gameplay mechanic allowing players to reverse time:

- **Position & Rotation Recording**: Continuously records player position and rotation
- **Configurable Record Duration**: Default 5 seconds of rewind time
- **Game Time Synchronization**: Restores in-game timer when rewinding
- **Physics Integration**: Switches rigidbody between kinematic (during rewind) and dynamic modes

### Camera System
Dynamic camera with cinematic effects powered by Cinemachine:

- **Camera Shake**: Screen shake on pickups and events
- **Smooth Zoom**: Dynamic zoom in/out with smooth transitions
- **Object Tracking**: Follows player with customizable tracking

### Pickup & Collectibles System
Multiple collectible types with distinct behaviors:

- **Coins**: Award points (100 points per coin)
- **Time Pickups**: Restore time (2 seconds per pickup)
- **Goal**: Level completion trigger
- **Visual Effects**: Particle effects on collection
- **Audio Feedback**: Unique sounds for each pickup type

### Game Management
Comprehensive game state and level management:

- **State Machine**: WaitingToStart → Normal → GameOver
- **Level System**: Multiple levels with progression
- **Score Tracking**: Points and time-based scoring
- **Scene Management**: Smooth transitions between scenes
- **Pause/Resume**: Full game pause functionality

### Audio System
Dual-layer audio with separate volume controls:

- **Music Manager**: Background music with persistent playback across scenes
- **Sound Manager**: Sound effects for:
  - Coin pickups
  - Time pickups
  - Jump, land, walk, dash
  - Success/fail states
- **Independent Volume Control**: Separate sliders for music and SFX

### Visual Effects
Rich particle and animation systems:

- **Player Effects**:
  - Walking dust particles
  - Jump particles
  - Landing impact particles
  - Death effects
- **Pickup Effects**: Particle bursts on collection
- **Parallax Background**: Multi-layer scrolling backgrounds

### Environment & Hazards
Interactive level elements:

- **Thorns/Traps**: Instant death on contact
- **Parallax Backgrounds**: Depth-based scrolling for visual appeal
- **Platforms**: Physics-based collision detection

### User Interface
Complete UI system for all game states:

- **Main Menu UI**: Start game and settings
- **Stats UI**: 
  - Real-time score display
  - Time remaining bar
  - Current level indicator
- **Pause UI**: Pause menu with resume/quit options
- **Pass UI**: Level completion screen
- **Game Over UI**: Retry and menu options

## 📂 Project Structure

```
Assets/Scripts/
├── Camera/
│   ├── CinemachineCameraShake2D.cs    # Impulse-based camera shake
│   └── CinemachineCameraZoom2D.cs     # Smooth camera zoom system
│
├── Core Gameplay/
│   ├── PointInTime.cs                 # Data structure for time rewind snapshots
│   └── TimeRewind.cs                  # Time rewind system implementation
│
├── Environment/
│   ├── ParallaxBackground.cs          # Parallax scrolling effect
│   └── Trap/
│       └── Thorns.cs                  # Hazard that triggers player death
│
├── Manager/
│   ├── GameManager.cs                 # Core game state and level management
│   ├── GameManagerVisual.cs           # Visual feedback for game events
│   ├── MusicManager.cs                # Background music control
│   └── SoundManager.cs                # Sound effects management
│
├── PickUp/
│   ├── CoinPickup.cs                  # Coin collectible
│   ├── Goal.cs                        # Level completion trigger
│   ├── PickupVisual.cs                # Particle effects for pickups
│   └── TimePickup.cs                  # Time restoration item
│
├── Player/
│   ├── PlayerAudio.cs                 # Player sound effects
│   ├── PlayerData.cs                  # ScriptableObject for movement parameters
│   ├── PlayerInteract.cs              # Collision detection and game state
│   ├── PlayerMovement.cs              # Advanced movement controller
│   └── PlayerVisual.cs                # Player visual effects
│
├── UI/
│   ├── GameOverUI.cs                  # Game over screen
│   ├── MainMenuUI.cs                  # Main menu interface
│   ├── PassUI.cs                      # Level completion screen
│   ├── PausedUI.cs                    # Pause menu
│   └── StatsUI.cs                     # In-game HUD
│
├── GameInput.cs                       # Input system (currently commented out)
├── GameLevel.cs                       # Level configuration
└── SceneLoader.cs                     # Scene loading utility
```

## 🔧 Technical Implementation Details

### PlayerMovement.cs - Advanced Movement
The movement system uses physics-based calculations with customizable parameters:

**Key Features:**
- **Dynamic Gravity Scaling**: Adjusts gravity based on jump state (falling, jump cut, jump hang)
- **Momentum Conservation**: Optional momentum preservation for realistic physics
- **Air Control**: Different acceleration/deceleration multipliers in air vs ground
- **Jump Hang**: Reduced gravity near jump apex for better control
- **Fast Fall**: Increased gravity when holding down
- **Wall Jump Duration**: Temporary movement restriction after wall jump
- **Dash System**: Two-phase dash (attack + end) inspired by Celeste
- **Dash Refill**: Automatic dash restoration when grounded

**Movement States:**
- `isFacingRight`: Character direction
- `isJumping`: Active jump state
- `isWallJumping`: Wall jump in progress
- `isDashing`: Dash ability active
- `isSliding`: Wall slide state

### PlayerData.cs - Configuration
ScriptableObject-based configuration system for easy tweaking:

- **Gravity Settings**: Custom gravity calculations based on jump parameters
- **Run Parameters**: Max speed, acceleration, deceleration
- **Jump Mechanics**: Height, time to apex, hang time effects
- **Wall Jump**: Force vectors, duration, movement lerp
- **Dash Properties**: Speed, duration, cooldown, refill time
- **Assist Features**: Coyote time, input buffering

### TimeRewind.cs - Time Manipulation
Records player state in a circular buffer and allows playback in reverse:

- **Recording**: Stores position, rotation, and game time every FixedUpdate
- **Rewind**: Plays back stored states in reverse order
- **Physics Mode Switching**: Changes Rigidbody2D to kinematic during rewind
- **Time Restoration**: Synchronizes in-game timer with rewound state

### PlayerInteract.cs - Game State Manager
Central hub for player interactions and game state:

- **Event-Driven Architecture**: Broadcasts events for pickups, goals, state changes
- **State Machine**: Manages game flow (WaitingToStart → Normal → GameOver)
- **Time Management**: Tracks and consumes player time
- **Score Tracking**: Maintains coin collection count
- **Collision Handling**: Processes triggers for all collectibles and hazards

### Audio Architecture
Singleton-based audio managers with independent volume control:

- **SoundManager**: Plays one-shot sound effects at game events
- **MusicManager**: Manages persistent background music across scenes
- **Volume Normalization**: Converts integer volume (0-10) to float (0-1)

### Camera Effects
Procedural camera effects using Cinemachine:

- **Impulse Source**: Generates camera shake via impulse signals
- **Lerp-based Zoom**: Smooth orthographic size transitions
- **Target Tracking**: Follows player or designated camera targets

## 🎯 Game Flow

1. **Start**: Main Menu → Select Level
2. **Waiting**: Player spawns, camera zooms out to show level
3. **Gameplay**: 
   - Move and collect coins
   - Avoid traps
   - Use time rewind strategically
   - Reach goal before time runs out
4. **Completion**: 
   - **Success**: Pass UI → Next Level
   - **Failure**: Game Over UI → Retry/Menu

## ⚙️ Customization

All movement parameters are exposed in `PlayerData` ScriptableObject:
- Jump height and timing
- Run speed and acceleration
- Wall jump forces
- Dash speed and duration
- Coyote time and input buffer windows
- Gravity multipliers for different states

## 🎨 Visual & Audio Polish

- **Particle Systems**: Custom effects for all player actions and pickups
- **Sound Design**: Layered audio feedback for all interactions
- **Camera Juice**: Screen shake and dynamic zoom enhance game feel
- **Parallax Layers**: Create depth and visual interest
- **UI Feedback**: Real-time stats and clear visual hierarchy

## 📝 Development Notes

- Built with **Unity 2D** and **Cinemachine**
- Uses **Rigidbody2D** for physics-based movement
- **Event-driven architecture** for loose coupling
- **Singleton pattern** for manager classes
- **ScriptableObjects** for data-driven design
- Physics calculations inspired by **Celeste's movement system**

---

**Last Updated**: January 2026
