using System;

namespace Soenneker.Utils.File.Utils;

internal sealed class ActionState
{
    private readonly Action<object?> _action;
    private readonly object? _state;

    public ActionState(Action<object?> action, object? state)
    {
        _action = action;
        _state = state;
    }

    public void Invoke() => _action(_state);
}