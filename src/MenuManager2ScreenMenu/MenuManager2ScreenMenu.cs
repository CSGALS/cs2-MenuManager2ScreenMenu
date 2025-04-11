using System.Threading;

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;

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
}
