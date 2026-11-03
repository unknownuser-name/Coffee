# Machine docking assistance

User: can reach machine but basket does not catch. Existing InsertAtMachine silently required Prep.Complete and a .4-radius XZ circle; manual placement and preparation status were not conveyed.

Fix: shared DockSnapModel proximity dx ±.5, dz -.75..+.32 relative to group. Prepared basket is drawn toward entrance during shared viewport drag; release runs existing approach/insert sequence to exact GroupSeat, then manual turn lock. Visible ring and ready/missing-step text explain the action. Unprepared drop restores original slot and preserves specific missing-step message after the animation. No bypass of tamping.

Verification: new missing-source red then DockSnapTests 7 pass. Full native smoke tests unprepared explanation and prepared approach from offset(.38,-.1,-.59) through actual DragHeld viewport math, then checks exact final GroupSeat and full extraction. Build success and native exit0/result passed; inspected dock approach and seated camera frames. Independent source reviewer found no important issues. OS mouse interaction remains unverified.

Isolated output CoffeeCraftSnap/Build. StartCoffeeCraft.cmd targets latest. Existing builds preserved.
