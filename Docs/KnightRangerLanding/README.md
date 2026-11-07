# AshenVale Knight and Ranger landings

Merge the Assets folder into the project with Rimecaller landing feedback installed. Exit Play Mode and choose **AshenVale → Hero polish → Install Knight and Ranger landings** after Unity imports the package.

- **Dragon Knight** uses a 0.14-second touchdown and recovery. He drops deeper into the landing, with a fuller leaf puff and deeper footfall on long falls.
- **Elven Ranger** uses a 0.085-second touchdown and recovery. He straightens up faster, with a smaller leaf puff and lighter footfall on long falls.

In Emberfields, falls of at least 2.8 world units trigger the leaves and local footfall, using the same sound clip as the Rimecaller with class-specific pitch and volume. The Knight's combat leap has its own landing. The ordinary landing poses use the refreshed Knight and Ranger artwork. In classic art mode, the long-fall sound and leaves still play.

Try a short hop, running jump, and long drop with each hero. Also try the Knight's combat leap and the Ranger's ladder exit. The installer checks the two target scripts before writing and puts their pre-install copies in ClassLandingBackups. The Console reports the location.

Landing_Preview.png compares the new poses with current idle sprites. The two pose atlases were made with the built-in image generator using AshenVale's current sprites as design references. The prompts asked for a deeply bent Knight with his spear upright and a lighter Ranger with his bow undrawn, followed by a planted-foot recovery for each. Each set uses one uniform export scale and the idle boot baseline.

Local checks covered C# syntax, source matching, sprite alignment and import settings. Unity compilation and gameplay still need verification.
