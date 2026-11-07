namespace AshenVale {
 public static class RimecallerRules {
  public const int BlinkCost=8; public const float BlinkCooldown=1.2f, BlinkDistance=3.2f;
  public const float BlinkUpRange=3.6f, BlinkDownRange=6f;   // Up / Down + Froststep: nearest platform this far above / below (Down also reaches the ground)
  public static bool Is(HeroDef h) { return h != null && h.folder=="Rimecaller"; }
  public static HeroDef CreateHero() { return new HeroDef {
   name="Rimecaller",folder="Rimecaller",weaponName="Crystal",roleLabel="FROST / STORM",tagline="Frozen Orb  •  Stormcall",
   style=AttackStyle.Magic,baseHp=85,baseMp=135,moveSpeed=5.5f,baseDamage=22,skillBonus=5,skillManaCost=24,
   attackCooldown=.6f,skillCooldown=1f,skillRecharge=3.5f,basicWindup=.18f,skillWindup=.24f,
   basicAnimA="orb_cast",basicAnimB="orb_cast",skillAnim="storm_cast"
  }; }
 }
}
