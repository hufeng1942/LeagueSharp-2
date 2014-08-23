﻿#region

using System;
using System.Drawing;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

#endregion

namespace Marksman
{
    internal class Jinx : Champion
    {
        public Spell E;
        public Spell Q;
        public float QAddRange;
        public Spell R;
        public Spell W;

        public Jinx()
        {
            Utils.PrintMessage("Jinx by Pingo loaded.");

            Q = new Spell(SpellSlot.Q, float.MaxValue);

            W = new Spell(SpellSlot.W, 1500);
            W.SetSkillshot(0.6f, 60f, 2000f, true, Prediction.SkillshotType.SkillshotLine);

            E = new Spell(SpellSlot.E, 900);
            E.SetSkillshot(0.7f, 120f, 1750f, false, Prediction.SkillshotType.SkillshotCircle);

            R = new Spell(SpellSlot.R, 25000);
            R.SetSkillshot(0.6f, 140f, 1700f, false, Prediction.SkillshotType.SkillshotLine);
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            Spell[] spellList = { W };
            var drawQbound = GetValue<Circle>("DrawQBound");

            foreach (var spell in spellList)
            {
                var menuItem = GetValue<Circle>("Draw" + spell.Slot);
                if (menuItem.Active)
                {
                    Utility.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
                }
            }

            if (drawQbound.Active)
            {
                if (HasFishBones())
                {
                    Utility.DrawCircle(
                        ObjectManager.Player.Position, 525f + ObjectManager.Player.BoundingRadius + 65f,
                        drawQbound.Color);
                }
                else
                {
                    Utility.DrawCircle(
                        ObjectManager.Player.Position,
                        525f + ObjectManager.Player.BoundingRadius + 65f + QAddRange + 20f, drawQbound.Color);
                }
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            QAddRange = 50 + 25 * ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level;

            var autoEi = GetValue<bool>("AutoEI");
            var autoEs = GetValue<bool>("AutoES");
            var autoEd = GetValue<bool>("AutoED");

            if (autoEs || autoEi || autoEd)
            {
                foreach (
                    var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(E.Range - 150)))
                {
                    if (autoEs && E.IsReady() && enemy.HasBuffOfType(BuffType.Slow))
                    {
                        var CastPosition =
                            Prediction.GetBestPosition(
                                enemy, 0.7f, 120f, 1750f, ObjectManager.Player.ServerPosition, 900f, false,
                                Prediction.SkillshotType.SkillshotCircle, ObjectManager.Player.ServerPosition)
                                .CastPosition;
                        var SlowEndTime = GetSlowEndTime(enemy);

                        if (SlowEndTime >= (Game.Time + E.Delay + 0.5f))
                        {
                            E.Cast(CastPosition);
                        }
                    }
                    if (autoEi && E.IsReady() &&
                        (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                         enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                         enemy.HasBuffOfType(BuffType.Taunt)))
                    {
                        E.CastIfHitchanceEquals(enemy, Prediction.HitChance.HighHitchance);
                    }
                    if (autoEd && E.IsReady() && enemy.IsDashing())
                    {
                        E.CastIfHitchanceEquals(enemy, Prediction.HitChance.Dashing);
                    }
                }
            }

            var castR = GetValue<KeyBind>("CastR").Active;

            if (castR && R.IsReady())
            {
                var target = SimpleTs.GetTarget(1500, SimpleTs.DamageType.Physical);

                if (target.IsValidTarget())
                {
                    if (DamageLib.getDmg(target, DamageLib.SpellType.R, DamageLib.StageType.FirstDamage) > target.Health)
                    {
                        R.Cast(target, false, true);
                    }
                }
            }

            var swapQ = GetValue<bool>("SwapQ");

            if (swapQ && HasFishBones() &&
                (LaneClearActive ||
                 (HarassActive && SimpleTs.GetTarget(675f + QAddRange, SimpleTs.DamageType.Physical) == null)))
            {
                Q.Cast();
            }

            if ((!ComboActive && !HarassActive) || !Orbwalking.CanMove(100))
            {
                return;
            }

            var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
            var useW = GetValue<bool>("UseW" + (ComboActive ? "C" : "H"));
            var useR = GetValue<bool>("UseRC");

            if (useW && W.IsReady())
            {
                var t = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Physical);
                var minW = GetValue<Slider>("MinWRange").Value;

                if (t.IsValidTarget() && GetRealDistance(t) >= minW)
                {
                    if (W.Cast(t) == Spell.CastStates.SuccessfullyCasted)
                    {
                        return;
                    }
                }
            }

