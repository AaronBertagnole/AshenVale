namespace AshenVale
{
    // Every hero has three abilities: Basic, Skill and Movement. This is the one place that says what an
    // ability is called and what its numbers are for a given hero and its ranks. The sim asks here instead
    // of reading HeroDef directly, so ranks (and later mutations) change the game from this table alone.
    //
    // Phase 1 (foundation): every ability is rank 1 and the values below are exactly the HeroDef / Tuning
    // numbers, so the game plays as before. Rank bonuses are added here in phase 2.
    public static class Abilities
    {
        public const int Basic = 0, Skill = 1, Movement = 2, Count = 3;
        public const int MaxRank = 5;       // rank 1 is free; each rank above costs one point
        public const int PickRank = 3;      // the rank at which an ability offers its mutation choice (phase 3)

        static readonly string[][] names =
        {
            new[] { "Spear Combo", "Dragon Fang", "Wyrm Leap" },
            new[] { "Quick Shot", "Multi Shot", "Dash" },
            new[] { "Frozen Orb", "Stormcall", "Froststep" },
        };
        static readonly string[][] blurbs =
        {
            new[] { "Thrust, then sweep.", "Hits every monster in reach, a little less for each.", "Leap and land as a fissure." },
            new[] { "A quick arrow.", "A volley of arrows that pierce.", "A burst of speed; nothing can hurt you for a moment." },
            new[] { "A frost orb that seeks a monster.", "Lightning strikes ahead of you.", "Blink forward, or up and down between platforms." },
        };
        static readonly string[] slotLabels = { "BASIC", "SKILL", "MOVEMENT" };

        public static string Name(Character ch, int slot) { return ch.hero < names.Length ? names[ch.hero][slot] : slotLabels[slot]; }
        public static string Blurb(Character ch, int slot) { return ch.hero < blurbs.Length ? blurbs[ch.hero][slot] : ""; }
        public static string SlotLabel(int slot) { return slotLabels[slot]; }
        public static GameAction Key(int slot) { return slot == Basic ? GameAction.Attack : slot == Skill ? GameAction.Skill : GameAction.Dash; }

        // ---- Effective values (what the sim uses) ----

        // Damage of a basic hit before the skill / leap bonus: hero, level and gear.
        public static int BaseDamage(PlayerState p) { return p.Hero.baseDamage + p.ch.level * Tuning.DamagePerLevel + p.ch.BonusAttack; }
        public static int BasicDamage(PlayerState p) { return BaseDamage(p); }
        public static float BasicCooldown(PlayerState p) { return p.Hero.attackCooldown; }
        public static float BasicRange(PlayerState p) { return p.Hero.meleeRange; }

        public static int SkillDamage(PlayerState p) { return BaseDamage(p) + p.Hero.skillBonus; }
        public static int SkillCost(PlayerState p) { return p.Hero.skillManaCost; }
        public static float SkillSwing(PlayerState p) { return p.Hero.skillCooldown; }       // recovery after the cast
        public static float SkillRecharge(PlayerState p) { return p.Hero.skillRecharge; }    // the skill's own cooldown
        public static float SkillRange(PlayerState p) { return p.Hero.skillRange; }
        public static int SkillArrows(PlayerState p) { return p.Hero.arrowsPerSkill; }
        public static int SkillPierce(PlayerState p) { return p.Hero.skillPierce; }

        public static int MoveCost(PlayerState p)
        {
            return RimecallerRules.Is(p.Hero) ? RimecallerRules.BlinkCost : p.Hero.dragonKnight ? p.Hero.leapManaCost : 0;
        }
        public static float MoveCooldown(PlayerState p)
        {
            return RimecallerRules.Is(p.Hero) ? RimecallerRules.BlinkCooldown : p.Hero.dragonKnight ? p.Hero.leapCooldown : Tuning.DashCooldown;
        }
        public static float DashImmunity(PlayerState p) { return Tuning.DashImmunity; }
        public static int LeapDamage(PlayerState p) { return BaseDamage(p) + p.Hero.leapBonus; }
        public static float FissureRange(PlayerState p) { return p.Hero.fissureRange; }

        // One line of numbers for the ability window.
        public static string Summary(PlayerState p, int slot)
        {
            if (slot == Basic) return "Damage " + BasicDamage(p) + "   ·   " + BasicCooldown(p).ToString("0.00") + "s per attack";
            if (slot == Skill)
            {
                string text = "Damage " + SkillDamage(p) + "   ·   " + SkillCost(p) + " MP   ·   " + SkillRecharge(p).ToString("0.0") + "s cooldown";
                if (p.Hero.style == AttackStyle.Arrows) text += "   ·   " + SkillArrows(p) + " arrows";
                return text;
            }
            string move = MoveCooldown(p).ToString("0.0") + "s cooldown";
            if (MoveCost(p) > 0) move = MoveCost(p) + " MP   ·   " + move;
            if (p.Hero.dragonKnight) move = "Damage " + LeapDamage(p) + "   ·   " + move;
            return move;
        }
    }
}
