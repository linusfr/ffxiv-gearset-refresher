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

var automaticRefresh = new AutomaticRefresh();
automaticRefresh.Queue(true, false, targets[1], "Level 42 reached.");
Assert(automaticRefresh.ShouldStart(true, false, targets[1]), "Enabled trigger should queue refresh.");
Assert(automaticRefresh.Message == "Level 42 reached.", "Queued refresh should retain trigger message.");
automaticRefresh.Clear();

automaticRefresh.Queue(false, false, targets[1], "Disabled.");
Assert(!automaticRefresh.ShouldStart(false, false, targets[1]), "Disabled setting should not queue refresh.");
automaticRefresh.Queue(true, true, targets[1], "Busy.");
Assert(!automaticRefresh.ShouldStart(true, false, targets[1]), "Running refresh should suppress automatic refresh.");
automaticRefresh.Queue(true, false, targets[1], "Loot received.");
Assert(!automaticRefresh.ShouldStart(true, false, targets[0]), "Gear-set change should cancel queued refresh.");
automaticRefresh.Queue(true, false, targets[1], "Setting disabled.");
Assert(!automaticRefresh.ShouldStart(false, false, targets[1]), "Disabling setting should cancel queued refresh.");

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

static void Assert(bool condition, string message)
{
	if (!condition)
		throw new InvalidOperationException(message);
}
