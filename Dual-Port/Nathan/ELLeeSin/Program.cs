﻿namespace ElLeeSin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EloBuddy;
    using EloBuddy.SDK;
    using EloBuddy.SDK.Events;
    using EloBuddy.SDK.Menu;
    using EloBuddy.SDK.Menu.Values;
    using LeagueSharp.Common;
    using SharpDX;
    using Damage = LeagueSharp.Common.Damage;
    using ItemData = LeagueSharp.Common.Data.ItemData;
    using Spell = LeagueSharp.Common.Spell;
    using Utility = LeagueSharp.Common.Utility;

    internal static class Program
    {
        #region Constants

        private const int FlashRange = 425;

        private const int WardRange = 600;

        #endregion

        #pragma warning disable 0649
        #pragma warning disable 414
        #region Static Fields

        public static bool CheckQ = true;

        public static bool ClicksecEnabled;

        public static Vector3 InsecClickPos;

        public static Vector2 InsecLinePos;

        public static Vector2 JumpPos;

        public static int LastQ, LastQ2, LastW, LastW2, LastE, LastE2, LastR, LastSpell, PassiveStacks;

        public static float LastWard;

        public static AIHeroClient Player = ObjectManager.Player;

        public static Spell smite = null;

        public static Dictionary<Spells, Spell> spells = new Dictionary<Spells, Spell>
                                                             {
                                                                 { Spells.Q, new Spell(SpellSlot.Q, 1100) },
                                                                 {
                                                                     Spells.W,
                                                                     new Spell(SpellSlot.W, 700 + Player.BoundingRadius)
                                                                 },
                                                                 {
                                                                     Spells.E,
                                                                     new Spell(SpellSlot.E, 350 + Player.BoundingRadius)
                                                                 },
                                                                 {
                                                                     Spells.R,
                                                                     new Spell(SpellSlot.R, 375 + Player.BoundingRadius)
                                                                 }
                                                             };

        public static int Wcasttime;

        private static readonly bool castWardAgain = true;

        private static readonly int[] SmiteBlue = { 3706, 1403, 1402, 1401, 1400 };

        private static readonly int[] SmiteRed = { 3715, 1415, 1414, 1413, 1412 };

        private static readonly string[] SpellNames =
            {
                "blindmonkqone", "blindmonkwone", "blindmonkeone",
                "blindmonkwtwo", "blindmonkqtwo", "blindmonketwo",
                "blindmonkrkick"
            };

        private static bool castQAgain;

        private static int clickCount;

        private static float doubleClickReset;

        private static SpellSlot flashSlot;

        private static SpellSlot igniteSlot;

        private static InsecComboStepSelect insecComboStep;

        private static Vector3 insecPos;

        private static bool isNullInsecPos = true;

        private static bool lastClickBool;

        private static Vector3 lastClickPos;

        private static float lastPlaced;

        private static Vector3 lastWardPos;

        private static Vector3 mouse = Game.CursorPos;

        private static float passiveTimer;

        private static bool q2Done;

        private static float q2Timer;

        private static bool reCheckWard = true;

        private static float resetTime;

        private static bool waitingForQ2;

        private static bool wardJumped;

        #endregion
        #pragma warning restore 0649    
        #pragma warning restore 414

        #region Enums

        internal enum Spells
        {
            Q,

            W,

            E,

            R
        }

        private enum InsecComboStepSelect
        {
            None,

            Qgapclose,

            Wgapclose,

            Pressr
        };

        private enum WCastStage
        {
            First,

            Second,

            Cooldown
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the Q Instance name
        /// </summary>
        /// <value>
        ///     Q instance name
        /// </value>
        public static bool QState
            => spells[Spells.Q].Instance.Name.Equals("BlindMonkQOne", StringComparison.InvariantCultureIgnoreCase);

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the E Instance name
        /// </summary>
        /// <value>
        ///     E instance name
        /// </value>
        private static bool EState
            => spells[Spells.E].Instance.Name.Equals("BlindMonkEOne", StringComparison.InvariantCultureIgnoreCase);

        /// <summary>
        ///     Gets Ravenous Hydra
        /// </summary>
        /// <value>
        ///     Ravenous Hydra
        /// </value>
        private static Items.Item Hydra => ItemData.Ravenous_Hydra_Melee_Only.GetItem();

        /// <summary>
        ///     Gets the Q2 Instance name
        /// </summary>
        /// <value>
        ///     Q2 instance name
        /// </value>
        private static bool Q2State
            => spells[Spells.Q].Instance.Name.Equals("BlindMonkQTwo", StringComparison.InvariantCultureIgnoreCase);

        /// <summary>
        ///     Gets Tiamat Item
        /// </summary>
        /// <value>
        ///     Tiamat Item
        /// </value>
        private static Items.Item Tiamat => ItemData.Tiamat_Melee_Only.GetItem();

        /// <summary>
        ///     Gets Titanic Hydra
        /// </summary>
        /// <value>
        ///     Titanic Hydra
        /// </value>
        private static Items.Item Titanic => ItemData.Titanic_Hydra_Melee_Only.GetItem();

        private static float WardFlashRange => WardRange + spells[Spells.R].Range - 100;

        /// <summary>
        ///     Gets the W Instance name
        /// </summary>
        /// <value>
        ///     W instance name
        /// </value>
        private static WCastStage WStage
        {
            get
            {
                if (!spells[Spells.W].IsReady())
                {
                    return WCastStage.Cooldown;
                }

                return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W)
                            .Name.Equals("blindmonkwtwo", StringComparison.InvariantCultureIgnoreCase)
                            ? WCastStage.Second
                            : WCastStage.First);
            }
        }

        /// <summary>
        ///     Gets the W Instance name
        /// </summary>
        /// <value>
        ///     W instance name
        /// </value>
        private static bool WState
            => spells[Spells.W].Instance.Name.Equals("BlindMonkWOne", StringComparison.InvariantCultureIgnoreCase);

        /// <summary>
        ///     Gets Youmuus Ghostblade
        /// </summary>
        /// <value>
        ///     Youmuus Ghostblade
        /// </value>
        private static Items.Item Youmuu => ItemData.Youmuus_Ghostblade.GetItem();

        #endregion

        #region Public Methods and Operators

        public static InventorySlot FindBestWardItem()
        {
            try
            {
                var slot = Items.GetWardSlot();
                if (slot == default(InventorySlot))
                {
                    return null;
                }

                var sdi = GetItemSpell(slot);
                if (sdi != default(SpellDataInst) && sdi.State == SpellState.Ready)
                {
                    return slot;
                }
                return slot;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return null;
        }

        public static Vector3 GetInsecPos(AIHeroClient target)
        {
            try
            {
                var jumpRange = 265;

                if (ClicksecEnabled && getCheckBoxItem(InitMenu.insecMenu, "clickInsec"))
                {
                    InsecLinePos = Drawing.WorldToScreen(InsecClickPos);
                    return V2E(InsecClickPos, target.Position, target.ELDistance(InsecClickPos) + jumpRange).To3D();
                }
                if (isNullInsecPos)
                {
                    isNullInsecPos = false;
                    insecPos = Player.Position;
                }

                if (GetAllyHeroes(target, 2000 + getSliderItem(InitMenu.insecMenu, "bonusRangeA")).Count > 0
                    && getCheckBoxItem(InitMenu.insecMenu, "ElLeeSin.Insec.Ally"))
                {
                    var insecPosition = InterceptionPoint(GetAllyInsec(GetAllyHeroes(target, 2000 + getSliderItem(InitMenu.insecMenu, "bonusRangeA"))));
                    InsecLinePos = Drawing.WorldToScreen(insecPosition);
                    return V2E(insecPosition, target.Position, target.ELDistance(insecPosition) + jumpRange).To3D();
                }

                if (getCheckBoxItem(InitMenu.insecMenu, "ElLeeSin.Insec.Tower"))
                {
                    var tower =ObjectManager.Get<Obj_AI_Turret>().Where(turret => turret.ELDistance(target) - 725 <= 950 && turret.IsAlly && turret.IsVisible && turret.Health > 0 && turret.ELDistance(target) <= 1300 && turret.ELDistance(target) > 400).MinOrDefault(i => target.ELDistance(Player));
                    if (tower != null)
                    {
                        InsecLinePos = Drawing.WorldToScreen(tower.Position);
                        return V2E(tower.Position, target.Position, target.ELDistance(tower.Position) + jumpRange).To3D();
                    }
                }

                if (getCheckBoxItem(InitMenu.insecMenu, "ElLeeSin.Insec.Original.Pos"))
                {
                    InsecLinePos = Drawing.WorldToScreen(insecPos);
                    return V2E(insecPos, target.Position, target.ELDistance(insecPos) + jumpRange).To3D();
                }
                return new Vector3();
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }
            return new Vector3();
        }

        public static bool HasQBuff(this Obj_AI_Base unit)
        {
            return (unit.HasAnyBuffs("BlindMonkQOne") || unit.HasAnyBuffs("blindmonkqonechaos")
                    || unit.HasAnyBuffs("BlindMonkSonicWave"));
        }

        public static void Orbwalk(Vector3 pos, AIHeroClient target = null)
        {
            EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, pos);
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

        /// <summary>
        ///     Gets the Q2 damage
        /// </summary>
        /// <param name="target"></param>
        /// <param name="subHP"></param>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static float Q2Damage(Obj_AI_Base target, float subHP = 0, bool monster = false)
        {
            var damage = (50 + (spells[Spells.Q].Level * 30)) + (0.09 * Player.FlatPhysicalDamageMod)
                         + ((target.MaxHealth - (target.Health - subHP)) * 0.08);

            if (monster && damage > 400)
            {
                return (float)Player.CalcDamage(target, DamageType.Physical, 400);
            }

            return (float)Player.CalcDamage(target, DamageType.Physical, damage);
        }

        #endregion

        #region Methods

        private static void AllClear()
        {
            try
            {
                var minions = MinionManager.GetMinions(spells[Spells.Q].Range).FirstOrDefault();
                if (minions == null)
                {
                    return;
                }

                UseItems(minions);

                if (getCheckBoxItem(InitMenu.waveclearMenu, "ElLeeSin.Lane.Q") && !QState && spells[Spells.Q].IsReady() && minions.HasQBuff()
                    && (LastQ + 2700 < Environment.TickCount || spells[Spells.Q].GetDamage(minions, 1) > minions.Health
                        || minions.ELDistance(Player) > Player.GetAutoAttackRange() + 50))
                {
                    spells[Spells.Q].Cast();
                }

                if (spells[Spells.Q].IsReady() && getCheckBoxItem(InitMenu.waveclearMenu, "ElLeeSin.Lane.Q") && LastQ + 200 < Environment.TickCount)
                {
                    if (QState && minions.ELDistance(Player) < spells[Spells.Q].Range)
                    {
                        spells[Spells.Q].Cast(minions);
                    }
                }

                if (getCheckBoxItem(InitMenu.comboMenu, "ElLeeSin.Combo.AAStacks")
                        && PassiveStacks > getSliderItem(InitMenu.comboMenu, "ElLeeSin.Combo.PassiveStacks")
                        && Player.GetAutoAttackRange() > Player.ELDistance(minions))
                {
                    return;
                }

                if (spells[Spells.E].IsReady() && getCheckBoxItem(InitMenu.waveclearMenu, "ElLeeSin.Lane.E"))
                {
                    if (EState && spells[Spells.E].IsInRange(minions))
                    {
                        LastE = Environment.TickCount;
                        spells[Spells.E].Cast();
                        return;
                    }

                    if (!EState && spells[Spells.E].IsInRange(minions) && LastE + 400 < Environment.TickCount)
                    {
                        spells[Spells.E].Cast();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }
        }

        private static void CastQ(Obj_AI_Base target, bool smiteQ = false)
        {
            try
            {
                if (!spells[Spells.Q].IsReady() || !target.LSIsValidTarget(spells[Spells.Q].Range))
                {
                    return;
                }

                var prediction = spells[Spells.Q].GetPrediction(target);

                if (prediction.Hitchance >= HitChance.High)
                {
                    spells[Spells.Q].Cast(target);
                }
                else if (getCheckBoxItem(InitMenu.miscMenu, "ElLeeSin.Smite.Q") && spells[Spells.Q].IsReady()
                         && target.LSIsValidTarget(spells[Spells.Q].Range)
                         && prediction.CollisionObjects.Count(a => a.NetworkId != target.NetworkId && a.IsMinion) == 1
                         && Player.GetSpellSlot(SmiteSpellName()).IsReady())
                {
                    Player.Spellbook.CastSpell(
                        Player.GetSpellSlot(SmiteSpellName()),
                        prediction.CollisionObjects.Where(a => a.NetworkId != target.NetworkId && a.IsMinion)
                            .ToList()[0]);

                    spells[Spells.Q].Cast(prediction.CastPosition);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }
        }

        private static void CastW(Obj_AI_Base obj)
        {
            if (500 >= Utils.TickCount - Wcasttime || WStage != WCastStage.First)
            {
                return;
            }

            spells[Spells.W].CastOnUnit(obj);
            Wcasttime = Utils.TickCount;
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(spells[Spells.Q].Range, DamageType.Physical);
            if (target == null)
            {
                return;
            }

            if (QState)
            {
                CastQ(target);
            }

            if (!target.IsZombie && spells[Spells.R].IsReady() && getCheckBoxItem(InitMenu.comboMenu, "ElLeeSin.Combo.R")
                && getCheckBoxItem(InitMenu.comboMenu, "ElLeeSin.Combo.Q") && target.LSIsValidTarget()
                && (spells[Spells.R].GetDamage(target) >= target.Health
                    || target.HasQBuff()
                    && target.Health
                    < spells[Spells.R].GetDamage(target) + Q2Damage(target, spells[Spells.R].GetDamage(target))))
            {
                spells[Spells.R].CastOnUnit(target);
            }

            if (getCheckBoxItem(InitMenu.comboMenu, "ElLeeSin.Combo.Q2") && spells[Spells.Q].IsReady()
                && spells[Spells.Q].Instance.Name.Equals(
                    "blindmonkqtwo",
                    StringComparison.InvariantCultureIgnoreCase) && target.HasQBuff() && (castQAgain))
            {
                CastQ(target);
            }

            if (target.HasQBuff() && getCheckBoxItem(InitMenu.comboMenu, "ElLeeSin.Combo.Q2") && target.LSIsValidTarget())
            {
                if (target.HasQBuff()
                    && target.Health
                    < spells[Spells.R].GetDamage(target) + Q2Damage(target, spells[Spells.R].GetDamage(target)))
                {
                    if (getCheckBoxItem(InitMenu.comboMenu, "ElLeeSin.Combo.KS.R"))
                    {
                        spells[Spells.R].CastOnUnit(target);
                    }
                }
            }

            if (target.LSIsValidTarget(385f))
            {
                UseItems(target);
            }

            if (spells[Spells.Q].IsReady() && getCheckBoxItem(InitMenu.comboMenu, "ElLeeSin.Combo.Q2"))
            {
                if (spells[Spells.Q].Instance.Name.Equals(
                    "blindmonkqtwo",
                    StringComparison.InvariantCultureIgnoreCase) && target.HasQBuff())
                {
                    if ((castQAgain || spells[Spells.Q].GetDamage(target, 1) > target.Health + target.AttackShield)
                        || ReturnQBuff()?.ELDistance(target) < Player.ELDistance(target)
                        && !target.LSIsValidTarget(Player.GetAutoAttackRange()))
                    {
                        spells[Spells.Q].Cast(target);
                    }
                }
            }

            if (!target.IsZombie && spells[Spells.R].GetDamage(target) >= target.Health
                && getCheckBoxItem(InitMenu.comboMenu, "ElLeeSin.Combo.KS.R") && target.LSIsValidTarget(spells[Spells.R].Range))
            {
                spells[Spells.R].CastOnUnit(target);
            }

            if (spells[Spells.Q].IsReady() && QState && getCheckBoxItem(InitMenu.comboMenu, "ElLeeSin.Combo.Q"))
            {
                CastQ(target);
            }

            if (getCheckBoxItem(InitMenu.comboMenu, "ElLeeSin.Combo.AAStacks")
                && PassiveStacks > getSliderItem(InitMenu.comboMenu, "ElLeeSin.Combo.PassiveStacks")
                && Player.GetAutoAttackRange() > Player.ELDistance(target))
            {
                return;
            }

            if (getCheckBoxItem(InitMenu.comboMenu, "ElLeeSin.Combo.Q2") && spells[Spells.Q].IsReady()
                && spells[Spells.Q].Instance.Name.Equals(
                    "blindmonkqtwo",
                    StringComparison.InvariantCultureIgnoreCase) && target.HasQBuff()
                && (castQAgain || Player.ELDistance(target) > Player.GetAutoAttackRange()
                    || Q2Damage(target) > target.Health + target.AttackShield))
            {
                CastQ(target);
            }

            if (getCheckBoxItem(InitMenu.comboMenu, "ElLeeSin.Combo.W"))
            {
                if (getCheckBoxItem(InitMenu.comboMenu, "ElLeeSin.Combo.Mode.WW")
                    && target.ELDistance(Player) > Player.GetAutoAttackRange())
                {
                    WardJump(target.Position, false, true);
                }

                if (!getCheckBoxItem(InitMenu.comboMenu, "ElLeeSin.Combo.Mode.WW") && target.ELDistance(Player) > spells[Spells.Q].Range)
                {
                    WardJump(target.Position, false, true);
                }
            }

            if (spells[Spells.E].IsReady() && getCheckBoxItem(InitMenu.comboMenu, "ElLeeSin.Combo.E") && target.ELDistance(Player) < 400
                && !Player.IsDashing())
            {
                if (Environment.TickCount - LastW <= 150)
                {
                    return;
                }

                if (EState)
                {
                    if (GetEHits().Item1 > 0 || (PassiveStacks == 0 && Player.Mana >= 70)
                        || target.ELDistance(Player) > Player.GetAutoAttackRange() + 70)
                    {
                        spells[Spells.E].Cast();
                    }
                }
                else
                {
                    if (LastE + 1800 < Environment.TickCount)
                    {
                        if (GetEHits().Item1 > 0
                            || target.ELDistance(Player) > Player.GetAutoAttackRange() + 50)
                        {
                            spells[Spells.E].Cast();
                        }
                    }
                }
            }

            if (spells[Spells.W].IsReady() && getCheckBoxItem(InitMenu.comboMenu, "ElLeeSin.Combo.W2"))
            {
                if (Environment.TickCount - LastE <= 250)
                {
                    return;
                }

                if (WState && target.LSIsValidTarget(Player.GetAutoAttackRange()))
                {
                    spells[Spells.W].Cast();
                    LastW = Environment.TickCount;
                }

                if (!WState && LastW + 1800 < Environment.TickCount)
                {
                    spells[Spells.W].Cast();
                }
            }
        }

        public static void Game_OnGameLoad()
        {
            if (Player.ChampionName != "LeeSin")
            {
                return;
            }

            igniteSlot = Player.GetSpellSlot("SummonerDot");
            flashSlot = Player.GetSpellSlot("summonerflash");

            spells[Spells.Q].SetSkillshot(0.275f, 60f, 1850f, true, SkillshotType.SkillshotLine);
            spells[Spells.E].SetTargetted(0.275f, float.MaxValue);

            JumpHandler.Load();

            try
            {
                InitMenu.Initialize();
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }

            Drawing.OnDraw += Drawings.OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            GameObject.OnCreate += OnCreate;
            Orbwalker.OnPostAttack += OrbwalkingAfterAttack;
            GameObject.OnDelete += GameObject_OnDelete;
            Game.OnWndProc += Game_OnWndProc;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            try
            {
                if (doubleClickReset <= Environment.TickCount && clickCount != 0)
                {
                    doubleClickReset = float.MaxValue;
                    clickCount = 0;
                }

                if (clickCount >= 2)
                {
                    resetTime = Environment.TickCount + 3000;
                    ClicksecEnabled = true;
                    InsecClickPos = Game.CursorPos;
                    clickCount = 0;
                }

                if (passiveTimer <= Environment.TickCount)
                {
                    PassiveStacks = 0;
                }

                if (resetTime <= Environment.TickCount && !getKeyBindItem(InitMenu.insecMenu, "InsecEnabled")
                    && ClicksecEnabled)
                {
                    ClicksecEnabled = false;
                }

                if (q2Timer <= Environment.TickCount)
                {
                    q2Done = false;
                }

                if (Player.IsDead || Player.LSIsRecalling())
                {
                    return;
                }

                if ((getCheckBoxItem(InitMenu.insecMenu, "insecMode")
                         ? TargetSelector.SelectedTarget
                         : TargetSelector.GetTarget(spells[Spells.Q].Range, DamageType.Physical)) == null)
                {
                    insecComboStep = InsecComboStepSelect.None;
                }

                if (getKeyBindItem(InitMenu.comboMenu, "starCombo"))
                {
                    WardCombo();
                }

                if (getCheckBoxItem(InitMenu.miscMenu, "ElLeeSin.Ignite.KS"))
                {
                    var newTarget = TargetSelector.GetTarget(600f, DamageType.True);

                    if (newTarget != null && igniteSlot != SpellSlot.Unknown
                        && Player.Spellbook.CanUseSpell(igniteSlot) == SpellState.Ready
                        && ObjectManager.Player.GetSummonerSpellDamage(newTarget, Damage.SummonerSpell.Ignite)
                        > newTarget.Health)
                    {
                        Player.Spellbook.CastSpell(igniteSlot, newTarget);
                    }
                }

                if (getKeyBindItem(InitMenu.insecMenu, "InsecEnabled") && spells[Spells.R].IsReady())
                {
                    if (getCheckBoxItem(InitMenu.insecMenu, "insecOrbwalk"))
                    {
                        Orbwalk(Game.CursorPos);
                    }

                    var newTarget = getCheckBoxItem(InitMenu.insecMenu, "insecMode")
                                        ? TargetSelector.SelectedTarget
                                        : TargetSelector.GetTarget(
                                            spells[Spells.Q].Range,
                                            DamageType.Physical);

                    if (newTarget != null)
                    {
                        InsecCombo(newTarget);
                    }
                }
                else
                {
                    isNullInsecPos = true;
                    wardJumped = false;
                }

                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                {
                    insecComboStep = InsecComboStepSelect.None;
                }

                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                {
                    Combo();
                }

                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) ||
                    Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
                {
                    AllClear();
                    JungleClear();
                }

                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
                {
                    Harass();
                }

                if (getKeyBindItem(InitMenu.wardjumpMenu, "ElLeeSin.Wardjump"))
                {
                    WardjumpToMouse();
                }

                if (getKeyBindItem(InitMenu.insecMenu, "ElLeeSin.Insec.UseInstaFlash"))
                {
                    var target = TargetSelector.GetTarget(spells[Spells.R].Range, DamageType.Physical);
                    if (target == null)
                    {
                        return;
                    }

                    if (spells[Spells.R].IsReady() && !target.IsZombie
                        && Player.Spellbook.CanUseSpell(flashSlot) == SpellState.Ready
                        && target.LSIsValidTarget(spells[Spells.R].Range))
                    {
                        spells[Spells.R].CastOnUnit(target);
                    }
                }

                if (getCheckBoxItem(InitMenu.kickMenu, "ElLeeSin.Combo.New"))
                {
                    float leeSinRKickDistance = 700;
                    float leeSinRKickWidth = 100;
                    var minREnemies = getSliderItem(InitMenu.kickMenu, "ElLeeSin.Combo.R.Count");
                    foreach (var enemy in HeroManager.Enemies)
                    {
                        var startPos = enemy.ServerPosition;
                        var endPos = Player.ServerPosition.ELExtend(
                            startPos,
                            Player.ELDistance(enemy) + leeSinRKickDistance);
                        var rectangle = new Geometry.Polygon.Rectangle(startPos, endPos, leeSinRKickWidth);

                        if (HeroManager.Enemies.Count(x => rectangle.IsInside(x)) >= minREnemies)
                        {
                            spells[Spells.R].Cast(enemy);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg != (uint)WindowsMessages.WM_LBUTTONDOWN)
            {
                return;
            }

            var asec = HeroManager.Enemies.Where(a => a.ELDistance(Game.CursorPos) < 200 && a.IsValid && !a.IsDead);

            if (asec.Any())
            {
                return;
            }
            if (!lastClickBool || clickCount == 0)
            {
                clickCount++;
                lastClickPos = Game.CursorPos;
                lastClickBool = true;
                doubleClickReset = Environment.TickCount + 600;
                return;
            }
            if (lastClickBool && lastClickPos.ELDistance(Game.CursorPos) < 200)
            {
                clickCount++;
                lastClickBool = false;
            }
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (!(sender is Obj_GeneralParticleEmitter))
            {
                return;
            }
            if (sender.Name.Contains("blindmonk_q_resonatingstrike") && waitingForQ2)
            {
                waitingForQ2 = false;
                q2Done = true;
                q2Timer = Environment.TickCount + 800;
            }
        }

        private static List<AIHeroClient> GetAllyHeroes(AIHeroClient position, int range)
        {
            return
                HeroManager.Allies.Where(hero => !hero.IsMe && !hero.IsDead && hero.ELDistance(position) < range).ToList();
        }

        private static List<AIHeroClient> GetAllyInsec(List<AIHeroClient> heroes)
        {
            byte alliesAround = 0;
            var tempObject = new AIHeroClient();
            foreach (var hero in heroes)
            {
                var localTemp =
                    GetAllyHeroes(hero, 500 + getSliderItem(InitMenu.insecMenu, "bonusRangeA")).Count;
                if (localTemp > alliesAround)
                {
                    tempObject = hero;
                    alliesAround = (byte)localTemp;
                }
            }
            return GetAllyHeroes(tempObject, 500 + getSliderItem(InitMenu.insecMenu, "bonusRangeA"));
        }

        private static Tuple<int, List<AIHeroClient>> GetEHits()
        {
            try
            {
                var hits =
                    HeroManager.Enemies.Where(
                        e => e.LSIsValidTarget() && e.ELDistance(Player) < 430f || e.ELDistance(Player) < 430f).ToList();

                return new Tuple<int, List<AIHeroClient>>(hits.Count, hits);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return new Tuple<int, List<AIHeroClient>>(0, null);
        }

        private static SpellDataInst GetItemSpell(InventorySlot invSlot)
        {
            return Player.Spellbook.Spells.FirstOrDefault(spell => (int)spell.Slot == invSlot.Slot + 4);
        }

        private static void Harass()
        {
            try
            {
                var target = TargetSelector.GetTarget(spells[Spells.Q].Range, DamageType.Physical);
                if (target == null)
                {
                    return;
                }

                if (!QState && LastQ + 200 < Environment.TickCount && getCheckBoxItem(InitMenu.harassMenu, "ElLeeSin.Harass.Q1") && !QState
                    && spells[Spells.Q].IsReady() && target.HasQBuff()
                    && (LastQ + 2700 < Environment.TickCount || spells[Spells.Q].GetDamage(target, 1) > target.Health
                        || target.ELDistance(Player) > Player.GetAutoAttackRange() + 50))
                {
                    spells[Spells.Q].Cast();
                }


                if (getCheckBoxItem(InitMenu.comboMenu, "ElLeeSin.Combo.AAStacks")
                        && PassiveStacks > getSliderItem(InitMenu.harassMenu, "ElLeeSin.Harass.PassiveStacks")
                        && Player.GetAutoAttackRange() > Player.ELDistance(target))
                {
                    return;
                }

                if (spells[Spells.Q].IsReady() && getCheckBoxItem(InitMenu.harassMenu, "ElLeeSin.Harass.Q1") && LastQ + 200 < Environment.TickCount)
                {
                    if (QState && target.ELDistance(Player) < spells[Spells.Q].Range)
                    {
                        CastQ(target);
                    }
                }

                if (spells[Spells.E].IsReady() && getCheckBoxItem(InitMenu.harassMenu, "ElLeeSin.Harass.E1") && LastE + 200 < Environment.TickCount)
                {
                    if (EState && target.ELDistance(Player) < spells[Spells.E].Range)
                    {
                        spells[Spells.E].Cast();
                        return;
                    }

                    if (!EState && target.ELDistance(Player) > Player.GetAutoAttackRange() + 50)
                    {
                        spells[Spells.E].Cast();
                    }
                }

                if (getCheckBoxItem(InitMenu.harassMenu, "ElLeeSin.Harass.Wardjump") && Player.ELDistance(target) < 50 && !(target.HasQBuff())
                    && (EState || !spells[Spells.E].IsReady() && getCheckBoxItem(InitMenu.harassMenu, "ElLeeSin.Harass.E1"))
                    && (QState || !spells[Spells.Q].IsReady() && getCheckBoxItem(InitMenu.harassMenu, "ElLeeSin.Harass.Q1")))
                {
                    var min =
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(a => a.IsAlly && a.ELDistance(Player) <= spells[Spells.W].Range)
                            .OrderByDescending(a => a.ELDistance(target))
                            .FirstOrDefault();

                    spells[Spells.W].CastOnUnit(min);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }
        }

        private static bool HasAnyBuffs(this Obj_AI_Base unit, string s)
        {
            return
                unit.Buffs.Any(
                    a => a.Name.ToLower().Contains(s.ToLower()) || a.DisplayName.ToLower().Contains(s.ToLower()));
        }

        private static void InsecCombo(AIHeroClient target)
        {
            try
            {
                if (target != null && target.IsVisible)
                {
                    if (Player.ELDistance(GetInsecPos(target)) < 200)
                    {
                        insecComboStep = InsecComboStepSelect.Pressr;
                    }
                    else if (insecComboStep == InsecComboStepSelect.None
                             && GetInsecPos(target).ELDistance(Player.Position) < 600)
                    {
                        insecComboStep = InsecComboStepSelect.Wgapclose;
                    }
                    else if (insecComboStep == InsecComboStepSelect.None
                             && target.ELDistance(Player) < spells[Spells.Q].Range)
                    {
                        insecComboStep = InsecComboStepSelect.Qgapclose;
                    }

                    switch (insecComboStep)
                    {
                        case InsecComboStepSelect.Qgapclose:
                            if (QState)
                            {
                                var pred1 = spells[Spells.Q].GetPrediction(target);
                                if (pred1.Hitchance >= HitChance.High)
                                {
                                    if (pred1.CollisionObjects.Count == 0)
                                    {
                                        if (spells[Spells.Q].Cast(pred1.CastPosition))
                                        {
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        CastQ(target);
                                    }
                                }

                                if (!getCheckBoxItem(InitMenu.insecMenu, "checkOthers1"))
                                {
                                    return;
                                }

                                var insecObjects =
                                    HeroManager.Enemies.Where(
                                        x => x.LSIsValidTarget(spells[Spells.Q].Range) && x.ELDistance(target) < 550f)
                                        .Concat(MinionManager.GetMinions(Player.ServerPosition, spells[Spells.Q].Range))
                                        .Where(
                                            m =>
                                            m.LSIsValidTarget(spells[Spells.Q].Range)
                                            && spells[Spells.Q].GetDamage(m) < m.Health + 15
                                            && m.ELDistance(target) < 400f)
                                        .ToList();

                                if (insecObjects.Count > 0)
                                {
                                    foreach (var insectObject in
                                        insecObjects.Select(i => spells[Spells.Q].GetPrediction(i))
                                            .Where(i => i.Hitchance >= HitChance.High)
                                            .OrderByDescending(i => i.Hitchance))
                                    {
                                        spells[Spells.Q].Cast(insectObject.CastPosition);
                                        break;
                                    }
                                }
                                else
                                {
                                    return;
                                }
                            }

                            if (!(target.HasQBuff()) && QState)
                            {
                                CastQ(target);
                            }
                            else if (target.HasQBuff())
                            {
                                spells[Spells.Q].Cast();
                                insecComboStep = InsecComboStepSelect.Wgapclose;
                            }
                            else
                            {
                                if (spells[Spells.Q].Instance.Name.Equals(
                                    "blindmonkqtwo",
                                    StringComparison.InvariantCultureIgnoreCase)
                                    && ReturnQBuff()?.ELDistance(target) <= 600)
                                {
                                    spells[Spells.Q].Cast();
                                }
                            }
                            break;

                        case InsecComboStepSelect.Wgapclose:

                            if (Player.ELDistance(target) < WardRange)
                            {
                                WardJump(GetInsecPos(target), false, true, true);

                                if (FindBestWardItem() == null && spells[Spells.R].IsReady()
                                    && getCheckBoxItem(InitMenu.insecMenu, "ElLeeSin.Flash.Insec")
                                    && Player.Spellbook.CanUseSpell(flashSlot) == SpellState.Ready)
                                {
                                    if ((GetInsecPos(target).ELDistance(Player.Position) < FlashRange
                                         && LastWard + 1000 < Environment.TickCount) || !spells[Spells.W].IsReady())
                                    {
                                        Player.Spellbook.CastSpell(flashSlot, GetInsecPos(target));
                                    }
                                }
                            }
                            else if (Player.ELDistance(target) < WardFlashRange)
                            {
                                WardJump(target.Position);

                                if (spells[Spells.R].IsReady() && getCheckBoxItem(InitMenu.insecMenu, "ElLeeSin.Flash.Insec")
                                    && Player.Spellbook.CanUseSpell(flashSlot) == SpellState.Ready)
                                {
                                    if (Player.ELDistance(target) < FlashRange - 25)
                                    {
                                        if (FindBestWardItem() == null || LastWard + 1000 < Environment.TickCount)
                                        {
                                            Player.Spellbook.CastSpell(flashSlot, GetInsecPos(target));
                                        }
                                    }
                                }
                            }
                            break;

                        case InsecComboStepSelect.Pressr:
                            spells[Spells.R].CastOnUnit(target);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }
        }

        private static Vector3 InterceptionPoint(List<AIHeroClient> heroes)
        {
            var result = new Vector3();
            foreach (var hero in heroes)
            {
                result += hero.Position;
            }
            result.X /= heroes.Count;
            result.Y /= heroes.Count;
            return result;
        }

        private static void JungleClear()
        {
            try
            {
                var minion =
                    MinionManager.GetMinions(
                        spells[Spells.Q].Range,
                        MinionTypes.All,
                        MinionTeam.Neutral,
                        MinionOrderTypes.MaxHealth).FirstOrDefault();

                if (minion == null)
                {
                    return;
                }

                if (PassiveStacks > 0 || LastSpell + 400 > Environment.TickCount)
                {
                    return;
                }

                if (spells[Spells.W].IsReady() && getCheckBoxItem(InitMenu.waveclearMenu, "ElLeeSin.Jungle.W"))
                {
                    if (Environment.TickCount - LastE <= 250)
                    {
                        return;
                    }

                    if (WState && minion.LSIsValidTarget(Player.GetAutoAttackRange()))
                    {
                        spells[Spells.W].Cast();
                        LastW = Environment.TickCount;
                    }

                    if (!WState && LastW + 1000 < Environment.TickCount)
                    {
                        spells[Spells.W].Cast();
                    }
                }

                if (spells[Spells.E].IsReady() && getCheckBoxItem(InitMenu.waveclearMenu, "ElLeeSin.Jungle.E"))
                {
                    if (EState && spells[Spells.E].IsInRange(minion))
                    {
                        spells[Spells.E].Cast();
                        LastSpell = Environment.TickCount;
                        return;
                    }

                    if (!EState && spells[Spells.E].IsInRange(minion) && LastE + 400 < Environment.TickCount)
                    {
                        spells[Spells.E].Cast();
                        LastSpell = Environment.TickCount;
                    }
                }

                if (spells[Spells.Q].IsReady() && getCheckBoxItem(InitMenu.waveclearMenu, "ElLeeSin.Jungle.Q"))
                {
                    if (QState && minion.ELDistance(Player) < spells[Spells.Q].Range
                        && LastQ + 200 < Environment.TickCount)
                    {
                        spells[Spells.Q].Cast(minion);
                        LastSpell = Environment.TickCount;
                        return;
                    }

                    spells[Spells.Q].Cast();
                    LastSpell = Environment.TickCount;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            try
            {
                if (!sender.IsMe)
                {
                    return;
                }

                if (SpellNames.Contains(args.SData.Name.ToLower()))
                {
                    PassiveStacks = 2;
                    passiveTimer = Environment.TickCount + 3000;
                }

                if (args.SData.Name.Equals("BlindMonkQOne", StringComparison.InvariantCultureIgnoreCase))
                {
                    castQAgain = false;
                    Utility.DelayAction.Add(2900, () => { castQAgain = true; });
                }

                if (spells[Spells.R].IsReady() && Player.Spellbook.CanUseSpell(flashSlot) == SpellState.Ready)
                {
                    var target = getCheckBoxItem(InitMenu.insecMenu, "insecMode")
                                     ? TargetSelector.SelectedTarget
                                     : TargetSelector.GetTarget(
                                         spells[Spells.R].Range,
                                         DamageType.Physical);

                    if (target == null)
                    {
                        return;
                    }
                    if (args.SData.Name.Equals("BlindMonkRKick", StringComparison.InvariantCultureIgnoreCase)
                        && getKeyBindItem(InitMenu.insecMenu, "ElLeeSin.Insec.UseInstaFlash"))
                    {
                        Utility.DelayAction.Add(80, () => Player.Spellbook.CastSpell(flashSlot, GetInsecPos(target)));
                    }
                }

                if (args.SData.Name.Equals("summonerflash", StringComparison.InvariantCultureIgnoreCase)
                    && insecComboStep != InsecComboStepSelect.None)
                {
                    var target = getCheckBoxItem(InitMenu.insecMenu, "insecMode")
                                     ? TargetSelector.SelectedTarget
                                     : TargetSelector.GetTarget(
                                         spells[Spells.Q].Range,
                                         DamageType.Physical);

                    insecComboStep = InsecComboStepSelect.Pressr;

                    Utility.DelayAction.Add(80, () => spells[Spells.R].CastOnUnit(target));
                }
                if (args.SData.Name.Equals("BlindMonkQTwo", StringComparison.InvariantCultureIgnoreCase))
                {
                    waitingForQ2 = true;
                    Utility.DelayAction.Add(3000, () => { waitingForQ2 = false; });
                }
                if (args.SData.Name.Equals("BlindMonkRKick", StringComparison.InvariantCultureIgnoreCase))
                {
                    insecComboStep = InsecComboStepSelect.None;
                }

                switch (args.SData.Name.ToLower())
                {
                    case "blindmonkqone":
                        LastQ = Environment.TickCount;
                        LastSpell = Environment.TickCount;
                        PassiveStacks = 2;
                        break;
                    case "blindmonkwone":
                        LastW = Environment.TickCount;
                        LastSpell = Environment.TickCount;
                        PassiveStacks = 2;
                        break;
                    case "blindmonkeone":
                        LastE = Environment.TickCount;
                        LastSpell = Environment.TickCount;
                        PassiveStacks = 2;
                        break;
                    case "blindmonkqtwo":
                        LastQ2 = Environment.TickCount;
                        LastSpell = Environment.TickCount;
                        PassiveStacks = 2;
                        CheckQ = false;
                        break;
                    case "blindmonkwtwo":
                        LastW2 = Environment.TickCount;
                        LastSpell = Environment.TickCount;
                        PassiveStacks = 2;
                        break;
                    case "blindmonketwo":
                        LastQ = Environment.TickCount;
                        LastSpell = Environment.TickCount;
                        PassiveStacks = 2;
                        break;
                    case "blindmonkrkick":
                        LastR = Environment.TickCount;
                        LastSpell = Environment.TickCount;
                        PassiveStacks = 2;
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }
        }

        private static void OnCreate(GameObject sender, EventArgs args)
        {
            if (Environment.TickCount < lastPlaced + 300)
            {
                var ward = (Obj_AI_Base)sender;
                if (spells[Spells.W].IsReady() && ward.Name.ToLower().Contains("ward")
                    && ward.ELDistance(lastWardPos) < 500)
                {
                    spells[Spells.W].Cast(ward);
                }
            }
        }

        private static void OrbwalkingAfterAttack(AttackableUnit target, EventArgs args)
        {
            try
            {
                if (PassiveStacks > 0)
                {
                    PassiveStacks--;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }
        }

        private static Obj_AI_Base ReturnQBuff()
        {
            try
            {
                return
                    ObjectManager.Get<Obj_AI_Base>()
                        .Where(a => a.LSIsValidTarget(1300))
                        .FirstOrDefault(unit => unit.HasQBuff());
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }
            return null;
        }

        private static string SmiteSpellName()
        {
            if (SmiteBlue.Any(a => Items.HasItem(a)))
            {
                return "s5_summonersmiteplayerganker";
            }

            if (SmiteRed.Any(a => Items.HasItem(a)))
            {
                return "s5_summonersmiteduel";
            }

            return "summonersmite";
        }

        private static bool UseItems(Obj_AI_Base target)
        {
            if (Player.IsDashing() || Player.Spellbook.IsAutoAttacking)
            {
                return false;
            }

            var youmuus = Youmuu;
            if (Youmuu.IsReady() && youmuus.Cast())
            {
                return true;
            }

            var heroes = Player.GetEnemiesInRange(385).Count;
            var count = heroes;

            var tiamat = Tiamat;
            if (tiamat.IsReady() && count > 0 && tiamat.Cast())
            {
                return true;
            }

            var hydra = Hydra;
            if (Hydra.IsReady() && count > 0 && hydra.Cast())
            {
                return true;
            }

            var titanic = Titanic;
            return titanic.IsReady() && count > 0 && titanic.Cast();
        }

        private static Vector2 V2E(Vector3 from, Vector3 direction, float distance)
        {
            return from.ELTo2D() + distance * Vector3.Normalize(direction - from).ELTo2D();
        }

        private static void WardCombo()
        {
            try
            {
                var target = TargetSelector.GetTarget(1500, DamageType.Physical);

                //Orbwalking.Orbwalk(target ?? null, Game.CursorPos, InitMenu.Menu.Item("ExtraWindup").GetValue<Slider>().Value, InitMenu.Menu.Item("HoldPosRadius").GetValue<Slider>().Value);

                Orbwalker.OrbwalkTo(Game.CursorPos);

                if (target == null)
                {
                    return;
                }

                UseItems(target);

                if (spells[Spells.Q].IsReady() && QState)
                {
                    CastQ(target);
                }

                if (target.HasQBuff())
                {
                    if (castQAgain
                        || target.HasBuffOfType(BuffType.Knockback) && !Player.LSIsValidTarget(300f)
                        && !spells[Spells.R].IsReady()
                        || !target.LSIsValidTarget(Player.GetAutoAttackRange())
                        && !spells[Spells.R].IsReady())
                    {
                        Utility.DelayAction.Add(300, () => { spells[Spells.Q].Cast(); });
                    }
                }

                if (target.ELDistance(Player) > spells[Spells.R].Range
                    && target.ELDistance(Player) < spells[Spells.R].Range + 580 && target.HasQBuff())
                {
                    WardJump(target.Position, false);
                }

                if (spells[Spells.E].IsReady() && EState && target.LSIsValidTarget(spells[Spells.E].Range)
                    && !Player.IsDashing())
                {
                    spells[Spells.E].Cast();
                }

                if (spells[Spells.R].IsReady() && spells[Spells.Q].IsReady() && target.HasQBuff())
                {
                    spells[Spells.R].CastOnUnit(target);
                }

                if (spells[Spells.Q].IsReady() && QState)
                {
                    CastQ(target);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }
        }

        private static void WardJump(
            Vector3 pos,
            bool m2M = true,
            bool maxRange = false,
            bool reqinMaxRange = false,
            bool minions = true,
            bool champions = true)
        {
            if (WStage != WCastStage.First)
            {
                return;
            }

            var basePos = Player.Position.ELTo2D();
            var newPos = (pos.ELTo2D() - Player.Position.ELTo2D());

            if (JumpPos == new Vector2())
            {
                if (reqinMaxRange)
                {
                    JumpPos = pos.ELTo2D();
                }
                else if (maxRange || Player.ELDistance(pos) > 590)
                {
                    JumpPos = basePos + (newPos.ELNormalized() * (590));
                }
                else
                {
                    JumpPos = basePos + (newPos.ELNormalized() * (Player.ELDistance(pos)));
                }
            }
            if (JumpPos != new Vector2() && reCheckWard)
            {
                reCheckWard = false;
                Utility.DelayAction.Add(
                    20,
                    () =>
                    {
                        if (JumpPos != new Vector2())
                        {
                            JumpPos = new Vector2();
                            reCheckWard = true;
                        }
                    });
            }
            if (m2M)
            {
                Orbwalk(pos);
            }
            if (!spells[Spells.W].IsReady() || WStage != WCastStage.First
                || reqinMaxRange && Player.ELDistance(pos) > spells[Spells.W].Range)
            {
                return;
            }

            if (minions || champions)
            {
                if (champions)
                {
                    var wardJumpableChampion =
                        ObjectManager.Get<AIHeroClient>()
                            .Where(
                                x =>
                                x.IsAlly && x.ELDistance(Player) < spells[Spells.W].Range && x.ELDistance(pos) < 200
                                && !x.IsMe)
                            .OrderByDescending(i => i.ELDistance(Player))
                            .ToList()
                            .FirstOrDefault();

                    if (wardJumpableChampion != null && WStage == WCastStage.First)
                    {
                        if (500 >= Utils.TickCount - Wcasttime || WStage != WCastStage.First)
                        {
                            return;
                        }

                        CastW(wardJumpableChampion);
                        return;
                    }
                }
                if (minions)
                {
                    var wardJumpableMinion =
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(
                                m =>
                                m.IsAlly && m.ELDistance(Player) < spells[Spells.W].Range && m.ELDistance(pos) < 200
                                && !m.Name.ToLower().Contains("ward"))
                            .OrderByDescending(i => i.ELDistance(Player))
                            .ToList()
                            .FirstOrDefault();

                    if (wardJumpableMinion != null && WStage == WCastStage.First)
                    {
                        if (500 >= Utils.TickCount - Wcasttime || WStage != WCastStage.First)
                        {
                            return;
                        }

                        CastW(wardJumpableMinion);
                        return;
                    }
                }
            }

            var isWard = false;

            var wardObject =
                ObjectManager.Get<Obj_AI_Base>()
                    .Where(o => o.IsAlly && o.Name.ToLower().Contains("ward") && o.ELDistance(JumpPos) < 200)
                    .ToList()
                    .FirstOrDefault();

            if (wardObject != null)
            {
                isWard = true;
                if (500 >= Utils.TickCount - Wcasttime || WStage != WCastStage.First)
                {
                    return;
                }

                CastW(wardObject);
            }

            if (!isWard && castWardAgain)
            {
                var ward = FindBestWardItem();
                if (ward == null)
                {
                    return;
                }

                if (spells[Spells.W].IsReady() && WState && LastWard + 400 < Utils.TickCount)
                {
                    Player.Spellbook.CastSpell(ward.SpellSlot, JumpPos.To3D());
                    lastWardPos = JumpPos.To3D();
                    LastWard = Utils.TickCount;
                }
            }
        }

        private static void WardjumpToMouse()
        {
            WardJump(
                Game.CursorPos,
                getCheckBoxItem(InitMenu.wardjumpMenu, "ElLeeSin.Wardjump.Mouse"),
                false,
                false,
                getCheckBoxItem(InitMenu.wardjumpMenu, "ElLeeSin.Wardjump.Minions"),
                getCheckBoxItem(InitMenu.wardjumpMenu, "ElLeeSin.Wardjump.Champions"));
        }

        #endregion
    }
}