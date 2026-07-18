import unittest

from pointpilot.core import TaskCoordinator


class TaskCoordinatorTests(unittest.TestCase):
    def test_interrupt_invalidates_old_lease_and_preserves_completed_history(self) -> None:
        coordinator = TaskCoordinator()
        first = coordinator.start("Click the Product layer")
        coordinator.record(first, "moved to Product layer")
        revised = coordinator.interrupt("Keep it on the left")

        self.assertEqual(revised.revision, 1)
        self.assertEqual([item.description for item in revised.completed], ["moved to Product layer"])
        with self.assertRaisesRegex(RuntimeError, "stale"):
            coordinator.validate(first)

    def test_stop_invalidates_current_lease(self) -> None:
        coordinator = TaskCoordinator()
        lease = coordinator.start("Select a layer")
        coordinator.stop()

        with self.assertRaises(RuntimeError):
            coordinator.validate(lease)
