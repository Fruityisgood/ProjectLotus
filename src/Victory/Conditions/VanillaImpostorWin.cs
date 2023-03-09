using System.Collections.Generic;
using System.Linq;
using TOHTOR.API;
using TOHTOR.Factions;

namespace TOHTOR.Victory.Conditions;

public class VanillaImpostorWin: IFactionWinCondition
{
    private static readonly List<Faction> ImpostorFaction = new() { Faction.Impostors };
    public bool IsConditionMet(out List<Faction> factions)
    {
        factions = ImpostorFaction;
        List<PlayerControl> aliveImpostors = Game.GetAliveImpostors();
        HashSet<byte> byteImpostors = aliveImpostors.Select(p => p.PlayerId).ToHashSet();

        List<PlayerControl> others = Game.GetAlivePlayers().Where(p => !byteImpostors.Contains(p.PlayerId)).ToList();

        return aliveImpostors.Count >= others.Count;
    }

    public WinReason GetWinReason() => Conditions.WinReason.FactionLastStanding;
}