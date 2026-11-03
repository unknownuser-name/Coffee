# Forgiving prep-tool placement

Screenshot identifies basket/leveler/tamper placement, rather than machine docking. Raised prep tool was tested by world XZ radius .3 even though the player aligns the projected tool with the lower basket opening. That could reject visible overlap.

Fix: PrepPlacementModel accepts ±.6 X/±.9 depth or visible proximity ±.095/±.145 viewport relative to basket mouth. Same predicate drives placement-ring feedback and DropPrep. Nearby drops animate to exact basket center. Expanded handle, leveler and tamper pickup volumes without changing visible art.

Validation: new standalone missing-source red then 4 assertions pass. Native new wide-leveler pickup failed against initially overlapping oversized tamper box; adjusted tamper center/height/depth while retaining wider X. Recorded initial failure in Review/overlapping-hitbox-diagnostic.txt. Final native smoke exit0/result passed, including actual Physics.Raycast edge points for both tools and ±.45X/-.45Z placements previously rejected. Full workflow still reaches espresso. Independent source review found no important issues. OS mouse interaction remains unverified.

Output CoffeeCraftEasy/Build, StartCoffeeCraft.cmd updated. Previous builds preserved.