            if (useQ)
            {
                foreach (
                    var t in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(t => t.IsValidTarget(GetRealPowPowRange(t) + QAddRange + 20f)))
                {
                    var swapDistance = GetValue<bool>("SwapDistance");
                    var swapAOE = GetValue<bool>("SwapAOE");
                    var Distance = GetRealDistance(t);
                    var PowPowRange = GetRealPowPowRange(t);

                    if (swapDistance && Q.IsReady())
                    {
                        if (Distance > PowPowRange && !HasFishBones())
                        {
                            if (Q.Cast())
                            {
                                return;
                            }
                        }
                        else if (Distance < PowPowRange && HasFishBones())
                        {
                            if (Q.Cast())
                            {
                                return;
                            }
                        }
                    }

                    if (swapAOE && Q.IsReady())
                    {
                        float PowPowStacks = GetPowPowStacks();

                        if (Distance > PowPowRange && PowPowStacks > 2 && !HasFishBones() && CountEnemies(t, 150) > 1)
                        {
                            if (Q.Cast())
                            {
                                return;
                            }
                        }
                    }
                }
            }

            if (useR && R.IsReady())
            {
                var checkROK = GetValue<bool>("ROverKill");
                var minR = GetValue<Slider>("MinRRange").Value;
                var maxR = GetValue<Slider>("MaxRRange").Value;
                var t = SimpleTs.GetTarget(maxR, SimpleTs.DamageType.Physical);

                if (t.IsValidTarget())
                {
                    var Distance = GetRealDistance(t);

                    if (!checkROK)
                    {
                        if (DamageLib.getDmg(t, DamageLib.SpellType.R, DamageLib.StageType.FirstDamage) > t.Health)
                        {
                            if (R.Cast(t, false, true) == Spell.CastStates.SuccessfullyCasted)
                            {
                                return;
                            }
                        }
                    }
                    else if (checkROK && Distance > minR)
                    {
                        var ADamage = DamageLib.getDmg(t, DamageLib.SpellType.AD);
                        var WDamage = DamageLib.getDmg(t, DamageLib.SpellType.W, DamageLib.StageType.FirstDamage);
                        var RDamage = DamageLib.getDmg(t, DamageLib.SpellType.R, DamageLib.StageType.FirstDamage);
                        var PowPowRange = GetRealPowPowRange(t);

                        if (Distance < (PowPowRange + QAddRange) && !(ADamage * 3.5 > t.Health))
                        {
                            if (!W.IsReady() || !(WDamage > t.Health) || W.GetPrediction(t).CollisionUnitsList.Count > 0)
                            {
                                if (CountAlliesNearTarget(t, 500) <= 3)
                                {
                                    if (RDamage > t.Health && !ObjectManager.Player.IsAutoAttacking &&
                                        !ObjectManager.Player.IsChanneling)
                                    {
                                        if (R.Cast(t, false, true) == Spell.CastStates.SuccessfullyCasted)
                                        {
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                        else if (Distance > (PowPowRange + QAddRange))
                        {
                            if (!W.IsReady() || !(WDamage > t.Health) || Distance > W.Range ||
                                W.GetPrediction(t).CollisionUnitsList.Count > 0)
                            {
                                if (CountAlliesNearTarget(t, 500) <= 3)
                                {
                                    if (RDamage > t.Health && !ObjectManager.Player.IsAutoAttacking &&
                                        !ObjectManager.Player.IsChanneling)
                                    {
                                        if (R.Cast(t, false, true) == Spell.CastStates.SuccessfullyCasted)
                                        {
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void Orbwalking_AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            if ((ComboActive || HarassActive) && unit.IsMe && (target is Obj_AI_Hero))
            {
                var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
                var useW = GetValue<bool>("UseW" + (ComboActive ? "C" : "H"));

                if (useW && W.IsReady())
                {
                    var t = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Physical);
                    var minW = GetValue<Slider>("MinWRange").Value;

                    if (t.IsValidTarget() && GetRealDistance(t) >= minW)
                    {
                        if (W.Cast(t) == Spell.CastStates.SuccessfullyCasted)
                        {
                            return;
                        }
                    }
                }

                if (useQ)
                {
                    foreach (
                        var t in
                            ObjectManager.Get<Obj_AI_Hero>()
                                .Where(t => t.IsValidTarget(GetRealPowPowRange(t) + QAddRange + 20f)))
                    {
                        var swapDistance = GetValue<bool>("SwapDistance");
                        var swapAOE = GetValue<bool>("SwapAOE");
                        var Distance = GetRealDistance(t);
                        var PowPowRange = GetRealPowPowRange(t);

                        if (swapDistance && Q.IsReady())
                        {
                            if (Distance > PowPowRange && !HasFishBones())
                            {
                                if (Q.Cast())
                                {
                                    return;
                                }
                            }
                            else if (Distance < PowPowRange && HasFishBones())
                            {
                                if (Q.Cast())
                                {
                                    return;
                                }
                            }
                        }

                        if (swapAOE && Q.IsReady())
                        {
                            float PowPowStacks = GetPowPowStacks();

                            if (Distance > PowPowRange && PowPowStacks > 2 && !HasFishBones() &&
                                CountEnemies(t, 150) > 1)
                            {
                                if (Q.Cast())
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool HasFishBones()
        {
            return ObjectManager.Player.AttackRange != 525f;
        }

        private int CountEnemies(Obj_AI_Hero target, float range)
        {
            var n = 0;

            foreach (
                var hero in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(
                            hero =>
                                hero.IsValidTarget() && hero.Team != ObjectManager.Player.Team &&
                                hero.ServerPosition.Distance(target.ServerPosition) <= range))
            {
                n++;
            }

            return n;
        }

        private int CountAlliesNearTarget(Obj_AI_Hero target, float range)
        {
            var n = 0;

            foreach (
                var hero in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(
                            hero =>
                                hero.Team == ObjectManager.Player.Team &&
                                hero.ServerPosition.Distance(target.ServerPosition) <= range))
            {
                n++;
            }

            return n;
        }

        private int GetPowPowStacks()
        {
            var n = 0;

            foreach (var buff in ObjectManager.Player.Buffs.Where(buff => buff.DisplayName.ToLower() == "jinxqramp"))
            {
                n = buff.Count;
            }

            return n;
        }

        private float GetRealPowPowRange(Obj_AI_Hero target)
        {
            return 525f + ObjectManager.Player.BoundingRadius + target.BoundingRadius;
        }

        private float GetRealDistance(Obj_AI_Hero target)
        {
            return ObjectManager.Player.Position.Distance(target.Position) + ObjectManager.Player.BoundingRadius +
                   target.BoundingRadius;
        }

        private float GetSlowEndTime(Obj_AI_Hero target)
        {
            var EndTime = 0f;

            foreach (var buff in target.Buffs.Where(buff => buff.Type == BuffType.Slow))
            {
                EndTime = buff.EndTime;
            }

            return EndTime;
        }

        public override void ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQC" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseWC" + Id, "Use W").SetValue(true));
            config.AddItem(new MenuItem("UseRC" + Id, "Use R").SetValue(true));
        }

        public override void HarassMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQH" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseWH" + Id, "Use W").SetValue(false));
        }

        public override void LaneClearMenu(Menu config)
        {
            config.AddItem(new MenuItem("SwapQ" + Id, "Always swap to Minigun").SetValue(false));
        }

        public override void MiscMenu(Menu config)
        {
            config.AddItem(new MenuItem("SwapDistance" + Id, "Swap Q for distance").SetValue(true));
            config.AddItem(new MenuItem("SwapAOE" + Id, "Swap Q for AOE").SetValue(false));
            config.AddItem(new MenuItem("MinWRange" + Id, "Min W range").SetValue(new Slider(700, 0, 1200)));
            config.AddItem(new MenuItem("AutoEI" + Id, "Auto-E on immobile").SetValue(true));
            config.AddItem(new MenuItem("AutoES" + Id, "Auto-E on slowed").SetValue(true));
            config.AddItem(new MenuItem("AutoED" + Id, "Auto-E on dashing").SetValue(true));
            config.AddItem(
                new MenuItem("CastR" + Id, "Cast R (2000 Range)").SetValue(
                    new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            config.AddItem(new MenuItem("ROverKill" + Id, "Check R Overkill").SetValue(true));
            config.AddItem(new MenuItem("MinRRange" + Id, "Min R range").SetValue(new Slider(300, 0, 1500)));
            config.AddItem(new MenuItem("MaxRRange" + Id, "Max R range").SetValue(new Slider(1700, 0, 4000)));
        }

        public override void DrawingMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("DrawQBound" + Id, "Draw Q bound").SetValue(
                    new Circle(true, Color.FromArgb(100, 255, 0, 0))));
            config.AddItem(
                new MenuItem("DrawW" + Id, "W range").SetValue(new Circle(false, Color.FromArgb(100, 255, 255, 255))));
        }
    }
}