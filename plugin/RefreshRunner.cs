using System;
using System.Collections.Generic;

using Dalamud.Plugin.Services;

namespace GearsetRefresher;

/// <summary>Runs gear changes across frames so each game operation can settle.</summary>
public sealed class RefreshRunner : IDisposable
{
	private static readonly TimeSpan ActionDelay = TimeSpan.FromMilliseconds(350);
	private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(8);

	private readonly GearsetService _gearsets;
	private readonly IFramework _framework;
	private readonly Action<string> _report;
	private readonly IPluginLog _log;

	private IReadOnlyList<GearsetTarget> _targets = Array.Empty<GearsetTarget>();
	private int _targetIndex;
	private int _startingGearsetId = -1;
	private bool _restoreAfterSuccess;
	private bool _forceRestore;
	private string _finalMessageAfterRestore = string.Empty;
	private Phase _phase;
	private DateTime _nextActionAt;
	private DateTime _deadline;

	public bool IsRunning => _phase != Phase.Idle;
	public int CompletedCount { get; private set; }
	public int TotalCount => _targets.Count;
	public string Status { get; private set; } = "Ready.";

	public RefreshRunner(
		GearsetService gearsets,
		IFramework framework,
		Action<string> report,
		IPluginLog log)
	{
		_gearsets = gearsets;
		_framework = framework;
		_report = report;
		_log = log;
		_framework.Update += OnUpdate;
	}

	public bool StartCurrent()
	{
		if (!CanStart())
			return false;

		var target = _gearsets.CurrentTarget();
		if (target is null)
		{
			Status = "Current equipment is not linked to a gear set.";
			return false;
		}

		return Start(new[] { target }, restoreStartingSet: false);
	}

	public bool StartAllJobs()
	{
		if (!CanStart())
			return false;

		var currentId = _gearsets.CurrentGearsetId;
		var targets = RefreshPlan.ForAllJobs(_gearsets.AllTargets(), currentId);
		if (targets.Count == 0)
		{
			Status = "No gear sets found.";
			return false;
		}

		return Start(targets, restoreStartingSet: true);
	}

	public void Cancel()
	{
		if (!IsRunning)
			return;

		BeginRestore($"Refresh cancelled after {CompletedCount} of {TotalCount} gear sets.", force: true);
	}

	private bool CanStart()
	{
		if (IsRunning)
		{
			Status = "A refresh is already running.";
			return false;
		}

		if (_gearsets.CanChangeGear(out var reason))
			return true;

		Status = reason;
		return false;
	}

	private bool Start(IReadOnlyList<GearsetTarget> targets, bool restoreStartingSet)
	{
		_targets = targets;
		_targetIndex = 0;
		CompletedCount = 0;
		_startingGearsetId = _gearsets.CurrentGearsetId;
		_restoreAfterSuccess = restoreStartingSet;
		_forceRestore = false;
		_finalMessageAfterRestore = string.Empty;
		_phase = Phase.EquipGearset;
		_nextActionAt = DateTime.UtcNow;
		Status = $"Starting refresh of {targets.Count} job{(targets.Count == 1 ? string.Empty : "s")}.";
		return true;
	}

	private void OnUpdate(IFramework framework)
	{
		if (!IsRunning || DateTime.UtcNow < _nextActionAt)
			return;

		if (!_gearsets.CanChangeGear(out var reason))
		{
			Status = reason;
			// Pauses should not consume transition timeout budget.
			_deadline = DateTime.UtcNow + OperationTimeout;
			return;
		}

		try
		{
			Advance();
		}
		catch (Exception exception)
		{
			_log.Error(exception, "Refresh failed in phase {Phase}.", _phase);
			Fail("Refresh failed; see /xllog for details.");
		}
	}

