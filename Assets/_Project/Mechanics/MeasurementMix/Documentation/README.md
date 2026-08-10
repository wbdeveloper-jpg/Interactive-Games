# Measurement Mix — Setup Guide

## Game pattern

One reusable five-round manager supports four question types:

1. Practical Mass — drag weights onto a traditional balance.
2. Practical Liquid — add or remove water from a graduated beaker.
3. Mass Conversion — choose an equivalent mass.
4. Liquid Conversion — choose an equivalent volume.

Every replay produces a fresh mixed order and fresh generated values.

## Build the rough UI

Open the desired Unity scene and run:

`Tools > Measurement Game > Build Rough UI In Current Scene`

The command:

- creates a 1920 x 1080 reference-resolution landscape Canvas;
- uses TextMeshPro;
- creates editable placeholder Images;
- builds gameplay, Hint, Pause, How-to-Play and Result UI;
- connects component references;
- does not create or save a scene.

If `MeasurementGameRoot` already exists, the tool asks before replacing only
that root. Other scene objects are not modified.

## Main Inspector

Select:

`MeasurementGameRoot > Managers > MeasurementGameSettings`

### Game

- `Difficulty`: Easy, Normal or Hard.
- `Questions Per Run`: maximum 5.
- `Mass Question Chance`: balance between mass and liquid domains.
- `Show How To Play At Start`: optional opening instructions.

### Scoring and timing

- Points per correct answer.
- Maximum remaining-time bonus.
- Hint penalty.
- Correct feedback duration.
- Timeout feedback duration.
- Panel fade duration.

Default transition times deliberately leave enough time for children to read
feedback before the next question.

## Difficulty profiles

Every profile independently controls:

- seconds available per question;
- conversion-question frequency;
- allowed weight denominations;
- generated mass target range;
- number of weights in a hidden solution;
- beaker capacity;
- water added or removed per tap;
- generated liquid target range;
- allowed mass units;
- allowed liquid units;
- conversion question style;
- three or four answer options;
- source-number range;
- decimal values and decimal step.
- optional decimal display for practical questions.

### Recommended defaults

| Profile | Practical units | Conversion frequency | Decimals |
| --- | --- | ---: | --- |
| Easy | g/kg and mL/L | 20% | Off |
| Normal | g/kg and mL/L | 40% | Off |
| Hard | mg/g/kg and mL/cL/L | 55% | On |

These are starting points. Unit flags are editable, so the same code can be
used for different class standards. Optional later-level units include
milligrams, tonnes, centilitres and decilitres.

## Advanced conversion questions

Two conversion styles are supported:

### Convert To Named Unit

Example format:

`Convert 80 kg to g.`

All answer options use the requested target unit.

### Choose Equivalent Measurement

Example format:

`Which measurement is equal to 80 kg?`

Options may use different allowed units.

Set `Conversion Style` to `Mixed` to use both formats.

Internally, unit values are converted from a shared base value. Distractors use
controlled place-value errors, and the correct option is shuffled.

Hard mode can generate values such as:

- `1.5 kg`
- `1.5 L`
- `2.5 cL`

Hard practical targets also use compact decimal display where appropriate, for
example `1.5 kg` or `1.5 L`, while the internal answer remains exact.

Adjust `Decimal Step` if smaller intervals are required.

## Practical mass generation

The generator selects an actual subset of available weight tokens and stores
that combination as the hidden solution. Therefore:

- every target is solvable;
- duplicate denominations are respected;
- Hint can highlight the exact required number of weights.

The rough UI contains two of each:

- 25 g
- 50 g
- 100 g
- 200 g
- 500 g
- 1000 g

Only denominations allowed by the active profile are shown.

## Practical liquid generation

All practical liquid values are stored as integer millilitres. Displayed
questions automatically use clear mixed units:

- `750 mL`
- `1 L`
- `1 L 250 mL`
- `1 L 500 mL`

Targets always align with the configured tap step, stay inside the container
capacity, and match the graduated scale. The default Normal and Hard beaker
capacity is 2 L.

The default generated scale includes minor graduations for every available
50 mL step in the 2 L profiles and every 100 mL step in Easy.

## Hint behaviour

The red liquid target line is hidden at the start of every round.

After a wrong answer:

- the Hint button pulses;
- the player can continue without using it;
- Hint applies its score penalty only once per round.

Using Hint:

- Practical Mass: pulses the exact generated weight combination.
- Practical Liquid: reveals the target line.
- Conversion: pulses the correct equivalent option.

Liquid hint duration and whether the line stays visible are Inspector options.

## Premium feedback and optimisation

DOTween is used for:

- scale and water movement;
- water-stream feedback;
- weight and conversion hints;
- Hint encouragement;
- option selection;
- wrong-answer shake;
- panel fades and feedback;
- Pause and Result panels.

The runtime avoids physics, LINQ and repeated allocation inside frame updates.
Components and shared working lists are cached for smoother mobile performance.

## Pause, result and callbacks

The rough UI includes Pause, Resume, Restart, Home, Result, Replay and
How-to-Play.

Assign `On Home Requested` in `MeasurementGameManager` to connect the game to
your loader/native mediator. Assign `On Game Completed` for reward or Bloom
integration.

## Custom artwork

Replace sprites or add artwork under the named placeholder objects. Keep the
gameplay components and references on their current objects.

Recommended source sizes are listed in:

`Assets/MeasurementMix/Art/Placeholders/PLACEHOLDER_SIZES.md`

## Dependencies

- Unity UI
- TextMeshPro
- DOTween
- EventSystem

Both legacy and new Input System UI modules are detected by the Editor builder.
