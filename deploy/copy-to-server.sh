#!/usr/bin/env bash
# Copies this deploy/ folder to a server, for use by deploy.sh/watch.sh there.
# Run this locally, not on the server.
#
# Usage: deploy/copy-to-server.sh user@host [remote-dir]
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

REMOTE="${1:?Usage: $0 user@host [remote-dir]}"
REMOTE_DIR="${2:-jsontocvapi-deploy}"

echo "Copying $script_dir/ to $REMOTE:~/$REMOTE_DIR/"

# --delete keeps the remote folder in sync with this one (e.g. removing scripts that were deleted
# locally), while the excludes protect server-local, never-committed files: an optional .env (e.g.
# for HOST_PORT), deploy state (.current-version), and logs.
rsync -avz --delete \
    --exclude '.env' \
    --exclude '.current-version' \
    --exclude '*.log' \
    "$script_dir/" "$REMOTE:~/$REMOTE_DIR/"

ssh "$REMOTE" "chmod +x ~/$REMOTE_DIR/deploy.sh ~/$REMOTE_DIR/watch.sh"

echo "Done. Scripts are executable at $REMOTE:~/$REMOTE_DIR/"
