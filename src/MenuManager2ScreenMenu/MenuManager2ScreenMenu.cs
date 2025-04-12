using System.Collections.Generic;
using System.Threading;

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;

using MenuManager;

namespace CsGals;

[MinimumApiVersion(314)]
public class MenuManager2ScreenMenu : BasePlugin
{
	public override string ModuleName => "MenuManager2Screen2Menu";
	public override string ModuleVersion => Verlite.Version.Full;
	public override string ModuleAuthor => "Ashleigh Adams";
	public override string ModuleDescription => "Adapt MenuManagerAPI to ScreenMenuAPI";

	private CancellationTokenSource Cts { get; set; } = null!;

	private static readonly PluginCapability<IMenuApi> MenuCapability = new("menu:nfcore");

	public override void Load(bool hotReload)
	{
		Cts = new CancellationTokenSource();

		Capabilities.RegisterPluginCapability(MenuCapability, () => new MenuApiAdapter(this, Logger));
	}

	public override void Unload(bool hotReload)
	{
		Cts.Cancel();
	}

	private Dictionary<ulong, PlayerScreenMenuState> MenuStates = new();
	public PlayerScreenMenuState GetMenuState(CCSPlayerController player)
	{
		if (!MenuStates.TryGetValue(player.SteamID, out var menuState))
			MenuStates.Add(player.SteamID, menuState = new());
		return menuState;
	}

	[ConsoleCommand("css_0"), ConsoleCommand("css_1"), ConsoleCommand("css_2"), ConsoleCommand("css_3"), ConsoleCommand("css_4")]
	[ConsoleCommand("css_5"), ConsoleCommand("css_6"), ConsoleCommand("css_7"), ConsoleCommand("css_8"), ConsoleCommand("css_9")]
	[ConsoleCommand("css_screenmenu_bound_buttons")]
	public void RegisterKeyCommands(CCSPlayerController player, CommandInfo info)
	{
		if (player is null || !player.IsValid)
			return;

		var menuState = GetMenuState(player);
		menuState.UsingKeyBinds = info.CallingContext == CommandCallingContext.Console;
	}
}
