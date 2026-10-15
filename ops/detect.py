#!/usr/bin/env python3
"""Deterministic band detector. Reads current metrics (JSON) and bands.yaml, prints the tier per metric,
and only then decides whether to invoke Claude. Usage: ops/detect.py ops/sample-metrics.json [--invoke]"""
import json, subprocess, sys
from pathlib import Path

def load_bands(path):
    # Tiny reader for the flat bands.yaml shape above, so the detector has no dependencies.
    bands, current = {}, None
    for line in Path(path).read_text().splitlines():
        s = line.split("#")[0].rstrip()
        if s.startswith("  ") and not s.startswith("    ") and s.strip().endswith(":") and not s.strip()[0].isdigit():
            current = s.strip()[:-1]; bands[current] = {}
        elif current and "baseline:" in s:
            inner = s.split("{")[1].split("}")[0]
            bands[current].update({k.strip(): float(v) for k, v in (p.split(":") for p in inner.split(","))})
        elif current and "runbook:" in s:
            bands[current]["runbook"] = s.split(":", 1)[1].strip()
        elif s.startswith("tiers:"):
            current = None
    return bands

def main():
    metrics = json.loads(Path(sys.argv[1]).read_text())
    bands = load_bands(Path(__file__).with_name("bands.yaml"))
    worst = 0
    for name, value in metrics.items():
        b = bands[name]
        sigma = abs(value - b["mean"]) / b["stddev"]
        tier = min(3, int(sigma))
        worst = max(worst, tier)
        print(f"{name}: value={value} sigma={sigma:.1f} tier={tier}")
        if tier >= 2 and "--invoke" in sys.argv:
            mode = "read-only diagnosis" if tier == 2 else "diagnosis plus a PR or the pre-approved runbook"
            prompt = (f"Metric {name} breached tier {tier} (value {value}, {sigma:.1f} sigma). "
                      f"Do a {mode}. Follow {b['runbook']}. Write your diagnosis as a new intent/NNNN-*.md.")
            tools = "Read,Grep,Glob" if tier == 2 else "Read,Grep,Glob,Edit,Write,Bash(dotnet test:*)"
            subprocess.run(["claude", "-p", prompt, "--allowedTools", tools], check=False)
    sys.exit(0 if worst < 2 else 10 + worst)

if __name__ == "__main__":
    main()
