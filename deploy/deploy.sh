#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
state_file="$script_dir/.current-version"

if [[ -f "$script_dir/.env" ]]; then
    set -a
    # shellcheck disable=SC1091
    source "$script_dir/.env"
    set +a
fi

IMAGE="ivan213k/jsontocvapi"
CONTAINER_NAME="jsontocvapi"
NETWORK_NAME="jobsprovider-net"
HOST_PORT="${HOST_PORT:-8080}"
OTEL_EXPORTER_OTLP_ENDPOINT="${OTEL_EXPORTER_OTLP_ENDPOINT-http://aspire-dashboard:18889}"

latest_version_tag() {
    curl -fsSL "https://hub.docker.com/v2/repositories/${IMAGE}/tags?page_size=100" \
        | jq -r '[.results[] | select(.name != "latest")] | sort_by(.last_updated) | last | .name // empty'
}

VERSION="${1:-$(latest_version_tag)}"

if [[ -z "$VERSION" ]]; then
    echo "Could not determine a version tag to deploy." >&2
    exit 1
fi

PREVIOUS_VERSION="$(cat "$state_file" 2>/dev/null || echo "")"

echo "Deploying $IMAGE:$VERSION"
docker pull "$IMAGE:$VERSION"

docker rm -f "$CONTAINER_NAME" >/dev/null 2>&1 || true

docker network create "$NETWORK_NAME" >/dev/null 2>&1 || true

docker run -d \
    --name "$CONTAINER_NAME" \
    --restart unless-stopped \
    --network "$NETWORK_NAME" \
    -p "${HOST_PORT}:8080" \
    ${OTEL_EXPORTER_OTLP_ENDPOINT:+-e "OTEL_EXPORTER_OTLP_ENDPOINT=$OTEL_EXPORTER_OTLP_ENDPOINT"} \
    "$IMAGE:$VERSION"

echo "$VERSION" > "$state_file"
echo "Now running $IMAGE:$VERSION"

docker images "$IMAGE" --format '{{.Tag}}' | sort -u | while read -r tag; do
    if [[ "$tag" != "$VERSION" && "$tag" != "$PREVIOUS_VERSION" && "$tag" != "latest" ]]; then
        echo "Removing old image $IMAGE:$tag"
        docker rmi "$IMAGE:$tag" >/dev/null 2>&1 || echo "  (skipped, still in use)"
    fi
done
