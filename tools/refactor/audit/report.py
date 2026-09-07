"""Writes the audit results as audit.json and audit-report.md."""
from __future__ import annotations

import json
from datetime import datetime, timezone
from pathlib import Path

from .gate import RATCHETED_FIGURES

SUMMARY_ORDER = [
    "fileLength", "functionShape", "functionNames", "duplication", "naming",
    "comments", "magicValues", "prose", "inventory",
]


def flattenSummaries(results: dict) -> dict:
    return {name: results[name]["summary"] for name in SUMMARY_ORDER if name in results}


def writeJson(results: dict, outputDirectory: Path) -> Path:
    payload = {
        "generatedAt": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "summaries": flattenSummaries(results),
        "details": results,
    }
    outputPath = outputDirectory / "audit.json"
    outputPath.write_text(json.dumps(payload, indent=2))
    return outputPath


def headline(results: dict) -> str:
    fileLength = results["fileLength"]["summary"]
    overLimit, total = fileLength["filesOverLimit"], fileLength["totalFiles"]
    share = 100 * overLimit / max(total, 1)
    return (
        f"**{overLimit:,} of {total:,} source files are over the {fileLength['limit']}-line limit "
        f"({share:.1f}%)**; the worst file is {fileLength['worstFileLines']:,} lines."
    )


def summaryTable(results: dict) -> str:
    rows = ["| Check | Key figures |", "| --- | --- |"]
    for name, summary in flattenSummaries(results).items():
        figures = ", ".join(f"{key}: {value}" for key, value in summary.items())
        rows.append(f"| {name} | {figures} |")
    return "\n".join(rows)


def baselineTable(results: dict, baselinePath: Path) -> str:
    if not baselinePath.exists():
        return "No `baseline.json` beside the audit — nothing to ratchet against."
    baseline = json.loads(baselinePath.read_text())["summaries"]
    current = flattenSummaries(results)
    rows = ["| Ratcheted figure | Baseline | Now | Verdict |", "| --- | --- | --- | --- |"]
    for checkName, figureName in RATCHETED_FIGURES:
        was, now = baseline.get(checkName, {}).get(figureName), current.get(checkName, {}).get(figureName)
        verdict = "—" if was is None or now is None else "worse" if now > was else "better" if now < was else "held"
        rows.append(f"| {checkName}.{figureName} | {was} | {now} | {verdict} |")
    return "\n".join(rows)


def worstFilesSection(results: dict) -> str:
    offenders = results.get("fileLength", {}).get("offenders", [])[:20]
    if not offenders:
        return "All files are within the limit."
    rows = ["| File | Lines |", "| --- | --- |"]
    rows += [f"| {offender['file']} | {offender['lines']} |" for offender in offenders]
    return "\n".join(rows)


def writeMarkdown(results: dict, outputDirectory: Path) -> Path:
    baselinePath = outputDirectory.parent / "baseline.json"
    body = "\n\n".join([
        "# Refactor audit",
        f"Generated {datetime.now(timezone.utc).strftime('%Y-%m-%d %H:%M UTC')}.",
        "## Headline", headline(results),
        "## Summary", summaryTable(results),
        "## Against the baseline", baselineTable(results, baselinePath),
        "## Worst files by length", worstFilesSection(results),
        "Full detail, including every offender list, is in `audit.json`.",
    ])
    outputPath = outputDirectory / "audit-report.md"
    outputPath.write_text(body + "\n")
    return outputPath
