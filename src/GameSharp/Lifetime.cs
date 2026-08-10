using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
#if NET9_0_OR_GREATER
using Lock = System.Threading.Lock;
#else
using Lock = System.Object;
#endif

namespace GameSharp;

/// <summary>
/// Provides an abstract base class for objects with a manageable lifetime.
/// </summary>
/// <typeparam name="TSelf">The type of the derived class.</typeparam>
public abstract class Lifetime<TSelf> where TSelf : Lifetime<TSelf>
{
    /// <summary>
    /// Represents a stage in the lifetime of an object.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="Stage"/> class with the specified lifetime.
    /// </remarks>
    /// <param name="lifetime">The lifetime instance associated with this stage.</param>
    public abstract class Stage(TSelf lifetime)
    {
        private int _completed;
        /// <summary>
        /// Gets a value indicating whether this stage has been started.
        /// </summary>
        public bool IsStarted => Volatile.Read(ref _completed) != 0;

        /// <summary>
        /// Gets a value indicating whether this stage has been completed.
        /// </summary>
        public virtual bool IsCompleted => IsStarted;

        /// <summary>
        /// Gets the previously completed lifecycle stage, or <see langword="null"/> if this is the first stage.
        /// </summary>
        protected internal Stage? Previous { get; private set; }

        /// <summary>
        /// Gets the lifetime instance associated with this stage.
        /// </summary>
        protected internal TSelf Lifetime { get; } = lifetime;

        internal bool TryComplete(ref Stage? current)
        {
            if (IsStarted)
            {
                return false;
            }

            lock (Lifetime.SyncRoot)
            {
                if (Interlocked.Exchange(ref _completed, 1) != 0)
                {
                    return false;
                }

                Previous = Interlocked.Exchange(ref current, this);
                Complete();

                return true;
            }
        }

        /// <summary>
        /// Completes the current stage of the lifetime with the specified state.
        /// </summary>
        protected abstract void Complete();
    }

    private protected Lock SyncRoot { get; } = new();
    private Stage? _lastStage;

    /// <summary>
    /// Completes the specified stage of the lifetime.
    /// </summary>
    /// <param name="stage">The stage to be completed.</param>
    /// <param name="paramName">The name of the stage parameter.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the <paramref name="stage"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if the <paramref name="stage"/> does not belong to this lifetime instance or has already been completed.
    /// </exception>
    protected void Complete(Stage stage, [CallerArgumentExpression(nameof(stage))] string? paramName = null)
    {
#if NET7_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(stage, paramName);
#else
        if (stage is null)
        {
            throw new ArgumentNullException(paramName);
        }
#endif
        if (!ReferenceEquals(stage.Lifetime, this))
        {
            ArgumentException ex = new("The specified stage does not belong to this lifetime instance.", paramName);
#if NET6_0_OR_GREATER
            Throw(ex);
#else
            throw ex;
#endif
        }

        if (!stage.TryComplete(ref _lastStage))
        {
            ArgumentException ex = new("The specified stage has already been completed.", paramName);
#if NET6_0_OR_GREATER
            Throw(ex);
#else
            throw ex;
#endif
        }
    }

    /// <summary>
    /// Attempts to complete the specified stage of the lifetime.
    /// </summary>
    /// <param name="stage">The stage to be completed.</param>
    /// <returns><see langword="true"/> if the stage was successfully completed; otherwise, <see langword="false"/>.</returns>
    protected bool TryComplete([NotNullWhen(true)] Stage? stage)
    {
        return stage is { } && ReferenceEquals(stage.Lifetime, this) && stage.TryComplete(ref _lastStage);
    }

#if NET6_0_OR_GREATER
    [DoesNotReturn, System.Diagnostics.StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
    private protected static void Throw(Exception exception)
    {
        throw exception;
    }
#endif
}
