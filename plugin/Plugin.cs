using System;

using Dalamud.Game.Command;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using GearsetRefresher.Windows;

using Lumina.Excel.Sheets;

namespace GearsetRefresher;

/// <summary>Dalamud entry point for Gearset Refresher.</summary>
public sealed class Plugin : IDalamudPlugin
{
	[PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
	[PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
	[PluginService] internal static IFramework Framework { get; private set; } = null!;
	[PluginService] internal static ICondition Condition { get; private set; } = null!;
	[PluginService] internal static IClientState ClientState { get; private set; } = null!;
	[PluginService] internal static IGameInventory GameInventory { get; private set; } = null!;
	[PluginService] internal static IDataManager DataManager { get; private set; } = null!;
	[PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
	[PluginService] internal static IPluginLog Log { get; private set; } = null!;

	private const string Command = "/gearrefresh";

	private readonly Configuration _configuration;
	private readonly GearsetService _gearsets;
	private readonly RefreshRunner _runner;
	private readonly MainWindow _window;
	private readonly AutomaticRefresh _levelUpRefresh = new();
	private readonly AutomaticRefresh _lootRefresh = new();

	public Plugin()
	{
		_configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
		_gearsets = new GearsetService(ClientState, Condition, Log);
		_runner = new RefreshRunner(_gearsets, Framework, Report, Log);
		_window = new MainWindow(_runner, _configuration, SaveConfiguration);

		CommandManager.AddHandler(Command, new CommandInfo(OnCommand)
		{
			HelpMessage = "Open Gearset Refresher, or use current, all, or cancel.",
		});

		PluginInterface.UiBuilder.Draw += _window.Draw;
		PluginInterface.UiBuilder.OpenMainUi += OpenWindow;
		PluginInterface.UiBuilder.OpenConfigUi += OpenWindow;
		ClientState.LevelChanged += OnLevelChanged;
		GameInventory.ItemAddedExplicit += OnItemAdded;
		Framework.Update += OnFrameworkUpdate;
	}

	private void OnLevelChanged(uint classJobId, uint level)
	{
		var target = _gearsets.CurrentTarget();
		if (target is null || target.ClassJobId != classJobId)
			return;

		_levelUpRefresh.Queue(
			_configuration.RefreshCurrentOnLevelUp,
			_runner.IsRunning,
			target,
			$"Level {level} reached; refreshing current gear set.");
	}

	private void OnItemAdded(InventoryItemAddedArgs data)
	{
		if (!DataManager.GetExcelSheet<Item>().TryGetRow(data.Item.BaseItemId, out var item) ||
			item.EquipSlotCategory.RowId == 0)
			return;

		_lootRefresh.Queue(
			_configuration.RefreshCurrentOnLoot,
			_runner.IsRunning,
			_gearsets.CurrentTarget(),
			"Equippable item received; refreshing current gear set.");
	}

	private void OnFrameworkUpdate(IFramework framework)
	{
		var target = _gearsets.CurrentTarget();
		var request = _levelUpRefresh.ShouldStart(
			_configuration.RefreshCurrentOnLevelUp,
			_runner.IsRunning,
			target)
			? _levelUpRefresh
			: _lootRefresh.ShouldStart(
				_configuration.RefreshCurrentOnLoot,
				_runner.IsRunning,
				target)
				? _lootRefresh
				: null;
		if (request is null)
			return;

		if (!_runner.StartCurrent())
			return;

		var message = request.Message;
		request.Clear();
		Report(message);
	}

	private void OnCommand(string command, string arguments)
	{
		switch (CommandArguments.Parse(arguments))
		{
			case CommandAction.Open:
				_window.IsVisible = true;
				break;
			case CommandAction.Current:
				_runner.StartCurrent();
				Report(_runner.Status);
				break;
			case CommandAction.All:
				// Typing the explicit bulk command serves as confirmation for macro use.
				_runner.StartAllJobs();
				Report(_runner.Status);
				break;
			case CommandAction.Cancel:
				if (_runner.IsRunning)
					_runner.Cancel();
				else
					Report("No refresh is running.");
				break;
			case CommandAction.Help:
				Report("Usage: /gearrefresh [current|all|cancel]");
				break;
		}
	}

	private void OpenWindow() => _window.IsVisible = true;

	private void SaveConfiguration() => PluginInterface.SavePluginConfig(_configuration);

	private static void Report(string message)
	{
		ChatGui.Print($"[Gearset Refresher] {message}");
		Log.Info(message);
	}

	public void Dispose()
	{
		Framework.Update -= OnFrameworkUpdate;
		GameInventory.ItemAddedExplicit -= OnItemAdded;
		ClientState.LevelChanged -= OnLevelChanged;
		PluginInterface.UiBuilder.Draw -= _window.Draw;
		PluginInterface.UiBuilder.OpenMainUi -= OpenWindow;
		PluginInterface.UiBuilder.OpenConfigUi -= OpenWindow;
		CommandManager.RemoveHandler(Command);
		_runner.Dispose();
	}
}
