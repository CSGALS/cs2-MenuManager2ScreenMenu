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

	private ScreenMenu? TheMenu;
	void IMenu.Open(CCSPlayerController player)
	{
		TheMenu = new ScreenMenu(player, ApiAdapter.Plugin)
		{
			PostSelect = PostSelectType,
			ParentMenu = ApiAdapter.CurrentParentMenu?.TheMenu,
			MenuType = MenuType switch
			{
				MenuType.Default => ScreenMenuType.KeyPress,
				MenuType.ChatMenu => ScreenMenuType.KeyPress,
				MenuType.ConsoleMenu => ScreenMenuType.KeyPress,
				MenuType.CenterMenu => ScreenMenuType.Scrollable,
				MenuType.ButtonMenu => ScreenMenuType.KeyPress,
				MenuType.MetamodMenu => ScreenMenuType.KeyPress,
				_ => ScreenMenuType.KeyPress,
			},
		};

		foreach (var option in MenuOptions)
		{
			Action<CCSPlayerController, IScreenMenuOption> callback = (player, menuOption) =>
			{
				// try to track parents
				var was = ApiAdapter.CurrentParentMenu;
				ApiAdapter.CurrentParentMenu = this;
				{
					option.OnSelect(player, option);
				}
				ApiAdapter.CurrentParentMenu = was;
			};

			// TODO: maybe strip HTML from option.Text
			TheMenu.AddItem(option.Text, callback, option.Disabled);
		}

		TheMenu.Display();
	}

	void IMenu.OpenToAll()
	{
		Log.LogWarning("IMenu.OpenToAll() is not supported");
		//throw new NotSupportedException("IMenu.OpenToAll() is not supported");
	}
}
