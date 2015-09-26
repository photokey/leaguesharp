﻿using System;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

namespace SharpShooter.Plugins
{
    public class Graves
    {
        private Spell Q, W, E, R;

        public Graves()
        {
            Q = new Spell(SpellSlot.Q, 850f) { DamageType = TargetSelector.DamageType.Physical, MinHitChance = HitChance.High };
            W = new Spell(SpellSlot.W, 850f);
            E = new Spell(SpellSlot.E, 425f);
            R = new Spell(SpellSlot.R, 1000f) { DamageType = TargetSelector.DamageType.Physical, MinHitChance = HitChance.High };

            Q.SetSkillshot(0.25f, 30f, 2000f, false, SkillshotType.SkillshotCone);
            W.SetSkillshot(0.25f, 250f, 1650f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.25f, 100f, 2100f, false, SkillshotType.SkillshotLine);

            MenuProvider.Champion.Combo.addUseQ();
            MenuProvider.Champion.Combo.addItem("Q Range", new Slider(425, 100, 850));
            MenuProvider.Champion.Combo.addUseW();
            MenuProvider.Champion.Combo.addUseR();

            MenuProvider.Champion.Harass.addUseQ();
            MenuProvider.Champion.Harass.addItem("Q Range", new Slider(425, 100, 850));
            MenuProvider.Champion.Harass.addIfMana(60);

            MenuProvider.Champion.Jungleclear.addUseQ();
            MenuProvider.Champion.Jungleclear.addIfMana(60);

            MenuProvider.Champion.Misc.addUseKillsteal();
            MenuProvider.Champion.Misc.addUseAntiGapcloser();

            MenuProvider.Champion.Drawings.addDrawQrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDrawWrange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawErange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawRrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDamageIndicator(GetComboDamage);

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;

            Console.WriteLine("Sharpshooter: Graves Loaded.");
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (!ObjectManager.Player.IsDead)
            {
                switch (MenuProvider.Orbwalker.ActiveMode)
                {
                    case Orbwalking.OrbwalkingMode.Combo:
                        {
                            if (MenuProvider.Champion.Combo.UseQ)
                                if (Q.isReadyPerfectly())
                                {
                                    var QTarget = TargetSelector.GetTarget(MenuProvider.Champion.Combo.getSliderValue("Q Range").Value, Q.DamageType);
                                    if (QTarget != null)
                                        Q.Cast(QTarget);
                                }

                            if (MenuProvider.Champion.Combo.UseW)
                                if (W.isReadyPerfectly())
                                    W.CastOnBestTarget();

                            if (MenuProvider.Champion.Combo.UseR)
                                if (R.isReadyPerfectly())
                                {
                                    var RKillableTarget = HeroManager.Enemies.FirstOrDefault(x => x.isKillableAndValidTarget(R.GetDamage(x), R.Range));
                                    if (RKillableTarget != null)
                                        R.Cast(RKillableTarget);
                                    R.CastIfWillHit(TargetSelector.GetTarget(R.Range, R.DamageType), 3);
                                }
                            break;
                        }
                    case Orbwalking.OrbwalkingMode.Mixed:
                        {
                            if (MenuProvider.Champion.Harass.UseQ)
                                if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                    if (Q.isReadyPerfectly())
                                    {
                                        var QTarget = TargetSelector.GetTarget(MenuProvider.Champion.Harass.getSliderValue("Q Range").Value, Q.DamageType);
                                        if (QTarget != null)
                                            Q.Cast(QTarget);
                                    }
                            break;
                        }
                    case Orbwalking.OrbwalkingMode.LaneClear:
                        {
                            //Jungleclear
                            if (MenuProvider.Champion.Jungleclear.UseQ)
                                if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Jungleclear.IfMana))
                                    if (Q.isReadyPerfectly())
                                    {
                                        var QTarget = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral).FirstOrDefault(x => x.IsValidTarget(Q.Range));
                                        if (QTarget != null)
                                            Q.Cast(QTarget);
                                    }
                            break;
                        }
                }

                if (MenuProvider.Champion.Misc.UseKillsteal)
                {
                    var QTarget = HeroManager.Enemies.OrderByDescending(x => Q.GetPrediction(x).Hitchance).FirstOrDefault(x => x.isKillableAndValidTarget(Q.GetDamage(x), Q.Range));
                    if (QTarget != null)
                        Q.Cast(QTarget);

                    var RTarget = HeroManager.Enemies.OrderByDescending(x => R.GetPrediction(x).Hitchance).FirstOrDefault(x => x.isKillableAndValidTarget(R.GetDamage(x), R.Range));
                    if (RTarget != null)
                        R.Cast(RTarget);
                }
            }
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (MenuProvider.Champion.Misc.UseAntiGapcloser)
                if (gapcloser.End.Distance(ObjectManager.Player.Position) <= 200)
                    if (gapcloser.Sender.IsValidTarget())
                        if (E.isReadyPerfectly())
                            E.Cast(ObjectManager.Player.Position.Extend(gapcloser.Sender.Position, -E.Range));
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (!ObjectManager.Player.IsDead)
            {
                if (MenuProvider.Champion.Drawings.DrawQrange.Active)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, MenuProvider.Champion.Drawings.DrawQrange.Color);

                if (MenuProvider.Champion.Drawings.DrawWrange.Active)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, MenuProvider.Champion.Drawings.DrawWrange.Color);

                if (MenuProvider.Champion.Drawings.DrawErange.Active)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, MenuProvider.Champion.Drawings.DrawErange.Color);

                if (MenuProvider.Champion.Drawings.DrawRrange.Active)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, MenuProvider.Champion.Drawings.DrawRrange.Color);
            }
        }

        private float GetComboDamage(Obj_AI_Base enemy)
        {
            return R.IsReady() ? R.GetDamage(enemy) : 0;
        }
    }
}