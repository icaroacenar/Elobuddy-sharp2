﻿using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SebbyLib;
using EloBuddy;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK;

namespace OneKeyToWin_AIO_Sebby
{
    class Caitlyn
    {
        private Menu Config = Program.Config;
        private LeagueSharp.Common.Spell E, Q, Qc, R, W;
        private float QMANA = 0, WMANA = 0, EMANA = 0, RMANA = 0;

        private float QCastTime = 0;

        public AIHeroClient Player { get { return ObjectManager.Player; } }
        public AIHeroClient LastW = ObjectManager.Player;

        private static string[] Spells =
        {
            "katarinar","drain","consume","absolutezero", "staticfield","reapthewhirlwind","jinxw","jinxr","shenstandunited","threshe","threshrpenta","threshq","meditate","caitlynpiltoverpeacemaker", "volibearqattack",
            "cassiopeiapetrifyinggaze","ezrealtrueshotbarrage","galioidolofdurand","luxmalicecannon", "missfortunebullettime","infiniteduress","alzaharnethergrasp","lucianq","velkozr","rocketgrabmissile"
        };

        public static bool getBushW()
        {
            return getCheckBoxItem(wMenu, "bushW");
        }

        public void LoadOKTW()
        {
            Q = new LeagueSharp.Common.Spell(SpellSlot.Q, 1250f);
            Qc = new LeagueSharp.Common.Spell(SpellSlot.Q, 1250f);
            W = new LeagueSharp.Common.Spell(SpellSlot.W, 800f);
            E = new LeagueSharp.Common.Spell(SpellSlot.E, 770f);
            R = new LeagueSharp.Common.Spell(SpellSlot.R, 3000f);


            Q.SetSkillshot(0.65f, 60f, 2200f, false, SkillshotType.SkillshotLine);
            Qc.SetSkillshot(0.65f, 60f, 2200f, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(1.5f, 20f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.30f, 70f, 2000f, true, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.7f, 200f, 1500f, false, SkillshotType.SkillshotCircle);

            LoadMenuOKTW();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            //SebbyLib.Orbwalking.BeforeAttack += BeforeAttack;
            //SebbyLib.Orbwalking.AfterAttack += afterAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }


        private void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (args.Slot == SpellSlot.W && getCheckBoxItem(wMenu, "overrideW"))
            {
                if (ObjectManager.Get<Obj_GeneralParticleEmitter>().Any(obj => obj.IsValid && obj.Position.LSDistance(args.EndPosition) < 300 && obj.Name.ToLower().Contains("yordleTrap_idle_green.troy".ToLower())))
                    args.Process = false;
            }
            if (args.Slot == SpellSlot.E && Player.Mana > RMANA + WMANA)
            {
                W.Cast(Player.Position.LSExtend(args.EndPosition, Player.LSDistance(args.EndPosition) + 50));
                LeagueSharp.Common.Utility.DelayAction.Add(10, () => E.Cast(args.EndPosition));
            }
        }

        public static Menu drawMenu, qMenu, wMenu, eMenu, rMenu, farmMenu;

        private void LoadMenuOKTW()
        {
            drawMenu = Config.AddSubMenu("Drawings");
            drawMenu.Add("noti", new CheckBox("Show notification & line", false));
            drawMenu.Add("qRange", new CheckBox("Q range", false));
            drawMenu.Add("wRange", new CheckBox("W range", false));
            drawMenu.Add("eRange", new CheckBox("E range", false));
            drawMenu.Add("rRange", new CheckBox("R range", false));
            drawMenu.Add("onlyRdy", new CheckBox("Draw only ready spells", true));

            qMenu = Config.AddSubMenu("Q Config");
            qMenu.Add("autoQ2", new CheckBox("Auto Q", true));
            qMenu.Add("autoQ", new CheckBox("Reduce Q use", true));
            qMenu.Add("Qaoe", new CheckBox("Q aoe", true));
            qMenu.Add("Qslow", new CheckBox("Q slow", true));

            wMenu = Config.AddSubMenu("W Config");
            wMenu.Add("overrideW", new CheckBox("Override Manual W?", true));
            wMenu.Add("autoW", new CheckBox("Auto W on hard CC", true));
            wMenu.Add("telE", new CheckBox("Auto W teleport", true));
            wMenu.Add("bushW", new CheckBox("Auto W bush after enemy enter", true));
            wMenu.Add("bushW2", new CheckBox("Auto W bush if full ammo", true));
            wMenu.Add("Wspell", new CheckBox("W on special spell detection", true));
            wMenu.AddSeparator();
            wMenu.AddGroupLabel("Gapclose : ");
            wMenu.Add("WmodeGC", new ComboBox("Gap Closer position mode", 0, "Dash end position", "My hero position"));
            foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(enemy => enemy.IsEnemy))
                wMenu.Add("WGCchampion" + enemy.ChampionName, new CheckBox("Gapclose : " + enemy.ChampionName, true));

            eMenu = Config.AddSubMenu("E Config");
            eMenu.Add("autoE", new CheckBox("Auto E", true));
            eMenu.Add("Ehitchance", new CheckBox("Auto E dash and immobile target", true));
            eMenu.Add("harrasEQ", new CheckBox("Harass E + Q", true));
            eMenu.Add("EQks", new CheckBox("Ks E + Q + AA", true));
            eMenu.Add("useE", new KeyBind("Dash E HotKeySmartcast", false, KeyBind.BindTypes.HoldActive, 'T'));
            eMenu.AddSeparator();
            eMenu.AddGroupLabel("Gapclose : ");
            eMenu.Add("EmodeGC", new ComboBox("Gap Closer position mode", 2, "Dash end position", "Cursor position", "Enemy position"));
            foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(enemy => enemy.IsEnemy))
                eMenu.Add("EGCchampion" + enemy.ChampionName, new CheckBox("Gapclose : " + enemy.ChampionName, true));

