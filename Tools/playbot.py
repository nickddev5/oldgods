"""Plays whole runs with the play bot and writes a run report with balance suggestions.

The Windows player's -playbot mode plays a run the way a careful player would (menu and
unlocks, every biome, bosses, The Last Test, chests, shrines, jumping, sliding and dodging)
and writes one JSON report per run. This tool starts those runs, then summarises them in
report.md and report.json with a short list of rule-based suggestions.

Usage:
  python Tools/playbot.py                        # every god, seeds 1111 and 2222, all content unlocked
  python Tools/playbot.py --gods god.storm --seeds 1111,2222,3333
  python Tools/playbot.py --runs 3 --jobs 2      # 3 seeds per god, two players at once (capped at 24 runs)
  python Tools/playbot.py --campaign 8           # fresh save, 8 runs in a row, the bot buys unlocks between runs
  python Tools/playbot.py --summarise DIR        # rebuild the report from the run JSONs in DIR

Output goes to TestResults/playbot/<time>/ unless --out is given. Build the player first with
Tools/build_windows.py (or pass --build).
"""
from __future__ import annotations

import argparse
import json
import shutil
import subprocess
import sys
import time
from collections import Counter, defaultdict
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
from typing import Any

sys.path.insert(0, str(Path(__file__).resolve().parent))
from unity_paths import DEFAULT_BUILD, PROJECT  # noqa: E402

RESULTS = PROJECT / "TestResults" / "playbot"
DEFAULT_SEEDS = ["1111", "2222"]
# Each run opens a game window. A sweep over this many asks for --max-runs, so nobody's
# desktop fills with windows for an hour by accident.
MAX_RUNS = 24
MAX_JOBS = 3

Run = dict[str, Any]


# ---------- Running the player ----------

def player_command(exe: Path, out: Path, log: Path, save: Path, god: str | None, seed: str | None,
                   picks: str, step: float, width: int, height: int, unlock_all: bool,
                   extra: list[str] | None = None) -> list[str]:
    cmd = [str(exe), "-screen-fullscreen", "0", "-screen-width", str(width), "-screen-height", str(height),
           "-logFile", str(log), "-playbot", "-botOut", str(out), "-botSave", str(save),
           "-botPicks", picks, "-botStep", f"{step:g}"]
    if god:
        cmd += ["-botGod", god]
    if seed:
        cmd += ["-seed", seed]
    if unlock_all:
        cmd.append("-botUnlockAll")
    return cmd + (extra or [])


def seeds_for(args_seeds: str | None, runs: int | None) -> list[str]:
    if args_seeds:
        return [s.strip() for s in args_seeds.split(",") if s.strip()]
    if runs:
        return [f"{0x1111 * (i + 1):X}" for i in range(runs)]
    return list(DEFAULT_SEEDS)


def list_gods(exe: Path, folder: Path, width: int, height: int, timeout: float) -> list[dict[str, Any]]:
    """Asks the player for its gods, so the tool follows the game data rather than a fixed list."""
    out = folder / "content.json"
    cmd = player_command(exe, out, folder / "content.log", folder / "content-save", None, None, "smart", 1 / 30,
                         width, height, True, ["-botList"])
    subprocess.call(cmd, timeout=timeout)
    if not out.exists():
        raise RuntimeError(f"the player wrote no content list; see {folder / 'content.log'}")
    return json.loads(out.read_text(encoding="utf-8"))["content"]["gods"]


def rejected(run: Run, code: int) -> str | None:
    """Why a player report does not count as a run (a typo'd god, a locked god, a crash), or None."""
    if run.get("outcome") in ("error", "locked", "", None):
        return run.get("note") or f"the run did not start ({run.get('outcome') or 'no outcome'})"
    if code != 0:
        return f"the player exited {code}"
    return None


def too_many(runs: int, jobs: int, max_runs: int) -> str | None:
    """Why a sweep should not start: more runs than allowed, or more windows at once than the cap."""
    if runs > max_runs:
        return (f"This would play {runs} runs, each in its own game window; the limit is {max_runs}. "
                f"Use fewer gods or seeds, or pass --max-runs {runs} to allow it.")
    if jobs > MAX_JOBS:
        return f"--jobs {jobs} would open {jobs} game windows at once; the limit is {MAX_JOBS}."
    return None


def fresh(path: Path) -> None:
    """Removes a save folder or report left by an earlier run into the same output folder."""
    if path.is_dir():
        shutil.rmtree(path)
    elif path.exists():
        path.unlink()


