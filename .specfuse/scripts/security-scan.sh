#!/usr/bin/env bash
# Security scan gate for the component agent verification contract.
#
# Runs `dotnet list package --vulnerable --include-transitive`, emits the JSON
# report to ./security-scan.json, and exits non-zero if any vulnerability of
# severity High or Critical is present. Moderate/Low findings are recorded in
# the report but do not fail the gate.
#
# Invoked both by the component agent verification skill (via the command
# declared in .specfuse/verification.yml) and by CI (.github/workflows/ci.yml).
# The two must stay aligned by design.

set -euo pipefail

REPORT_PATH="${REPORT_PATH:-./security-scan.json}"

dotnet list package --vulnerable --include-transitive --format json > "$REPORT_PATH"

python3 - "$REPORT_PATH" <<'PY'
import json
import sys

report_path = sys.argv[1]
with open(report_path) as f:
    data = json.load(f)

critical = []
high = []
for project in data.get("projects", []):
    for framework in project.get("frameworks", []):
        for key in ("topLevelPackages", "transitivePackages"):
            for pkg in framework.get(key, []):
                for vuln in pkg.get("vulnerabilities", []):
                    severity = vuln.get("severity", "")
                    entry = (
                        f"{project['path']} :: "
                        f"{pkg['id']}@{pkg['resolvedVersion']} "
                        f"({severity}) -> {vuln.get('advisoryurl', '')}"
                    )
                    if severity == "Critical":
                        critical.append(entry)
                    elif severity == "High":
                        high.append(entry)

if critical or high:
    print(
        f"FAIL: {len(critical)} Critical, {len(high)} High "
        f"vulnerabilities found",
        file=sys.stderr,
    )
    for line in critical + high:
        print(f"  - {line}", file=sys.stderr)
    sys.exit(1)

print(f"PASS: 0 High or Critical vulnerabilities (report at {report_path})")
PY
