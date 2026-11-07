# AshenVale Rimecaller landing feedback

1. Exit Play Mode. Extract this ZIP and merge its Assets folder into your Unity project.
2. Wait for compilation and choose **AshenVale → Rimecaller → Install landing feedback**.
3. Repeat the Emberfields route as the Rimecaller. Try a hop in place, a running jump, and a drop from the upper platform to the ground.

The two-pose touchdown takes roughly one tenth of a second. It triggers when the rendered feet reach the platform and lets movement and attacks continue. A descent of at least 2.8 world units from the jump apex or ledge also kicks up six leaves in Emberfields and plays a soft footfall for the local player. That height is a first-pass tuning value. The refreshed Rimecaller artwork from Hero Polish is the reference for the sprite canvas, scale, and boot contact.

The installer applies narrow edits to WorldView.cs and copies the landing renderer next to it. If a checked source fragment differs, it reports the mismatch in the Console before editing scripts. It saves the pre-install WorldView.cs under RimecallerLandingBackups in your project root.

Landing_Preview.png compares the exported 128px poses with the current idle. The built-in image generator used the in-game sprite as reference. The pose direction was a right-facing, knees-bent touchdown followed by recovery, with the same navy coat, silver-blue hair, fur collar, and cyan crystal. The export uses one scale and a shared boot contact row. The 0.24-second footfall combines a soft thump with a short leaf rustle.

C# syntax, patch matching, sprite contact and sound file format were checked locally. Unity compilation and gameplay need a Play Mode test.
