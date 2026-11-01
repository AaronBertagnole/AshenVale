namespace AshenVale
{
    public enum AttackStyle { Melee, Arrows }

    // One playable hero. Add a hero by adding an entry to Heroes.All and a sprite folder
    // under Assets/Resources/Heroes/<folder>/<animation>/00.png, 01.png ...
    public sealed class HeroDef
    {
        public string name, folder, weaponName, roleLabel, tagline;
        public AttackStyle style;
        public int baseHp, baseMp;
        public float moveSpeed;
        public int baseDamage;          // basic attack damage at level 0 (plus DamagePerLevel per level and gear)
        public int skillBonus;          // extra damage of the skill (K)
        public int skillManaCost;
        public float attackCooldown, skillCooldown;
        public float meleeRange, skillRange;        // Melee only
        public int arrowsPerSkill = 1, skillPierce = 1; // Arrows only
        // Animation folder names
        public string basicAnimA, basicAnimB, skillAnim; // basic attacks alternate A / B
    }

    public static class Heroes
    {
        public static readonly HeroDef[] All =
        {
            new HeroDef
            {
                name = "Dragon Knight", folder = "DragonKnight", weaponName = "Spear",
                roleLabel = "SPEAR / MELEE", tagline = "Heavy strikes  •  Durable",
                style = AttackStyle.Melee, baseHp = 125, baseMp = 80, moveSpeed = 5.3f,
                baseDamage = 25, skillBonus = 32, skillManaCost = 25,
                attackCooldown = .25f, skillCooldown = .72f,
                meleeRange = 1.4f, skillRange = 2.15f,     // Dragon Strike hits everything in range, 20% less per target
                basicAnimA = "attack_thrust", basicAnimB = "attack_sweep", skillAnim = "dragon_strike",
            },
            new HeroDef
            {
                name = "Elven Ranger", folder = "ElvenRanger", weaponName = "Bow",
                roleLabel = "BOW / RANGED", tagline = "Agile attacks  •  Long range",
                style = AttackStyle.Arrows, baseHp = 95, baseMp = 100, moveSpeed = 6.1f,
                baseDamage = 17, skillBonus = 24, skillManaCost = 30,
                attackCooldown = .3f, skillCooldown = .9f,
                arrowsPerSkill = 3, skillPierce = 2,       // Multi Shot: each monster takes one arrow per volley
                basicAnimA = "quick_shot", basicAnimB = "quick_shot", skillAnim = "multi_shot",
            },
        };
    }
}
