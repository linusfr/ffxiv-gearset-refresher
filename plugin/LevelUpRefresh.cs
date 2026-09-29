namespace GearsetRefresher;

/// <summary>Tracks a level-up refresh until game state allows it to start.</summary>
internal sealed class LevelUpRefresh
{
	private uint? _classJobId;

	/// <summary>Gets level associated with pending refresh.</summary>
	public uint Level { get; private set; }

	/// <summary>Queues refresh only when level change belongs to current gear set.</summary>
	public void Queue(
		bool enabled,
		bool refreshRunning,
		uint classJobId,
		uint level,
		GearsetTarget? currentTarget)
	{
		if (!enabled || refreshRunning || currentTarget is null || currentTarget.ClassJobId != classJobId)
			return;

		_classJobId = classJobId;
		Level = level;
	}

	/// <summary>Returns whether pending refresh still matches current gear set.</summary>
	public bool ShouldStart(bool refreshRunning, GearsetTarget? currentTarget)
	{
		if (_classJobId is null)
			return false;

		if (refreshRunning || currentTarget is null || currentTarget.ClassJobId != _classJobId)
		{
			Clear();
			return false;
		}

		return true;
	}

	/// <summary>Clears pending refresh after it starts or becomes invalid.</summary>
	public void Clear()
	{
		_classJobId = null;
		Level = 0;
	}
}
