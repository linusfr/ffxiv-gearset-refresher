namespace GearsetRefresher;

/// <summary>Tracks an automatic current-set refresh until game state allows it to start.</summary>
internal sealed class AutomaticRefresh
{
	private int? _gearsetId;

	/// <summary>Gets message describing pending refresh.</summary>
	public string Message { get; private set; } = string.Empty;

	/// <summary>Queues refresh for exact gear set that was current when trigger fired.</summary>
	public void Queue(
		bool enabled,
		bool refreshRunning,
		GearsetTarget? currentTarget,
		string message)
	{
		if (!enabled || refreshRunning || currentTarget is null)
			return;

		_gearsetId = currentTarget.Id;
		Message = message;
	}

	/// <summary>Returns whether pending refresh still matches current gear set.</summary>
	public bool ShouldStart(bool enabled, bool refreshRunning, GearsetTarget? currentTarget)
	{
		if (_gearsetId is null)
			return false;

		if (!enabled || refreshRunning || currentTarget is null || currentTarget.Id != _gearsetId)
		{
			Clear();
			return false;
		}

		return true;
	}

	/// <summary>Clears pending refresh after it starts or becomes invalid.</summary>
	public void Clear()
	{
		_gearsetId = null;
		Message = string.Empty;
	}
}
