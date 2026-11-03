# Single hand cursor

User wants a hand-shaped pointer that closes on the object when grabbed, visibly showing the grasp location.

Implementation: PixelHandCursor draws authored 24×28 open/hover/grip/press pixel shapes on a camera overlay quad; included overlay shader keeps it visible over the grasped object. Free/hover uses fingertip hotspot at pointer; grip uses palm hotspot anchored to actual basket handle, prep head or cup handle. Existing mesh-hand renderers are disabled; GameObject state remains for camera/animation flow. Focus loss/dispose restores OS cursor; outside workbench OS cursor is visible. No separate floating mesh hand.

Checks: HandCursorModel missing-source red then5 assertions pass. Full native workflow exit0/result passed, including grip+exact projected handle anchor after pickup, grip during leveling, press during tamping. Inspected camera frames000/001/058 with visible cursor. Initial build missed DOTween extension namespace; fixed before successful build. Native direct actions now set rotating/pressing just as manual event start does so capture and gameplay use identical pose states. Independent review found no important issues. Actual OS mouse full play remains unverified.

Output CoffeeCraftCursor/Build; StartCoffeeCraft.cmd targets it. Art remains temporary pixel-hand sprites; original builds retained.
