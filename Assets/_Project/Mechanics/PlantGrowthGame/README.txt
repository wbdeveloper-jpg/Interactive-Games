PLANT GROWTH GAME — UNITY 2D UI PACKAGE
========================================

Purpose
-------
This package adds the complete plant-growth game UI to your CURRENT OPEN SCENE.
It does not create a scene, replace a scene, load a scene, or save a scene.

Recommended Unity version
-------------------------
Unity 2021.3 LTS or newer with Unity UI (uGUI) available.
DOTween must already be installed in the project. The package uses your existing
DOTween installation and does not include or configure DOTween.

Installation
------------
1. Copy the included Assets/PlantGrowthGame folder into your Unity project's
   Assets folder.
2. Wait for Unity to finish compiling and importing the PNG files.
3. Open the scene where you want the game UI.
4. Run:
   Tools > Plant Growth Game > Install UI Into Current Scene
5. Unity adds one self-contained object named PlantGrowthGame_UI.
6. Review the selected controller in the Inspector and save your scene manually.

What the installer creates
--------------------------
- A Screen Space Overlay Canvas with 1920 x 1080 responsive scaling.
- Seven full-screen stage sprites.
- Responsive invisible hit zones over Play, Harvest, Pause, Sound and the three
  visible answer cards.
- Gentle cross-fades and child-friendly correct/wrong feedback.
- DOTween-powered stage fades, feedback pop-ins and pause-panel transitions.
- Pause, Resume, Restart and Exit UI.
- EventSystem only when the scene does not already contain one.
- Two optional AudioSources.

Game flow
---------
Welcome > Water > Warmth > Sunlight > Pollination > Ripening > Harvest

Inspector options
-----------------
- Transition Duration
- Correct Feedback Duration
- Wrong Feedback Duration
- Pause Scene Time
- Background Music and sound-effect clips
- On Game Started callback
- On Game Completed callback
- On Exit Requested callback

Important integration notes
---------------------------
- The installer never saves the scene automatically.
- Existing objects and existing Canvases are left unchanged.
- If your project has its own navigation or Android mediator, connect it to
  On Exit Requested in the Inspector.
- Connect your reward/result flow to On Game Completed.
- Correct answers intentionally move between left, centre and right so children
  cannot succeed by repeatedly tapping one location.
- If you replace or reorder stage artwork, update Correct Option Indexes on the
  controller: 0 = left, 1 = centre, 2 = right.
- To change Canvas mode or sorting, select PlantGrowthGame_UI and edit its
  Canvas component after installation.

Folder structure
----------------
Assets/PlantGrowthGame/
  Art/
    Stage_00_Welcome.png
    Stage_01_Water.png
    Stage_02_Warmth.png
    Stage_03_Sunlight.png
    Stage_04_Pollination.png
    Stage_05_Ripening.png
    Stage_06_Harvest.png
  Runtime/
    PlantGrowthGameController.cs
  Editor/
    PlantGrowthGameInstaller.cs
  README.txt
