#!/usr/bin/env bash
set -euo pipefail

name=$1
output=$2
assets=$(gh api "repos/$GITHUB_REPOSITORY/releases/$RELEASE_ID/assets")
id=$(jq -er --arg name "$name" '.[] | select(.name == $name and .state == "uploaded") | .id' <<<"$assets")
gh api -H 'Accept: application/octet-stream' "repos/$GITHUB_REPOSITORY/releases/assets/$id" > "$output"
digest=$(jq -r --arg name "$name" '.[] | select(.name == $name) | .digest // ""' <<<"$assets")
if [[ $digest == sha256:* ]]; then
  if command -v sha256sum >/dev/null; then
    hash=(sha256sum)
  else
    hash=(shasum -a 256)
  fi
  printf '%s  %s\n' "${digest#sha256:}" "$output" | "${hash[@]}" -c -
fi