def play(job: dict[str, Any], timeout: float) -> Run | None:
    print(f"  playing {job['label']} ...", flush=True)
    started = time.time()
    fresh(job["out"])
    if job.get("fresh_save"):
        fresh(job["save"])
    try:
        code = subprocess.call(job["cmd"], timeout=timeout)
    except subprocess.TimeoutExpired:
        print(f"  {job['label']}: timed out after {timeout:.0f}s; see {job['log']}")
        return None
    out: Path = job["out"]
    if not out.exists():
        print(f"  {job['label']}: no report (exit {code}); see {job['log']}")
        return None
    run = json.loads(out.read_text(encoding="utf-8"))
    why = rejected(run, code)
    if why:
        print(f"  {job['label']}: not counted: {why}; see {job['log']}")
        return None
    print(f"  {job['label']}: {run.get('outcome')} after {run.get('stagesCleared', 0)} stage(s), "
          f"level {run.get('level', 0)}, {run.get('seconds', 0):.0f}s of play in {time.time() - started:.0f}s")
    return run


def load_runs(folder: Path) -> list[Run]:
    runs = []
    for f in sorted(folder.glob("run-*.json")):
        try:
            runs.append(json.loads(f.read_text(encoding="utf-8")))
        except (OSError, json.JSONDecodeError) as e:
            print(f"skipping {f.name}: {e}")
    return runs


# ---------- Summary ----------

def mean(values: list[float]) -> float:
    return sum(values) / len(values) if values else 0.0


def boss_of(source: str) -> str | None:
    """'Stone Warden: Slam' -> 'Stone Warden'."""
    return source.split(":", 1)[0] if ":" in source else None


