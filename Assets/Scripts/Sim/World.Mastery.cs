using System.Collections.Generic;
using UnityEngine;

namespace AshenVale
{
    // The mastery skill: each hero's fourth ability, on its own key, there from level Abilities.MasteryLevel (when
    // the other three are at full rank). Numbers live in Abilities.Mastery*.
    //   Dragon Knight  Dragonfire     a wave of flame along the ground ahead; burns everything in its path
    //   Ranger         Arrow Storm    arrows rain on the ground ahead for a moment
    //   Rimecaller     Glacial Nova   a burst of ice around the caster; ordinary monsters freeze solid
    public sealed partial class World
    {
        sealed class ArrowRain { public int owner, map, damage, remaining; public float wait, x, y; }
        readonly List<ArrowRain> arrowRains = new List<ArrowRain>();
        // A Dragonfire that has been cast and not yet landed (the wind-up before the flame).
        sealed class Breath { public int owner, map, facing, damage; public float wait, x, y; }
        readonly List<Breath> breaths = new List<Breath>();

        void CastMastery(PlayerState p, MapRuntime map)
        {
            if (!Abilities.MasteryUnlocked(p.ch) || p.masteryCooldown > 0f || p.attackCooldown > 0f || p.climbing || p.knockTime > 0f || p.dead) return;
            int cost = Abilities.MasteryCost(p);
            if (p.mp < cost) return;
            p.mp -= cost;
            p.masteryCooldown = Abilities.MasteryCooldown(p);
            p.lastWasSkill = true; p.comboStep++; p.attackSerial++; p.attackPending = false;     // plays the hero's skill swing
            p.attackCooldown = Abilities.MasterySwing; p.attackDuration = p.attackVisual = Abilities.MasterySwing;
            int damage = Abilities.MasteryDamage(p);
            int kind = Abilities.MasteryKind(p.Hero);
            events.Add(new SimEvent { kind = SimEventKind.Mastery, map = p.map, playerId = p.id, x = p.x, y = p.y, facing = p.facing, amount = kind });

            if (kind == Abilities.Dragonfire)
            {
                // The flame is fixed where it was cast; the damage lands with its brightest frame (see TickMastery).
                breaths.Add(new Breath { owner = p.id, map = p.map, facing = p.facing, damage = damage, wait = Abilities.DragonfireHitDelay, x = p.x, y = p.y });
            }
            else if (kind == Abilities.ArrowStorm)
            {
                float x = Mathf.Clamp(p.x + p.facing * Abilities.ArrowStormAhead, map.def.minX, map.def.maxX);
                arrowRains.Add(new ArrowRain { owner = p.id, map = p.map, damage = damage, remaining = Abilities.ArrowStormWaves,
                                               wait = Abilities.ArrowStormDelay, x = x, y = FloorAt(map.def, x, p.y) });
            }
            else
            {
                float r = Abilities.NovaRadius;
                foreach (var m in MonstersIn(map, p.x - r, p.x + r, p.y - 1.2f, p.y + 3.2f))
                {
                    if (!m.Def.boss) m.frozen = Abilities.NovaFreeze;        // set first: a monster that survives the hit stays frozen
                    HitMonster(map, m, damage, p);
                }
            }
        }

        // Monsters whose bodies reach into the box (a copy: hitting them may remove some from the map).
        static List<MonsterState> MonstersIn(MapRuntime map, float left, float right, float bottom, float top)
        {
            var found = new List<MonsterState>();
            foreach (var m in map.monsters)
            {
                var d = m.Def;
                if (m.x + d.width * .5f < left || m.x - d.width * .5f > right) continue;
                if (m.y + d.hover > top || m.y + d.hover + d.height < bottom) continue;
                found.Add(m);
            }
            return found;
        }

        void TickMastery(float dt)
        {
            for (int i = breaths.Count - 1; i >= 0; i--)
            {
                var b = breaths[i];
                b.wait -= dt;
                if (b.wait > 0f) continue;
                breaths.RemoveAt(i);
                var caster = GetPlayer(b.owner);
                if (caster == null || caster.map != b.map) continue;
                var map = maps[b.map];
                float far = b.x + b.facing * Abilities.DragonfireReach;
                var burned = MonstersIn(map, Mathf.Min(b.x, far), Mathf.Max(b.x, far), b.y - .3f, b.y + 2.4f);
                // `heavy` tells the view the flame caught something (hit pause, a harder kick).
                events.Add(new SimEvent { kind = SimEventKind.Mastery, map = b.map, playerId = b.owner, x = b.x, y = b.y, facing = b.facing,
                                          amount = Abilities.DragonfireHit, heavy = burned.Count > 0 });
                foreach (var m in burned) HitMonster(map, m, b.damage, caster);
            }

            for (int i = arrowRains.Count - 1; i >= 0; i--)
            {
                var rain = arrowRains[i];
                var p = GetPlayer(rain.owner);
                if (p == null || p.map != rain.map) { arrowRains.RemoveAt(i); continue; }     // the arrows are already in the air: dying does not stop them
                rain.wait -= dt;
                var map = maps[rain.map];
                while (rain.wait <= 0f && rain.remaining > 0)
                {
                    events.Add(new SimEvent { kind = SimEventKind.Mastery, map = rain.map, playerId = rain.owner, x = rain.x, y = rain.y, facing = 1, amount = Abilities.ArrowStormWave });
                    float half = Abilities.ArrowStormHalfWidth;
                    foreach (var m in MonstersIn(map, rain.x - half, rain.x + half, rain.y - .3f, rain.y + 3f)) HitMonster(map, m, rain.damage, p);
                    rain.remaining--; rain.wait += Abilities.ArrowStormEvery;
                }
                if (rain.remaining <= 0) arrowRains.RemoveAt(i);
            }
        }
    }
}
