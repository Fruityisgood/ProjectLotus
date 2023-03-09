using AmongUs.GameOptions;
using VentLib.Logging;
using VentLib.Options.Game;

namespace TOHTOR.Roles;

public class ImpostorGhost : GuardianAngel
{
    protected override RoleModifier Modify(RoleModifier roleModifier)
    {
        base.Modify(roleModifier)
         .VanillaRole(RoleTypes.CrewmateGhost);
        VentLogger.Warn($"{this.RoleName} Not Implemented Yet", "RoleImplementation");
        return roleModifier;
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream)
    {
        return new GameOptionBuilder();
    }
}