def summarise(runs: list[Run]) -> dict[str, Any]:
    finished = [r for r in runs if r.get("outcome") in ("won", "died", "timeout", "stage limit")]
    s: dict[str, Any] = {"runs": len(finished)}
    if not finished:
        return s
    s["wins"] = sum(1 for r in finished if r.get("won"))
    s["winRate"] = s["wins"] / len(finished)
    s["outcomes"] = dict(Counter(r["outcome"] for r in finished))
    s["avgStagesCleared"] = mean([r.get("stagesCleared", 0) for r in finished])
    s["avgSeconds"] = mean([r.get("seconds", 0) for r in finished])
    s["avgRealSeconds"] = mean([r.get("realSeconds", 0) for r in finished])
    s["avgSpeedup"] = mean([r.get("speedup", 0) for r in finished])

    gods: dict[str, dict[str, Any]] = {}
    for g, rs in group(finished, lambda r: r.get("godName") or r.get("god", "?")).items():
        gods[g] = {
            "runs": len(rs),
            "wins": sum(1 for r in rs if r.get("won")),
            "winRate": sum(1 for r in rs if r.get("won")) / len(rs),
            "avgStagesCleared": mean([r.get("stagesCleared", 0) for r in rs]),
            "avgLevel": mean([r.get("level", 0) for r in rs]),
            "avgKills": mean([r.get("kills", 0) for r in rs]),
            "killedBy": dict(Counter(r["killedBy"] for r in rs if r.get("killedBy"))),
        }
    s["gods"] = gods

    stages: dict[str, dict[str, Any]] = {}
    for r in finished:
        died_in = r.get("killedInStage", -1) if r.get("outcome") == "died" else -2
        for st in r.get("stages", []):
            name = st.get("biomeName") or st.get("biome") or f"Stage {st.get('index', 0) + 1}"
            e = stages.setdefault(name, {"order": st.get("index", 0), "boss": st.get("boss", ""), "reached": 0, "cleared": 0,
                                         "deaths": 0, "deathsToBoss": 0, "seconds": [], "damage": [], "lowest": [],
                                         "bossSeconds": [], "bossDamage": [], "woke": 0, "chests": [], "level": [],
                                         "stuck": 0, "fellOut": 0, "swarm": 0, "peakAlive": 0, "bossUnfinished": 0})
            e["reached"] += 1
            e["cleared"] += 1 if st.get("cleared") else 0
            e["seconds"].append(st.get("seconds", 0))
            e["damage"].append(st.get("damageTaken", 0))
            e["lowest"].append(st.get("lowestHealth", 1))
            e["chests"].append(st.get("chestsOpened", 0))
            e["level"].append(st.get("levelAtEnd", 0))
            e["stuck"] += st.get("stuck", 0)
            e["fellOut"] += st.get("fellOut", 0)
            e["swarm"] += 1 if st.get("reachedSwarm") else 0
            e["peakAlive"] = max(e["peakAlive"], st.get("peakAlive", 0))
            if st.get("bossWokeAt", -1) >= 0:
                e["woke"] += 1
                if st.get("bossSeconds", -1) < 0 and r.get("outcome") == "timeout" and st is r["stages"][-1]:
                    e["bossUnfinished"] += 1
                e["bossDamage"].append(st.get("bossDamageTaken", 0))
                if st.get("bossSeconds", -1) >= 0:
                    e["bossSeconds"].append(st["bossSeconds"])
            if st.get("index") == died_in:
                e["deaths"] += 1
                if boss_of(r.get("killedBy", "")) == st.get("boss"):
                    e["deathsToBoss"] += 1
    for e in stages.values():
        for k in ("seconds", "damage", "lowest", "bossSeconds", "bossDamage", "chests", "level"):
            e["avg" + k[0].upper() + k[1:]] = mean(e.pop(k))
    s["stages"] = dict(sorted(stages.items(), key=lambda kv: kv[1]["order"]))

    damage: dict[str, float] = defaultdict(float)
    hits: dict[str, int] = defaultdict(int)
    for r in finished:
        for d in r.get("damage", []):
            damage[d["name"]] += d.get("amount", 0)
            hits[d["name"]] += d.get("hits", 0)
    total = sum(damage.values()) or 1.0
    s["damage"] = [{"source": k, "amount": round(v), "share": v / total, "hits": hits[k]}
                   for k, v in sorted(damage.items(), key=lambda kv: -kv[1])]
    s["killers"] = dict(Counter(r["killedBy"] for r in finished if r.get("killedBy")).most_common())
    s["decisions"] = decision_stats(finished)

    cards = [p for r in finished for p in r.get("draft", [])]
    restore_from = [min((p["level"] for p in r.get("draft", []) if p.get("kind") == "Restore"), default=None) for r in finished]
    restore_from = [x for x in restore_from if x is not None]
    s["draft"] = {"cards": len(cards), "restoreShare": sum(1 for p in cards if p.get("kind") == "Restore") / len(cards) if cards else 0.0,
                  "restoreFromLevel": mean(restore_from)}
    s["weapons"] = weapon_stats(finished)
    s["passives"] = passive_stats(finished)

    s["economy"] = {
        "avgGoldEarned": mean([r.get("goldEarned", 0) for r in finished]),
        "avgGoldLeft": mean([r.get("goldLeft", 0) for r in finished]),
        "avgChests": mean([r.get("chestsOpened", 0) for r in finished]),
        "avgShrines": mean([r.get("shrinesUsed", 0) for r in finished]),
        "avgItems": mean([r.get("itemsFound", 0) for r in finished]),
        "avgEmbers": mean([r.get("embersEarned", 0) for r in finished]),
        "purchases": [p for r in finished for p in r.get("purchases", [])],
    }
    s["moves"] = {k: mean([r.get("moves", {}).get(k, 0) for r in finished]) for k in ("jumps", "slides", "dodges", "interactions", "metres")}
    s["stuckSpots"] = clusters(finished, "stuck")
    s["fellOut"] = clusters(finished, "fellOut")
    perf = [r.get("perf", {}) for r in finished if r.get("perf", {}).get("frames", 0) > 0]
    s["perf"] = {
        "fpsMean": mean([p["fpsMean"] for p in perf]),
        "fpsLow1": min([p["fpsLow1"] for p in perf], default=0.0),
        "fpsMin": min([p["fpsMin"] for p in perf], default=0.0),
        "peakAlive": max([p["peakAlive"] for p in perf], default=0),
        "simMsMean": mean([p["simMsMean"] for p in perf]),
        "simMsMax": max([p["simMsMax"] for p in perf], default=0.0),
    }
    s["content"] = finished[-1].get("content", {})
    return s


def group(runs: list[Run], key) -> dict[str, list[Run]]:
    out: dict[str, list[Run]] = defaultdict(list)
    for r in runs:
        out[key(r)].append(r)
    return dict(out)


def decision_stats(runs: list[Run]) -> dict[str, list[dict[str, Any]]]:
    """Share of play time on each branch of the bot's goal and stance trees, most used first."""
    goal: dict[str, float] = defaultdict(float)
    stance: dict[str, float] = defaultdict(float)
    for r in runs:
        for d in r.get("decisions", []):
            name, seconds = d["name"], d.get("amount", 0.0)
            if name.startswith("Stance: "):
                stance[name[len("Stance: "):]] += seconds
            else:
                goal[name] += seconds

    def shares(table: dict[str, float]) -> list[dict[str, Any]]:
        total = sum(table.values()) or 1.0
        return [{"branch": k, "seconds": round(v), "share": v / total} for k, v in sorted(table.items(), key=lambda kv: -kv[1])]

    return {"goal": shares(goal), "stance": shares(stance)}


