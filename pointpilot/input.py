from __future__ import annotations

import ctypes
import threading
from dataclasses import dataclass

from .core import TargetInspector, TargetLease, TaskCoordinator, TaskLease


INPUT_MOUSE = 0
INPUT_KEYBOARD = 1
MOUSEEVENTF_MOVE = 0x0001
MOUSEEVENTF_LEFTDOWN = 0x0002
MOUSEEVENTF_LEFTUP = 0x0004
MOUSEEVENTF_ABSOLUTE = 0x8000
MOUSEEVENTF_VIRTUALDESK = 0x4000
KEYEVENTF_KEYUP = 0x0002
KEYEVENTF_UNICODE = 0x0004


class MOUSEINPUT(ctypes.Structure):
    _fields_ = [("dx", ctypes.c_long), ("dy", ctypes.c_long), ("mouseData", ctypes.c_ulong), ("dwFlags", ctypes.c_ulong), ("time", ctypes.c_ulong), ("dwExtraInfo", ctypes.c_void_p)]


class KEYBDINPUT(ctypes.Structure):
    _fields_ = [("wVk", ctypes.c_ushort), ("wScan", ctypes.c_ushort), ("dwFlags", ctypes.c_ulong), ("time", ctypes.c_ulong), ("dwExtraInfo", ctypes.c_void_p)]


class INPUTUNION(ctypes.Union):
    _fields_ = [("mi", MOUSEINPUT), ("ki", KEYBDINPUT)]


class INPUT(ctypes.Structure):
    _fields_ = [("type", ctypes.c_ulong), ("union", INPUTUNION)]


@dataclass(frozen=True)
class Action:
    kind: str
    x: int | None = None
    y: int | None = None
    text: str = ""


class WindowsInputExecutor:
    """The only PointPilot component that injects Windows input."""

    def __init__(self, tasks: TaskCoordinator, targets: TargetInspector) -> None:
        self._tasks = tasks
        self._targets = targets
        self._lock = threading.Lock()
        self._user32 = ctypes.windll.user32

    def run(self, lease: TaskLease, target: TargetLease, action: Action) -> str:
        with self._lock:
            self._tasks.validate(lease)
            self._targets.validate(target, mutating=True)
            try:
                if action.kind == "move":
                    self._move(target, action)
                elif action.kind == "click":
                    self._move(target, action)
                    self._send_mouse(MOUSEEVENTF_LEFTDOWN)
                    self._send_mouse(MOUSEEVENTF_LEFTUP)
                elif action.kind == "type":
                    self._type(action.text)
                elif action.kind == "key" and action.text.lower() in {"enter", "escape", "tab"}:
                    vk = {"enter": 0x0D, "escape": 0x1B, "tab": 0x09}[action.text.lower()]
                    self._send_key(vk)
                else:
                    raise RuntimeError("PointPilot rejected an unsupported computer action.")
                self._tasks.record(lease, action.kind)
                return f"Executed guarded {action.kind}."
            finally:
                self._release_inputs()

    def _move(self, target: TargetLease, action: Action) -> None:
        if action.x is None or action.y is None:
            raise RuntimeError("Mouse actions require target-relative coordinates.")
        if not (0 <= action.x < target.width and 0 <= action.y < target.height):
            raise RuntimeError("PointPilot rejected coordinates outside the target window.")
        x, y = target.left + action.x, target.top + action.y
        virtual_width = self._user32.GetSystemMetrics(78)
        virtual_height = self._user32.GetSystemMetrics(79)
        virtual_left = self._user32.GetSystemMetrics(76)
        virtual_top = self._user32.GetSystemMetrics(77)
        dx = round((x - virtual_left) * 65535 / max(virtual_width - 1, 1))
        dy = round((y - virtual_top) * 65535 / max(virtual_height - 1, 1))
        self._send(INPUT(type=INPUT_MOUSE, union=INPUTUNION(mi=MOUSEINPUT(dx, dy, 0, MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK, 0, None))))

    def _type(self, text: str) -> None:
        if not text or len(text) > 500:
            raise RuntimeError("PointPilot rejected unsafe text input.")
        for char in text:
            self._send(INPUT(type=INPUT_KEYBOARD, union=INPUTUNION(ki=KEYBDINPUT(0, ord(char), KEYEVENTF_UNICODE, 0, None))))
            self._send(INPUT(type=INPUT_KEYBOARD, union=INPUTUNION(ki=KEYBDINPUT(0, ord(char), KEYEVENTF_UNICODE | KEYEVENTF_KEYUP, 0, None))))

    def _send_mouse(self, flags: int) -> None:
        self._send(INPUT(type=INPUT_MOUSE, union=INPUTUNION(mi=MOUSEINPUT(0, 0, 0, flags, 0, None))))

    def _send_key(self, vk: int) -> None:
        self._send(INPUT(type=INPUT_KEYBOARD, union=INPUTUNION(ki=KEYBDINPUT(vk, 0, 0, 0, None))))
        self._send(INPUT(type=INPUT_KEYBOARD, union=INPUTUNION(ki=KEYBDINPUT(vk, 0, KEYEVENTF_KEYUP, 0, None))))

    def _send(self, event: INPUT) -> None:
        if self._user32.SendInput(1, ctypes.byref(event), ctypes.sizeof(INPUT)) != 1:
            raise ctypes.WinError()

    def _release_inputs(self) -> None:
        self._send_mouse(MOUSEEVENTF_LEFTUP)
        for key in (0x10, 0x11, 0x12):
            self._send(INPUT(type=INPUT_KEYBOARD, union=INPUTUNION(ki=KEYBDINPUT(key, 0, KEYEVENTF_KEYUP, 0, None))))

