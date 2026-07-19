#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
messaging_source="${MESSAGING_SOURCE:-$repo_root/../Maliev.MessagingContracts}"
defaults_source="${SERVICE_DEFAULTS_SOURCE:-$repo_root/../Maliev.Aspire}"
output_dir="$repo_root/.ci-packages"
messaging_version="${SHARED_LIBRARY_VERSION:-1.0.96-alpha}"
service_defaults_version="${SERVICE_DEFAULTS_VERSION:-1.0.89-alpha}"

rm -rf "$output_dir"
mkdir -p "$output_dir"

messaging_project="$messaging_source/generated/csharp/Maliev.MessagingContracts.csproj"
defaults_project="$defaults_source/Maliev.Aspire.ServiceDefaults/Maliev.Aspire.ServiceDefaults.csproj"

dotnet restore "$messaging_source/tools/Generator/Generator.csproj" --configfile "$repo_root/nuget.validation.config"
(cd "$messaging_source" && dotnet run --project tools/Generator/Generator.csproj --configuration Release --no-restore)
dotnet restore "$messaging_project" --configfile "$repo_root/nuget.validation.config"
dotnet pack "$messaging_project" --configuration Release --no-restore -p:NoWarn=CS1570 -p:PackageVersion="$messaging_version" --output "$output_dir"
dotnet restore "$defaults_project" --configfile "$repo_root/nuget.validation.config" -p:GITHUB_ACTIONS=true -p:SharedLibraryVersion="$messaging_version"
dotnet pack "$defaults_project" --configuration Release --no-restore -p:GITHUB_ACTIONS=true -p:SharedLibraryVersion="$messaging_version" -p:PackageVersion="$service_defaults_version" --output "$output_dir"

test -s "$output_dir/Maliev.MessagingContracts.$messaging_version.nupkg"
test -s "$output_dir/Maliev.Aspire.ServiceDefaults.$service_defaults_version.nupkg"
(cd "$output_dir" && sha256sum "Maliev.MessagingContracts.$messaging_version.nupkg" "Maliev.Aspire.ServiceDefaults.$service_defaults_version.nupkg" > SHA256SUMS && sha256sum --check SHA256SUMS)
