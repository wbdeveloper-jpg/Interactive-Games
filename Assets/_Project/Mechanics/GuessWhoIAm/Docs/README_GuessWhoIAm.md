# Guess Who I Am - Unity Mini-Game Package

## Pattern
Progressive clue-based quiz. Player guesses the correct answer from 3 or 4 options. Revealing more clues lowers answer value.

## Reusability
Yes. Same code supports people, animals, places, products, school topics, festivals, history, science, etc. Replace the ScriptableObject database and reskin the generated UI.

## Current Layout Update
This version follows the provided landscape reference closely:
- thin top HUD with question progress line and step markers,
- coin/help/pause actions on the right,
- score block removed from the visible top bar to keep HUD cleaner,
- 3 near-square clue cards in one horizontal row,
- selected clue card is bright white,
- revealed but non-selected clue cards are dull white,
- locked clue cards stay dim purple,
- 4 dark answer buttons in a 2x2 grid with capped height,
- fixed-width right guide panel,
- mascot area contains a blank white placeholder image only,
- feedback uses the mascot speech bubble, not a bottom feedback bar,
- reveal button and next button sit under the speech bubble.

## Fast Architecture
- `GuessWhoQuestionData.cs` - one question entry.
- `GuessWhoQuestionDatabase.cs` - ScriptableObject database.
- `GuessWhoIAmOptionGameManager.cs` - gameplay, Bloom flow, loading, image-based how-to, and result flow.
- `GuessWhoIAmAudioManager.cs` - mechanic-specific SFX and background music.
- `GuessWhoIAmResponsiveLayout.cs` - ExecuteAlways responsive UI sizing.
- `GuessWhoIAmUIStyler.cs` - primary/secondary TMP font assignment.
- `GuessWhoIAmSceneBuilder.cs` - editor menu that builds and wires the responsive UI.

## Setup
1. Import this `Assets/GuessWhoIAm` folder into your Unity project.
2. DOTween, TextMeshPro, and your existing Bloom Reward System should already be installed.
3. Open a landscape scene.
4. Click: `Tools > Guess Who I Am > Create Mockup Matched Responsive Game UI`.
5. Press Play.

The editor tool will:
- delete previous generated Canvas/Manager to avoid duplicates,
- create a responsive Canvas,
- create EventSystem if missing,
- create the thin HUD bar,
- create 3 clue cards in one row,
- create 4 answer buttons in a 2x2 grid,
- create the right mascot guide panel,
- create blank white placeholder images for mascot/icons,
- create loading, image-based how-to-play, pause, and result panels,
- create Continue button in result panel,
- create reveal and next buttons,
- create/update the demo question database with 50 easy class 3-4 questions,
- create and wire the game manager,
- create a rounded-rectangle UI sprite under `Assets/GuessWhoIAm/Generated` for cleaner mockup panels.

## Startup Flow
Runtime order:
1. Bloom pre-game panel opens using `RewardManager.Instance.ShowPreGame(...)`.
2. Gameplay waits until `RewardManager.Instance.IsPreGameComplete`.
3. Local Loading Panel appears with a big game title, progress Slider, and `Loading...` text.
4. How To Play panel appears by default.
5. If guide sprites are assigned, the player uses Previous/Next and Start appears on the final slide. If no guide image is assigned, the panel shows backup text and a direct Continue button.
6. After Start/Continue, the actual quiz round starts.

Do not add `RewardManager` to this scene. It must already exist in the LoadingScene and persist with `DontDestroyOnLoad`.

## Result / Bloom Flow
At round complete:
- local Result Panel opens,
- player taps `Continue`,
- game builds normalized `GameEvaluationData`,
- Bloom post-game opens using `RewardManager.Instance.ShowPostGame(...)`.

The game manager implements:
- `IGameSceneCallbacks`
- `IGameAudioCallbacks`

Bloom Play Again reloads the current scene. Bloom Home loads `Loader Scene` by default. Change `Home Scene Name` in the manager Inspector if your loader scene name is different.

## Audio
On `GuessWhoIAmAudioManager`:
- assign Background Music Clip,
- tune Background Music Volume,
- assign default correct/wrong/reveal/click/result clips.

Background music starts when gameplay starts and stops when Bloom reward screen opens.

## Replacing Images
Generated placeholders are blank white `Image` objects. Replace their sprites directly.

