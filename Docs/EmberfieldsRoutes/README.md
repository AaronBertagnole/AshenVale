# Emberfields route trial

This first layout pass gives Emberfields a continuous ground path from Emberhaven to Amberleaf Trail and a second route through the branches. Climb to the west perch for a possible chest, join the branch route partway through via the middle ladder, or take the high detour above the east terrace. The east ladder connects the branch route back toward the exit.

## Try it in Unity

1. Exit Play Mode and extract the ZIP. Merge the included Assets folder into your project.
2. After Unity compiles the installer, choose **AshenVale → Map layouts → Apply Emberfields routes**.
3. Enter Play Mode. From the Emberhaven side, jump onto the first low ledge and work east through the branches. Then try entering from Amberleaf Trail and climbing back up the east side.
4. Use **AshenVale → Map layouts → Restore original Emberfields** outside Play Mode to compare.

The installer swaps the Emberfields text layout in MapDefs.cs after checking it against the expected layout. The two options can be applied repeatedly. Map size, portal positions and monster and chest counts match the checked-in map. Chest markers show possible locations under the existing chest rules. Use the same layout on each copy of the game during co-op tests.

Emberfields_Route_Comparison.png diagrams the two layouts. Static checks verified every spawn and ladder endpoint rests on a floor. A 60 Hz jump calculation based on the current movement rules found a route to all 11 ledges at each class's movement speed. Unity compilation, art alignment, and the feel of the traversal still need a Play Mode test.
