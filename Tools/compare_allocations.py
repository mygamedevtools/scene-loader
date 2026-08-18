#!/usr/bin/env python3
"""Diff two allocation reports into a markdown summary.

Informational only: this never exits non-zero for a regression, because these are
editor-playmode figures from a shared CI runner and a threshold tight enough to catch a
real change would fire constantly on noise. Exit codes signal a broken run, not a slow one.

    compare_allocations.py --baseline allocation-baseline.json --current allocation-report.json
"""

import argparse
import json
import sys

# Below this, a change is reported as unchanged. Runner-to-runner variance on a shared
# GitHub runner is worse than the ~10% seen locally, so the band is deliberately wide.
NOISE_BAND_PERCENT = 15.0

# The number tracked per case. `workBytes` is the frame total minus measured ambient: it
# includes the engine's own work, but unlike the marker scopes it misses nothing.
TRACKED = "workBytes"


def load(path):
    with open(path) as handle:
        return json.load(handle)


def format_bytes(value):
    return f"{value:,}"


def delta_cell(base, current):
    if base is None:
        return "new", True
    if current is None:
        return "removed", True

    diff = current - base
    if base == 0:
        return ("no change", False) if diff == 0 else (f"{diff:+,} B", True)

    percent = diff / base * 100.0
    if abs(percent) < NOISE_BAND_PERCENT:
        return "no change", False

    return f"{diff:+,} B ({percent:+.1f}%)", True


def scope_table(name, base_case, current_case):
    """Per-marker detail, shown only for cases whose tracked number actually moved."""
    base_scopes = (base_case or {}).get("scopes", {})
    current_scopes = (current_case or {}).get("scopes", {})

    keys = sorted(set(base_scopes) | set(current_scopes))
    rows = []
    for key in keys:
        base = base_scopes.get(key)
        current = current_scopes.get(key)
        if base == current:
            continue
        rows.append(f"| `{key}` | {format_bytes(base or 0)} | {format_bytes(current or 0)} | {(current or 0) - (base or 0):+,} |")

    if not rows:
        return ""

    header = [
        f"<details><summary>Scope breakdown — {name}</summary>",
        "",
        "| Scope | Base | Head | Delta |",
        "|---|---:|---:|---:|",
    ]
    return "\n".join(header + rows + ["", "</details>", ""])


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--baseline", required=True)
    parser.add_argument("--current", required=True)
    parser.add_argument("--output", help="write markdown here instead of stdout")
    args = parser.parse_args()

    try:
        baseline = load(args.baseline)
    except FileNotFoundError:
        baseline = {"cases": {}}

    current = load(args.current)

    base_cases = baseline.get("cases", {})
    current_cases = current.get("cases", {})

    lines = ["## Allocation report", ""]

    if baseline.get("unityVersion") and baseline["unityVersion"] != current.get("unityVersion"):
        lines.append(
            f"> Baseline was recorded on Unity {baseline['unityVersion']}, this run is "
            f"{current.get('unityVersion')}. Deltas across editor versions are not meaningful."
        )
        lines.append("")

    lines += [
        "| Case | Base | Head | Change | Package | Engine |",
        "|---|---:|---:|---|---:|---:|",
    ]

    moved = []
    for name in sorted(set(base_cases) | set(current_cases)):
        base_case = base_cases.get(name)
        current_case = current_cases.get(name)

        base_value = base_case.get(TRACKED) if base_case else None
        current_value = current_case.get(TRACKED) if current_case else None

        cell, changed = delta_cell(base_value, current_value)
        if changed:
            moved.append(name)

        package = current_case.get("packageBytes", 0) if current_case else 0
        engine = current_case.get("engineBytes", 0) if current_case else 0

        lines.append(
            f"| `{name}` "
            f"| {format_bytes(base_value) if base_value is not None else '—'} "
            f"| {format_bytes(current_value) if current_value is not None else '—'} "
            f"| {cell} "
            f"| {format_bytes(package)} "
            f"| {format_bytes(engine)} |"
        )

    lines.append("")
    lines.append(
        f"`Base`/`Head` are `{TRACKED}` — frame total minus measured ambient "
        f"({current.get('ambientPerFrameBytes', 0):,} B/frame this run). `Package` and `Engine` are "
        "the profiler-marker scopes, which cover synchronous regions only and so account for a "
        "fraction of the total; the rest is async continuations and engine internals. "
        f"Changes under {NOISE_BAND_PERCENT:.0f}% are reported as no change."
    )
    lines.append("")

    for name in moved:
        table = scope_table(name, base_cases.get(name), current_cases.get(name))
        if table:
            lines.append(table)

    if not moved:
        lines.append("No case moved outside the noise band.")

    markdown = "\n".join(lines) + "\n"

    if args.output:
        with open(args.output, "w") as handle:
            handle.write(markdown)
    else:
        sys.stdout.write(markdown)


if __name__ == "__main__":
    main()