            rMenu = Config.AddSubMenu("R Config");
            rMenu.Add("autoR", new CheckBox("Auto R KS", true));
            rMenu.Add("Rcol", new Slider("R collision width [400]", 400, 1, 1000));
            rMenu.Add("Rrange", new Slider("R minimum range [1000]", 1000, 1, 15000));
            rMenu.Add("useR", new KeyBind("Semi-manual cast R key", false, KeyBind.BindTypes.HoldActive, 'T'));
            rMenu.Add("Rturrent", new CheckBox("Don't R under turret", true));

            farmMenu = Config.AddSubMenu("Farm");
            farmMenu.Add("farmQ", new CheckBox("Lane clear Q", true));
            farmMenu.Add("Mana", new Slider("LaneClear Mana", 80, 30, 100));
            farmMenu.Add("LCminions", new Slider("LaneClear minimum minions", 2, 0, 10));
        }

        public static bool getCheckBoxItem(Menu m, string item)
        {
            return m[item].Cast<CheckBox>().CurrentValue;
        }

        public static int getSliderItem(Menu m, string item)
        {
            return m[item].Cast<Slider>().CurrentValue;
        }

        public static bool getKeyBindItem(Menu m, string item)
        {
            return m[item].Cast<KeyBind>().CurrentValue;
        }

        public static int getBoxItem(Menu m, string item)
        {
            return m[item].Cast<ComboBox>().CurrentValue;
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && (args.SData.Name == "CaitlynPiltoverPeacemaker" || args.SData.Name == "CaitlynEntrapment"))
            {
                QCastTime = Game.Time;
            }

            if (!W.IsReady() || sender.IsMinion || !sender.IsEnemy || !getCheckBoxItem(wMenu, "Wspell") || !sender.IsValid<AIHeroClient>() || !sender.LSIsValidTarget(W.Range))
                return;

