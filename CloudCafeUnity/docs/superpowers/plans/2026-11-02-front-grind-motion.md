# Front grind motion prototype

Goal: playable lift → approach → seat → grind → withdraw → set down, viewed from the operator side.
Spec: GrindStation/FRONT_VIEW_REVISION.md; user authorized implementing steps 1–4.
Architecture: separate Unity scene and executable, shared GrindModel, low resolution orthographic 3D rendering with point scaling. DOTween sequences provide lift/seat/withdraw contact poses. Temporary mesh art establishes motion, not final matching sprite art.

- [x] Create mesh workbench, bowl with real cavity, separate grounds, front cradle and hands.
- [x] Implement ray-plane mouse reach, automatic lift, controlled seat/withdraw, physical switch and contact audio.
- [x] Build isolated executable; scripted smoke covers switch gate, seating, final grains, mass conservation, complete return, invalid drop and interrupted motion.
- [x] Inspect actual camera captures and deliver preview plus executable.

Keep MilkCraft and prior GrindStation executable intact. No new external dependencies. Native capture must use the same camera render as play. Smoke is scripted controller input, not proof of OS mouse operation. Fixed camera must show cavity and cradle contact without camera movement.

Verification: model tests 818 passed; native result passed; first hidden-window captures were black, fixed by explicitly rendering the same play camera before readback, then visually inspected frames 000/001/009. Packaging rejects blank frames. Physical mouse check could not run: CUA getApp approval timed out after 300 seconds. Do not describe manual interaction as verified. Mesh art and contact sounds are temporary; no final matching sprite sheet or Blender model delivered.

User follow-up: continue with temporary pixel art, replace assets later.
- [x] Add separate distributor and tamper on rack; drag onto same filled basket.
- [x] Distributor circle gesture → lower surface; tamp downward gesture → compress and reveal on release.
- [x] PuckPrepModel red/green: missing source then 9 assertions passed. Native smoke now also checks distributor seating/rotation, tamper seating/press/release. Inspected camera frames 043 and 058.
Scope still ends at tamping; group-head insertion and extraction remain next.
