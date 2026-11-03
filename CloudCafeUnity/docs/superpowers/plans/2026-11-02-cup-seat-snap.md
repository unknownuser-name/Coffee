# Assisted cup seating

User requests cup placement beneath espresso spouts to catch and fit like the basket/tool assistance.

Changed: shared forgiving placement predicate compares cup base with CupSeat in world and projected space; ReachCup draws nearby cup toward seat while keeping lift height, DropCup animates alignment then descent to exact CupSeat. Visible ring highlights the saucer while holding cup and text explains release. Added collider to the visible cup handle; hand cursor remains anchored there.

Verification: shared placement4 and cursor5 tests pass. Final Unity build succeeds. Extended full native smoke raycasts actual handle edge, verifies dropping at original shelf does not dock or enable pump, approaches cup seat from(.45,0,-.5) then verifies exact CupSeat after release, and finishes36g extraction. Native exit0/result passed. Inspected approach and seated camera frames. OS mouse full play is still unverified.

Output CoffeeCraftCup/Build; latest launcher updated after pass. Existing builds retained.
