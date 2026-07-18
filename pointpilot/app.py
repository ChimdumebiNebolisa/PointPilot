from __future__ import annotations

import sys

from dotenv import load_dotenv

from PyQt6.QtCore import Qt
from PyQt6.QtWidgets import QApplication

from .ui import Companion


def main() -> None:
    load_dotenv(".env.local")
    app = QApplication(sys.argv)
    app.setApplicationName("PointPilot")
    app.setApplicationDisplayName("PointPilot")
    app.setQuitOnLastWindowClosed(False)
    companion = Companion()
    companion.show()
    raise SystemExit(app.exec())
