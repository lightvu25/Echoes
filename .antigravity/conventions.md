# Project Conventions

## Primary Language & Framework
The project is predominantly written in C# (over 8000 files) and built using the Unity game engine. It leverages Unity's core systems for game logic, asset management, and compilation.

## Project Structure Overview
The project follows a standard Unity directory structure. Key directories include:
- `Assets`: Contains all game-specific code (e.g., `Scripts/Combat System`, `Player`, `Data`), art, audio, and scenes. Scripts are organized into logical sub-folders by system or feature.
- `Packages`: Manages Unity Package Manager (UPM) dependencies.
- `ProjectSettings`: Stores project-wide configuration for Unity.
- `artifacts`: A custom directory likely for build outputs or generated files.

## Code Style Observations
- **Modular Scripting:** C# scripts are organized into domain-specific folders (e.g., `Combat System`, `Player`), promoting modularity.
- **Component-Based:** Code like `HealthSystem`, `MemoryInventorySystem` suggests adherence to Unity's component-based architecture.
- **Event-Driven:** Use of events (e.g., `OnSlotsChanged`) for inter-component communication is observed.
- **Data Structures:** Clearly defined C# classes (e.g., `ProfileData`) for game state and player data.

## Testing Approach
There is currently no formal testing approach or framework (e.g., Unity Test Runner, NUnit) configured or in use, as indicated by "Has tests: False". Testing is presumably manual.

## CI/CD Setup
No Continuous Integration or Continuous Delivery pipelines are set up ("Has CI: False"). Build and deployment processes are likely manual. Docker is not used.