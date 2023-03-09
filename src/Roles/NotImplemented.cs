using VentLib.Logging;

namespace TOHTOR.Roles;

public class NotImplemented: CustomRole
{
    protected override RoleModifier Modify(RoleModifier roleModifier)
    {
        VentLogger.Warn($"{this.RoleName} Not Implemented Yet", "RoleImplementation");
        return roleModifier;
    }
}