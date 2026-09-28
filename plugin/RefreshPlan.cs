using System.Collections.Generic;
using System.Linq;

namespace GearsetRefresher;

/// <summary>Builds deterministic, non-destructive refresh plans.</summary>
public static class RefreshPlan
{
	/// <summary>
	/// Selects one gear set per job, preferring current set and then lowest set number.
	/// Alternate sets remain untouched because they often represent intentional loadouts.
	/// </summary>
	public static IReadOnlyList<GearsetTarget> ForAllJobs(
		IEnumerable<GearsetTarget> targets,
		int currentGearsetId)
	{
		return targets
			.Where(target => target.Id is >= 0 and < 100 && target.ClassJobId > 0)
			.GroupBy(target => target.ClassJobId)
			.Select(group => group.FirstOrDefault(target => target.Id == currentGearsetId)
				?? group.OrderBy(target => target.Id).First())
			.OrderBy(target => target.Id)
			.ToArray();
	}
}
