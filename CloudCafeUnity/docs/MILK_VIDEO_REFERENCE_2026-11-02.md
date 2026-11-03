# Milk preparation and latte pouring video observations

User asked to watch real videos before adding the next action. Research only; no gameplay code changed in this turn. Videos were played and relevant frames revisited; this is not a claim that every minute was watched or audio was directly heard.

## Breville: Pouring the Dot (57 seconds)
https://www.youtube.com/watch?v=wAoeVUdA5CA

- Around 0:12: one hand cradles the tilted espresso cup; the other holds the milk pitcher. The cup is brought toward the pitcher, not left flat on a counter.
- 0:26: a narrower stream enters and mixes with the brown surface while the spout is relatively higher.
- 0:31: the spout is very close to the liquid and a white region appears/spreads on top.
- Around 0:40: the cup is more upright and the white dot remains on the brown surface.
- Important distinction for implementation: stream height changes the visible result; a white drawing must not begin at the same rate for every height and tilt. White shape is deposited progressively rather than revealed as a finished decal.

## Golden Brown Coffee: Milk Steaming Tutorial 2025 (7:14)
https://www.youtube.com/watch?v=PwhKWLCy5HQ

Relevant parts played/searched/frame inspected, not full uninterrupted viewing:
- 4:40: angled steam wand; pitcher is brought underneath.
- Around 5:20–5:33: one hand holds the pitcher handle, the other supports/touches its side; wand enters off-center and milk is visibly agitated. Need surface/foam movement independent of the metal shell.
- 6:18–6:20: pitcher on counter, held by handle; tap and swirl preparation is shown and named in captions. Swirl is a small circular movement, not a full rotation of the handle around the body.
- Later pouring segment: cup is held up by one hand while the other draws a flowing pattern with the pitcher.

Manufacturer cross-check for tap/swirl terminology and sequencing:
https://www.breville.com/en-us/blog/coffee-and-espresso/how-to-steam-milk

## Proposed game sequence (design inference, not implemented)

1. Lift the same espresso cup from the machine and move to a closer pouring view.
2. Milk in separate pitcher → steam wand position → froth and mix → stop steam.
3. Tap then swirl pitcher; surface becomes smoother.
4. Left hand supports tilted cup; hand cursor manipulates right-hand pitcher. Supporting-hand pose is scene animation, not a second independently controlled cursor.
5. High/thin initial pour mixes the coffee color. Near-surface pour spreads white foam.
6. Cup gradually straightens as it fills; pitcher height/tilt/position govern flow and pattern.
7. Return pitcher upright to stop the stream, keep last drops, set finished cup down.

Start with a simple white dot and clear flowing/mixing feedback. Heart/rosetta patterns follow after the basic motion is usable. Exact input mappings and simulation thresholds still need implementation decisions; no claim about measured real-world angles, sound, or fluid parameters.
