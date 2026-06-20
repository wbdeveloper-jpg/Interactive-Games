# Grid Adventure - v16 Gameplay Polish Patch

This version is based on the stable v15 responsive layout fix.

## Important

The responsive layout core was not changed in this patch:

- `GridAdventureMainScreenLayout`
- `GridAdventureCenterSquareLayout`
- `GridAdventureCoordinateGridLayout`
- `GridAdventureResponsiveGrid`

## Changed in v16

### 1. Better drop animation

Correct drop now uses the dragged clone position as the snap start point.

Old behaviour looked like the item was coming from its basket slot after drop.
New behaviour:

- drag clone follows pointer
- on correct drop, real card appears at clone/drop position
- snaps smoothly into the active cell
- small scale/rotation polish after snap

### 2. Better wrong return

Wrong drop now animates the drag clone back to the original basket card.
The original card fades back only after the clone returns.

### 3. Cell coordinate labels removed

The internal coordinate data still exists for matching, but visible labels like `A1`, `B2`, etc. are hidden by default.

For existing scenes, labels are hidden automatically when cells initialize.

### 4. Image-only mode fixed

`GridAdventureItemCard` now applies `ImageOnly` mode to the template and runtime cards.

If you set `ItemCardTemplate > Display Mode = ImageOnly`, the label object is disabled.

Manager now has:

```text
Prefer Item Template Display Mode
```

Keep it enabled if you want the template to decide whether cards are image-only or image+label.

### 5. Pause panel How To Play button

Generated pause overlay now has:

```text
Resume
How To Play
Restart
```

When opened from pause, the How To Play panel closes back to the pause state. It does not restart or affect the starting tutorial flow.

## Existing Scene Notes

You do not need to touch the stable responsive layout.

For your current scene:

1. Replace scripts from this package.
2. Existing coordinate labels will hide automatically on play.
3. For image-only cards, set:

```text
GridAdventureManager > Prefer Item Template Display Mode = true
ItemCardTemplate > Display Mode = ImageOnly
```

4. Add a new button under `PauseOverlayRoot/MainCard` named `How To Play Button` and assign it to:

```text
GridAdventureManager > Pause Overlay > Pause How To Play Button
```

The button calls `GridAdventureManager.OpenHowToPlayFromPause()`.

For a fresh scene, run:

```text
Tools > Grid Adventure > Create Rough Working UI
```
