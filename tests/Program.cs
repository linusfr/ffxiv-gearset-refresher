using System;
using System.Linq;

using GearsetRefresher;

var targets = new[]
{
	new GearsetTarget(8, 19, "Paladin alternate"),
	new GearsetTarget(2, 19, "Paladin primary"),
	new GearsetTarget(5, 24, "White Mage"),
	new GearsetTarget(100, 25, "Invalid ID"),
	new GearsetTarget(7, 0, "Invalid job"),
};

AssertIds(RefreshPlan.ForAllJobs(targets, 8), 5, 8);
AssertIds(RefreshPlan.ForAllJobs(targets, 5), 2, 5);
AssertIds(RefreshPlan.ForAllJobs(Array.Empty<GearsetTarget>(), -1));

AssertCommand("", CommandAction.Open);
AssertCommand(" current ", CommandAction.Current);
AssertCommand("ALL", CommandAction.All);
AssertCommand("cancel", CommandAction.Cancel);
AssertCommand("unknown", CommandAction.Help);

Console.WriteLine("All tests passed.");

static void AssertIds(System.Collections.Generic.IReadOnlyList<GearsetTarget> actual, params int[] expected)
{
	var ids = actual.Select(target => target.Id).ToArray();
	if (!ids.SequenceEqual(expected))
		throw new InvalidOperationException($"Expected [{string.Join(", ", expected)}], got [{string.Join(", ", ids)}].");
}

static void AssertCommand(string arguments, CommandAction expected)
{
	var actual = CommandArguments.Parse(arguments);
	if (actual != expected)
		throw new InvalidOperationException($"Expected command {expected}, got {actual}.");
}
