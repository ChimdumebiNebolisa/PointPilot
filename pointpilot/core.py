from __future__ import annotations

import base64
import ctypes
from ctypes import wintypes
import hashlib
import os
import re
import threading
import time
import uuid
from dataclasses import dataclass, field
from enum import Enum
from pathlib import Path
from typing import Callable, Iterable

import mss
from PIL import Image


class SessionState(str, Enum):
    IDLE = "Idle"
    LISTENING = "Listening"
    THINKING = "Thinking"
    POINTING = "Pointing"
    ACTING = "Acting"
    SPEAKING = "Speaking"
    PAUSED = "Paused"
    ERROR = "Error"


@dataclass(frozen=True)
class TaskLease:
    task_id: str
    revision: int
    cancelled: threading.Event


@dataclass(frozen=True)
class CompletedAction:
    revision: int
    description: str
    completed_at: float


@dataclass(frozen=True)
class TaskSnapshot:
    task_id: str | None
    revision: int
    goal: str
    constraints: tuple[str, ...]
    completed: tuple[CompletedAction, ...]
    cancelled: bool


class TaskCoordinator:
    """One revisioned task. A new spoken turn invalidates old work first."""

    def __init__(self) -> None:
        self._lock = threading.RLock()
        self._task_id: str | None = None
        self._revision = 0
        self._goal = ""
        self._constraints: list[str] = []
        self._completed: list[CompletedAction] = []
        self._cancelled = threading.Event()

    def start(self, goal: str, constraints: Iterable[str] = ()) -> TaskLease:
        if not goal.strip():
            raise ValueError("A task goal is required.")
        with self._lock:
            self._cancelled.set()
            self._task_id = str(uuid.uuid4())
            self._revision = 0
            self._goal = goal.strip()
            self._constraints = [item.strip() for item in constraints if item.strip()]
            self._completed = []
            self._cancelled = threading.Event()
            return self.lease()

    def lease(self) -> TaskLease:
        with self._lock:
            if self._task_id is None:
                raise RuntimeError("No active task.")
            return TaskLease(self._task_id, self._revision, self._cancelled)

    def interrupt(self, correction: str = "") -> TaskSnapshot:
        with self._lock:
            self._cancelled.set()
            self._revision += 1
            self._cancelled = threading.Event()
            if correction.strip():
                self._constraints.append(correction.strip())
            return self.snapshot()

    def stop(self) -> TaskSnapshot:
        with self._lock:
            self._cancelled.set()
            return self.snapshot()

    def validate(self, lease: TaskLease) -> None:
        with self._lock:
            if (
                lease.cancelled.is_set()
                or lease.task_id != self._task_id
                or lease.revision != self._revision
            ):
                raise RuntimeError("The computer action is stale and was not run.")

    def record(self, lease: TaskLease, description: str) -> None:
        self.validate(lease)
        with self._lock:
            self._completed.append(CompletedAction(lease.revision, description, time.time()))

    def snapshot(self) -> TaskSnapshot:
        with self._lock:
            return TaskSnapshot(
                self._task_id,
                self._revision,
                self._goal,
                tuple(self._constraints),
                tuple(self._completed),
                self._cancelled.is_set(),
            )


@dataclass(frozen=True)
class TargetLease:
    hwnd: int
    pid: int
    executable: str
    title: str
    left: int
    top: int
    width: int
    height: int

    @property
    def bounds(self) -> tuple[int, int, int, int]:
        return self.left, self.top, self.width, self.height


@dataclass(frozen=True)
class Screenshot:
    png_base64: str
    digest: str
    width: int
    height: int


class TargetInspector:
    """Foreground-only Windows target discovery and capture."""

    _sensitive = re.compile(
        r"password|sign.?in|log.?in|credential|authenticator|bank|payment|wallet|secret|\.env",
        re.IGNORECASE,
    )

    def __init__(self, own_pid: int | None = None) -> None:
        self._own_pid = own_pid or os.getpid()
        self._user32 = ctypes.windll.user32
        self._kernel32 = ctypes.windll.kernel32

    def foreground(self) -> TargetLease:
        hwnd = int(self._user32.GetForegroundWindow())
        if not hwnd:
            raise RuntimeError("No external foreground window is available.")
        pid = ctypes.c_ulong()
        self._user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pid))
        if pid.value == self._own_pid:
            raise RuntimeError("PointPilot will not target its own window.")
        rect = wintypes.RECT()
        if not self._user32.GetWindowRect(hwnd, ctypes.byref(rect)):
            raise RuntimeError("The foreground window bounds are unavailable.")
        width, height = rect.right - rect.left, rect.bottom - rect.top
        if width <= 0 or height <= 0:
            raise RuntimeError("The foreground window is minimized or has invalid bounds.")
        title_buffer = ctypes.create_unicode_buffer(512)
        self._user32.GetWindowTextW(hwnd, title_buffer, len(title_buffer))
        executable = self._process_path(pid.value)
        lease = TargetLease(hwnd, pid.value, executable, title_buffer.value, rect.left, rect.top, width, height)
        if self._sensitive.search(f"{lease.title} {lease.executable}"):
            raise RuntimeError("PointPilot will not capture or control a sensitive window.")
        return lease

    def validate(self, lease: TargetLease, *, mutating: bool) -> None:
        current = self.foreground()
        if current.hwnd != lease.hwnd or current.pid != lease.pid or current.bounds != lease.bounds:
            raise RuntimeError("The target changed, moved, or resized. Start again from the current screen.")
        if mutating and Path(current.executable).name.lower() not in {"gimp-3.exe", "gimp.exe"}:
            raise RuntimeError("PointPilot only controls foreground GIMP in this release.")

    def capture(self, lease: TargetLease) -> Screenshot:
        self.validate(lease, mutating=False)
        with mss.mss() as camera:
            image = camera.grab({"left": lease.left, "top": lease.top, "width": lease.width, "height": lease.height})
            pil = Image.frombytes("RGB", image.size, image.rgb)
        from io import BytesIO

        output = BytesIO()
        pil.save(output, format="PNG")
        payload = output.getvalue()
        return Screenshot(base64.b64encode(payload).decode("ascii"), hashlib.sha256(payload).hexdigest(), lease.width, lease.height)

    def _process_path(self, pid: int) -> str:
        handle = self._kernel32.OpenProcess(0x1000, False, pid)
        if not handle:
            return ""
        try:
            buffer = ctypes.create_unicode_buffer(32768)
            length = ctypes.c_ulong(len(buffer))
            if self._kernel32.QueryFullProcessImageNameW(handle, 0, buffer, ctypes.byref(length)):
                return buffer.value
            return ""
        finally:
            self._kernel32.CloseHandle(handle)


@dataclass(frozen=True)
class VerificationResult:
    succeeded: bool
    summary: str


def verify_visible_change(before: Screenshot, after: Screenshot) -> VerificationResult:
    if before.digest == after.digest:
        return VerificationResult(False, "The target screenshot did not change, so PointPilot stopped without claiming success.")
    return VerificationResult(True, "The target screenshot changed after the action.")
