using System;
using System.Collections.Generic;

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace GearsetRefresher;

/// <summary>Thin boundary around game-owned gear and recommendation modules.</summary>
public sealed class GearsetService
{
	private readonly IClientState _clientState;
	private readonly ICondition _condition;
	private readonly IPluginLog _log;

	public GearsetService(IClientState clientState, ICondition condition, IPluginLog log)
	{
		_clientState = clientState;
		_condition = condition;
		_log = log;
	}

	public bool CanChangeGear(out string reason)
	{
		if (!_clientState.IsLoggedIn)
		{
			reason = "Log in before refreshing gear sets.";
			return false;
		}

		if (_condition[ConditionFlag.InCombat])
		{
			reason = "Paused while in combat.";
			return false;
		}

		if (_condition[ConditionFlag.BoundByDuty])
		{
			reason = "Leave the duty before refreshing gear sets.";
			return false;
		}

		if (_condition[ConditionFlag.BetweenAreas])
		{
			reason = "Paused while changing areas.";
			return false;
		}

		reason = string.Empty;
		return true;
	}

	public unsafe int CurrentGearsetId
	{
		get
		{
			var module = RaptureGearsetModule.Instance();
			return module == null ? -1 : module->CurrentGearsetIndex;
		}
	}

	public unsafe GearsetTarget? CurrentTarget()
	{
		var module = RaptureGearsetModule.Instance();
		if (module == null || module->CurrentGearsetIndex is < 0 or >= 100)
			return null;

		return ReadTarget(module, module->CurrentGearsetIndex);
	}

	public unsafe IReadOnlyList<GearsetTarget> AllTargets()
	{
		var result = new List<GearsetTarget>();
		var module = RaptureGearsetModule.Instance();
		if (module == null)
			return result;

		for (var id = 0; id < 100; id++)
		{
			var target = ReadTarget(module, id);
			if (target is not null)
				result.Add(target);
		}

		return result;
	}

	public unsafe bool Equip(int gearsetId)
	{
		var module = RaptureGearsetModule.Instance();
		return module != null && module->EquipGearset(gearsetId) == 0;
	}

	public unsafe bool SetupRecommendations(byte classJobId)
	{
		var module = RecommendEquipModule.Instance();
		return module != null && module->SetupForClassJob(classJobId);
	}

	public unsafe bool EquipRecommendations()
	{
		var module = RecommendEquipModule.Instance();
		if (module == null)
			return false;

		module->EquipRecommendedGear();
		return true;
	}

	public unsafe bool RecommendationsUpdating
	{
		get
		{
			var module = RecommendEquipModule.Instance();
			return module != null && module->IsUpdating;
		}
	}

	public unsafe bool Save(int gearsetId)
	{
		var module = RaptureGearsetModule.Instance();
		return module != null && module->UpdateGearset(gearsetId) >= 0;
	}

	private unsafe GearsetTarget? ReadTarget(RaptureGearsetModule* module, int id)
	{
		if (!module->IsValidGearset(id))
			return null;

		var entry = module->GetGearset(id);
		if (entry == null || !entry->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists))
			return null;

		// Names are intentionally synthesized until string decoding is verified across client languages.
		var target = new GearsetTarget(id, entry->ClassJob, $"Gear Set {id + 1}");
		_log.Verbose("Found {Name} for class job {ClassJobId}.", target.Name, target.ClassJobId);
		return target;
	}
}
