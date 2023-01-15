using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.CoreScripts;
using HarmonyLib;
using Hazel;
using TownOfHost.Extensions;
using TownOfHost.RPC;
using UnityEngine;
using VentLib.Logging;
using static TownOfHost.Managers.Translator;

namespace TownOfHost.Patches.Chat;

//[HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
public class ChatPatch
{
    public static Dictionary<string, ChatController> ChatHistory = new();
    /* public static void Postfix(ChatController __instance)
     {
         ChatHistory.Add(__instance.TextArea.text, __instance);
         // return false;
     }
     */
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
class ChatCommands
{
    public static List<string> ChatHistory = new();
    public static bool Prefix(ChatController __instance)
    {
        if (__instance.TextArea.text == "") return false;
        __instance.TimeSinceLastMessage = 3f;
        var text = __instance.TextArea.text;
        if (ChatHistory.Count == 0 || ChatHistory[^1] != text) ChatHistory.Add(text);
        ChatControllerUpdatePatch.CurrentHistorySelection = ChatHistory.Count;
        string[] args = text.Split(' ');
        string subArgs = "";
        var canceled = false;
        var cancelVal = "";
        TOHPlugin.isChatCommand = true;
        VentLogger.Old(text, "SendChat");
        ChatPatch.ChatHistory.Remove(__instance.TextArea.text);
        switch (args[0])
        {
            case "/dump":
                canceled = true;
                Utils.DumpLog();
                break;
            case "/v":
            case "/version":
                canceled = true;
                string version_text = "";
                foreach (var kvp in TOHPlugin.playerVersion.OrderBy(pair => pair.Key))
                {
                    version_text += $"{kvp.Key}:{Utils.GetPlayerById(kvp.Key)?.Data?.PlayerName}:{kvp.Value.forkId}/{kvp.Value.version}({kvp.Value.tag})\n";
                }
                if (version_text != "") HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, version_text);
                break;
            default:
                TOHPlugin.isChatCommand = false;
                break;
        }
        if (AmongUsClient.Instance.AmHost)
        {
            TOHPlugin.isChatCommand = true;
            switch (args[0])
            {
                case "/win":
                case "/winner":
                    canceled = true;
                    break;

                case "/l":
                case "/lastresult":
                    canceled = true;
                    Utils.ShowLastResult();
                    break;

                case "/r":
                case "/rename":
                    canceled = true;
                    PlayerControl.LocalPlayer.RpcSetName(args[1]);
                    break;

                case "/hn":
                case "/hidename":
                    canceled = true;
                    TOHPlugin.HideName.Value = args.Length > 1 ? args.Skip(1).Join(delimiter: " ") : TOHPlugin.HideName.DefaultValue.ToString();
                    GameStartManagerPatch.GameStartManagerStartPatch.HideName.text =
                        ColorUtility.TryParseHtmlString(TOHPlugin.HideColor.Value, out _)
                            ? $"<color={TOHPlugin.HideColor.Value}>{TOHPlugin.HideName.Value}</color>"
                            : $"<color={TOHPlugin.ModColor}>{TOHPlugin.HideName.Value}</color>";
                    break;

                case "/n":
                case "/now":
                    canceled = true;
                    subArgs = args.Length < 2 ? "" : args[1];
                    switch (subArgs)
                    {
                        case "r":
                        case "roles":
                            Utils.ShowActiveRoles();
                            break;
                        default:
                            Utils.ShowActiveSettings();
                            break;
                    }
                    break;

                case "/dis":
                    canceled = true;
                    subArgs = args.Length < 2 ? "" : args[1];
                    switch (subArgs)
                    {
                        case "crewmate":
                            GameManager.Instance.enabled = false;
                            GameManager.Instance.RpcEndGame(GameOverReason.HumansDisconnect, false);
                            break;

                        case "impostor":
                            GameManager.Instance.enabled = false;
                            GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
                            break;

                        default:
                            __instance.AddChat(PlayerControl.LocalPlayer, "crewmate | impostor");
                            cancelVal = "/dis";
                            break;
                    }
                    ShipStatus.Instance.RpcRepairSystem(SystemTypes.Admin, 0);
                    break;

                case "/h":
                case "/help":
                    canceled = true;
                    subArgs = args.Length < 2 ? "" : args[1];
                    switch (subArgs)
                    {
                        case "r":
                        case "roles":
                            subArgs = args.Length < 3 ? "" : args[2];
                            GetRolesInfo(subArgs);
                            break;

                        case "a":
                        case "addons":
                            subArgs = args.Length < 3 ? "" : args[2];
                            switch (subArgs)
                            {
                                case "lastimpostor":
                                case "limp":
                                    Utils.SendMessage(Utils.GetRoleName(Roles.LastImpostor.Ref<Roles.LastImpostor>()) + GetString("LastImpostorInfoLong"));
                                    break;

                                default:
                                    Utils.SendMessage($"{GetString("Command.h_args")}:\n lastimpostor(limp)");
                                    break;
                            }
                            break;

                        case "m":
                        case "modes":
                            subArgs = args.Length < 3 ? "" : args[2];
                            switch (subArgs)
                            {
                                case "hideandseek":
                                case "has":
                                    Utils.SendMessage(GetString("HideAndSeekInfo"));
                                    break;

                                case "nogameend":
                                case "nge":
                                    Utils.SendMessage(GetString("NoGameEndInfo"));
                                    break;

                                case "syncbuttonmode":
                                case "sbm":
                                    Utils.SendMessage(GetString("SyncButtonModeInfo"));
                                    break;

                                case "randommapsmode":
                                case "rmm":
                                    Utils.SendMessage(GetString("RandomMapsModeInfo"));
                                    break;

                                default:
                                    Utils.SendMessage($"{GetString("Command.h_args")}:\n hideandseek(has), nogameend(nge), syncbuttonmode(sbm), randommapsmode(rmm)");
                                    break;
                            }
                            break;


                        case "n":
                        case "now":
                            Utils.ShowActiveSettingsHelp();
                            break;

                        default:
                            Utils.ShowHelp();
                            break;
                    }
                    break;

                case "/m":
                case "/myrole":
                    canceled = true;
                    var role = PlayerControl.LocalPlayer.GetCustomRole();
                    if (GameStates.IsInGame)
                        HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, GetString(role.ToString()) + PlayerControl.LocalPlayer.GetCustomRole().Description);
                    break;

                case "/t":
                case "/template":
                    canceled = true;
                    if (args.Length > 1) TemplateManager.SendTemplate(args[1]);
                    else HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, $"{GetString("ForExample")}:\n{args[0]} test");
                    break;

