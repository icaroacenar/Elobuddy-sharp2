using System;
using System.Linq;
using EloBuddy;
using LeagueSharp.SDK;
using LeagueSharp.SDK.Core.Utils;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK;

namespace NabbActivator
{
    /// <summary>
    ///     The activator class.
    /// </summary>
    internal partial class Activator
    {
        /// <summary>
        ///     Loads the smite.
        /// </summary>
        public static void SmiteInit()
        {
            Drawing.OnDraw += delegate
            {
                /// <summary>
                ///     The Smite Logics.
                /// </summary>
                if (Vars.Smite != null &&
                    Vars.Smite.IsReady() &&
                    Vars.Smite.Slot != SpellSlot.Unknown)
                {
                    if (!Vars.KeysMenu["smite"].Cast<KeyBind>().CurrentValue)
                    {
                        return;
                    }

                    /// <summary>
                    ///     The Jungle Smite Logic.
                    /// </summary>
                    foreach (var minion in Targets.JungleMinions.Where(
                        m =>
                            m.IsValidTarget(Vars.Smite.Range)))
                    {
                        if (minion.Health >
                                GameObjects.Player.GetBuffCount(GameObjects.Player.Buffs.FirstOrDefault(
                                    b =>
                                        b.Name.ToLower().Contains("smitedamagetracker")).Name))
                        {
                            return;
                        }

                        if (Vars.SmiteMiscMenu["limit"].Cast<CheckBox>().CurrentValue)
                        {
                            if (!minion.CharData.BaseSkinName.Equals("SRU_Baron") &&
                                !minion.CharData.BaseSkinName.Equals("SRU_RiftHerald") &&
                                !minion.CharData.BaseSkinName.Contains("SRU_Dragon"))
                            {
                                return;
                            }
                        }

                        if (Vars.SmiteMiscMenu["stacks"].Cast<CheckBox>().CurrentValue)
                        {
                            if (GameObjects.Player.Spellbook.GetSpell(Vars.Smite.Slot).Ammo == 1)
                            {
                                if (!minion.CharData.BaseSkinName.Equals("SRU_Baron") &&
                                    !minion.CharData.BaseSkinName.Equals("SRU_RiftHerald") &&
                                    !minion.CharData.BaseSkinName.Contains("SRU_Dragon"))
                                {
                                    return;
                                }
                            }
                        }

                        Vars.Smite.CastOnUnit(minion);
                    }
                }
            };
        }

