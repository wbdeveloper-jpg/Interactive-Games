ODD SUCK FIRST-TIME INTERACTIVE TUTORIAL
========================================

FILES
-----
Runtime/OddSuckManager.cs
    Replace the existing OddSuckManager script with this updated version.

Runtime/OddSuckFirstTimeTutorialController.cs
    New mode-aware tutorial controller.

Editor/OddSuckFirstTimeTutorialInstaller.cs
    New additive toolbar installer.

Editor/OddSuckSceneBuilder.cs
    Existing builder with the new How To Play dropdown default applied.


INSTALLATION
------------
1. Copy the OddSuckMechanic folder into Assets, preserving:

   OddSuckMechanic/Scripts/Runtime
   OddSuckMechanic/Scripts/Editor

2. Let Unity compile.

3. Open the gameplay scene containing OddSuckManager.

4. Run:

   Tools > Odd Suck > Install or Upgrade First-Time Tutorial

The installer adds only OddSuckFirstTimeTutorialRoot and tutorial-owned children.
It does not rebuild the scene or change existing UI layout, colours, positions,
How To Play objects, templates, or gameplay art. Running it again upgrades the
same root and does not create duplicates.


INSPECTOR SETUP
---------------
Select OddSuckFirstTimeTutorialRoot.

1. Hand Pointer
   Assign your hand sprite to TutorialHandPointer > Image.
   The installer intentionally leaves this Image empty.

2. Mode
   AutoFromGameMode:
     SpriteOnly -> image practice
     MathOnly / EnglishOnly -> text practice
     MixedRandom -> Mixed Mode Fallback selection

   TextBased and ImageBased can force a scene-specific tutorial format.

3. Guided Practice
   Enter the scene-specific question.
   Add at least two options.
   For text practice, fill Option Text.
   For image practice, assign an Image on every option.
   Mark exactly one option Is Correct.

4. Independent Practice
   Enter a second question and options. If fewer than two options are supplied,
   the guided content is reused.

5. Templates
   The installer copies the manager's left, centre, right, and image template
   references automatically. Verify them if the scene uses custom wiring.

6. Layout and messages
   Practice positions, catch-zone width, hand offset, instructions, feedback,
   animation timings, ghost alpha, UFO practice speed, and final hold time are
   editable on the tutorial controller.

   The full-screen dim overlay is disabled by default so the game art remains
   clear. Enable Use Dim Overlay only if a particular scene needs it.


HOW TO PLAY PANEL
-----------------
OddSuckManager now contains How To Display Mode:

FirstTimeAutomatically
EveryGameStartAutomatically
ManualButtonOnly

The existing How To Play button continues to call ShowHowToFromPause. An
additional external/manual button may call ShowHowToManually.

How To Play and interactive-tutorial completion use separate PlayerPrefs keys.
Both keys include the active scene name.

When both are automatic, the sequence is:

How To Play -> close -> interactive tutorial -> untouched real game


TESTING
-------
In Play Mode, use the OddSuckManager component context menu:

Reset How To Viewed Status
Reset First-Time Tutorial Progress
Force Play First-Time Tutorial

The tutorial controller also provides reset and force-play context commands.

Confirm the following:

- Guided practice retries safely after a wrong or early tap.
- Independent practice completes only after the correct real tap.
- Score, health, timer, speed, wave count, attempts, rewards, and results do not
  change during practice.
- The real first wave begins afterward with full starting values.
- Exiting before completion causes the tutorial to appear again.
- Finishing it prevents automatic replay unless reset or forced.


DEPENDENCIES
------------
The implementation uses the packages already referenced by the project:

- DOTween
- TextMesh Pro
- Unity UI

No new third-party package is added.