def weapon_stats(runs: list[Run]) -> dict[str, dict[str, Any]]:
    """Per weapon: how often it was offered and taken, held at the end, its share of damage, and wins."""
    names = {w["id"]: w["name"] for r in runs for w in r.get("content", {}).get("weapons", [])}
    unlocked = {w["id"] for r in runs for w in r.get("content", {}).get("weapons", []) if w.get("unlocked", True)}
    starting = {g.get("weapon") for r in runs for g in r.get("content", {}).get("gods", [])}
    stats: dict[str, dict[str, Any]] = {wid: {"name": n, "offered": 0, "taken": 0, "held": 0, "wins": 0, "shares": [], "starting": wid in starting,
                                              "unlocked": wid in unlocked} for wid, n in names.items()}
    for r in runs:
        total = sum(w.get("damage", 0) for w in r.get("weapons", [])) or 1.0
        for p in r.get("draft", []):
            for o in p.get("offered", []):
                kind, _, wid = o.partition(" ")
                if kind in ("NewWeapon", "UpgradeWeapon") and wid in stats:
                    stats[wid]["offered"] += 1
            if p.get("kind") in ("NewWeapon", "UpgradeWeapon") and p.get("taken") in stats:
                stats[p["taken"]]["taken"] += 1
        for w in r.get("weapons", []):
            e = stats.setdefault(w["id"], {"name": w.get("name", w["id"]), "offered": 0, "taken": 0, "held": 0, "wins": 0, "shares": [], "starting": False})
            e["held"] += 1
            e["wins"] += 1 if r.get("won") else 0
            e["shares"].append(w.get("damage", 0) / total)
    for e in stats.values():
        e["avgShare"] = mean(e.pop("shares"))
        e["winRate"] = e["wins"] / e["held"] if e["held"] else 0.0
    return stats


def passive_stats(runs: list[Run]) -> dict[str, dict[str, Any]]:
    names = {p["id"]: p["name"] for r in runs for p in r.get("content", {}).get("passives", [])}
    unlocked = {p["id"] for r in runs for p in r.get("content", {}).get("passives", []) if p.get("unlocked", True)}
    stats = {pid: {"name": n, "offered": 0, "taken": 0, "held": 0, "unlocked": pid in unlocked} for pid, n in names.items()}
    for r in runs:
        for p in r.get("draft", []):
            for o in p.get("offered", []):
                kind, _, pid = o.partition(" ")
                if kind in ("NewPassive", "UpgradePassive") and pid in stats:
                    stats[pid]["offered"] += 1
            if p.get("kind") in ("NewPassive", "UpgradePassive") and p.get("taken") in stats:
                stats[p["taken"]]["taken"] += 1
        for h in r.get("passives", []):
            if h["id"] in stats:
                stats[h["id"]]["held"] += 1
    return stats


def clusters(runs: list[Run], field: str, cell: float = 12.0) -> list[dict[str, Any]]:
    """Spots grouped by stage and nearness (within `cell` metres of a group's first spot), most frequent first."""
    groups: list[tuple[str, dict[str, Any], list[dict[str, Any]]]] = []
    for r in runs:
        biomes = {st.get("index"): st.get("biomeName", "") for st in r.get("stages", [])}
        for spot in r.get(field, []):
            biome = biomes.get(spot.get("stage"), f"Stage {spot.get('stage', 0) + 1}")
            for b, first, members in groups:
                if b == biome and (first["x"] - spot["x"]) ** 2 + (first["z"] - spot["z"]) ** 2 <= cell * cell:
                    members.append(spot)
                    break
            else:
                groups.append((biome, spot, [spot]))
    out = []
    for biome, _, spots in groups:
        out.append({"biome": biome, "x": round(mean([p["x"] for p in spots])), "z": round(mean([p["z"] for p in spots])),
                    "count": len(spots), "doing": Counter(p.get("doing", "") for p in spots).most_common(1)[0][0]})
    return sorted(out, key=lambda c: -c["count"])


# ---------- Suggestions ----------

def pct(x: float) -> str:
    return f"{x * 100:.0f}%"


