"""Shared helpers for Old Gods tooling: find the project, the Unity editor and the player build."""
from __future__ import annotations

import os
import re
from pathlib import Path

PROJECT = Path(__file__).resolve().parent.parent
DEFAULT_BUILD = PROJECT / "Builds" / "Windows" / "TheOldGods.exe"


def editor_version(project: Path = PROJECT) -> str:
    """Reads m_EditorVersion from ProjectSettings/ProjectVersion.txt."""
    text = (project / "ProjectSettings" / "ProjectVersion.txt").read_text(encoding="utf-8")
    match = re.search(r"^m_EditorVersion:\s*(\S+)", text, re.MULTILINE)
    if not match:
        raise RuntimeError("m_EditorVersion not found in ProjectVersion.txt")
    return match.group(1)


def unity_exe(project: Path = PROJECT, env: dict[str, str] | None = None) -> Path:
    """UNITY_EXE wins; otherwise the Hub install for the project's editor version."""
    env = dict(os.environ) if env is None else env
    override = env.get("UNITY_EXE")
    if override:
        return Path(override)
    version = editor_version(project)
    return Path(r"C:\Program Files\Unity\Hub\Editor") / version / "Editor" / "Unity.exe"


def project_is_open(project: Path = PROJECT) -> bool:
    """An open Editor holds Temp/UnityLockfile; the file cannot be removed while it does."""
    lock = project / "Temp" / "UnityLockfile"
    if not lock.exists():
        return False
    try:
        probe = lock.with_suffix(".probe")
        os.replace(lock, probe)
        os.replace(probe, lock)
    except OSError:
        return True
    return False
