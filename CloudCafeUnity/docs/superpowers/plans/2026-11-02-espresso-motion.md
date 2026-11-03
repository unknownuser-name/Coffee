# Espresso motion continuation

User approved continuing with temporary pixel graphics and replacing art later.
Goal: existing grind and tamp → lift the same basket → insert underneath group head → turn handle to lock → set cup → start pump → liquid fills the cup.
Architecture: keep FrontGrind controller/model compatible, add isolated BrewModel and machine/cup artwork. Camera pans to adjacent station after tamping. Build to FrontBrew/Build so the user's currently open FrontGrind executable remains intact.

- [x] Test insertion/lock/cup gates, pump stop and last liquid, conservation, reset with a standalone model.
- [x] Add temporary machine, real cup cavity, separate coffee surface and twin spouts/streams.
- [x] Extend direct manipulation with insertion path, handle turn and cup placement. Pause/reset/focus loss stop pumping.
- [x] Build FrontBrew; run old model tests and full scripted camera smoke through extraction; inspect frames and package.

Proposal values: 40 degree locking arc, 36 g shot, 1.2 s start delay and accelerated extraction. These are prototype controls, not claims about real equipment calibration. No final asset, milk integration or narrative flow in this increment. Native smoke is scripted controller input, not OS mouse verification.

Checks: BrewTests initially failed for missing source, then 293 assertions passed. Existing 818 grind and 9 prep assertions passed. Initial full native sequence passed; reviewed inserted/flow camera frames and widened twin spouts so the handle does not occlude streams. Independent source review found camera pause omitted and handle drag surviving a paused mouse release. TogglePause now pauses both tweens and cancels handle drag; reviewer confirmed fixes. Added native camera-only pause/drag cancellation assertion; final rebuild and smoke run required before delivery.

Final evidence: final Unity build succeeded; final Windows smoke exit 0 and Review/result.json passed including espresso. Package camera blank-image checks and ZIP integrity checks passed. New FrontBrew window confirmed in actual Windows inventory, ID 3869468. Old FrontGrind window 528292 preserved. OS mouse completion is still unverified.
