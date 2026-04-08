using ProceduralGraph.Events;
using ProceduralGraph.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph.Generic;

/// <summary>
/// Provides a handle for accessing and managing a scene member within a scene graph. Enables manipulation of scene
/// member properties and traversal of the scene hierarchy.
/// </summary>
/// <inheritdoc cref="LifecycleGraphNode{TSceneMember}"/>
public abstract class Control<TSceneMember> : Disposable, IEquatable<TSceneMember>, IEquatable<Control<TSceneMember>> 
    where TSceneMember : class
{
    private Transform _reference;
    private readonly ReaderWriterLockSlim _syncRoot;
    private readonly AsyncEventSubscription<TSceneMember> _destroyedSubscription;
    private readonly AsyncEventSubscription<Transform> _transformChangedSubscription;

    private volatile bool _destroyed;
    /// <summary>
    /// Gets a value indicating whether the current scene member has been destroyed.
    /// </summary>
    protected bool Destroyed => _destroyed;

    /// <summary>
    /// Gets the manager responsible for handling scene member operations and interactions for the current control.
    /// </summary>
    protected ISceneMemberManager<TSceneMember> Manager { get; }

    /// <summary>
    /// Gets the underlying scene member associated with the current handle.
    /// </summary>
    public abstract TSceneMember SceneMember { get; }

    /// <summary>
    /// Gets the parent of the current scene member, if one exists.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if the current scene member has been destroyed.</exception>
    public TSceneMember? Parent
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowHelpers.ThrowIfDisposed(Disposed, this);
            return Manager.GetParent(SceneMember);
        }
    }

    /// <summary>
    /// Gets the root of the scene graph that contains the current scene member.
    /// </summary>
    public TSceneMember Root
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowHelpers.ThrowIfDisposed(Disposed, this);
            return Manager.GetRoot(SceneMember);
        }
    }

    /// <summary>
    /// Gets the immediate children of the current scene member.
    /// </summary>
    public IReadOnlyCollection<TSceneMember> Children
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowHelpers.ThrowIfDisposed(Disposed, this);
            return Manager.GetChildren(SceneMember);
        }
    }

    /// <summary>
    /// Gets or sets the name associated with the current scene member.
    /// </summary>
    public abstract string Name { get; set; }

    /// <summary>
    /// Gets or sets the position of the current scene member.
    /// </summary>
    public Double3 Position
    {
        get
        {
            ThrowHelpers.ThrowIfDisposed(Disposed, this);

            _syncRoot.EnterReadLock();
            try
            {
                return _reference.Translation;

            }
            finally
            {
                _syncRoot.ExitReadLock();
            }
        }
        set
        {
            ThrowHelpers.ThrowIfDisposed(Disposed, this);
            _syncRoot.EnterUpgradeableReadLock();
            try
            {
                if (Double3.ApproximatelyEquals(_reference.Translation, value))
                {
                    return;
                }
                _syncRoot.EnterWriteLock();
                try
                {
                    _reference = _reference with { Translation = value };
                }
                finally
                {
                    _syncRoot.ExitWriteLock();
                }
                _transformChanged.Publish(_reference);
            }
            finally
            {
                _syncRoot.ExitUpgradeableReadLock();
            }
        }
    }

    /// <summary>
    /// Gets or sets the rotation of the current scene member.
    /// </summary>
    public Quaternion Rotation
    {
        get
        {
            ThrowHelpers.ThrowIfDisposed(Disposed, this);

            _syncRoot.EnterReadLock();
            try
            {
                return _reference.Rotation;
            }
            finally
            {
                _syncRoot.ExitReadLock();
            }
        }
        set
        {
            ThrowHelpers.ThrowIfDisposed(Disposed, this);
            _syncRoot.EnterUpgradeableReadLock();
            try
            {
                if (Quaternion.ApproximatelyEquals(_reference.Rotation, value))
                {
                    return;
                }
                _syncRoot.EnterWriteLock();
                try
                {
                    _reference = _reference with { Rotation = value };
                }
                finally
                {
                    _syncRoot.ExitWriteLock();
                }
                _transformChanged.Publish(_reference);
            }
            finally
            {
                _syncRoot.ExitUpgradeableReadLock();
            }
        }
    }

    /// <summary>
    /// Gets or sets the scale of the current scene member.
    /// </summary>
    public Vector3 Scale
    {
        get
        {
            ThrowHelpers.ThrowIfDisposed(Disposed, this);

            _syncRoot.EnterReadLock();
            try
            {
                return _reference.Scale;
            }
            finally
            {
                _syncRoot.ExitReadLock();
            }
        }
        set
        {
            ThrowHelpers.ThrowIfDisposed(Disposed, this);
            _syncRoot.EnterUpgradeableReadLock();
            try
            {
                if (Vector3.ApproximatelyEquals(_reference.Scale, value))
                {
                    return;
                }
                _syncRoot.EnterWriteLock();
                try
                {
                    _reference = _reference with { Scale = value };
                }
                finally
                {
                    _syncRoot.ExitWriteLock();
                }
                _transformChanged.Publish(_reference);
            }
            finally
            {
                _syncRoot.ExitUpgradeableReadLock();
            }
        }
    }

    /// <summary>
    /// Gets or sets the transform of the current scene member.
    /// </summary>
    public Transform Transform
    {
        get
        {
            ThrowHelpers.ThrowIfDisposed(Disposed, this);
            _syncRoot.EnterReadLock();
            try
            {
                _syncRoot.ExitUpgradeableReadLock();
                return _reference;
            }
            finally
            {
                _syncRoot.ExitReadLock();
            }
        }
        set
        {
            ThrowHelpers.ThrowIfDisposed(Disposed, this);
            _syncRoot.EnterUpgradeableReadLock();
            try
            {
                if (Vector3.ApproximatelyEquals(_reference.Scale, value.Scale) &&
                    Quaternion.ApproximatelyEquals(_reference.Rotation, value.Rotation) &&
                    Double3.ApproximatelyEquals(_reference.Translation, value.Translation))
                {
                    return;
                }
                _syncRoot.EnterWriteLock();
                try
                {
                    _reference = value;
                }
                finally
                {
                    _syncRoot.ExitWriteLock();
                }
                _transformChanged.Publish(_reference);
            }
            finally
            {
                _syncRoot.ExitUpgradeableReadLock();
            }
        }
    }

    private readonly AsyncEventPublisher<Transform> _transformChanged;
    /// <summary>
    /// Gets the asynchronous event that is raised when the transform changes.
    /// </summary>
    public AsyncEvent<Transform> TransformChanged => _transformChanged.Event;

    /// <summary>
    /// Initializes a new instance of the <see cref="Control{TSceneMember}"/> class.
    /// </summary>
    /// <inheritdoc cref="AsyncEventPublisher.CreateConflating{TArgs}(ILogger)"/>
    public Control(ISceneMemberManager<TSceneMember> manager, ILogger logger)
    {
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
        ThrowHelpers.ThrowIfNull(logger);
        _transformChanged = AsyncEventPublisher.CreateConflating<Transform>(logger);
        _syncRoot = new ReaderWriterLockSlim();
        _destroyedSubscription = manager.Destroyed.Subscribe(OnSceneMemberDestroyed);
    }

    private async ValueTask OnSceneMemberDestroyed(TSceneMember value, CancellationToken cancellationToken)
    {
        if (!Manager.Equals(SceneMember, value))
        {
            return;
        }

        _destroyedSubscription.Dispose();
        _destroyed = true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(TSceneMember? other)
    {
        return Manager.Equals(SceneMember, other);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals([NotNullWhen(true)] Control<TSceneMember>? other)
    {
        return Manager.Equals(SceneMember, other?.SceneMember);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Control<TSceneMember> other && Equals(other);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        return Manager.GetHashCode(SceneMember);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string? ToString()
    {
        return SceneMember.ToString();
    }

    /// <inheritdoc/>
    protected override void OnDisposing()
    {
        base.OnDisposing();
        _syncRoot.Dispose();
        _destroyedSubscription.Dispose();
    }

    /// <summary>
    /// Compares two values to determine equality.
    /// </summary>
    /// <param name="left">The value to compare with <paramref name="right"/>.</param>
    /// <param name="right">The value to compare with <paramref name="left"/>.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="left"/> is equal to <paramref name="right"/>; 
    /// otherwise, <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Control<TSceneMember> left, Control<TSceneMember> right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two values to determine inequality.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if <paramref name="left"/> is not equal to <paramref name="right"/>; 
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <inheritdoc cref="operator ==(Control{TSceneMember}, Control{TSceneMember})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Control<TSceneMember> left, Control<TSceneMember> right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc cref="operator ==(Control{TSceneMember}, Control{TSceneMember})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Control<TSceneMember> left, TSceneMember right)
    {
        return left.Equals(right);
    }

    /// <inheritdoc cref="operator !=(Control{TSceneMember}, Control{TSceneMember})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Control<TSceneMember> left, TSceneMember right)
    {
        return !left.Equals(right);
    }
}
