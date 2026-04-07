using ProceduralGraph.Events;
using ProceduralGraph.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ProceduralGraph.Generic;

/// <summary>
/// Provides a handle for accessing and managing a scene member within a scene graph. Enables manipulation of scene
/// member properties and traversal of the scene hierarchy.
/// </summary>
/// <inheritdoc cref="LifecycleGraphNode{TSceneMember}"/>
public readonly struct SceneMemberHandle<TSceneMember> : IDisposable, IEquatable<TSceneMember>, IEquatable<SceneMemberHandle<TSceneMember>> 
    where TSceneMember : class
{
    private readonly ISceneMemberManager<TSceneMember> _manager;
    private readonly TSceneMember _sceneMember;

    /// <summary>
    /// Gets the underlying scene member associated with the current handle.
    /// </summary>
    public TSceneMember Value => _sceneMember;

    /// <summary>
    /// Gets the parent of the current scene member, if one exists.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if the current scene member has been destroyed.</exception>
    public TSceneMember? Parent
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowHelpers.ThrowIfDisposed(IsDisposed, this);
            return _manager.GetParent(_sceneMember);
        }
    }

    /// <summary>
    /// Gets the root of the scene graph that contains the current scene member.
    /// </summary>
    /// <inheritdoc cref="Parent"/>
    public TSceneMember Root
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowHelpers.ThrowIfDisposed(IsDisposed, this);
            return _manager.GetRoot(_sceneMember);
        }
    }

    /// <summary>
    /// Gets the immediate children of the current scene member.
    /// </summary>
    /// <inheritdoc cref="Parent"/>
    public IReadOnlyCollection<TSceneMember> Children
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowHelpers.ThrowIfDisposed(IsDisposed, this);
            return _manager.GetChildren(_sceneMember);
        }
    }

    /// <summary>
    /// Gets or sets the name associated with the current scene member.
    /// </summary>
    /// <inheritdoc cref="Parent"/>
    public string Name
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowHelpers.ThrowIfDisposed(IsDisposed, this);
            return _manager.GetName(_sceneMember);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            ThrowHelpers.ThrowIfDisposed(IsDisposed, this);
            _manager.SetName(_sceneMember, value);
        }
    }

    /// <summary>
    /// Gets or sets the position of the current scene member.
    /// </summary>
    /// <inheritdoc cref="Parent"/>
    public Double3 Position
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowHelpers.ThrowIfDisposed(IsDisposed, this);
            return _manager.GetPosition(_sceneMember);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            ThrowHelpers.ThrowIfDisposed(IsDisposed, this);
            _manager.SetPosition(_sceneMember, value);
        }
    }

    /// <summary>
    /// Gets or sets the rotation of the current scene member.
    /// </summary>
    /// <inheritdoc cref="Parent"/>
    public Quaternion Rotation
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowHelpers.ThrowIfDisposed(IsDisposed, this);
            return _manager.GetRotation(_sceneMember);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            ThrowHelpers.ThrowIfDisposed(IsDisposed, this);
            _manager.SetRotation(_sceneMember, value);
        }
    }

    /// <summary>
    /// Gets or sets the scale of the current scene member.
    /// </summary>
    /// <inheritdoc cref="Parent"/>
    public Vector3 Scale
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowHelpers.ThrowIfDisposed(IsDisposed, this);
            return _manager.GetScale(_sceneMember);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            ThrowHelpers.ThrowIfDisposed(IsDisposed, this);
            _manager.SetScale(_sceneMember, value);
        }
    }

    /// <summary>
    /// Gets or sets the transform of the current scene member.
    /// </summary>
    /// <inheritdoc cref="Parent"/>
    public Transform Transform
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowHelpers.ThrowIfDisposed(IsDisposed, this);
            return _manager.GetTransform(_sceneMember);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            ThrowHelpers.ThrowIfDisposed(IsDisposed, this);
            _manager.SetTransform(_sceneMember, value);
        }
    }

    /// <summary>
    /// Gets a value indicating whether the current scene member has been destroyed.
    /// </summary>
    public bool IsDisposed
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _manager.IsDestroyed(_sceneMember);
    }

    internal SceneMemberHandle(TSceneMember parent, ISceneMemberManager<TSceneMember> manager)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        _sceneMember = manager.CreateChild(parent);
    }

    /// <inheritdoc cref="ISceneMemberManager{TSceneMember}.SubscribeTransformChanged(TSceneMember, AsyncEventHandler{Transform})"/>
    public AsyncEventSubscription<Transform> SubscribeTransformChanged(AsyncEventHandler<Transform> handler)
    {
        ThrowHelpers.ThrowIfDisposed(IsDisposed, this);
        return _manager.SubscribeTransformChanged(_sceneMember, handler);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(TSceneMember? other)
    {
        return _manager.Equals(_sceneMember, other);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(SceneMemberHandle<TSceneMember> other)
    {
        return _manager.Equals(_sceneMember, other._sceneMember);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is SceneMemberHandle<TSceneMember> other && Equals(other);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        return _manager.GetHashCode(_sceneMember);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string? ToString()
    {
        return _sceneMember.ToString();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        _manager.Destroy(_sceneMember);
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
    public static bool operator ==(SceneMemberHandle<TSceneMember> left, SceneMemberHandle<TSceneMember> right)
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
    /// <inheritdoc cref="operator ==(SceneMemberHandle{TSceneMember}, SceneMemberHandle{TSceneMember})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(SceneMemberHandle<TSceneMember> left, SceneMemberHandle<TSceneMember> right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc cref="operator ==(SceneMemberHandle{TSceneMember}, SceneMemberHandle{TSceneMember})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(SceneMemberHandle<TSceneMember> left, TSceneMember right)
    {
        return left.Equals(right);
    }

    /// <inheritdoc cref="operator !=(SceneMemberHandle{TSceneMember}, SceneMemberHandle{TSceneMember})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(SceneMemberHandle<TSceneMember> left, TSceneMember right)
    {
        return !left.Equals(right);
    }
}
