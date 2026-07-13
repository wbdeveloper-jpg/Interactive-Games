SPELL-BOT RESCUE - FULL PACKAGE SETUP
=====================================

Purpose
-------
Reusable 2D educational spelling-correction mechanic for Classes 3-5.
The child fixes a misspelled word using a custom on-screen keyboard or external keyboard.

Requirements
------------
- TextMeshPro installed.
- DOTween installed.
- Bloom Reward System already exists in project.
- RewardManager prefab must remain only in LoadingScene / Loader Scene with DontDestroyOnLoad.
- Do not place RewardManager in this game scene.

Folder Structure
----------------
Mechanics/SpellBotRescue/Scripts/Runtime
- SpellBotRescueManager.cs
- SpellBotWordDatabase.cs
- SpellBotKeyboardKey.cs
- SpellBotKeyboardView.cs
- SpellBotUIFeedback.cs
- SpellBotRobotView.cs
- SpellBotWordCaretInput.cs
- SpellBotHowToPlayImagePager.cs
- SpellBotSafeAreaFitter.cs
- SpellBotBgmPlayer.cs

Mechanics/SpellBotRescue/Scripts/Editor
- SpellBotRescueSceneCreator.cs

Create New Rough Scene
----------------------
Use this for future new scenes:
Tools > Spell Bot Rescue > Create Rough Scene UI

The editor creates:
- Canvas with 1920x1080 reference resolution.
- SafeAreaRoot for mobile/tablet safe areas.
- Home page with game title, robot image, Start, How To Play.
- Bloom pre-game flow before Home page.
- Image-based How-To-Play panel with Prev / Next / Start Game.
- Top bar with Round text, Progress Slider, Score, Hint, Pause.
- Streak label + 3 image star slots.
- Robot placeholder.
- TMP_InputField monitor with professional caret.
- Taller monitor and hint area.
- Scene-editable custom keyboard keys, not prefabs.
- Pause panel.
- Result panel with Play Again + Continue.
- SpellBotBGM object with SpellBotBgmPlayer. Clip is empty by default.
- Sample word database asset with 50+ entries.

Current Finished UI Scene
-------------------------
Do not regenerate your current polished scene.
For your existing scene, add only the missing components manually:
- Add SpellBotBgmPlayer to an empty SpellBotBGM object.
- Assign music clip.
- Assign Overdrive Sprite on SpellBotRobotView if needed.
- Keep your existing UI layout.

Main Flow
---------
Scene Start:
1. RewardManager.Instance.ShowPreGame(bloomSkills)
2. Wait until RewardManager.Instance.IsPreGameComplete
3. Home page appears
4. Player clicks Start
5. How-To-Play opens first
6. HTP Start Game begins actual 10-round gameplay
7. Result panel appears after all rounds
8. Continue button opens Bloom post-game panel

Bloom Integration
-----------------
Direct Bloom integration is used. No scripting define symbol is needed.
SpellBotRescueManager implements:
- IGameSceneCallbacks
- IGameAudioCallbacks

Inspector fields:
- Use Bloom Reward System = true
- Expected Max Time = 120
- Bloom Home Scene Name = Loader Scene
- Count Show Answer As Bloom Mistake = true
- Stop Audio When Bloom Post Opens = true

Bloom scoring:
- timeScore = Clamp01(1 - timeTaken / Expected Max Time)
- accuracyScore = correctRounds / totalRounds
- mistakeCount = wrongAttempts + answerReveals when enabled
- timeTaken = raw seconds

Callbacks:
- OnPlayAgain reloads current scene.
- OnHome loads Bloom Home Scene Name.
- OnRewardScreenOpen stops game SFX AudioSource and fades SpellBotBGM if available.

Score / Hint / Show Answer
--------------------------
- Top label uses Score, not gears.
- Hint button is beside Pause.
- Hint shows hint text in the hint area.
- After hint is used, Show Answer button appears with DOTween attention animation.
- Show Answer displays correct spelling in the hint area only.
- Show Answer does not auto-fill the word.
- Show Answer does not auto-advance round.
- Student must still type the correct word manually.
- That round gives 0 score.
- Show Answer breaks streak and exits overdrive.

