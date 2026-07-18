from pointpilot.core import TaskCoordinator


def test_interrupt_invalidates_old_lease_and_preserves_completed_history() -> None:
    coordinator = TaskCoordinator()
    first = coordinator.start("Click the Product layer")
    coordinator.record(first, "moved to Product layer")
    revised = coordinator.interrupt("Keep it on the left")

    assert revised.revision == 1
    assert [item.description for item in revised.completed] == ["moved to Product layer"]
    try:
        coordinator.validate(first)
    except RuntimeError as error:
        assert "stale" in str(error)
    else:
        raise AssertionError("old lease remained valid after interruption")


def test_stop_invalidates_current_lease() -> None:
    coordinator = TaskCoordinator()
    lease = coordinator.start("Select a layer")
    coordinator.stop()

    try:
        coordinator.validate(lease)
    except RuntimeError:
        pass
    else:
        raise AssertionError("stopped lease remained valid")

