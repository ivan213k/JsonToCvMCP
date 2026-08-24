#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
state_file="$script_dir/.current-version"

IMAGE="ivan213k/jsontocvapi"

latest_version_tag() {
    curl -fsSL "https://hub.docker.com/v2/repositories/${IMAGE}/tags?page_size=100" \
        | jq -r '[.results[] | select(.name != "latest")] | sort_by(.last_updated) | last | .name // empty'
}

REMOTE_VERSION="$(latest_version_tag || true)"

if [[ -z "$REMOTE_VERSION" ]]; then
    echo "Could not reach Docker Hub or no version tags found; skipping." >&2
    exit 0
fi

CURRENT_VERSION="$(cat "$state_file" 2>/dev/null || echo "")"

if [[ "$REMOTE_VERSION" == "$CURRENT_VERSION" ]]; then
    exit 0
fi

echo "New version detected: '$CURRENT_VERSION' -> '$REMOTE_VERSION'"
"$script_dir/deploy.sh" "$REMOTE_VERSION"
