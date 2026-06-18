# Changelog

All notable changes to this project will be documented in this file.

## Unreleased

- No changes yet.

## v0.2.0 - Documentation, Cleanup, and Optimization

### Added
- Main menu scene flow.
- Pause menu and pause-state handling.
- Game state manager for gameplay, pause, game-over, and level-complete states.
- Enemy line-of-sight detection.
- Enemy last-known-position investigation behavior.
- NavMesh-based enemy navigation.
- Enemy prefab.
- Level2.
- XML documentation for major gameplay systems.

### Changed
- Improved code maintainability with concise summaries on player health, player attack, enemy health, enemy detection, enemy patrol, exit door, objective, game state, pause menu, and main menu systems.
- Cached repeated squared-distance thresholds in enemy AI and contact-damage checks.
- Reduced avoidable player-attack console warning spam when no camera is available.
- Kept gameplay behavior unchanged during cleanup.

### Fixed
- Removed an obsolete pause-menu placeholder comment.

## v0.1.0 - Prototype

### Changed
- Prepared the project for the `v0.1.0` prototype release.
- Set the playable GameScene as the only enabled build scene.
- Reduced routine gameplay debug logging for cleaner release validation.
- Removed the temporary test interaction object and script.
- Updated project documentation with setup, controls, and release validation notes.

### Fixed
- Assigned serialized PlayerAttack camera-controller and attack-radius values in GameScene.

### Added
- Player movement
- Sprint and jump
- First-person camera
- Third-person camera
- Camera switching
- Interaction system
- Collectibles
- Objective system
- Exit door
- HUD
- Enemy patrol
- Enemy detection
- Enemy chase
- Enemy attack
- Enemy attack standoff
- Enemy health
- Enemy death feedback
- Player attack
- Player health
- Game over UI
- Restart system
- Level complete UI
- Level blockout v01
