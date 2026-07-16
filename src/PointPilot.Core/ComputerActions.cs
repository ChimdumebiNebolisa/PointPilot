namespace PointPilot.Core;

public enum ComputerActionType { Screenshot, Move, Click, DoubleClick, RightClick, MouseDown, MouseUp, Drag, Scroll, TypeText, Keypress, Wait }
public enum ActionPolicyLevel { Observe, ReversibleEdit, Consequential, Prohibited }

public sealed record ComputerAction(
    ComputerActionType Type,
    int? X = null,
    int? Y = null,
    int ScrollX = 0,
    int ScrollY = 0,
    string? Text = null,
    IReadOnlyList<string>? Keys = null,
    IReadOnlyList<ScreenPoint>? Path = null,
    int WaitMilliseconds = 0,
    IReadOnlyList<string>? Modifiers = null);

public static class ActionPolicy
{
    private static readonly string[] ProhibitedTerms = ["password", "credential", "payment", "purchase", "uac", "security prompt", "shell", "terminal", "delete permanently", "publish", "send externally"];
    private static readonly string[] ConsequentialTerms = ["export", "overwrite", "replace file", "save", "close", "destination path", "png", "write file"];

    public static ActionPolicyLevel Classify(ComputerAction action) => action.Type switch
    {
        ComputerActionType.Screenshot or ComputerActionType.Move or ComputerActionType.Wait => ActionPolicyLevel.Observe,
        _ => ActionPolicyLevel.ReversibleEdit
    };

    public static ActionPolicyLevel ClassifyGoal(string goal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(goal);
        if (ProhibitedTerms.Any(term => goal.Contains(term, StringComparison.OrdinalIgnoreCase))) return ActionPolicyLevel.Prohibited;
        if (ConsequentialTerms.Any(term => goal.Contains(term, StringComparison.OrdinalIgnoreCase))) return ActionPolicyLevel.Consequential;
        return ActionPolicyLevel.ReversibleEdit;
    }
}
