using EloBuddy;
using ExorAIO.Utilities;
using LeagueSharp;
using LeagueSharp.SDK;
using LeagueSharp.SDK.Core.Utils;

namespace ExorAIO.Champions.Vayne
{
    /// <summary>
    ///     The logics class.
    /// </summary>
    internal partial class Logics
    {
        /// <summary>
        ///     Called on do-cast.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The args.</param>
        public static void Weaving(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!(args.Target is AIHeroClient) ||
                Invulnerable.Check(args.Target as AIHeroClient))
            {
                return;
            }

            /// <summary>
            ///     The Q Weaving Logic.
            /// </summary>
            if (Vars.Q.IsReady() &&
                Vars.getCheckBoxItem(Vars.QMenu, "combo"))
            {
                if (Vars.getCheckBoxItem(Vars.MiscMenu, "wstacks"))
                {
                    if ((args.Target as AIHeroClient).GetBuffCount("vaynesilvereddebuff") != 1)
                    {
                        return;
                    }
                }

                if (!Vars.getCheckBoxItem(Vars.MiscMenu, "alwaysq"))
                {
                    if (GameObjects.Player.Distance(Game.CursorPos) > Vars.AARange &&
                        (args.Target as AIHeroClient).Distance(
                            GameObjects.Player.Position.LSExtend(Game.CursorPos, Vars.Q.Range - Vars.AARange)) < Vars.AARange)
                    {
                        Vars.Q.Cast(Game.CursorPos);
                    }
                }
                else
                {
                    Vars.Q.Cast(Game.CursorPos);
                }
            }
        }
    }
}