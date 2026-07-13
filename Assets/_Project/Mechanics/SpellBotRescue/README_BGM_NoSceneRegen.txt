Spell-Bot Rescue - Background Music Patch
========================================

This patch does NOT require scene regeneration.

Files added:
- Scripts/Runtime/SpellBotBgmPlayer.cs

Setup in your existing scene:

1. Import/replace the script.
2. In your existing Spell-Bot scene, create an empty GameObject:
   SpellBotBGM
3. Add component:
   SpellBotBgmPlayer
4. Assign your music clip to:
   Background Music
5. Recommended settings:
   Target Volume = 0.35 to 0.5
   Loop = true
   Play On Start = true
   Use Fade In = true
   Fade In Duration = 0.8
   Fade Out Duration = 0.35
   Keep Playing Across Scene Loads = false

Bloom post panel / Continue button:

If you want the game BGM to stop when the result Continue button opens Bloom post-game:

1. Select Result Panel > Continue Button.
2. In Button OnClick, add one extra event BEFORE or AFTER the existing manager Continue call.
3. Drag SpellBotBGM object into the slot.
4. Choose:
   SpellBotBgmPlayer > StopMusic()

This will fade out the music before Bloom reward audio/panel becomes the focus.

Optional pause panel:
- Pause button can call SpellBotBgmPlayer.PauseMusic()
- Resume button can call SpellBotBgmPlayer.ResumeMusic()

No UI layout changes needed.
No editor scene creator needed.
No scene regeneration needed.