        /// <summary>
        ///     Called when the game updates itself.
        /// </summary>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
        public static void Spells(EventArgs args)
        {
            if (!Vars.TypesMenu["spells"].Cast<CheckBox>().CurrentValue)
            {
                return;
            }

            /// <summary>
            ///     The Remove Scurvy Logic.
            /// </summary>
            if (GameObjects.Player.ChampionName.Equals("Gangplank"))
            {
                if (Vars.W.IsReady() &&
                    Bools.ShouldCleanse(GameObjects.Player))
                {
                    DelayAction.Add(
                        Vars.TypesMenu["cleansers"].Cast<Slider>().CurrentValue, () =>
{
    Vars.W.Cast();
});
                }
            }

            /// <summary>
            ///     The Cleanse Logic.
            /// </summary>
            if (SpellSlots.Cleanse.IsReady())
            {
                if (Bools.ShouldCleanse(GameObjects.Player))
                {
                    DelayAction.Add(
                        Vars.TypesMenu["cleansers"].Cast<Slider>().CurrentValue, () =>
                        {
                            GameObjects.Player.Spellbook.CastSpell(SpellSlots.Cleanse);
                        });
                }
            }

            /// <summary>
            ///     The Clarity Logic.
            /// </summary>
            if (SpellSlots.Clarity.IsReady())
            {
                if (GameObjects.AllyHeroes.Count(a => a.ManaPercent <= 60) >= 3)
                {
                    GameObjects.Player.Spellbook.CastSpell(SpellSlots.Clarity);
                }
            }

            /// <summary>
            ///     The Ignite Logic.
            /// </summary>
            if (SpellSlots.Ignite.IsReady())
            {
                foreach (var target in GameObjects.EnemyHeroes.Where(t => t.LSIsValidTarget(600f)))
                {
                    if (Vars.GetIgniteDamage > target.Health ||
                        Health.GetPrediction(target, (int)(1000 + Game.Ping / 2f)) <= 0)
                    {
                        GameObjects.Player.Spellbook.CastSpell(SpellSlots.Ignite, target);
                    }
                }
            }

            /// <summary>
            ///     The Barrier Logic.
            /// </summary>
            if (SpellSlots.Barrier.IsReady())
            {
                if (GameObjects.Player.CountEnemyHeroesInRange(700f) > 0 &&
                    Health.GetPrediction(GameObjects.Player, (int)(1000 + Game.Ping / 2f)) <= GameObjects.Player.MaxHealth / 6)
                {
                    GameObjects.Player.Spellbook.CastSpell(SpellSlots.Barrier);
                    return;
                }
            }

            /// <summary>
            ///     The Heal Logic.
            /// </summary>
            if (SpellSlots.Heal.IsReady())
            {
                if (GameObjects.Player.CountEnemyHeroesInRange(850f) > 0 &&
                    Health.GetPrediction(GameObjects.Player, (int)(1000 + Game.Ping / 2f)) <= GameObjects.Player.MaxHealth / 6)
                {
                    GameObjects.Player.Spellbook.CastSpell(SpellSlots.Heal);
                }
                else if (Vars.TypesMenu["heal1"].Cast<CheckBox>().CurrentValue)
                {
                    foreach (var ally in GameObjects.AllyHeroes.Where(
                        a =>
                            a.LSIsValidTarget(850f, false) &&
                            a.CountEnemyHeroesInRange(850f) > 0 &&
                            Health.GetPrediction(a, (int)(1000 + Game.Ping / 2f)) <= a.MaxHealth / 6))
                    {
                        GameObjects.Player.Spellbook.CastSpell(SpellSlots.Heal, ally);
                    }
                }
            }


            /// <summary>
            ///     The Smite Logics.
            /// </summary>
            if (Vars.Smite.IsReady() &&
                Vars.Smite.Slot != SpellSlot.Unknown)
            {
                if (!Vars.KeysMenu["smite"].Cast<KeyBind>().CurrentValue)
                {
                    return;
                }


                /// <summary>
                ///     The Combo Smite Logic.
                /// </summary>
                if (Vars.SmiteMiscMenu["combo"].Cast<CheckBox>().CurrentValue)
                {
                    if (Orbwalker.LastTarget as AIHeroClient != null)
                    {
                        Vars.Smite.CastOnUnit(Orbwalker.LastTarget as AIHeroClient);
                    }
                }

                /// <summary>
                ///     The Killsteal Smite Logic.
                /// </summary>
                if (Vars.SmiteMiscMenu["killsteal"].Cast<CheckBox>().CurrentValue)
                {
                    if (GameObjects.Player.HasBuff("smitedamagetrackerstalker") ||
                        GameObjects.Player.HasBuff("smitedamagetrackerskirmisher"))
                    {
                        if (Vars.SmiteMiscMenu["stacks"].Cast<CheckBox>().CurrentValue)
                        {
                            if (GameObjects.Player.Spellbook.GetSpell(Vars.Smite.Slot).Ammo == 1)
                            {
                                return;
                            }
                        }

                        foreach (var target in GameObjects.EnemyHeroes.Where(t => t.LSIsValidTarget(Vars.Smite.Range)))
                        {
                            if (Vars.GetChallengingSmiteDamage > target.Health &&
                                GameObjects.Player.HasBuff("smitedamagetrackerstalker"))
                            {
                                Vars.Smite.CastOnUnit(target);
                            }
                            else if (Vars.GetChallengingSmiteDamage > target.Health &&
                                GameObjects.Player.HasBuff("smitedamagetrackerskirmisher"))
                            {
                                Vars.Smite.CastOnUnit(target);
                            }
                        }
                    }
                }
            }

            if (!Targets.Target.LSIsValidTarget())
            {
                return;
            }

            /// <summary>
            ///     The Exhaust Logic.
            /// </summary>
            if (SpellSlots.Exhaust.IsReady())
            {
                foreach (var ally in GameObjects.AllyHeroes.Where(
                    a =>
                        a.Distance(Targets.Target) <= 650f &&
                        Health.GetPrediction(a, (int)(1000 + Game.Ping / 2f)) <= a.MaxHealth / 6))
                {
                    GameObjects.Player.Spellbook.CastSpell(SpellSlots.Exhaust, Targets.Target);
                }
            }
        }
    }
}