# Emberfields hills and hollows

Emberfields now has a walkable west rise, a central hollow, an east ridge, and a shallow dip before the exit. The player and ground enemies follow the same surface. Ground loot and Froststep use the local height. The painted road bends with the surface. Ladders lead from the ground to the higher branch route and the possible chest locations.

1. Exit Play Mode and merge this ZIP's Assets folder into the Unity project.
2. When Unity finishes compiling, choose **AshenVale → Map layouts → Apply Emberfields hills**.
3. Walk from Emberhaven toward Amberleaf Trail, then try jumping from a slope, climbing from the hollow, and Froststepping uphill and downhill.

The installer accepts the original Emberfields layout or the previous Emberfields Route Trial layout. It checks the source fragments before writing and saves the pre-install scripts in EmberfieldsHillsBackups. If testing in co-op, apply the same update to each copy of the project.

To return to the prior version, close Unity and restore the four backed-up scripts to their matching paths. Remove the three added runtime scripts and their .meta files from Assets/Scripts: Data/RollingGround.cs, View/RollingGroundArt.cs, and View/RollingGroundMeshLifetime.cs. Reopen Unity. The Console lists the backup location after installation.

The diagram shows the hills and ladder entrances. Local checks covered C# syntax, the source patch, continuous ground, ladder endpoints, and branch jumps at all three class speeds. The backdrop bend and gameplay need a Unity test.
