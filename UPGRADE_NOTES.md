# TwisTime – upgraded to Unity 6 (6000.5.10f1) and ported to C#, 2026-10-16

Source: `C:\Users\razva\projects\twis-time` (Unity 4, 2012, iPhone / iPad; the free "Time Trial only" edition).
That folder was not touched; this folder is a copy without its old `Library` and the Visual Studio / MonoDevelop files.

Build: menu **TwisTime > Build Windows** (`Assets/Editor/GameBuild.cs`) → `Builds/Windows/TwisTime.exe`, 105 MB.
Portrait 640 x 960 window, resizable: the picture keeps its 2:3 shape, with black bars on other window shapes.

## Removed (no longer relevant)
- **iAd** (Apple's ad network, closed 2016): `Plugins/iAd` (binding, manager, adapter, test scenes, prefabs), its native
  iOS code in `Editor/iAd`, and the banner calls in the menu and the game.
- **iOS / Xcode build hooks**: `Editor/PostprocessBuildPlayer`, `runner.py`, `UpdateXcode.pyc`.
- **App Store links**: the "Available on the App Store" button (it opened the paid version's page) with its two
  pictures, and the "Legal" button (Apple's App Store licence). The unused "buy" confirmation page.
- **OpenFeint**: only a leftover "OpenFeint:" text was left; removed from `Resources/lang_en.txt` (with "Legal").
  There was no in-app purchase code in this edition; the App Store button was its only "buy" path.
- **iTween** (2.0.45, 280 KB, used only for `MoveTo`) → `Common/Tween.cs`; **PlayerPrefsX.js** → `Common/PrefsArrays.cs`.
- Unused: `fpsCounter.cs`, `menuRot.js`, the 20 sample particle effects of Standard Assets with their materials,
  textures, shader and animations, three stray materials, a language-button picture, two loose sounds, the iOS
  splash screen and app icons (the 144 px icon is kept: it is now the Windows icon). 64 files / folders in all.
- The default "multiplayer center" package.

## The C# port (was UnityScript, one 1500-line class for the game and a 700-line one for the menu)
Scripts that sat on scene objects / prefabs keep their old GUID, so every link in the scenes still holds:
gameLogic.js → GameController, gemLogic.js → Gem, progressBar.js → TimeBar, pstBar.js → TimeBarTail,
mainMenu.js → MainMenuController, menuItemLogic.js → MenuItem.

| File | What it is |
|---|---|
| Game/Board.cs | the 8 x 8 matrix and its rules, no Unity objects: runs, chunks, big chunks, falling, filling, moves, chunk score. The four near-copies of the chunk code (animate / remove, normal / big) are one search |
| Game/GameMode.cs | the five modes and, in one table, what differs per mode (save keys, background, music, timer) – was an if-chain in eight places |
| Game/SaveGame.cs | the game to continue and the high score table, same PlayerPrefs keys as in 2012 |
| Game/GameController.cs | the board on screen and the move sequence: select, swap, cascade, combos, time out, game over, high score |
| Game/PauseMenu.cs | pause / hint buttons, pause menu, yes / no, help page |
| Game/Gem.cs, TimeBar.cs, TimeBarTail.cs | one gem; the time bar and its end piece |
| Menu/MainMenuController.cs, MenuItem.cs | the menu pages; the four near-copies of the page animation are "hide page" + "show page" |
| Common/ | Tween, PrefsArrays, GameOptions (sound / vibration + sound effects), Pointer (finger or mouse as a touch), HudButton, SpriteSheet (the 5 x 5 sprite animation that three scripts had copied), TextManager |
| LegacyGUI/ | stand-ins for GUITexture / GUIText / GUILayer (see below) |

Behaviour kept as it was, with these deliberate changes:
- **Mouse = touch.** The touch code now also runs for the mouse (in 2012 the mouse only half worked, editor only),
  so the pause menu, yes / no and help work with the mouse. Dragging across gems swaps them, as with a finger.
- **Timing.** The game ran at 20 fps (the menu set it, nothing changed it back) and the waits were tuned to that.
  It now renders at 60; the sprite-sheet animations still step at 1/20 s, so the timings match.
- **Steam effect** is always on (in 2012 it was removed except on iPhone 4S / iPad 3, which a PC is not).
- **Bug fixed:** when there was no move left in a continued game, the old code kept the stuck board; it now always
  deals a new one. **Bug kept:** in Combo 20 with combos left and no move left, nothing happens (as before).
- **Bug fixed:** the gem swapped into a group of three did not spin with the others before they vanished (its swap
  spin was still running, and a running spin blocked the vanish spin). The vanish spin now cuts the other one off.
- High score names are the computer's name (SystemInfo.deviceName), as the iPhone's name was before.

## Unity 4 parts that Unity 6 no longer has
- **GUITexture / GUIText / GUILayer** (110 elements: backgrounds, board tiles, buttons, menus, texts): settings read
  from the original scenes and put back as `LegacyGUITexture` / `LegacyGUIText`, and `LegacyGUILayer` on the cameras.
  They draw like GUILayer did: after each camera, only what its culling mask sees (back camera: background and
  tiles; main camera, after the 3D gems: score, buttons, menus), all textures first and then all texts, each by z
  (the game relies on texts never being under a picture). Pixel layout is the 640 x 960 of the iPhone 4.
- **Legacy particles** (the burst when gems vanish, the steam over the board and the menu): rebuilt as
  ParticleSystems from their recorded settings (size, life, emission, ellipsoid, velocity, damping, colour animation,
  size growth, auto-destruct).
- Menu item labels: their font material was a Unity 4 sub-asset that no longer exists; they use the font's material
  now, and draw after their panel (both lie in the same plane).
- Textures compressed: 34 to BC7; 17 stay uncompressed (sizes not divisible by 4). All assets saved as text.

## Test (`Assets/Scripts/Common/GameTestRun.cs`, only with `-tt-test <folder>`; does nothing in normal play)
Menu → Scores → Help → Options → Play → New Game → the hinted move (swap, chunk spins and bursts, cascade, score
100 / 300) → pause menu → clock run out → Game Over → New High Score (saved, 10th place, computer name) → back to
the menu. 0 errors. Also checked: a 1200 x 700 window shows the 2:3 picture centred with black bars, gems on tiles.
The test's high scores and window settings were deleted from the registry afterwards.

## All five modes unlocked (2026-10-16, later)
The free edition was locked to Time Trial (Play went straight to it, Scores showed only its table). Now:
- Play → mode page (Classic, Time Trial, Survival, Combo 20, Combo Mode) → New Game / Continue for that mode;
  back from New Game / Continue returns to the mode page.
- Scores → mode page → that mode's table; back returns to the mode page.
- Options > Clear Data clears the saved games and tables of all five modes (was Time Trial only).
- The free edition shipped only Time Trial's background (`back2`) and music (`musicTime`); the other modes' files
  (`back1`, `back3`, `musicClassic`, `musicSurvival`, `musicCombo20`, `musicCombo`) were never in it, so those modes
  use Time Trial's. Drop the real files into `Assets/Resources` under those names and they are used instead.
- Help text rewritten: one line per mode instead of "the only available game mode in this version is TIME TRIAL".
- Test, once per mode (`-tt-test <folder> <0..4>`), 0 errors each: Classic (no time bar; Quit saves the game, the
  mode's Continue unlocks), Time Trial and Survival (time bar; time out, high score saved), Combo 20 (counter x20 ->
  x19 after a scoring move), Combo Mode (starts with x1; Quit saves). Test high scores deleted afterwards.

## Controls
Mouse or touch: click a gem, then a neighbour (or drag from one to the other). "?" = hint, the pause button = menu.
