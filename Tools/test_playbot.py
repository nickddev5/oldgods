"""Tests for Tools/playbot.py: the player command, the summary and the suggestion rules."""
from __future__ import annotations

import json
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import playbot  # noqa: E402

CONTENT = {
    "gods": [{"id": "god.storm", "name": "Storm", "weapon": "w.bolt", "unlocked": True, "cost": 0},
             {"id": "god.forge", "name": "Forge", "weapon": "w.hammer", "unlocked": True, "cost": 150}],
    "weapons": [{"id": "w.bolt", "name": "Bolt"}, {"id": "w.hammer", "name": "Hammer"}, {"id": "w.orb", "name": "Orb"},
                {"id": "w.unused", "name": "Unused"}],
    "passives": [{"id": "p.swift", "name": "Swift"}, {"id": "p.never", "name": "Never"}],
}


def stage(index, name, boss, cleared=True, woke=100.0, boss_seconds=40.0, boss_damage=30.0, lowest=0.5, stuck=0, fell=0):
    return {"index": index, "biome": name.lower(), "biomeName": name, "boss": boss, "cleared": cleared, "final": False,
            "seconds": 500, "bossWokeAt": woke, "bossSeconds": boss_seconds, "bossDamageTaken": boss_damage,
            "damageTaken": 80, "lowestHealth": lowest, "chestsOpened": 3, "levelAtEnd": 10 + index * 8,
            "stuck": stuck, "fellOut": fell, "reachedSwarm": False, "peakAlive": 200}


def run(god="Storm", seed="1111", outcome="died", killed_by="Stone Warden: Slam", killed_in=0, stages=None, won=False,
        weapons=None, fps_low=55.0, stuck=None, fell=None):
    stages = stages if stages is not None else [stage(0, "Grey Steppe", "Stone Warden", cleared=False)]
    return {
        "god": "god." + god.lower(), "godName": god, "seed": seed, "outcome": outcome, "won": won,
        "killedBy": killed_by if outcome == "died" else "", "killedInStage": killed_in if outcome == "died" else -1,
        "stagesCleared": sum(1 for s in stages if s["cleared"]), "level": 12, "kills": 900, "seconds": 600,
        "realSeconds": 150, "speedup": 4.0, "goldEarned": 400, "goldLeft": 40, "chestsOpened": 4, "shrinesUsed": 2,
        "itemsFound": 4, "embersEarned": 50, "purchases": [],
        "stages": stages,
        "damage": [{"name": "Stone Warden: Slam", "amount": 120, "hits": 4}, {"name": "Stone Warden: contact", "amount": 20, "hits": 2},
                   {"name": "Husk", "amount": 60, "hits": 30}],
        "draft": [{"t": 20, "stage": 0, "level": 2, "offered": ["NewWeapon w.orb", "NewPassive p.swift", "UpgradeWeapon w.bolt"],
                   "taken": "w.orb", "kind": "NewWeapon", "name": "Orb", "rarity": "Common"}],
        "weapons": weapons if weapons is not None else [{"id": "w.bolt", "name": "Bolt", "level": 3, "damage": 9000},
                                                       {"id": "w.orb", "name": "Orb", "level": 1, "damage": 300}],
        "passives": [{"id": "p.swift", "name": "Swift", "level": 1}],
        "stuck": stuck or [], "fellOut": fell or [],
        "moves": {"jumps": 10, "slides": 4, "dodges": 6, "interactions": 5, "metres": 3000},
        "perf": {"frames": 1000, "fpsMean": 120, "fpsLow1": fps_low, "fpsMin": 40, "peakAlive": 300, "simMsMean": 1.0, "simMsMax": 3.0},
        "content": CONTENT,
    }


class CommandTests(unittest.TestCase):
    def test_player_command_carries_the_bot_switches(self):
        cmd = playbot.player_command(Path("p.exe"), Path("o.json"), Path("l.log"), Path("save"), "god.storm", "1111",
                                     "smart", 1 / 30, 960, 540, True, ["-noDressing"])
        self.assertIn("-playbot", cmd)
        self.assertEqual(cmd[cmd.index("-botGod") + 1], "god.storm")
        self.assertEqual(cmd[cmd.index("-seed") + 1], "1111")
        self.assertEqual(cmd[cmd.index("-botSave") + 1], "save")
        self.assertIn("-botUnlockAll", cmd)
        self.assertEqual(cmd[-1], "-noDressing")

    def test_campaign_command_leaves_unlocks_to_the_bot(self):
        cmd = playbot.player_command(Path("p.exe"), Path("o.json"), Path("l.log"), Path("save"), "newest", None,
                                     "first", 0.05, 960, 540, False)
        self.assertNotIn("-botUnlockAll", cmd)
        self.assertNotIn("-seed", cmd)
        self.assertEqual(cmd[cmd.index("-botStep") + 1], "0.05")

    def test_seeds(self):
        self.assertEqual(playbot.seeds_for(None, None), ["1111", "2222"])
        self.assertEqual(playbot.seeds_for("a, b", 5), ["a", "b"])
        self.assertEqual(playbot.seeds_for(None, 3), ["1111", "2222", "3333"])


