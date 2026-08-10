using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace GameSharp;

/// <summary>
/// Provides extension methods for the <see cref="CancellationToken"/> struct.
/// </summary>
public static class CancellationTokenExtensions
{
#if NET6_0_OR_GREATER
    /// <typeparam name="TState">The type of the state object to pass to the callback.</typeparam>
    /// <inheritdoc cref="CancellationToken.Register(Action{object, CancellationToken}, object)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CancellationTokenRegistration Register<TState>(
        this CancellationToken cancellationToken, 
        Action<TState, CancellationToken> callback, 
        TState state) 
        where TState : class?
    {
        return cancellationToken.Register(Unsafe.As<Action<object?, CancellationToken>>(callback), state);
    }
#endif

    /// <typeparam name="TState">The type of the state object to pass to the callback.</typeparam>
    /// <inheritdoc cref="CancellationToken.Register(Action{object}, object)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CancellationTokenRegistration Register<TState>(
        this CancellationToken cancellationToken, 
        Action<TState> callback, 
        TState state)
        where TState : class?
    {
        return cancellationToken.Register(Unsafe.As<Action<object?>>(callback), state);
    }

    /// <typeparam name="TState">The type of the state object to pass to the callback.</typeparam>
    /// <inheritdoc cref="CancellationToken.Register(Action{object}, object)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CancellationTokenRegistration Register<TState>(
        this CancellationToken cancellationToken, Action<TState> callback, 
        TState state, 
        bool useSynchronizationContext)
        where TState : class?
    {
        return cancellationToken.Register(Unsafe.As<Action<object?>>(callback), state, useSynchronizationContext);
    }
}
