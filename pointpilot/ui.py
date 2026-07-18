from __future__ import annotations

import ctypes
from dataclasses import dataclass

from PyQt6.QtCore import Qt, QTimer
from PyQt6.QtGui import QAction, QColor, QKeySequence, QPainter, QPen
from PyQt6.QtWidgets import QApplication, QHBoxLayout, QLabel, QPushButton, QStyle, QSystemTrayIcon, QTextEdit, QVBoxLayout, QWidget

from .runtime import PointPilotController


STATE_COLORS = {
    "Listening": "#0d766e",
    "Thinking": "#886a00",
    "Pointing": "#1d4ed8",
    "Acting": "#b45309",
    "Speaking": "#7c3aed",
    "Paused": "#475569",
    "Error": "#b91c1c",
    "Idle": "#475569",
}


class PointerOverlay(QWidget):
    def __init__(self) -> None:
        super().__init__(None, Qt.WindowType.FramelessWindowHint | Qt.WindowType.Tool | Qt.WindowType.WindowStaysOnTopHint | Qt.WindowType.WindowDoesNotAcceptFocus)
        self.setAttribute(Qt.WidgetAttribute.WA_TransparentForMouseEvents)
        self.setAttribute(Qt.WidgetAttribute.WA_TranslucentBackground)
        self.setWindowFlag(Qt.WindowType.WindowTransparentForInput, True)
        self._label = ""
        self._timer = QTimer(self)
        self._timer.setSingleShot(True)
        self._timer.timeout.connect(self.hide)

    def point_to(self, left: int, top: int, width: int, height: int, label: str) -> None:
        self._label = label
        self.setGeometry(left, top, width, height)
        self.show()
        self.raise_()
        self._timer.start(6000)

    def paintEvent(self, _event) -> None:  # type: ignore[no-untyped-def]
        painter = QPainter(self)
        painter.setRenderHint(QPainter.RenderHint.Antialiasing)
        pen = QPen(QColor("#0d766e"), 3)
        painter.setPen(pen)
        painter.drawRoundedRect(self.rect().adjusted(3, 3, -3, -3), 10, 10)
        painter.setBrush(QColor("#0d766e"))
        painter.setPen(Qt.PenStyle.NoPen)
        painter.drawRoundedRect(10, 10, min(max(120, len(self._label) * 7), self.width() - 20), 28, 8, 8)
        painter.setPen(QColor("white"))
        painter.drawText(18, 30, self._label[:64])


class Companion(QWidget):
    def __init__(self) -> None:
        super().__init__(None, Qt.WindowType.Tool | Qt.WindowType.WindowStaysOnTopHint)
        self.setWindowTitle("PointPilot")
        self.setAccessibleName("PointPilot companion")
        self.setMinimumWidth(320)
        self.setMaximumWidth(380)
        self.controller = PointPilotController()
        self.overlay = PointerOverlay()
        self._build()
        self.controller.state_changed.connect(self._state)
        self.controller.detail_changed.connect(self._detail)
        self.controller.point_requested.connect(self.overlay.point_to)
        self._tray()

    def _build(self) -> None:
        layout = QVBoxLayout(self)
        layout.setContentsMargins(16, 16, 16, 16)
        layout.setSpacing(10)
        title = QLabel("PointPilot")
        title.setStyleSheet("font-size: 19px; font-weight: 700;")
        layout.addWidget(title)
        self.subtitle = QLabel("Ask about anything on your screen.\nShow me, explain it, or do it for me.")
        self.subtitle.setWordWrap(True)
        self.subtitle.setStyleSheet("color: #475569;")
        layout.addWidget(self.subtitle)
        state_row = QHBoxLayout()
        self.state_label = QLabel("Idle")
        self.state_label.setAccessibleName("Current PointPilot state")
        self.state_label.setStyleSheet("font-weight: 700; color: #475569;")
        state_row.addWidget(self.state_label)
        state_row.addStretch()
        layout.addLayout(state_row)
        self.details = QTextEdit()
        self.details.setReadOnly(True)
        self.details.setAccessibleName("PointPilot details")
        self.details.setMaximumHeight(100)
        self.details.setPlainText("Start one voice session to talk naturally across follow-up questions.")
        layout.addWidget(self.details)
        buttons = QHBoxLayout()
        self.start_button = QPushButton("Start session")
        self.start_button.setAccessibleName("Start PointPilot voice session")
        self.start_button.clicked.connect(self.controller.start)
        self.stop_button = QPushButton("Stop")
        self.stop_button.setAccessibleName("Stop PointPilot")
        self.stop_button.clicked.connect(self.controller.stop)
        self.stop_button.setEnabled(False)
        buttons.addWidget(self.start_button)
        buttons.addWidget(self.stop_button)
        layout.addLayout(buttons)
        self.setStyleSheet("QWidget { background: #f8fafc; color: #172033; } QPushButton { padding: 8px 12px; border: 1px solid #cbd5e1; border-radius: 8px; background: white; } QPushButton:disabled { color: #94a3b8; background: #f1f5f9; }")

    def _tray(self) -> None:
        self.tray = QSystemTrayIcon(self)
        self.tray.setIcon(self.style().standardIcon(QStyle.StandardPixmap.SP_ComputerIcon))
        self.tray.setToolTip("PointPilot")
        menu = self.tray.contextMenu() or __import__("PyQt6.QtWidgets", fromlist=["QMenu"]).QMenu(self)
        show = QAction("Show PointPilot", self)
        show.triggered.connect(self.showNormal)
        stop = QAction("Stop session", self)
        stop.triggered.connect(self.controller.stop)
        quit_action = QAction("Quit", self)
        quit_action.triggered.connect(QApplication.quit)
        menu.addAction(show)
        menu.addAction(stop)
        menu.addSeparator()
        menu.addAction(quit_action)
        self.tray.setContextMenu(menu)
        self.tray.show()

    def _state(self, state: str) -> None:
        self.state_label.setText(state)
        self.state_label.setStyleSheet(f"font-weight: 700; color: {STATE_COLORS.get(state, '#475569')};")
        active = state not in {"Idle", "Paused", "Error"}
        self.stop_button.setEnabled(active)
        self.start_button.setEnabled(not active)

    def _detail(self, message: str) -> None:
        self.details.setPlainText(message)

    def keyPressEvent(self, event) -> None:  # type: ignore[no-untyped-def]
        if event.key() == Qt.Key.Key_Escape:
            self.controller.escape()
            event.accept()
            return
        super().keyPressEvent(event)
