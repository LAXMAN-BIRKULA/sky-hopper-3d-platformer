# Sky Hopper

A stylized third-person 3D platformer built with Unity 6 and the Universal Render Pipeline. Hop across floating platforms, collect every coin, and finish two hand-built courses. The same build plays with a keyboard and mouse on desktop or with on-screen controls on a phone.

**This is my first project in game development.** It began as a learning exercise in Unity and grew into a complete, playable game with two levels, menus, touch controls and a macOS and browser build.

## Features

- **Two levels plus a level select menu.** Level 1 is the introductory course; Level 2 is a longer ascending course with 13 coins.
- **Responsive movement.** Camera-relative WASD, acceleration and air control, coyote time, buffered and double jumps, and a run animation whose playback speed is matched to the player's real travel speed so his feet do not slide.
- **Ladders.** Hold `Space` / `JUMP` near a ladder to climb, or simply walk into it to grab it. Climb down with `S` or the stick, and step off the top onto the platform without re-grabbing.
- **Coins, score and a HUD.** Collect all coins in a level to trigger the win screen.
- **Win screen.** NEXT LEVEL (where applicable), RESTART, LEVEL SELECT, and QUIT GAME.
- **Pause menu.** `Esc` / `P` or the on-screen pause button opens RESUME / RETRY LEVEL / LEVEL SELECT / QUIT GAME. Time and audio freeze while paused, and the overlay reliably closes when you leave the level.
- **Death and retry.** Falling plays a death animation and shows a restart prompt.
- **Touch controls.** A virtual joystick, jump/climb button, right-side look drag, and pause button are created at runtime when a touch device is detected. No scene needs a touch component.

## Controls

### Keyboard and mouse

| Action | Input |
| --- | --- |
| Move | `WASD` or arrow keys |
| Look around | Mouse (cursor locks during play) |
| Jump | `Space` |
| Climb a ladder | Hold `Space` near it, or walk into it |
| Climb down | `S` |
| Pause / resume | `Esc` or `P` |
| Start a level from the menu | `1` or `2` |
| Quit from the menu | `Q` |
| Win screen | `N` next, `R` restart, `L` levels, `Q` quit |
| Restart after death | `R`, `Space`, or `Enter` |

### Touch

| Action | Input |
| --- | --- |
| Move | Left-side virtual joystick |
| Jump / climb | `JUMP` button on the right (hold it near a ladder to climb) |
| Look | Drag on the right side of the screen |
| Pause | Pause button in the top-right corner |
| Restart after death | Tap anywhere |

On a browser, append `?touch=1` to force the touch controls on a desktop or `?touch=0` to hide them.

## Playing the builds

Player builds are generated locally under `Builds/`; that folder is ignored by Git, so it is not part of this repository.

**macOS:** Open `Builds/SkyHopper.app`, or unzip `Builds/SkyHopper-macOS.zip` first. The app is unsigned, so on another Mac use right-click → Open, or remove the quarantine flag:

```sh
xattr -dr com.apple.quarantine "Sky Hopper.app"
```

**Browser / phone:** Serve the WebGL build over HTTP (browsers do not load it from `file://`):

```sh
cd Builds/WebGL
python3 -m http.server 8099 --bind 0.0.0.0
```

Then open `http://localhost:8099/?touch=1` on the same computer, or `http://<computer's-LAN-IP>:8099/?touch=1` on a phone connected to the same network.

## Building from source

- Unity **6000.6.7f1** with the Universal Render Pipeline and Input System packages installed, plus the macOS and WebGL build support modules.
- Open the project and use these scenes in Build Settings, in this order: `LevelSelect` (0), `Level1` (1), `Level2` (2).
- WebGL settings: Gzip compression with decompression fallback, and "Run In Background" enabled.
- macOS settings: release build, universal x64 + ARM64 architecture.

The SSAO renderer feature was deliberately removed from the URP renderer: its shaders get stripped in players and caused a render-loop crash. Post-processing is therefore disabled in player builds.

## Repository layout

| Path | Contents |
| --- | --- |
| `Assets/Scenes/` | The three build scenes: menu and both levels |
| `Assets/Platformer/Scripts/` | Player, camera, ladder, game flow, UI and input scripts |
| `Assets/Resources/Textures/` | Ground, stone, wood and fabric textures, with credits |
| `Assets/Settings/` | URP assets and WebGL build profiles |
| `Screenshots/` | Captures of the menus, touch HUD and pause screen |

## Credits

- Character and animation assets in `Assets/ThirdParty/` come from Quaternius (PlatformerKit) and ManAnimated.
- The textures in `Assets/Resources/Textures/` are CC0 from [ambientCG](https://ambientcg.com/); see `CREDITS.txt` for the full list.

The game intentionally has no enemies, multiplayer, or save system.
