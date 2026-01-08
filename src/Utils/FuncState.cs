using System;

namespace Soenneker.Utils.File.Utils;

internal sealed class FuncState<T>
{
    private readonly Func<object?, T> _func;
    private readonly object? _state;

    public FuncState(Func<object?, T> func, object? state)
    {
        _func = func;
        _state = state;
    }

    public T Invoke() => _func(_state);
}