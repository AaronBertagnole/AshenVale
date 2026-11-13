namespace AshenVale
{
    // Every hero has three abilities: Basic, Skill and Movement. This is the one place that says what an
    // ability is called and what its numbers are for a given hero and its ranks. The sim asks here instead
    // of reading HeroDef directly, so ranks (and later mutations) change the game from this table alone.
    //
    // A hero starts with the basic attack only (rank 1). The Skill and the Movement ability start locked
    // (rank 0): the first point put into one unlocks it, once the hero has reached its UnlockLevel. Every
    // level from 2 earns one point, and every rank costs one, so all three abilities reach MaxRank at
    // level 30 (29 points). Rank 1 always plays with the plain HeroDef / Tuning numbers.
    public static class Abilities
    {
        public const int Basic = 0, Skill = 1, Movement = 2, Count = 3;
        public const int MaxRank = 10;
        public const int PickRank = 5;      // the rank at which an ability offers its mutation choice (not built yet)
        public static readonly int[] UnlockLevel = { 1, 2, 5 };     // hero level needed before the first point can go in

        // Per rank above the first (see the functions below for where each applies).
        public const float BasicDamagePerRank = .04f, ReachPerRank = .02f;
        public const float SkillDamagePerRank = .05f, SkillRechargePerRank = .1f;
        public const float MoveCooldownPerRank = .04f, LeapDamagePerRank = .05f, FissurePerRank = .03f, ImmunityPerRank = .015f;

        public static int StartRank(int slot) { return slot == Basic ? 1 : 0; }
        public static int Rank(PlayerState p, int slot) { return p.ch.abilityRanks[slot]; }
        public static bool Unlocked(Character ch, int slot) { return ch.abilityRanks[slot] >= 1; }
        public static bool Unlocked(PlayerState p, int slot) { return Unlocked(p.ch, slot); }
        static int Steps(PlayerState p, int slot) { return System.Math.Max(0, p.ch.abilityRanks[slot] - 1); }

        // Can a point go into this ability right now?
        public static bool CanRankUp(Character ch, int slot)
        {
            return slot >= 0 && slot < Count && ch.UnspentPoints > 0 && ch.abilityRanks[slot] < MaxRank && ch.level >= UnlockLevel[slot];
        }

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

        // What a rank adds, for the ability window.
        public static string RankText(Character ch, int slot)
        {
            var hero = ch.Def;
            if (slot == Basic) return "Each rank: +4% damage" + (hero.style == AttackStyle.Melee ? ", +2% reach" : "");
            if (slot == Skill)
                return "Each rank: +5% damage, 0.1s faster cooldown" + (hero.style == AttackStyle.Melee ? ", +2% reach" : "") + "; every 2nd rank: -1 MP"
                     + (hero.style == AttackStyle.Arrows ? "; ranks 5 and 10: +1 arrow" : hero.style == AttackStyle.Magic ? "; ranks 5 and 10: +1 strike" : "");
            if (RimecallerRules.Is(hero)) return "Each rank: blinks further, 0.04s faster cooldown; every 3rd rank: -1 MP";
            if (hero.dragonKnight) return "Each rank: leaps further, +5% damage, +3% fissure reach, 0.04s faster cooldown";
            return "Each rank: dashes further, 0.04s faster cooldown, a little longer untouchable";
        }

        // ---- Effective values (what the sim uses) ----

        // Damage of a basic hit before the skill / leap bonus: hero, level and gear.
        public static int BaseDamage(PlayerState p) { return p.Hero.baseDamage + p.ch.level * Tuning.DamagePerLevel + p.ch.BonusAttack; }
        static int Scaled(int value, float perRank, int steps) { return UnityEngine.Mathf.RoundToInt(value * (1f + perRank * steps)); }

        public static int BasicDamage(PlayerState p) { return Scaled(BaseDamage(p), BasicDamagePerRank, Steps(p, Basic)); }
        public static float BasicCooldown(PlayerState p) { return p.Hero.attackCooldown * (1f - p.ch.AttackHaste / 100f); }     // gear haste (boss weapons)
        public static float BasicRange(PlayerState p) { return p.Hero.meleeRange * (1f + ReachPerRank * Steps(p, Basic)); }

        public static int SkillDamage(PlayerState p) { return Scaled(BaseDamage(p) + p.Hero.skillBonus, SkillDamagePerRank, Steps(p, Skill)); }
        public static int SkillCost(PlayerState p) { return System.Math.Max(1, p.Hero.skillManaCost - Steps(p, Skill) / 2); }
        public static float SkillSwing(PlayerState p) { return p.Hero.skillCooldown; }       // recovery after the cast
        public static float SkillRecharge(PlayerState p) { return p.Hero.skillRecharge - SkillRechargePerRank * Steps(p, Skill); }    // the skill's own cooldown
        public static float SkillRange(PlayerState p) { return p.Hero.skillRange * (1f + ReachPerRank * Steps(p, Skill)); }
        static int Milestones(PlayerState p, int slot) { int r = p.ch.abilityRanks[slot]; return (r >= 5 ? 1 : 0) + (r >= 10 ? 1 : 0); }
        public static int SkillArrows(PlayerState p) { return p.Hero.arrowsPerSkill + Milestones(p, Skill); }
        public static int SkillPierce(PlayerState p) { return p.Hero.skillPierce; }
        public static int SkillStrikes(PlayerState p) { return 3 + Milestones(p, Skill); }    // Stormcall

        public static int MoveCost(PlayerState p)
        {
            return RimecallerRules.Is(p.Hero) ? System.Math.Max(1, RimecallerRules.BlinkCost - Steps(p, Movement) / 3) : p.Hero.dragonKnight ? p.Hero.leapManaCost : 0;
        }
        public static float MoveCooldown(PlayerState p)
        {
            float cooldown = RimecallerRules.Is(p.Hero) ? RimecallerRules.BlinkCooldown : p.Hero.dragonKnight ? p.Hero.leapCooldown : Tuning.DashCooldown;
            return UnityEngine.Mathf.Max(.2f, cooldown - MoveCooldownPerRank * Steps(p, Movement));
        }
        // How far the movement ability carries: short at rank 1, the old distance around rank 6, further at rank 10.
        public const float MoveReachAtRank1 = .65f, MoveReachAtMax = 1.25f;
        public static float MoveReach(PlayerState p)
        {
            return UnityEngine.Mathf.Lerp(MoveReachAtRank1, MoveReachAtMax, Steps(p, Movement) / (float)(MaxRank - 1));
        }
        public static float BlinkDistance(PlayerState p) { return RimecallerRules.BlinkDistance * MoveReach(p); }  // sideways Froststep
        public static float LeapSpeed(PlayerState p) { return p.Hero.leapSpeed * MoveReach(p); }                  // same arc, more ground covered
        public static float DashSpeed(PlayerState p) { return Tuning.DashSpeed * MoveReach(p); }                  // same time, more ground covered
        public static float DashImmunity(PlayerState p) { return Tuning.DashImmunity + ImmunityPerRank * Steps(p, Movement); }
        public static int LeapDamage(PlayerState p) { return Scaled(BaseDamage(p) + p.Hero.leapBonus, LeapDamagePerRank, Steps(p, Movement)); }
        public static float FissureRange(PlayerState p) { return p.Hero.fissureRange * (1f + FissurePerRank * Steps(p, Movement)); }

        // One line of numbers for the ability window.
        public static string Summary(PlayerState p, int slot)
        {
            if (slot == Basic) return "Damage " + BasicDamage(p) + "   ·   " + BasicCooldown(p).ToString("0.00") + "s per attack";
            if (slot == Skill)
            {
                string text = "Damage " + SkillDamage(p) + "   ·   " + SkillCost(p) + " MP   ·   " + SkillRecharge(p).ToString("0.0") + "s cooldown";
                if (p.Hero.style == AttackStyle.Arrows) text += "   ·   " + SkillArrows(p) + " arrows";
                if (p.Hero.style == AttackStyle.Magic) text += "   ·   " + SkillStrikes(p) + " strikes";
                return text;
            }
            string move = UnityEngine.Mathf.RoundToInt(MoveReach(p) * 100f) + "% distance   ·   " + MoveCooldown(p).ToString("0.0") + "s cooldown";
            if (MoveCost(p) > 0) move = MoveCost(p) + " MP   ·   " + move;
            if (p.Hero.dragonKnight) move = "Damage " + LeapDamage(p) + "   ·   " + move;
            return move;
        }
    }
}