class SummaryTests(unittest.TestCase):
    def test_empty(self):
        self.assertEqual(playbot.summarise([])["runs"], 0)
        self.assertTrue(playbot.suggestions(playbot.summarise([])))

    def test_counts_boss_deaths_against_their_stage(self):
        s = playbot.summarise([run(), run(seed="2222")])
        st = s["stages"]["Grey Steppe"]
        self.assertEqual((st["reached"], st["deaths"], st["deathsToBoss"], st["woke"]), (2, 2, 2, 2))
        self.assertEqual(s["killers"], {"Stone Warden: Slam": 2})

    def test_weapon_offers_picks_and_damage_share(self):
        s = playbot.summarise([run()])
        orb, bolt = s["weapons"]["w.orb"], s["weapons"]["w.bolt"]
        self.assertEqual((orb["offered"], orb["taken"], orb["held"]), (1, 1, 1))
        self.assertEqual(bolt["offered"], 1)
        self.assertTrue(bolt["starting"])
        self.assertAlmostEqual(orb["avgShare"], 300 / 9300)
        self.assertEqual(s["passives"]["p.swift"]["taken"], 0)
        self.assertEqual(s["passives"]["p.swift"]["offered"], 1)

    def test_damage_shares_add_up(self):
        s = playbot.summarise([run()])
        self.assertAlmostEqual(sum(d["share"] for d in s["damage"]), 1.0)
        self.assertEqual(s["damage"][0]["source"], "Stone Warden: Slam")

    def test_stuck_spots_cluster_by_place(self):
        spot = {"stage": 0, "t": 10, "x": 40.0, "z": -20.0, "doing": "Gate"}
        near = dict(spot, x=42.0)
        s = playbot.summarise([run(stuck=[spot]), run(stuck=[near]), run(stuck=[dict(spot, x=-90.0)])])
        self.assertEqual(s["stuckSpots"][0]["count"], 2)
        self.assertEqual(s["stuckSpots"][0]["biome"], "Grey Steppe")


class SuggestionTests(unittest.TestCase):
    def tips(self, runs):
        return "\n".join(playbot.suggestions(playbot.summarise(runs)))

    def test_deadly_boss_names_its_attack(self):
        t = self.tips([run(), run(seed="2222")])
        self.assertIn("Stone Warden killed 2 of 2", t)
        self.assertIn("Slam (86%)", t)

    def test_boss_fight_that_never_ends(self):
        st = [stage(0, "Grey Steppe", "Stone Warden", cleared=False, boss_seconds=-1)]
        t = self.tips([run(outcome="timeout", stages=st), run(seed="2", outcome="timeout", stages=st)])
        self.assertIn("Stone Warden was still standing when 2 of 2 runs ran out of time", t)

    def test_weak_and_unseen_weapons(self):
        t = self.tips([run(seed=str(i)) for i in range(4)])
        self.assertIn("Orb deals only 3%", t)
        self.assertIn("Unused was unlocked but never offered", t)
        self.assertIn("Passive Never was unlocked but never offered", t)

    def test_locked_content_is_not_flagged(self):
        r = run()
        r["content"] = dict(CONTENT, weapons=CONTENT["weapons"][:3] + [{"id": "w.unused", "name": "Unused", "unlocked": False}])
        self.assertNotIn("Unused", self.tips([dict(r, seed=str(i)) for i in range(4)]))

    def test_draft_running_dry(self):
        r = run()
        r["draft"] = r["draft"] + [{"t": 900, "stage": 2, "level": 60 + i, "offered": ["Restore restore"], "taken": "restore", "kind": "Restore"} for i in range(60)]
        self.assertIn("offered only Restore", self.tips([r]))

    def test_easy_stage(self):
        easy = [stage(0, "Grey Steppe", "Stone Warden", lowest=0.9, boss_seconds=60, boss_damage=40)]
        t = self.tips([run(outcome="won", won=True, stages=easy), run(seed="2222", outcome="won", won=True, stages=easy)])
        self.assertIn("Grey Steppe never got the bot below 90%", t)

    def test_god_balance(self):
        two = [stage(0, "Grey Steppe", "Stone Warden"), stage(1, "Ash Wood", "Ash Stag"), stage(2, "Drowned Coast", "Tide Mother", cleared=False)]
        runs = [run(god="Storm", stages=two, killed_in=2, killed_by="Husk"), run(god="Storm", seed="2", stages=two, killed_in=2, killed_by="Husk"),
                run(god="Forge"), run(god="Forge", seed="2")]
        t = self.tips(runs)
        self.assertIn("Forge clears 0.0 stages", t)
        self.assertIn("underpowered", t)
        self.assertIn("Storm clears 2.0 stages", t)

    def test_stuck_fall_and_frame_rate(self):
        spot = {"stage": 0, "t": 10, "x": 40.0, "z": -20.0, "doing": "Gate"}
        t = self.tips([run(stuck=[spot], fell=[spot], fps_low=20), run(seed="2", stuck=[spot])])
        self.assertIn("stuck 2 times near (40, -20) in Grey Steppe while going for gate", t)
        self.assertIn("fell out of the world 1 time(s)", t)
        self.assertIn("1% low frame rate is 20 fps", t)

    def test_unlock_pace(self):
        self.assertIn("unlocking every god (150 Embers) takes about 3 runs", self.tips([run()]))


class ReportTests(unittest.TestCase):
    def test_writes_markdown_and_json(self):
        with tempfile.TemporaryDirectory() as d:
            folder = Path(d)
            for i, r in enumerate([run(), run(seed="2222", outcome="won", won=True)]):
                (folder / f"run-{i}.json").write_text(json.dumps(r), encoding="utf-8")
            runs = playbot.load_runs(folder)
            playbot.write_report(folder, runs)
            md = (folder / "report.md").read_text(encoding="utf-8")
            self.assertIn("# Play-bot report", md)
            self.assertIn("2 runs, 1 won (50%)", md)
            self.assertIn("| Grey Steppe |", md)
            data = json.loads((folder / "report.json").read_text(encoding="utf-8"))
            self.assertEqual(data["summary"]["runs"], 2)
            self.assertTrue(data["suggestions"])


if __name__ == "__main__":
    unittest.main()
