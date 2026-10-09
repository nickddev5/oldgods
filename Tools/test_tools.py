"""Tests for the Old Gods tooling. Run: uv run --no-project python -B -m unittest discover -s Tools"""
from __future__ import annotations

import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_windows  # noqa: E402
import smoke  # noqa: E402
import unity_paths  # noqa: E402


class UnityPathsTests(unittest.TestCase):
    def test_editor_version_reads_project_version(self):
        with tempfile.TemporaryDirectory() as d:
            p = Path(d)
            (p / "ProjectSettings").mkdir()
            (p / "ProjectSettings" / "ProjectVersion.txt").write_text(
                "m_EditorVersion: 6000.6.3f1\nm_EditorVersionWithRevision: x\n")
            self.assertEqual(unity_paths.editor_version(p), "6000.6.3f1")

    def test_unity_exe_prefers_env(self):
        self.assertEqual(unity_paths.unity_exe(env={"UNITY_EXE": r"D:\U\Unity.exe"}), Path(r"D:\U\Unity.exe"))

    def test_unity_exe_uses_hub_path(self):
        exe = unity_paths.unity_exe(env={})
        self.assertTrue(str(exe).endswith(r"Editor\Unity.exe"))
        self.assertIn(unity_paths.editor_version(), str(exe))

    def test_project_without_lock_is_not_open(self):
        with tempfile.TemporaryDirectory() as d:
            self.assertFalse(unity_paths.project_is_open(Path(d)))

    def test_stale_lock_is_not_open(self):
        with tempfile.TemporaryDirectory() as d:
            (Path(d) / "Temp").mkdir()
            (Path(d) / "Temp" / "UnityLockfile").write_text("")
            self.assertFalse(unity_paths.project_is_open(Path(d)))


class CommandTests(unittest.TestCase):
    def test_build_command_has_method_and_out(self):
        cmd = build_windows.build_command(Path("U.exe"), Path("out.exe"), True, Path("log.txt"))
        self.assertIn("OldGods.Editor.Build.Windows", cmd)
        self.assertEqual(cmd[cmd.index("-buildOut") + 1], "out.exe")
        self.assertIn("-development", cmd)

    def test_smoke_command_modes(self):
        smoke_cmd = smoke.player_command(Path("g.exe"), "smoke", Path("s.log"), Path("p.log"), None, 800, 600)
        self.assertIn("-smoke", smoke_cmd)
        probe_cmd = smoke.player_command(Path("g.exe"), "probe", Path("p.json"), Path("p.log"), "ABC", 800, 600, ["-x"])
        self.assertIn("-probe", probe_cmd)
        self.assertEqual(probe_cmd[probe_cmd.index("-seed") + 1], "ABC")
        self.assertEqual(probe_cmd[-1], "-x")
        with self.assertRaises(ValueError):
            smoke.player_command(Path("g.exe"), "nope", Path("x"), Path("y"), None, 1, 1)


if __name__ == "__main__":
    unittest.main()
