# Hand following workbench view

User correction: moving right was blocked; view should move with the hand holding the tool.

Cause: Reach/ReachPrep max X was 1.7 until brewView, and brewView/camera pan waited for completed tamping. Screen capture showed preparation pending. Fixed by allowing full counter movement independently of prep, removing post-tamp automatic pan, and following held tool positions within a horizontal camera comfort band. Machine attachment still requires prepared grounds.

Implementation: HandCameraModel.Next bounds camera X to -0.25..2.15 with 3 units/s travel; stationary center band prevents constant camera motion. FrontBrewInteraction.LateUpdate follows basket, prep tools or cup, including hand-visible return motions, and stops while paused or after releasing. Mouse viewport rays use the current camera each frame so continued edge drag travels through the scene. Reach X always allows -1.8..4.3.

Verification: HandCameraTests missing-source red then 7 passed. Native smoke uses the same DragHeld(viewport) mapping as mouse Update, before prep completion; drags right then left, checks pause and reset, then runs full grind/tamp/insert/lock/cup/espresso. First frame-count-only test failed because hidden-window dt was 0.000067s; recorded in Review/fast-frame-test-diagnostic.txt. Changed the simulated input to 1/60s intervals without weakening the assertions. Final native exit 0/result passed; inspected actual right/left camera frames. Independent source review found no important issues. OS mouse play remains unverified.

Build isolated to CoffeeCraftHand/Build/CoffeeCraftHand.exe; StartCoffeeCraft.cmd now targets it. Packaging verifies camera frames and ZIP integrity. New Windows UI inventory confirmed CoffeeCraftHand window ID 4260750.
