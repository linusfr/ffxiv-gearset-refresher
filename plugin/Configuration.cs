using System;

using Dalamud.Configuration;

namespace GearsetRefresher;

/// <summary>Settings persisted by Dalamud for Gearset Refresher.</summary>
[Serializable]
public sealed class Configuration : IPluginConfiguration
{
	/// <summary>Gets or sets configuration schema version.</summary>
	public int Version { get; set; } = 1;

	/// <summary>Gets or sets whether current gear refreshes after its job gains a level.</summary>
	public bool RefreshCurrentOnLevelUp { get; set; }

	/// <summary>Gets or sets whether current gear refreshes after receiving equippable loot.</summary>
	public bool RefreshCurrentOnLoot { get; set; }
}
