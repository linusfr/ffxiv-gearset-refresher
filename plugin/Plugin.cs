using System;

using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using GearsetRefresher.Windows;

namespace GearsetRefresher;

/// <summary>Dalamud entry point for Gearset Refresher.</summary>
public sealed class Plugin : IDalamudPlugin
{
	[PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
	[PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
	[PluginService] internal static IFramework Framework { get; private set; } = null!;
	[PluginService] internal static ICondition Condition { get; private set; } = null!;
	[PluginService] internal static IClientState ClientState { get; private set; } = null!;
	[PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
	[PluginService] internal static IPluginLog Log { get; private set; } = null!;

	private const string Command = "/gearrefresh";

	private readonly RefreshRunner _runner;
	private readonly MainWindow _window;

	public Plugin()
	{
		var gearsets = new GearsetService(ClientState, Condition, Log);
		_runner = new RefreshRunner(gearsets, Framework, Report, Log);
		_window = new MainWindow(_runner);

		CommandManager.AddHandler(Command, new CommandInfo(OnCommand)
		{
			HelpMessage = "Open Gearset Refresher, or use current, all, or cancel.",
		});

		PluginInterface.UiBuilder.Draw += _window.Draw;
		PluginInterface.UiBuilder.OpenMainUi += OpenWindow;
		PluginInterface.UiBuilder.OpenConfigUi += OpenWindow;
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

	private static void Report(string message)
	{
		ChatGui.Print($"[Gearset Refresher] {message}");
		Log.Info(message);
	}

	public void Dispose()
	{
		PluginInterface.UiBuilder.Draw -= _window.Draw;
		PluginInterface.UiBuilder.OpenMainUi -= OpenWindow;
		PluginInterface.UiBuilder.OpenConfigUi -= OpenWindow;
		CommandManager.RemoveHandler(Command);
		_runner.Dispose();
	}
}