def suggestions(s: dict[str, Any]) -> list[str]:
    """Plain rules over the numbers. Each names what it saw, so Nick can judge it."""
    out: list[str] = []
    n = s.get("runs", 0)
    if not n:
        return ["No run finished, so there is nothing to judge. Check the player logs."]

    timeouts = s.get("outcomes", {}).get("timeout", 0)
    if timeouts:
        out.append(f"{timeouts} of {n} runs timed out instead of ending. Read the stuck spots below; the bot may not reach the gate or portal there.")

    for name, st in s.get("stages", {}).items():
        boss = st.get("boss") or "the boss"
        if st["woke"] >= 2 and st["deathsToBoss"] / st["woke"] >= 0.5:
            top = top_attack(s, boss)
            out.append(f"{boss} killed {st['deathsToBoss']} of {st['woke']} runs that woke it"
                       + (f"; most of its damage comes from {top}" if top else "")
                       + ". Consider less damage or a longer telegraph.")
        elif st["woke"] >= 2 and st.get("bossUnfinished", 0) >= max(2, st["woke"] // 6):
            out.append(f"{boss} was still standing when {st['bossUnfinished']} of {st['woke']} runs ran out of time, though the bot was not dying. "
                       "Weapons aim at the nearest enemy, so a thick horde soaks the shots; consider making bosses a priority target or thinning the horde during the fight.")
        elif st["woke"] >= 2 and st["avgBossSeconds"] > 0 and st["avgBossSeconds"] < 25 and st["avgBossDamage"] < 15:
            out.append(f"{boss} falls in {st['avgBossSeconds']:.0f}s on average and deals {st['avgBossDamage']:.0f} damage. It may be too easy.")
        other_deaths = st["deaths"] - st["deathsToBoss"]
        if st["reached"] >= 2 and other_deaths / st["reached"] >= 0.5:
            out.append(f"{name}: {other_deaths} of {st['reached']} runs died to the horde before the boss. The stage may ramp too fast.")
        if st["reached"] >= 2 and st["cleared"] == st["reached"] and st["avgLowest"] >= 0.7:
            out.append(f"{name} never got the bot below {pct(st['avgLowest'])} health on average. It may be too easy.")
        if st["swarm"] and st["reached"]:
            out.append(f"{name}: {st['swarm']} of {st['reached']} runs were still there when the final swarm began.")

    gods = s.get("gods", {})
    if len(gods) >= 2:
        avg = s.get("avgStagesCleared", 0)
        for g, e in gods.items():
            if e["runs"] >= 2 and e["avgStagesCleared"] <= avg - 1:
                out.append(f"{g} clears {e['avgStagesCleared']:.1f} stages against an average of {avg:.1f}. The god may be underpowered.")
            if e["runs"] >= 2 and e["avgStagesCleared"] >= avg + 1:
                out.append(f"{g} clears {e['avgStagesCleared']:.1f} stages against an average of {avg:.1f}. The god may be overpowered.")

    for w in s.get("weapons", {}).values():
        if n >= 4 and w.get("unlocked", True) and w["offered"] == 0 and w["held"] == 0:
            out.append(f"{w['name']} was unlocked but never offered or held in {n} runs. Check the draft.")
        elif w["held"] >= 2 and w["avgShare"] < 0.08:
            out.append(f"{w['name']} deals only {pct(w['avgShare'])} of weapon damage when held. It may be too weak.")
        elif w["held"] >= 2 and not w.get("starting") and w["avgShare"] > 0.6:
            out.append(f"{w['name']} deals {pct(w['avgShare'])} of weapon damage when held. It may be too strong.")
    for p in s.get("passives", {}).values():
        if n >= 4 and p.get("unlocked", True) and p["offered"] == 0 and p["held"] == 0:
            out.append(f"Passive {p['name']} was unlocked but never offered or held in {n} runs.")

    picks = s.get("draft", {})
    if picks.get("cards", 0) >= 50 and picks.get("restoreShare", 0) >= 0.25:
        out.append(f"{pct(picks['restoreShare'])} of level-ups offered only Restore: every slot was full and maxed, from level {picks['restoreFromLevel']:.0f} on average. "
                   "Late runs outgrow the draft; consider more slots, higher caps or fewer levels.")

    dmg = {d["source"]: d["share"] for d in s.get("damage", [])}
    if dmg.get("Fall", 0) >= 0.05:
        out.append(f"Falls deal {pct(dmg['Fall'])} of all damage taken. Cliffs may be too punishing, or the bot walks off them.")

    for c in s.get("stuckSpots", []):
        if c["count"] >= 2:
            out.append(f"The bot got stuck {c['count']} times near ({c['x']}, {c['z']}) in {c['biome']} while going for {c['doing'].lower()}. Check the level there.")
    for c in s.get("fellOut", []):
        out.append(f"The player fell out of the world {c['count']} time(s) near ({c['x']}, {c['z']}) in {c['biome']}. There is a hole in the ground's collision.")

    eco = s.get("economy", {})
    stage_count = sum(st["reached"] for st in s.get("stages", {}).values() if not st.get("final")) or 1
    if eco.get("avgChests", 0) * n / stage_count < 1.5:
        out.append(f"Only {eco.get('avgChests', 0):.1f} chests opened per run. Gold may be too scarce or chests too dear.")
    if eco.get("avgGoldLeft", 0) >= 150:
        out.append(f"Runs end with {eco['avgGoldLeft']:.0f} unspent gold on average. There may be too little to spend it on.")

    perf = s.get("perf", {})
    if perf.get("fpsLow1", 0) and perf["fpsLow1"] < 30:
        out.append(f"1% low frame rate is {perf['fpsLow1']:.0f} fps (peak horde {perf.get('peakAlive', 0)}). Look at the horde and effects cost.")
    if perf.get("simMsMax", 0) > 8:
        out.append(f"Horde simulation peaked at {perf['simMsMax']:.1f} ms in one frame.")

    content = s.get("content", {})
    locked = [g for g in content.get("gods", []) if g.get("cost", 0) > 0]
    if locked and eco.get("avgEmbers", 0) > 0:
        total = sum(g["cost"] for g in locked)
        out.append(f"Runs earn {eco['avgEmbers']:.0f} Embers on average, so unlocking every god ({total} Embers) takes about {total / eco['avgEmbers']:.0f} runs like these.")
    return out


def top_attack(s: dict[str, Any], boss: str) -> str | None:
    attacks = [d for d in s.get("damage", []) if boss_of(d["source"]) == boss]
    if not attacks:
        return None
    total = sum(d["amount"] for d in attacks) or 1
    best = attacks[0]
    return f"{best['source'].split(': ', 1)[1]} ({pct(best['amount'] / total)})"


# ---------- Markdown ----------

def clock(seconds: float) -> str:
    return f"{int(seconds) // 60}:{int(seconds) % 60:02d}"


def render(s: dict[str, Any], tips: list[str], runs: list[Run]) -> str:
    lines = ["# Play-bot report", ""]
    if not s.get("runs"):
        return "\n".join(lines + ["No runs finished."]) + "\n"
    lines += [f"{s['runs']} runs, {s['wins']} won ({pct(s['winRate'])}), {s['avgStagesCleared']:.1f} stages cleared on average. "
              f"A run took {clock(s['avgSeconds'])} of game time in {clock(s['avgRealSeconds'])} ({s['avgSpeedup']:.1f}x real time).", ""]
    lines += ["## Suggestions", ""] + [f"- {t}" for t in tips] + [""]

    lines += ["## Runs", "", "| God | Seed | Outcome | Stages | Level | Kills | Time | Killed by |", "|---|---|---|---|---|---|---|---|"]
    for r in runs:
        lines.append(f"| {r.get('godName', r.get('god'))} | {r.get('seed')} | {r.get('outcome')} | {r.get('stagesCleared', 0)} | {r.get('level', 0)} | "
                     f"{r.get('kills', 0)} | {clock(r.get('seconds', 0))} | {r.get('killedBy', '')} |")
    lines.append("")

    lines += ["## Gods", "", "| God | Runs | Win rate | Stages | Level | Kills |", "|---|---|---|---|---|---|"]
    for g, e in s["gods"].items():
        lines.append(f"| {g} | {e['runs']} | {pct(e['winRate'])} | {e['avgStagesCleared']:.1f} | {e['avgLevel']:.1f} | {e['avgKills']:.0f} |")
    lines.append("")

    lines += ["## Stages", "", "| Stage | Reached | Cleared | Deaths (boss) | Time | Lowest health | Boss fight | Boss damage | Chests | Level at end | Stuck |",
              "|---|---|---|---|---|---|---|---|---|---|---|"]
    for name, e in s["stages"].items():
        lines.append(f"| {name} | {e['reached']} | {e['cleared']} | {e['deaths']} ({e['deathsToBoss']}) | {clock(e['avgSeconds'])} | {pct(e['avgLowest'])} | "
                     f"{e['avgBossSeconds']:.0f}s | {e['avgBossDamage']:.0f} | {e['avgChests']:.1f} | {e['avgLevel']:.1f} | {e['stuck']} |")
    lines.append("")

    lines += ["## Damage taken by source", "", "| Source | Damage | Share | Hits |", "|---|---|---|---|"]
    for d in s["damage"][:15]:
        lines.append(f"| {d['source']} | {d['amount']} | {pct(d['share'])} | {d['hits']} |")
    lines.append("")

    lines += ["## Weapons", "", "| Weapon | Offered | Taken | Held at end | Damage share | Win rate when held |", "|---|---|---|---|---|---|"]
    for w in sorted(s["weapons"].values(), key=lambda w: -w["held"]):
        lines.append(f"| {w['name']}{' (start)' if w.get('starting') else ''} | {w['offered']} | {w['taken']} | {w['held']} | {pct(w['avgShare'])} | {pct(w['winRate'])} |")
    lines.append("")
    lines += ["## Passives", "", "| Passive | Offered | Taken | Held at end |", "|---|---|---|---|"]
    for p in sorted(s["passives"].values(), key=lambda p: -p["held"]):
        lines.append(f"| {p['name']} | {p['offered']} | {p['taken']} | {p['held']} |")
    lines.append("")

    dec = s.get("decisions", {})
    if dec.get("goal"):
        lines += ["## How the bot spent its time", "",
                  "Each row is a leaf of the bot's goal tree (the last question asked, its answer, and what the bot did).", "",
                  "| Decision | Share of play time |", "|---|---|"]
        lines += [f"| {d['branch']} | {pct(d['share'])} |" for d in dec["goal"][:15]]
        lines += ["", "| Stance toward the horde | Share of play time |", "|---|---|"]
        lines += [f"| {d['branch']} | {pct(d['share'])} |" for d in dec["stance"]]
        lines.append("")

    e, m, p = s["economy"], s["moves"], s["perf"]
    lines += ["## Economy, movement and performance", "",
              f"- Gold earned {e['avgGoldEarned']:.0f}, left unspent {e['avgGoldLeft']:.0f}; chests {e['avgChests']:.1f}, shrines {e['avgShrines']:.1f}, items {e['avgItems']:.1f}; Embers {e['avgEmbers']:.0f} per run.",
              f"- Level-ups: {s['draft']['cards']} cards taken in all, {pct(s['draft']['restoreShare'])} of them Restore (nothing else left to offer).",
              f"- Per run: {m['jumps']:.0f} jumps, {m['slides']:.0f} slides, {m['dodges']:.0f} telegraph dodges, {m['interactions']:.0f} interactions, {m['metres']:.0f} m walked.",
              f"- Frame rate {p['fpsMean']:.0f} fps mean, {p['fpsLow1']:.0f} fps 1% low, {p['fpsMin']:.0f} fps worst frame; peak horde {p['peakAlive']}; horde simulation {p['simMsMean']:.2f} ms mean, {p['simMsMax']:.2f} ms max."]
    if e["purchases"]:
        lines.append(f"- Bought between runs: {', '.join(e['purchases'])}.")
    if s["stuckSpots"]:
        lines.append("- Stuck spots: " + "; ".join(f"{c['biome']} ({c['x']}, {c['z']}) x{c['count']}" for c in s["stuckSpots"][:10]) + ".")
    if s["fellOut"]:
        lines.append("- Fell out of the world: " + "; ".join(f"{c['biome']} ({c['x']}, {c['z']}) x{c['count']}" for c in s["fellOut"]) + ".")
    lines.append("")
    lines += ["The bot is a steady, careful player, not a skilled one: it kites (or, holding a close-range weapon, stays at that weapon's reach), takes the best-looking card, opens what it can afford, "
              "leaves Greed, Curse and Challenge shrines alone and wakes each boss at 70% of the stage clock (later if below half health). Read its numbers as a floor.", ""]
    return "\n".join(lines)


def write_report(folder: Path, runs: list[Run]) -> tuple[dict[str, Any], list[str]]:
    s = summarise(runs)
    tips = suggestions(s)
    (folder / "report.json").write_text(json.dumps({"summary": s, "suggestions": tips}, indent=2), encoding="utf-8")
    (folder / "report.md").write_text(render(s, tips, runs), encoding="utf-8")
    return s, tips


# ---------- Command line ----------

def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Old Gods play bot: whole runs, a report and suggestions")
    parser.add_argument("--gods", default="all", help="'all' or comma-separated god ids from the game data")
    parser.add_argument("--seeds", help="comma-separated hex seeds (default 1111,2222)")
    parser.add_argument("--runs", type=int, help="seeds per god, instead of --seeds")
    parser.add_argument("--campaign", type=int, metavar="N", help="N runs on one fresh save; the bot buys unlocks and plays its newest god")
    parser.add_argument("--picks", default="smart", choices=["smart", "first", "random"], help="how the bot takes level-up cards")
    parser.add_argument("--step", type=float, default=1 / 30, help="game seconds per frame; the run goes as fast as frames render")
    parser.add_argument("--jobs", type=int, default=1, help=f"players (game windows) at once, at most {MAX_JOBS}")
    parser.add_argument("--max-runs", type=int, default=MAX_RUNS, help=f"refuse to start more runs than this (default {MAX_RUNS})")
    parser.add_argument("--exe", type=Path, default=DEFAULT_BUILD)
    parser.add_argument("--out", type=Path, help="output folder (default TestResults/playbot/<time>)")
    parser.add_argument("--timeout", type=float, default=1800.0, help="real seconds allowed per run")
    parser.add_argument("--width", type=int, default=960)
    parser.add_argument("--height", type=int, default=540)
    parser.add_argument("--build", action="store_true", help="build the player first")
    parser.add_argument("--summarise", type=Path, metavar="DIR", help="only rebuild the report from DIR's run JSONs")
    parser.add_argument("extra", nargs="*", help="extra player arguments, after --")
    args = parser.parse_args(argv)

    if args.summarise:
        runs = load_runs(args.summarise)
        _, tips = write_report(args.summarise, runs)
        print(f"{len(runs)} runs; report: {args.summarise / 'report.md'}")
        return 0

    if args.build:
        import build_windows
        code = build_windows.main([])
        if code != 0:
            return code
    if not args.exe.exists():
        print(f"Player not found at {args.exe}; run Tools/build_windows.py first (or pass --build).")
        return 2

    folder = args.out or RESULTS / time.strftime("%Y%m%d-%H%M%S")
    folder.mkdir(parents=True, exist_ok=True)
    seeds = seeds_for(args.seeds, args.runs)
    jobs: list[dict[str, Any]] = []

    def job(label: str, god: str | None, seed: str | None, save: Path, unlock_all: bool, fresh_save: bool) -> dict[str, Any]:
        out = folder / f"run-{label}.json"
        log = folder / f"run-{label}.log"
        return {"label": label, "out": out, "log": log, "save": save, "fresh_save": fresh_save,
                "cmd": player_command(args.exe, out, log, save, god, seed, args.picks, args.step, args.width, args.height, unlock_all, args.extra)}

    if args.campaign:
        # One save carried from run to run; each run must finish before the next starts.
        save = folder / "campaign-save"
        for i in range(args.campaign):
            seed = seeds[i % len(seeds)] if args.seeds else f"{0x1111 * (i + 1):X}"
            # Only the first run starts from an empty save; the rest carry it on.
            jobs.append(job(f"{i + 1:02d}-campaign-{seed}", "newest", seed, save, False, fresh_save=i == 0))
        args.jobs = 1
    else:
        if args.gods == "all":
            gods = [g["id"] for g in list_gods(args.exe, folder, args.width, args.height, 300)]
        else:
            gods = [g.strip() for g in args.gods.split(",") if g.strip()]
        print(f"Gods: {', '.join(gods)}; seeds: {', '.join(seeds)}")
        for god in gods:
            for seed in seeds:
                label = f"{god.replace('god.', '')}-{seed}"
                jobs.append(job(label, god, seed, folder / f"save-{label}", True, fresh_save=True))

    problem = too_many(len(jobs), args.jobs, args.max_runs)
    if problem:
        print(problem)
        return 2
    print(f"Playing {len(jobs)} run(s) into {folder}, {args.jobs} game window(s) open at a time")
    if args.jobs > 1:
        with ThreadPoolExecutor(max_workers=args.jobs) as pool:
            results = list(pool.map(lambda j: play(j, args.timeout), jobs))
    else:
        results = [play(j, args.timeout) for j in jobs]
    runs = [r for r in results if r]
    s, tips = write_report(folder, runs)
    print()
    print(f"{s.get('runs', 0)} runs, {s.get('wins', 0)} won, {s.get('avgStagesCleared', 0):.1f} stages cleared on average")
    for t in tips:
        print(f"- {t}")
    print(f"Report: {folder / 'report.md'}")
    if len(runs) < len(jobs):
        print(f"{len(jobs) - len(runs)} of {len(jobs)} run(s) did not count; see the lines above.")
    return 0 if runs and len(runs) == len(jobs) else 1


if __name__ == "__main__":
    sys.exit(main())
