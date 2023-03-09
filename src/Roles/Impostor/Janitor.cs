using AmongUs.GameOptions;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.GUI;
using TOHTOR.Options;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using UnityEngine;

namespace TOHTOR.Roles;

public class Janitor: Impostor
{
    private Cooldown cleanCooldown;

    [RoleAction(RoleActionType.AttemptKill)]
    public new bool TryKill(PlayerControl target)
    {
        cleanCooldown.Start(DesyncOptions.OriginalHostOptions.GetFloat(FloatOptionNames.KillCooldown));
        return base.TryKill(target);
    }

    [RoleAction(RoleActionType.SelfReportBody)]
    private void JanitorCleanBody(GameData.PlayerInfo target, ActionHandle handle)
    {
        if (cleanCooldown.NotReady()) return;
        handle.Cancel();
        cleanCooldown.Start();
        SyncOptions();

        foreach (DeadBody deadBody in Object.FindObjectsOfType<DeadBody>())
            if (deadBody.ParentId == target.Object.PlayerId)
            {
                Game.GameStates.UnreportableBodies.Add(target.PlayerId);
                Object.Destroy(deadBody.gameObject);
            }

        MyPlayer.RpcGuardAndKill(MyPlayer);
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .OptionOverride(Override.KillCooldown, () => cleanCooldown.Duration * 2, () => cleanCooldown.NotReady());
}