using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CSTimer = CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Menu;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace LastRequest;

public class LastRequest : BasePlugin
{
    public override string ModuleVersion => "0.0.1";
    public override string ModuleName => "LastRequest";
    public override string ModuleAuthor => "serk@n";

    public override void Load(bool hotReload)
    {
        AddCommand("lr", "Last Request", Command_LastRequest);
        RegisterEventHandler<EventPlayerDeath>((@event, info) =>
        {
            if (!DuelloActive)
            {
                return HookResult.Continue;
            }

            CCSPlayerController attacker = @event.Attacker;
            if (attacker == null)
            {
                return HookResult.Continue;
            }

            CCSPlayerController victim = @event.Userid;

            if (DuelistSlotAccepter == victim.Slot && DuelistSlotChallenger == attacker.Slot)
            {
                return HookResult.Continue;
            }

            Server.PrintToChatAll($"{victim.PlayerName} ile {attacker.PlayerName} arasýndaki düelloyu {attacker.PlayerName} kazandý!");

            DuelistSlotChallenger = InValidSlot;
            DuelistSlotAccepter = InValidSlot;

            return HookResult.Continue;
        });

        RegisterEventHandler<EventRoundStart>((@event, info) =>
        {
            DuelistSlotChallenger = InValidSlot;
            DuelistSlotAccepter = InValidSlot;

            return HookResult.Continue;
        });

        RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            CCSPlayerController player = @event.Userid;
            if (player == null)
            {
                return HookResult.Continue;
            }

            if (DuelistSlotAccepter == player.Slot || DuelistSlotChallenger == player.Slot)
            {
                Server.PrintToChatAll($"{player.PlayerName} düelloyu terk etti!");
            }

            return HookResult.Continue;
        });
    }

    public const int InValidSlot = -3;
    public int DuelistSlotChallenger { get; set; } = InValidSlot;
    public int DuelistSlotAccepter { get; set; } = InValidSlot;
    public bool DuelloActive { get; set; } = false;

    public static void SetColor(CCSPlayerController player, Color color)
    {
        CCSPlayerPawn? pawn = player.PlayerPawn.Value;

        if (pawn != null && player.IsValid)
        {
            pawn.RenderMode = RenderMode_t.kRenderTransColor;
            pawn.Render = color;
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        }
    }

    public void Command_LastRequest(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null)
        {
            return;
        }

        if (player.TeamNum != 2)
        {
            player.PrintToChat("Bu komutu sadece mahkumlar kullanabilir.");
            return;
        }

        if (Utilities.GetPlayers().Where(p => p.IsValid && p.PawnIsAlive).Count() != 1)
        {
            player.PrintToChat("Bu komutu yalnýzca sona kalan mahkum kullanabilir.");
            return;
        }

        if (commandInfo.ArgCount == 0)
        {
            return;
        }

        var target = commandInfo.GetArgTargetResult(1);

        if (target.Players.Count == 0)
        {
            player.PrintToChat("Böyle bir oyuncu bulunmamaktadýr.");
            return;
        }
        else if (target.Players.Count > 1)
        {
            player.PrintToChat("Birden fazla oyuncu bulunmaktadýr.");
            return;
        }

        player.RemoveWeapons();
        player.GiveNamedItem(CsItem.Deagle);
        player.PawnHealth = 100;
        SetColor(player, Color.DarkRed);

        DuelistSlotChallenger = player.Slot;
        DuelloActive = true;

        foreach (var targetpawn in target)
        {
            targetpawn.RemoveWeapons();
            targetpawn.GiveNamedItem(CsItem.Deagle);
            targetpawn.PawnHealth = 100;
            SetColor(targetpawn, Color.DarkBlue);

            DuelistSlotAccepter = targetpawn.Slot;
        }
    }
}