                case "/mw":
                case "/messagewait":
                    canceled = true;
                    if (args.Length > 1 && int.TryParse(args[1], out int sec))
                    {
                        TOHPlugin.MessageWait.Value = sec;
                        Utils.SendMessage(string.Format(GetString("Message.SetToSeconds"), sec), 0);
                    }
                    else Utils.SendMessage($"{GetString("Message.MessageWaitHelp")}\n{GetString("ForExample")}:\n{args[0]} 3", 0);
                    break;

                case "/say":
                    canceled = true;
                    if (args.Length > 1)
                        Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<color=#ff0000>{GetString("MessageFromTheHost")}</color>");
                    break;

                case "/exile":
                    canceled = true;
                    if (args.Length < 2 || !int.TryParse(args[1], out int id)) break;
                    Utils.GetPlayerById(id)?.RpcExileV2();
                    break;

                case "/kill":
                    canceled = true;
                    if (args.Length < 2 || !int.TryParse(args[1], out int id2)) break;
                    Utils.GetPlayerById(id2)?.RpcMurderPlayer(Utils.GetPlayerById(id2));
                    break;

                default:
                    TOHPlugin.isChatCommand = false;
                    break;
            }
        }
        if (canceled)
        {
            VentLogger.Old("Command Canceled", "ChatCommand");
            __instance.TextArea.Clear();
            __instance.TextArea.SetText(cancelVal);
            __instance.quickChatMenu.ResetGlyphs();
        }
        return !canceled;
    }

    public static void GetRolesInfo(string role)
    {
        var roleList = new Dictionary<CustomRoles, string>
        {
            //GM
            { CustomRoles.GM, "gm" },
            //Impostor役職
            { (CustomRoles)(-1), $"== {GetString("Impostor")} ==" }, //区切り用
            { CustomRoles.BountyHunter, "bo" },
            { CustomRoles.EvilTracker,"et" },
            { CustomRoles.FireWorks, "fw" },
            { CustomRoles.Mare, "ma" },
            { CustomRoles.Mafia, "mf" },
            { CustomRoles.SerialKiller, "sk" },
            //{ CustomRoles.ShapeMaster, "sha" },
            { CustomRoles.TimeThief, "tt"},
            { CustomRoles.Sniper, "snp" },
            { CustomRoles.Puppeteer, "pup" },
            { CustomRoles.Vampire, "va" },
            { CustomRoles.Warlock, "wa" },
            { CustomRoles.Witch, "wi" },
            //Madmate役職
            { (CustomRoles)(-2), $"== {GetString("Madmate")} ==" }, //区切り用
            { CustomRoles.MadGuardian, "mg" },
            { CustomRoles.Madmate, "mm" },
            { CustomRoles.MadSnitch, "msn" },
            { CustomRoles.SKMadmate, "sm" },
            //両陣営役職
            { (CustomRoles)(-3), $"== {GetString("Impostor")} or {GetString("Crewmate")} ==" }, //区切り用
            { CustomRoles.Watcher, "wat" },
            //Crewmate役職
            { (CustomRoles)(-4), $"== {GetString("Crewmate")} ==" }, //区切り用
            { CustomRoles.Bait, "ba" },
            { CustomRoles.Dictator, "dic" },
            { CustomRoles.Doctor, "doc" },
            { CustomRoles.Lighter, "li" },
            { CustomRoles.Mayor, "my" },
            { CustomRoles.SabotageMaster, "sa" },
            { CustomRoles.Seer,"se" },
            { CustomRoles.Sheriff, "sh" },
            { CustomRoles.Snitch, "sn" },
            { CustomRoles.SpeedBooster, "sb" },
            { CustomRoles.Trapper, "tra" },
            //Neutral役職
            { (CustomRoles)(-5), $"== {GetString("Neutral")} ==" }, //区切り用
            { CustomRoles.Arsonist, "ar" },
            { CustomRoles.Egoist, "eg" },
            { CustomRoles.Executioner, "exe" },
            { CustomRoles.Jester, "je" },
            { CustomRoles.Opportunist, "op" },
            { CustomRoles.SchrodingerCat, "sc" },
            { CustomRoles.Terrorist, "te" },
            { CustomRoles.Jackal, "jac" },
            //属性
            { (CustomRoles)(-6), $"== {GetString("Addons")} ==" }, //区切り用
            {CustomRoles.Lovers, "lo" },
            //HAS
            { (CustomRoles)(-7), $"== {GetString("HideAndSeek")} ==" }, //区切り用
            { CustomRoles.HASFox, "hfo" },
            { CustomRoles.HASTroll, "htr" },

        };
        var msg = "";
        var rolemsg = $"{GetString("Command.h_args")}";
        foreach (var r in roleList)
        {
            var roleName = r.Key.ToString();
            var roleShort = r.Value;

            if (String.Compare(role, roleName, true) == 0 || String.Compare(role, roleShort, true) == 0)
            {
                Utils.SendMessage(GetString(roleName) + GetString($"{roleName}InfoLong"));
                return;
            }

            var roleText = $"{roleName.ToLower()}({roleShort.ToLower()}), ";
            if ((int)r.Key < 0)
            {
                msg += rolemsg + "\n" + roleShort + "\n";
                rolemsg = "";
            }
            else if ((rolemsg.Length + roleText.Length) > 40)
            {
                msg += rolemsg + "\n";
                rolemsg = roleText;
            }
            else
            {
                rolemsg += roleText;
            }
        }
        msg += rolemsg;
        Utils.SendMessage(msg);
    }
    public static void OnReceiveChat(PlayerControl player, string text)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        string[] args = text.Split(' ');
        string cancelVal = "";
        string subArgs = "";
        ChatController controller = null;
        if (text != "")
        {
            if (ChatPatch.ChatHistory.ContainsKey(text))
                controller = ChatPatch.ChatHistory[text];
        }
        switch (args[0])
        {
            case "/test":
                if (controller == null) break;
                VentLogger.Old("Command Canceled", "ChatCommand");
                controller.TextArea.Clear();
                controller.TextArea.SetText(cancelVal);
                controller.quickChatMenu.ResetGlyphs();
                break;
            case "/l":
            case "/lastresult":
                Utils.ShowLastResult(player.PlayerId);
                break;

            case "/n":
            case "/now":
                subArgs = args.Length < 2 ? "" : args[1];
                switch (subArgs)
                {
                    case "r":
                    case "roles":
                        Utils.ShowActiveRoles(player.PlayerId);
                        break;

                    default:
                        Utils.ShowActiveSettings(player.PlayerId);
                        break;
                }
                break;

            case "/h":
            case "/help":
                subArgs = args.Length < 2 ? "" : args[1];
                switch (subArgs)
                {
                    case "n":
                    case "now":
                        Utils.ShowActiveSettingsHelp(player.PlayerId);
                        break;
                }
                break;

            case "/m":
            case "/myrole":
                var role = player.GetCustomRole();
                if (GameStates.IsInGame)
                    Utils.SendMessage(GetString(role.ToString()) + player.GetCustomRole().Description, player.PlayerId);
                break;

            case "/t":
            case "/template":
                if (args.Length > 1) TemplateManager.SendTemplate(args[1], player.PlayerId);
                else Utils.SendMessage($"{GetString("ForExample")}:\n{args[0]} test", player.PlayerId);
                break;

            default:
                break;
        }
    }
}
[HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
class ChatUpdatePatch
{
    public static bool DoBlockChat = false;
    public static void Postfix(ChatController __instance)
    {
        if (!AmongUsClient.Instance.AmHost || TOHPlugin.MessagesToSend.Count < 1 || (TOHPlugin.MessagesToSend[0].Item2 == byte.MaxValue && TOHPlugin.MessageWait.Value > __instance.TimeSinceLastMessage)) return;
        if (DoBlockChat) return;
        var player = PlayerControl.AllPlayerControls.ToArray().OrderBy(x => x.PlayerId).FirstOrDefault(x => !x.Data.IsDead);
        if (player == null) return;
        (string msg, byte sendTo, string title) = TOHPlugin.MessagesToSend[0];
        TOHPlugin.MessagesToSend.RemoveAt(0);
        int clientId = sendTo == byte.MaxValue ? -1 : Utils.GetPlayerById(sendTo).GetClientId();
        var name = player.Data.PlayerName;
        if (clientId == -1)
        {
            player.SetName(title);
            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
            player.SetName(name);
        }
        var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
        writer.StartMessage(clientId);
        writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
            .Write(title)
            .EndRpc();
        writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
            .Write(msg)
            .EndRpc();
        writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
            .Write(player.Data.PlayerName)
            .EndRpc();
        writer.EndMessage();
        writer.SendMessage();
        __instance.TimeSinceLastMessage = 0f;
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
class AddChatPatch
{
    public static void Postfix(string chatText)
    {
        switch (chatText)
        {
            default:
                break;
        }
        if (!AmongUsClient.Instance.AmHost) return;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSendChat))]
class RpcSendChatPatch
{
    public static bool Prefix(PlayerControl __instance, string chatText, ref bool __result)
    {
        if (string.IsNullOrWhiteSpace(chatText))
        {
            __result = false;
            return false;
        }
        int return_count = PlayerControl.LocalPlayer.name.Count(x => x == '\n');
        chatText = new StringBuilder(chatText).Insert(0, "\n", return_count).ToString();
        if (AmongUsClient.Instance.AmClient && DestroyableSingleton<HudManager>.Instance)
            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(__instance, chatText);
        if (chatText.IndexOf("who", StringComparison.OrdinalIgnoreCase) >= 0)
            DestroyableSingleton<Telemetry>.Instance.SendWho();
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(__instance.NetId, (byte)RpcCalls.SendChat, SendOption.None);
        messageWriter.Write(chatText);
        messageWriter.EndMessage();
        __result = true;
        return false;
    }
}