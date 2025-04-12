using System;
using System.Collections.Generic;

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;

using Microsoft.Extensions.Logging;

using MenuManager;

using ScreenMenu = CS2ScreenMenuAPI.Menu;
using IScreenMenuOption = CS2ScreenMenuAPI.IMenuOption;
using ScreenMenuType = CS2ScreenMenuAPI.MenuType;
using ScreenMenuPostSelect = CS2ScreenMenuAPI.PostSelect;
using ScreenMenuApi = CS2ScreenMenuAPI.MenuAPI;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System.Linq;

namespace CsGals;

internal class MenuApiAdapter : IMenuApi
{
	public MenuManager2ScreenMenu Plugin { get; }
	public ILogger Log { get; }

	public MenuApiAdapter(MenuManager2ScreenMenu plugin, ILogger log)
	{
		Plugin = plugin;
		Log = log;
	}

	internal MenuInstanceAdapter? CurrentParentMenu { get; set; }

	void IMenuApi.CloseMenu(CCSPlayerController player)
	{
		ScreenMenuApi.CloseActiveMenu(player);
	}

	IMenu IMenuApi.GetMenu(string title, Action<CCSPlayerController> back_action, Action<CCSPlayerController> reset_action)
	{
		return new MenuInstanceAdapter(Log, title, this, back_action, reset_action, MenuType.Default, ScreenMenuPostSelect.Close);
	}

	IMenu IMenuApi.GetMenuForcetype(string title, MenuType type, Action<CCSPlayerController> back_action, Action<CCSPlayerController> reset_action)
	{
		return new MenuInstanceAdapter(Log, title, this, back_action, reset_action, type, ScreenMenuPostSelect.Close);
	}

	MenuType IMenuApi.GetMenuType(CCSPlayerController player)
	{
		return ScreenMenuApi.GetActiveMenu(player)?.MenuType switch
		{
			ScreenMenuType.KeyPress => MenuType.ButtonMenu,
			ScreenMenuType.Scrollable => MenuType.CenterMenu,
			ScreenMenuType.Both => MenuType.ButtonMenu,
			_ => MenuType.Default,
		};
	}

	bool IMenuApi.HasOpenedMenu(CCSPlayerController player)
	{
		return ScreenMenuApi.GetActiveMenu(player) is not null;
	}

	IMenu IMenuApi.NewMenu(string title, Action<CCSPlayerController> back_action)
	{
		return new MenuInstanceAdapter(Log, title, this, back_action, null, MenuType.Default, ScreenMenuPostSelect.Close);
	}

	IMenu IMenuApi.NewMenuForcetype(string title, MenuType type, Action<CCSPlayerController> back_action)
	{
		return new MenuInstanceAdapter(Log, title, this, back_action, null, type, ScreenMenuPostSelect.Close);
	}
}

internal class MenuInstanceAdapter : IMenu
{
	public string Title { get; set; }
	public MenuApiAdapter ApiAdapter { get; }
	public bool ExitButton { get; set; }

	public ILogger Log { get; }
	public List<ChatMenuOption> MenuOptions { get; } = new();
	public Action<CCSPlayerController>? BackAction { get; }
	public Action<CCSPlayerController>? ResetAction { get; }
	public MenuType MenuType { get; set; }
	public ScreenMenuPostSelect PostSelectType { get; }

	public MenuInstanceAdapter(
		ILogger log,
		string title,
		MenuApiAdapter apiAdapter,
		Action<CCSPlayerController>? backAction,
		Action<CCSPlayerController>? resetAction,
		MenuType menuType,
		ScreenMenuPostSelect postSelectType)
	{
		Log = log;
		Title = title;
		ApiAdapter = apiAdapter;
		BackAction = backAction;
		ResetAction = resetAction;
		MenuType = menuType;
		PostSelectType = postSelectType;
	}

	ChatMenuOption IMenu.AddMenuOption(string display, Action<CCSPlayerController, ChatMenuOption> onSelect, bool disabled)
	{
		var ret = new ChatMenuOption(display, disabled, onSelect);
		MenuOptions.Add(ret);
		return ret;
	}

	// private ScreenMenu? TheMenu;
	private SortedDictionary<ulong, ScreenMenu> PlayerMenus = new();

	void IMenu.Open(CCSPlayerController player)
	{
		if (PlayerMenus.TryGetValue(player.SteamID, out var menu))
		{
			menu.Display();
			return;
		}

		var menuState = ApiAdapter.Plugin.GetMenuState(player);
		
		menu = PlayerMenus[player.SteamID] = new ScreenMenu(player, ApiAdapter.Plugin)
		{
			Title = Title,
			PostSelect = PostSelectType,
			PrevMenu = menuState.ExecutingScreenMenu,
			IsSubMenu = menuState.ExecutingScreenMenu is not null,
			MenuType = menuState.UsingKeyBinds ? ScreenMenuType.KeyPress : ScreenMenuType.Both,
		};

		foreach (var option in MenuOptions)
		{
			Action<CCSPlayerController, IScreenMenuOption> callback = (playerCb, menuOption) =>
			{
				// try to track parents
				var was = menuState.ExecutingScreenMenu;
				menuState.ExecutingScreenMenu = menu;
				{
					option.OnSelect(playerCb, option);
					ResetAction?.Invoke(playerCb);
				}
				menuState.ExecutingScreenMenu = null;
			};

			var doc = HtmlParser.ParseFragment(option.Text, null!);
			var text = string.Join(string.Empty, doc.Select(x => x.Text()));

			menu.AddItem(text, callback, option.Disabled);
		}

		menu.Display();
	}
	private readonly HtmlParser HtmlParser = new HtmlParser(new HtmlParserOptions() { IsStrictMode = false });

	void IMenu.OpenToAll()
	{
		Log.LogWarning("IMenu.OpenToAll() is not supported");
		//throw new NotSupportedException("IMenu.OpenToAll() is not supported");
	}
}
