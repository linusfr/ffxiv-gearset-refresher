<div align="center">
	<img src="images/icon.png" alt="Gearset Refresher icon" width="128">
	<h1>Gearset Refresher</h1>
	<p>Keep your FFXIV gear sets updated with the game's recommended equipment.</p>
	<p>
		<a href="https://github.com/linusfr/ffxiv-gearset-refresher/releases/latest"><img src="https://img.shields.io/github/v/release/linusfr/ffxiv-gearset-refresher?sort=semver&amp;display_name=tag&amp;label=latest&amp;color=blue&amp;cacheSeconds=300" alt="Latest release"></a>
		<a href="https://github.com/linusfr/ffxiv-gearset-refresher/actions/workflows/ci.yml"><img src="https://img.shields.io/github/actions/workflow/status/linusfr/ffxiv-gearset-refresher/ci.yml?branch=main&amp;label=ci&amp;cacheSeconds=300" alt="CI status"></a>
		<a href="LICENSE"><img src="https://img.shields.io/github/license/linusfr/ffxiv-gearset-refresher?color=blue" alt="MIT license"></a>
	</p>
</div>

Gearset Refresher is a Dalamud plugin that equips recommended gear and saves it
back to your existing gear sets. Refresh your current job from the plugin window
or update one gear set per job with a single command.

## Install

In `/xlsettings`, open **Experimental** > **Custom Plugin Repositories**, paste
this URL, click `+`, then save:

```text
https://raw.githubusercontent.com/linusfr/ffxiv-gearset-refresher/main/pluginmaster.json
```

Then open `/xlplugins`, search for **Gearset Refresher**, and select **Install**.

## Features

- Refresh the currently equipped gear set.
- Optionally refresh current gear automatically after gaining a level.
- Optionally refresh current gear after receiving equippable loot.
- Refresh one existing gear set for every job, then return to the starting set.
- Use slash commands directly in FFXIV macros.
- Cancel a refresh while it is running.
- Pause safely during combat or area changes.

## Usage

| Command | Action |
| --- | --- |
| `/gearrefresh` | Open the plugin window. |
| `/gearrefresh current` | Refresh the current gear set. |
| `/gearrefresh all` | Refresh one gear set per job. |
| `/gearrefresh cancel` | Stop the active refresh and restore the starting set. |

The explicit `all` argument confirms the bulk update, making the command safe to
use in macros without an additional dialog.

## Behavior

When a job has multiple gear sets, Gearset Refresher updates the currently
equipped set if it belongs to that job. Otherwise, it updates the lowest-numbered
set. Other loadouts remain untouched.

Bulk refreshes cannot start inside duties. Current-set refreshes can run inside a
duty but wait through combat and area changes before continuing.

Both automatic options are disabled by default. Enable **Refresh current gear on
level up** in the plugin window to queue a
refresh for the equipped gear set. It starts after combat ends, including inside
a duty. Switching jobs or starting another refresh cancels the queued automatic
refresh.

Enable **Refresh current gear after receiving loot** to apply the same behavior
when equipment enters inventory or Armoury Chest. Materials, consumables, and
inventory moves do not trigger a refresh.

## Important Notes

Gearset Refresher uses Dalamud API 15 and `FFXIVClientStructs` to equip and save
gear. Test it in a development plugin installation before relying on it for
important gear sets.

Updating gear may make linked portraits stale, matching normal in-game behavior.

## Development

```bash
just build
just test
just install
```

`DALAMUD_HOME` defaults to `~/.xlcore/dalamud/Hooks/dev`. Override it when
Dalamud lives elsewhere.

## Versioning

`go-semantic-release` reads conventional commits on `main`. CI builds and tests
the plugin before creating a release, attaching `GearsetRefresher.zip`, and
updating `pluginmaster.json`.

## Architecture

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

## License

MIT, see [`LICENSE`](LICENSE).
