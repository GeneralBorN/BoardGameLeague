#!/usr/bin/env bash
set -euo pipefail

input=$(cat)
log_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
log_file="$log_dir/agent_communications.log"
timestamp="$(date -u +'%Y-%m-%dT%H:%M:%SZ')"

printf '=== %s ===\n%s\n\n' "$timestamp" "$input" >> "$log_file"
printf '{"continue": true}'