Keyboard / Caret
----------------
The word monitor uses TMP_InputField for clean caret and selection visuals.
The game still controls allowed input.

Supported:
- Tap/click between letters to move caret.
- A-Z on-screen keys insert at caret.
- External keyboard A-Z inserts at caret.
- Left/Right arrow moves caret.
- Home/End moves caret.
- Backspace deletes left of caret.
- Delete deletes at caret when enabled.
- Enter submits Fixed.

Blocked:
- Space
- Numbers
- Punctuation
- Symbols

Mobile / Tablet Keyboard
------------------------
Native mobile/tablet keyboard is suppressed.
Children use the custom keyboard.
External keyboard still works when attached.

Important manager settings:
- Use Unity InputField Caret = true
- Prevent Mobile And Tablet Keyboard = true
- Block Native InputField Typing = true
- Keep Word Input Focused = true
- Force Visible InputField Caret = true

Fonts
-----
Assign on SpellBotRescueManager:
- Primary Font: headings, buttons, counters.
- Secondary Font: word text, hint text, keyboard/body text.
- Font Apply Root: SafeAreaRoot.

Icons / Stars
-------------
No emoji glyphs are used.
Stars are plain Images.
Assign Star Sprite on SpellBotRescueManager.
Other icons are default UI placeholders so you can replace them.

Overdrive Robot
---------------
No UI glow is needed.
Use one optional Overdrive Sprite on SpellBotRobotView.

Recommended setup:
- Idle Sprite = normal robot, optional if image already has normal sprite.
- Overdrive Sprite = overdrive robot.
- Use Emotion Sprites = false unless you want separate happy/sad sprites.
- Use Overdrive Sprite = true.
- Never Allow Zero Scale = true.

Behavior:
- Normal = idle robot.
- 3-streak = overdrive sprite.
- Correct while overdrive = happy animation then returns to overdrive sprite.
- Wrong or Show Answer = exits overdrive, returns to normal sprite.

Robot Scale Fix
---------------
SpellBotRobotView caches/restores the base scale.
It prevents the robot from becoming invisible due to scale 0.
If your old scene already has scale 0, set the robot transform scale manually to 1,1,1 once.
Then use the component context menu:
SpellBot/Refresh Base Transform From Current

Background Music
----------------
For current scene without regeneration:
1. Create empty GameObject: SpellBotBGM
2. Add SpellBotBgmPlayer
3. Assign Background Music clip
4. Recommended settings:
   - Target Volume = 0.35 to 0.5
   - Loop = true
   - Play On Start = true
   - Use Fade In = true
   - Fade In Duration = 0.8
   - Fade Out Duration = 0.35
   - Keep Playing Across Scene Loads = false

For new generated scenes:
- Editor creates SpellBotBGM automatically.
- Assign clip and enable Play On Start if needed.

Result Continue / Bloom Post
----------------------------
Continue button calls SpellBotRescueManager.ContinueFromResult().
This opens Bloom post-game.
If Stop Audio When Bloom Post Opens is enabled, BGM fades out automatically through SpellBotBgmPlayer.Instance.

Responsive Notes
----------------
The generated layout uses:
- Canvas Scaler: Scale With Screen Size, 1920x1080.
- SafeAreaRoot with SpellBotSafeAreaFitter.
- Anchored top/middle/keyboard zones.
- Flexible layout groups for top bar and keyboard.

Still manually polish for final mobile/tablet art if your design needs exact custom positions.

Fast Test Checklist
-------------------
1. Make sure RewardManager exists from LoadingScene.
2. Open game scene and press Play.
3. Bloom pre-game appears first.
4. Home page appears after Bloom completes.
5. Start opens How-To-Play.
6. HTP Start Game starts gameplay.
7. Tap between letters, type, backspace, arrow keys.
8. Hint shows hint text.
9. Show Answer shows correct spelling only and score becomes 0 for that round.
10. Finish 10 rounds.
11. Result Continue opens Bloom post-game.
12. Play Again / Home callbacks work from Bloom.