For the How To Play guide, assign sprites in the `How To Guide Sprites` list on `GuessWhoIAm_GameManager`. The main guide image preserves aspect ratio. Leave the list empty to use the fallback text + Continue flow.

Common placeholder names:
- `MascotWhiteImagePlaceholder_ReplaceSpriteHere`
- `CoinIconWhitePlaceholder_ReplaceSpriteHere`
- `SpeechIconWhitePlaceholder_ReplaceSpriteHere`
- `RevealIconWhitePlaceholder_ReplaceSpriteHere`
- `ActionArrowWhitePlaceholder_ReplaceSpriteHere`
- `NextIconWhitePlaceholder_ReplaceSpriteHere`
- `PauseButtonWhiteIconPlaceholder_ReplaceSpriteHere`
- `HelpButtonWhiteIconPlaceholder_ReplaceSpriteHere`
- `HowToGuideMainImage_ReplaceSpriteHere`

Keep text out of the mascot image area.

## Editing Questions
Open:
`Assets/GuessWhoIAm/Data/GuessWhoIAmDemoDatabase.asset`

The generated demo database now contains 50 simple class 3-4 questions based on animals, fruits, vegetables, and everyday school/home objects. The clue pattern is intentionally progressive: clue 1 is broad, clue 2 narrows the choice, and clue 3 makes the answer clear. Manual wrong answers are set for every demo question.

Each question supports:
- question id,
- answer,
- clue 1, clue 2, clue 3,
- manual wrong options,
- optional correct/wrong/reveal audio.

## Gameplay Rules
- Clue 1 visible by default: correct answer gives +10.
- Reveal clue 2: correct answer gives +7.
- Reveal clue 3: correct answer gives +5.
- Wrong answer disables options, reveals all clues, highlights correct answer, and shows Next.
- Correct answer adds score, disables options, keeps only already revealed clues visible, and shows Next.
- Next button auto-advances with an integrated Slider fill.

## Inspector Fields To Tune
On `GuessWhoIAm_GameManager`:
- Question Database
- Round Question Count
- Option Count: 3 or 4
- Startup Flow settings
- Loading title/text settings
- How To Guide Sprites and fallback text
- Bloom skills and expected max time
- Points per clue
- Starting coins
- Auto-next seconds
- Guide messages
- UI colors
- Tween speed
- Audio manager

On `GuessWhoIAmResponsiveLayout`:
- Max Option Button Height
- Min Option Button Height
- Tablet option width cap
- Top bar height range
- Right panel width range

## Fonts
On the generated Canvas, use `GuessWhoIAmUIStyler`:
1. Assign Primary Font and Secondary Font.
2. Right-click component menu: `Collect Texts From Children`.
3. Right-click component menu: `Apply Fonts`.

Primary font is intended for clue text, option text, and major buttons. Secondary font is intended for badges, chips, progress, score/coin, and helper text.

## Responsive Targets
Designed for:
- 1920x1080
- 2340x1080
- 2160x1080
- 2048x1536
- 2560x1440

The responsive script runs in Edit Mode and Play Mode.

## Reskin Tips
Keep the code. Change the feel by replacing:
- mascot placeholder sprite,
- icon placeholder sprites,
- clue card colors,
- answer button colors,
- background art,
- fonts,
- question database.

Do not add a gameplay title, bottom feedback bar, or extra question image space if you want to keep the requested layout style.

## Script-Only Update: Read-Only Open Clues + Points Popup
This update does not require scene regeneration.

Open clue cards are now read-only:
- Clue 1 starts active and gives +10.
- Revealing Clue 2 makes Clue 2 the active scoring card and gives +7.
- Revealing Clue 3 makes Clue 3 the active scoring card and gives +5.
- Tapping an already-open clue does nothing, so players will not think they can return to +10.
- Tapping the next locked clue still reveals it.
- Tapping Clue 3 before Clue 2 still gives the existing shake/pulse hint.

Optional points popup setup:
1. In your Canvas, create a small TextMeshProUGUI object.
2. Text can be `+10` as a preview only.
3. Make it bold, bright, and disable Raycast Target.
4. Drag it into Project window to make a prefab.
5. Delete/disable the scene preview object.
6. Assign that prefab to `Points Popup Text Prefab` on `GuessWhoIAm_GameManager`.
7. Optional: create an empty RectTransform under Canvas named `PointsPopupParent` and assign it to `Points Popup Parent`.

If the popup prefab is not assigned, the game works normally with no popup.
