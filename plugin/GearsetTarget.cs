namespace GearsetRefresher;

/// <summary>Gear set selected for refresh.</summary>
public sealed record GearsetTarget(int Id, byte ClassJobId, string Name);
