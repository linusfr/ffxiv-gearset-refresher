using System;

namespace GearsetRefresher;

/// <summary>Parses slash-command arguments without depending on Dalamud.</summary>
public static class CommandArguments
{
	public static CommandAction Parse(string arguments) => arguments.Trim().ToLowerInvariant() switch
	{
		"" => CommandAction.Open,
		"current" => CommandAction.Current,
		"all" => CommandAction.All,
		"cancel" => CommandAction.Cancel,
		_ => CommandAction.Help,
	};
}

public enum CommandAction
{
	Open,
	Current,
	All,
	Cancel,
	Help,
}
