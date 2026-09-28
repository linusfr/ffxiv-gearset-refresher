project := "plugin/GearsetRefresher.csproj"
tests := "tests/GearsetRefresher.Tests.csproj"
dev_plugins := env_var('HOME') / ".xlcore/devPlugins/GearsetRefresher"
dalamud_home := env_var_or_default('DALAMUD_HOME', env_var('HOME') / ".xlcore/dalamud/Hooks/dev")
dotnet := "nix shell nixpkgs#dotnet-sdk_10 -c env DALAMUD_HOME=" + dalamud_home + " DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 dotnet"

@default:
	just --list

restore:
	{{dotnet}} restore {{project}}

build: restore
	{{dotnet}} build {{project}} --configuration Debug --no-restore

test:
	nix shell nixpkgs#dotnet-sdk_10 -c dotnet run --project {{tests}}

install: build
	rm -rf {{dev_plugins}}
	mkdir -p {{dev_plugins}}
	cp -r plugin/bin/Debug/. {{dev_plugins}}/

clean:
	rm -rf plugin/bin plugin/obj tests/bin tests/obj
