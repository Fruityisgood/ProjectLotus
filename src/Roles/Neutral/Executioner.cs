#nullable enable
using System.Collections.Generic;
using System.Linq;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Factions;
using TOHTOR.GUI;
using TOHTOR.Options;
using UnityEngine;
using TOHTOR.Roles.Internals.Attributes;
using TOHTOR.Roles.Neutral;
using TOHTOR.Victory.Conditions;
using VentLib.Options.Game;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;

namespace TOHTOR.Roles;

public class Executioner : CustomRole
{
    private bool canTargetImpostors;
    private bool canTargetNeutrals;
    private int roleChangeWhenTargetDies;

    private PlayerControl? target;

    /*[DynElement(UI.Misc)]
    private string TargetDisplay() => target == null ? "" : Color.red.Colorize("Target: ") + Color.white.Colorize(target.GetRawName());*/

    public override void OnGameStart()
    {
        target = Game.GetAllPlayers().Where(p =>
        {
            if (p.PlayerId == MyPlayer.PlayerId) return false;
            Faction[] factions = p.GetCustomRole().Factions;
            if (!canTargetImpostors && factions.IsImpostor()) return false;
            return canTargetNeutrals || !factions.Contains(Faction.Solo);
        }).ToList().GetRandom();
        DynamicName targetName = target.GetDynamicName();
        targetName.AddRule(GameState.Roaming, UI.Name, new DynamicString(RoleColor.Colorize("{0}")), MyPlayer.PlayerId);
        targetName.AddRule(GameState.InMeeting, UI.Name, new DynamicString(RoleColor.Colorize("{0}")), MyPlayer.PlayerId);
    }

    [RoleAction(RoleActionType.OtherExiled)]
    private void CheckExecutionerWin(PlayerControl exiled)
    {
        if (target == null || target.PlayerId != exiled.PlayerId) return;
        List<PlayerControl> winners = new() { MyPlayer };
        if (target.GetCustomRole() is Jester) winners.Add(target);
        ManualWin win = new(winners, WinReason.SoloWinner);
        win.Activate();
    }

    [RoleAction(RoleActionType.AnyDeath)]
    private void CheckChangeRole(PlayerControl dead)
    {
        if (roleChangeWhenTargetDies == 0 || target == null || target.PlayerId != dead.PlayerId) return;
        switch ((ExeRoleChange)roleChangeWhenTargetDies)
        {
            case ExeRoleChange.Jester:
                MyPlayer.RpcSetCustomRole(CustomRoleManager.Static.Jester);
                break;
            case ExeRoleChange.Opportunist:
                MyPlayer.RpcSetCustomRole(CustomRoleManager.Static.Opportunist);
                break;
            case ExeRoleChange.SchrodingerCat:
                MyPlayer.RpcSetCustomRole(CustomRoleManager.Static.SchrodingerCat);
                break;
            case ExeRoleChange.Crewmate:
                MyPlayer.RpcSetCustomRole(CustomRoleManager.Static.Crewmate);
                break;
            case ExeRoleChange.None:
            default:
                break;
        }

        target = null;
        MyPlayer.GetDynamicName().Render();
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
        .Tab(DefaultTabs.NeutralTab)
            .SubOption(sub => sub
                .Name("Can Target Impostors")
                .Bind(v => canTargetImpostors = (bool)v)
                .AddOnOffValues(false).Build())
            .SubOption(sub => sub
                .Name("Can Target Neutrals")
                .Bind(v => canTargetNeutrals = (bool)v)
                .AddOnOffValues(false).Build())
            .SubOption(sub => sub
                .Name("Role Change When Target Dies")
                .Bind(v => roleChangeWhenTargetDies = (int)v)
                .Value(v => v.Text("Jester").Value(1).Color(new Color(0.93f, 0.38f, 0.65f)).Build())
                .Value(v => v.Text("Opportunist").Value(2).Color(Color.green).Build())
                .Value(v => v.Text("Schrodinger's Cat").Value(3).Color(Color.black).Build())
                .Value(v => v.Text("Crewmate").Value(4).Color(new Color(0.71f, 0.94f, 1f)).Build())
                .Value(v => v.Text("Off").Value(0).Color(Color.red).Build())
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier.RoleColor(new Color(0.55f, 0.17f, 0.33f));

    private enum ExeRoleChange
    {
        None,
        Jester,
        Opportunist,
        SchrodingerCat,
        Crewmate
    }
}