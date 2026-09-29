using System;
using System.Numerics;

using Dalamud.Bindings.ImGui;

namespace GearsetRefresher.Windows;

/// <summary>Main control surface for refresh operations.</summary>
public sealed class MainWindow
{
	private readonly RefreshRunner _runner;
	private readonly Configuration _configuration;
	private readonly Action _saveConfiguration;
	private bool _isVisible;
	private bool _bulkConfirmed;

	public bool IsVisible
	{
		get => _isVisible;
		set => _isVisible = value;
	}

	public MainWindow(RefreshRunner runner, Configuration configuration, Action saveConfiguration)
	{
		_runner = runner;
		_configuration = configuration;
		_saveConfiguration = saveConfiguration;
	}

	public void Draw()
	{
		if (!IsVisible)
			return;

		ImGui.SetNextWindowSize(new Vector2(480, 330), ImGuiCond.FirstUseEver);
		if (!ImGui.Begin("Gearset Refresher###GearsetRefresher", ref _isVisible))
		{
			ImGui.End();
			return;
		}

		ImGui.TextWrapped("Equip the game's recommended gear, then save it into a gear set.");
		var refreshOnLevelUp = _configuration.RefreshCurrentOnLevelUp;
		if (ImGui.Checkbox("Refresh current gear on level up", ref refreshOnLevelUp))
		{
			_configuration.RefreshCurrentOnLevelUp = refreshOnLevelUp;
			_saveConfiguration();
		}
		var refreshOnLoot = _configuration.RefreshCurrentOnLoot;
		if (ImGui.Checkbox("Refresh current gear after receiving loot", ref refreshOnLoot))
		{
			_configuration.RefreshCurrentOnLoot = refreshOnLoot;
			_saveConfiguration();
		}
		ImGui.Spacing();

		ImGui.BeginDisabled(_runner.IsRunning);
		if (ImGui.Button("Refresh current job", new Vector2(-1, 0)))
			_runner.StartCurrent();

		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();
		ImGui.TextWrapped("All jobs processes one existing gear set per job. Current set wins for its job; otherwise the lowest-numbered set is used. Alternate sets stay untouched.");
		ImGui.Checkbox("I understand selected gear sets will be overwritten", ref _bulkConfirmed);
		ImGui.BeginDisabled(!_bulkConfirmed);
		if (ImGui.Button("Refresh all jobs", new Vector2(-1, 0)))
		{
			if (_runner.StartAllJobs())
				_bulkConfirmed = false;
		}
		ImGui.EndDisabled();
		ImGui.EndDisabled();

		if (_runner.IsRunning)
		{
			ImGui.Spacing();
			if (_runner.TotalCount > 0)
				ImGui.ProgressBar((float)_runner.CompletedCount / _runner.TotalCount, new Vector2(-1, 0));
			if (ImGui.Button("Cancel and restore starting job", new Vector2(-1, 0)))
				_runner.Cancel();
		}

		ImGui.Spacing();
		ImGui.TextWrapped(_runner.Status);
		ImGui.TextDisabled("Only run while idle, outside duties, with enough Armoury Chest space.");
		ImGui.End();
	}
}