            var foundSpell = Spells.Find(x => args.SData.Name.ToLower() == x);
            if (foundSpell != null)
            {
                W.Cast(sender.Position);
            }
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Player.Mana > RMANA + WMANA)
            {
                var t = gapcloser.Sender;
                if (E.IsReady() && t.LSIsValidTarget(E.Range) && getCheckBoxItem(eMenu, "EGCchampion" + t.ChampionName))
                {
                    if (getBoxItem(eMenu, "EmodeGC") == 0)
                        E.Cast(gapcloser.End);
                    else if (getBoxItem(eMenu, "EmodeGC") == 1)
                        E.Cast(Game.CursorPos);
                    else
                        E.Cast(t.ServerPosition);
                }
                else if (W.IsReady() && t.LSIsValidTarget(W.Range) && getCheckBoxItem(wMenu, "WGCchampion" + t.ChampionName))
                {
                    if (getBoxItem(eMenu, "WmodeGC") == 0)
                        W.Cast(gapcloser.End);
                    else
                        W.Cast(Player.ServerPosition);
                }
            }
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.LSIsRecalling())
                return;

            if (getKeyBindItem(rMenu, "useR") && R.IsReady())
            {
                var t = TargetSelector.GetTarget(R.Range, DamageType.Physical);
                if (t.LSIsValidTarget())
                    R.CastOnUnit(t);
            }



            if (Program.LagFree(0))
            {
                SetMana();
                R.Range = (500 * R.Level) + 1500;
                //debug("" + ObjectManager.Player.AttackRange);
            }

            if (Program.LagFree(1) && E.IsReady() && !Player.Spellbook.IsAutoAttacking)
                LogicE();
            if (Program.LagFree(2) && W.IsReady() && !Player.Spellbook.IsAutoAttacking)
                LogicW();
            if (Program.LagFree(3) && Q.IsReady() && !Player.Spellbook.IsAutoAttacking && getCheckBoxItem(qMenu, "autoQ2"))
                LogicQ();
            if (Program.LagFree(4) && R.IsReady() && getCheckBoxItem(rMenu, "autoR") && !ObjectManager.Player.UnderTurret(true) && Game.Time - QCastTime > 1)
                LogicR();
            return;
        }

        private void LogicR()
        {
            bool cast = false;

            if (Player.UnderTurret(true) && getCheckBoxItem(rMenu, "Rturrent"))
                return;


            foreach (var target in Program.Enemies.Where(target => target.LSIsValidTarget(R.Range) && Player.LSDistance(target.Position) > getSliderItem(rMenu, "Rrange") && target.LSCountEnemiesInRange(getSliderItem(rMenu, "Rcol")) == 1 && target.CountAlliesInRange(500) == 0 && OktwCommon.ValidUlt(target)))
            {
                if (target.Health < R.GetDamage(target) * 0.6f)
                {
                    cast = true;
                    PredictionOutput output = R.GetPrediction(target);
                    Vector2 direction = output.CastPosition.LSTo2D() - Player.Position.LSTo2D();
                    direction.Normalize();
                    List<AIHeroClient> enemies = Program.Enemies.Where(x => x.LSIsValidTarget()).ToList();
                    foreach (var enemy in enemies)
                    {
                        if (enemy.BaseSkinName == target.BaseSkinName || !cast)
                            continue;
                        PredictionOutput prediction = R.GetPrediction(enemy);
                        Vector3 predictedPosition = prediction.CastPosition;
                        Vector3 v = output.CastPosition - Player.ServerPosition;
                        Vector3 w = predictedPosition - Player.ServerPosition;
                        double c1 = Vector3.Dot(w, v);
                        double c2 = Vector3.Dot(v, v);
                        double b = c1 / c2;
                        Vector3 pb = Player.ServerPosition + ((float)b * v);
                        float length = Vector3.Distance(predictedPosition, pb);
                        if (length < (getSliderItem(rMenu, "Rcol") + enemy.BoundingRadius) && Player.LSDistance(predictedPosition) < Player.LSDistance(target.ServerPosition))
                            cast = false;
                    }
                    if (cast)
                        R.CastOnUnit(target);
                }
            }
        }

        private void LogicW()
        {
            if (Player.Mana > RMANA + WMANA)
            {
                if (Program.Combo && Player.Spellbook.IsAutoAttacking)
                    return;
                if (getCheckBoxItem(wMenu, "autoW"))
                {
                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.LSIsValidTarget(W.Range) && !OktwCommon.CanMove(enemy) && !enemy.HasBuff("caitlynyordletrapinternal")))
                    {
                        if (Utils.TickCount - W.LastCastAttemptT > 1000)
                        {
                            W.Cast(enemy.Position, true);
                            LastW = enemy;
                        }
                        else if (LastW.NetworkId != enemy.NetworkId)
                        {
                            W.Cast(enemy.Position, true);
                            LastW = enemy;
                        }
                    }
                }

                if (getCheckBoxItem(wMenu, "telE"))
                {
                    var trapPos = OktwCommon.GetTrapPos(W.Range);
                    if (!trapPos.IsZero)
                        W.Cast(trapPos);
                }
                if ((int)(Game.Time * 10) % 2 == 0 && getCheckBoxItem(wMenu, "bushW2"))
                {
                    if (Player.Spellbook.GetSpell(SpellSlot.W).Ammo == new int[] { 0, 3, 3, 4, 4, 5 }[W.Level] && Player.LSCountEnemiesInRange(1000) == 0)
                    {
                        var points = OktwCommon.CirclePoints(8, W.Range, Player.Position);
                        foreach (var point in points)
                        {
                            if (NavMesh.IsWallOfGrass(point, 0) || point.UnderTurret(true))
                            {
                                W.Cast(point);
                                return;
                            }
                        }
                    }
                }
            }
        }

        private void LogicQ()
        {
            if (Program.Combo && Player.Spellbook.IsAutoAttacking)
                return;
            var t = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
            if (t.LSIsValidTarget(Q.Range))
            {
                if (GetRealDistance(t) > bonusRange() + 250 && !SebbyLib.Orbwalking.InAutoAttackRange(t) && OktwCommon.GetKsDamage(t, Q) > t.Health && Player.LSCountEnemiesInRange(400) == 0)
                {
                    Program.CastSpell(Q, t);
                    Program.debug("Q KS");
                }
                else if (Program.Combo && Player.Mana > RMANA + QMANA + EMANA + 10 && Player.LSCountEnemiesInRange(bonusRange() + 100 + t.BoundingRadius) == 0 && !getCheckBoxItem(qMenu, "autoQ"))
                    Program.CastSpell(Q, t);
                if ((Program.Combo || Program.Farm) && Player.Mana > RMANA + QMANA && Player.LSCountEnemiesInRange(400) == 0)
                {
                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.LSIsValidTarget(Q.Range) && (!OktwCommon.CanMove(enemy) || enemy.HasBuff("caitlynyordletrapinternal"))))
                        Q.Cast(enemy, true);
                    if (Player.LSCountEnemiesInRange(bonusRange()) == 0 && OktwCommon.CanHarras())
                    {
                        if (t.HasBuffOfType(BuffType.Slow) && getCheckBoxItem(qMenu, "Qslow"))
                            Q.Cast(t);
                        if (getCheckBoxItem(qMenu, "Qaoe"))
                            Q.CastIfWillHit(t, 2, true);
                    }
                }
            }
            else if (Program.LaneClear && Player.ManaPercent > getSliderItem(farmMenu, "Mana") && getCheckBoxItem(farmMenu, "farmQ") && Player.Mana > RMANA + QMANA)
            {
                var minionList = Cache.GetMinions(Player.ServerPosition, Q.Range);
                var farmPosition = Q.GetLineFarmLocation(minionList, Q.Width);
                if (farmPosition.MinionsHit > getSliderItem(farmMenu, "LCminions"))
                    Q.Cast(farmPosition.Position);
            }
        }

        private void LogicE()
        {
            if (Program.Combo && Player.Spellbook.IsAutoAttacking)
                return;
            if (getCheckBoxItem(eMenu, "autoE"))
            {
                var t = TargetSelector.GetTarget(E.Range, DamageType.Physical);
                if (t.LSIsValidTarget())
                {
                    var positionT = Player.ServerPosition - (t.Position - Player.ServerPosition);

                    if (Q.IsReady() && Player.Position.LSExtend(positionT, 400).LSCountEnemiesInRange(700) < 2)
                    {
                        var eDmg = E.GetDamage(t);
                        var qDmg = Q.GetDamage(t);
                        if (getCheckBoxItem(eMenu, "EQks") && qDmg + eDmg + Player.LSGetAutoAttackDamage(t) > t.Health && Player.Mana > EMANA + QMANA)
                        {
                            Program.CastSpell(E, t);
                            Program.debug("E + Q FINISH");
                        }
                        else if ((Program.Farm || Program.Combo) && getCheckBoxItem(eMenu, "harrasEQ") && Player.Mana > EMANA + QMANA + RMANA)
                        {
                            Program.CastSpell(E, t);
                            Program.debug("E + Q Harras");
                        }
                    }

                    if (Player.Mana > RMANA + EMANA)
                    {
                        if (getCheckBoxItem(eMenu, "Ehitchance"))
                        {
                            E.CastIfHitchanceEquals(t, HitChance.Dashing);
                        }
                        if (Player.Health < Player.MaxHealth * 0.3)
                        {
                            if (GetRealDistance(t) < 500)
                                E.Cast(t, true);
                            if (Player.CountEnemiesInRange(250) > 0)
                                E.Cast(t, true);
                        }
                    }

                }
            }
            if (getKeyBindItem(eMenu, "useE"))
            {
                var position = Player.ServerPosition - (Game.CursorPos - Player.ServerPosition);
                E.Cast(position, true);
            }
        }

        private float GetRealRange(GameObject target)
        {
            return 680f + Player.BoundingRadius + target.BoundingRadius;
        }

        private float GetRealDistance(GameObject target)
        {
            return Player.ServerPosition.LSDistance(target.Position) + ObjectManager.Player.BoundingRadius + target.BoundingRadius;
        }
        public float bonusRange()
        {
            return 720f + Player.BoundingRadius;
        }
        private void SetMana()
        {
            if ((Program.getCheckBoxItem("manaDisable") && Program.Combo) || Player.HealthPercent < 20)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
                return;
            }

            QMANA = Q.Instance.SData.Mana;
            WMANA = W.Instance.SData.Mana;
            EMANA = E.Instance.SData.Mana;

            if (!R.IsReady())
                RMANA = QMANA - Player.PARRegenRate * Q.Instance.Cooldown;
            else
                RMANA = R.Instance.SData.Mana;
        }

        public static void drawLine(Vector3 pos1, Vector3 pos2, int bold, System.Drawing.Color color)
        {
            var wts1 = Drawing.WorldToScreen(pos1);
            var wts2 = Drawing.WorldToScreen(pos2);

            Drawing.DrawLine(wts1[0], wts1[1], wts2[0], wts2[1], bold, color);
        }

        private void Drawing_OnDraw(EventArgs args)
        {

            if (getCheckBoxItem(drawMenu, "qRange"))
            {
                if (getCheckBoxItem(drawMenu, "onlyRdy"))
                {
                    if (Q.IsReady())
                        LeagueSharp.Common.Utility.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan, 1, 1);
                }
                else
                    LeagueSharp.Common.Utility.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan, 1, 1);
            }
            if (getCheckBoxItem(drawMenu, "wRange"))
            {
                if (getCheckBoxItem(drawMenu, "onlyRdy"))
                {
                    if (W.IsReady())
                        LeagueSharp.Common.Utility.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange, 1, 1);
                }
                else
                    LeagueSharp.Common.Utility.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange, 1, 1);
            }
            if (getCheckBoxItem(drawMenu, "eRange"))
            {
                if (getCheckBoxItem(drawMenu, "onlyRdy"))
                {
                    if (E.IsReady())
                        LeagueSharp.Common.Utility.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Yellow, 1, 1);
                }
                else
                    LeagueSharp.Common.Utility.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Yellow, 1, 1);
            }
            if (getCheckBoxItem(drawMenu, "rRange"))
            {
                if (getCheckBoxItem(drawMenu, "onlyRdy"))
                {
                    if (R.IsReady())
                        LeagueSharp.Common.Utility.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Gray, 1, 1);
                }
                else
                    LeagueSharp.Common.Utility.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Gray, 1, 1);
            }
            if (getCheckBoxItem(drawMenu, "noti"))
            {
                var t = TargetSelector.GetTarget(R.Range, DamageType.Physical);

                if (t.LSIsValidTarget() && R.IsReady())
                {
                    var rDamage = R.GetDamage(t);
                    if (rDamage > t.Health)
                    {
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.5f, System.Drawing.Color.Red, "Ult can kill: " + t.ChampionName + " have: " + t.Health + "hp");
                        drawLine(t.Position, Player.Position, 10, System.Drawing.Color.Yellow);
                    }
                }

                var tw = TargetSelector.GetTarget(W.Range, DamageType.Physical);
                if (tw.LSIsValidTarget())
                {
                    if (Q.GetDamage(tw) > tw.Health)
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.4f, System.Drawing.Color.Red, "Q can kill: " + t.ChampionName + " have: " + t.Health + "hp");
                }
            }
        }
    }
}