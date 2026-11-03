# Direct milk crafting prototype

Approved scope: user asked to start the previously discussed Unity milk-pouring prototype. Reuse own cup/pitcher split sprites; no Coffee Talk assets or decompiled code. Keep existing main game intact while validating a dedicated playable crafting scene.

- [x] Add independent volume/flow model and tests: tilt threshold, empty pitcher, delayed landing, misses/overflow, conservation, reset, time-step independence. C# executable: 1,517 assertions pass (includes repeated conservation checks).
- [x] Add pixel renderer: own shell back/front, masked separate liquids, forward pitch deformation without sideways rotation, continuous spout-attached stream, droplets and cup surface response.
- [x] Add input: move pitcher, drag down to pitch forward, release to straighten, keyboard alternative, reset and finish. OS-driven mouse-drag validation remains pending; Computer Use app approval timed out.
- [x] Build native Windows scene and run scripted native validation; capture upright/pouring/stopped/missed frames. Build and native smoke markers confirmed, native collected ~128ml, spill/reset verified.
- [x] Inspect captures, document controls and current visual limitations, deliver executable and preview. Corrected transparent pitcher cavity and cup fill depiction after image inspection. Packaged a 29-frame GIF and Windows zip.

This is an initial playable implementation for review, not a finalized art/physics system. Forward pose uses a 2.5D depth projection of own split artwork; it is not a newly authored set of correct 3D silhouettes. Full latte-art scoring and the other coffee preparation stages follow after this interaction is reviewed.