	private void Advance()
	{
		if (_phase == Phase.RestoreStartingSet)
		{
			RestoreStartingSet();
			return;
		}

		if (_phase == Phase.WaitForRestore)
		{
			WaitForRestore();
			return;
		}

		var target = _targets[_targetIndex];
		switch (_phase)
		{
			case Phase.EquipGearset:
				Status = $"Equipping {target.Name} ({_targetIndex + 1}/{TotalCount}).";
				if (!_gearsets.Equip(target.Id))
				{
					Fail($"Could not equip {target.Name}.");
					return;
				}

				Wait(Phase.WaitForGearset);
				break;

			case Phase.WaitForGearset:
				if (_gearsets.CurrentGearsetId == target.Id)
					Delay(Phase.SetupRecommendations);
				else if (TimedOut())
					Fail($"Timed out equipping {target.Name}.");
				break;

			case Phase.SetupRecommendations:
				Status = $"Calculating recommended gear for {target.Name}.";
				if (!_gearsets.SetupRecommendations(target.ClassJobId))
				{
					Fail($"Could not calculate gear for {target.Name}.");
					return;
				}

				Delay(Phase.EquipRecommendations);
				break;

			case Phase.EquipRecommendations:
				Status = $"Equipping recommended gear for {target.Name}.";
				if (!_gearsets.EquipRecommendations())
				{
					Fail($"Could not equip recommended gear for {target.Name}.");
					return;
				}

				Wait(Phase.WaitForRecommendations);
				break;

			case Phase.WaitForRecommendations:
				if (!_gearsets.RecommendationsUpdating)
					Delay(Phase.SaveGearset);
				else if (TimedOut())
					Fail($"Timed out equipping recommended gear for {target.Name}.");
				break;

			case Phase.SaveGearset:
				Status = $"Saving {target.Name}.";
				if (!_gearsets.Save(target.Id))
				{
					Fail($"Could not save {target.Name}.");
					return;
				}

				CompletedCount++;
				_targetIndex++;
				if (_targetIndex < _targets.Count)
					Delay(Phase.EquipGearset);
				else if (_restoreAfterSuccess)
					BeginRestore(SuccessMessage(), force: false);
				else
					Finish(SuccessMessage());
				break;
		}
	}

	private void RestoreStartingSet()
	{
		if (_startingGearsetId < 0 || (!_forceRestore && _gearsets.CurrentGearsetId == _startingGearsetId))
		{
			Finish(_finalMessageAfterRestore);
			return;
		}

		Status = "Restoring starting gear set.";
		if (!_gearsets.Equip(_startingGearsetId))
		{
			Finish($"{_finalMessageAfterRestore} Could not restore starting gear set.");
			return;
		}

		_forceRestore = false;
		Wait(Phase.WaitForRestore);
	}

	private void WaitForRestore()
	{
		if (_gearsets.CurrentGearsetId == _startingGearsetId)
			Finish(_finalMessageAfterRestore);
		else if (TimedOut())
			Finish($"{_finalMessageAfterRestore} Restoring starting gear set timed out.");
	}

	private void Fail(string message)
	{
		if (_startingGearsetId >= 0)
		{
			BeginRestore(message, force: true);
			return;
		}

		Finish(message);
	}

	private void BeginRestore(string finalMessage, bool force)
	{
		_finalMessageAfterRestore = finalMessage;
		_forceRestore = force;
		Status = $"{finalMessage} Restoring starting gear set.";
		_phase = Phase.RestoreStartingSet;
		_nextActionAt = DateTime.UtcNow + ActionDelay;
	}

	private string SuccessMessage() =>
		$"Refreshed {CompletedCount} gear set{(CompletedCount == 1 ? string.Empty : "s")}.";

	private void Finish(string message)
	{
		Status = message;
		_phase = Phase.Idle;
		_report(message);
	}

	private void Delay(Phase next)
	{
		_phase = next;
		_nextActionAt = DateTime.UtcNow + ActionDelay;
	}

	private void Wait(Phase next)
	{
		_phase = next;
		_nextActionAt = DateTime.UtcNow + ActionDelay;
		_deadline = DateTime.UtcNow + OperationTimeout;
	}

	private bool TimedOut() => DateTime.UtcNow >= _deadline;

	public void Dispose() => _framework.Update -= OnUpdate;

	private enum Phase
	{
		Idle,
		EquipGearset,
		WaitForGearset,
		SetupRecommendations,
		EquipRecommendations,
		WaitForRecommendations,
		SaveGearset,
		RestoreStartingSet,
		WaitForRestore,
	}
}
