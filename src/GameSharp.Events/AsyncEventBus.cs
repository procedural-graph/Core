using Microsoft.Extensions.Logging;
using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameSharp.Events;

internal sealed class AsyncEventBus<TSender, TEventArgs>(IAsyncEventListenerFactory<TEventArgs> factory, ILogger logger) : 
    ICollection<AsyncEventListener<TSender, TEventArgs>>
{
    private ImmutableArray<AsyncEventListener<TSender, TEventArgs>> _listeners;
    public ImmutableArray<AsyncEventListener<TSender, TEventArgs>> CurrentListeners => _listeners;

    private readonly IAsyncEventListenerFactory<TEventArgs> _factory = factory;
    private readonly ILogger _logger = logger;

    private readonly Lock _syncRoot = new();

    public int Count => _listeners.Length;

    bool ICollection<AsyncEventListener<TSender, TEventArgs>>.IsReadOnly => false;

    public AsyncEventListener<TSender, TEventArgs> GetOrAdd(AsyncEventHandler<TSender, TEventArgs> eventHandler)
    {
        ImmutableArray<AsyncEventListener<TSender, TEventArgs>> currentListeners = _listeners, previousListeners;

        if (TryGet(currentListeners, eventHandler, out AsyncEventListener<TSender, TEventArgs>? listener))
        {
            return listener;
        }

        lock (_syncRoot)
        {
            (currentListeners, previousListeners) = (_listeners, currentListeners);
            if (currentListeners == previousListeners || !TryGet(currentListeners, eventHandler, out listener))
            {
                listener = Start(eventHandler);
                _listeners = currentListeners.Add(listener);
            }
        }

        return listener;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(AsyncEventHandler<TSender, TEventArgs> eventHandler, [NotNullWhen(true)] out AsyncEventListener<TSender, TEventArgs>? result)
    {
        return TryGet(_listeners, eventHandler, out result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(AsyncEventListener<TSender, TEventArgs> item)
    {
        return _listeners.Contains(item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(AsyncEventListener<TSender, TEventArgs>[] array, int arrayIndex)
    {
        _listeners.CopyTo(array, arrayIndex);
    }

    public void Clear()
    {
        ImmutableArray<AsyncEventListener<TSender, TEventArgs>> currentListeners = _listeners, previousListeners;
        
        if (currentListeners.IsDefaultOrEmpty)
        {
            return;
        }

        lock (_syncRoot)
        {
            (currentListeners, previousListeners) = (_listeners, currentListeners);
            if (currentListeners == previousListeners || !currentListeners.IsDefaultOrEmpty)
            {
                _listeners = currentListeners.Clear();
            }
        }
    }

    public bool Remove(AsyncEventListener<TSender, TEventArgs> item)
    {
        ImmutableArray<AsyncEventListener<TSender, TEventArgs>> currentListeners = _listeners, previousListeners;

        int index = currentListeners.IndexOf(item);
        if (index < 0)
        {
            return false;
        }

        lock (_syncRoot)
        {
            (currentListeners, previousListeners) = (_listeners, currentListeners);
            if (currentListeners == previousListeners || (index = currentListeners.IndexOf(item)) > 0)
            {
                _listeners = RemoveAt(currentListeners, index);
            }
        }

        return false;
    }

    private static ImmutableArray<AsyncEventListener<TSender, TEventArgs>> RemoveAt(ImmutableArray<AsyncEventListener<TSender, TEventArgs>> items, int index)
    {
        int oldLength = items.Length, newLength = oldLength - 1;
        AsyncEventListener<TSender, TEventArgs>[] array = new AsyncEventListener<TSender, TEventArgs>[newLength];
        if (index == newLength)
        {
            items.CopyTo(0, array, 0, newLength);
        }
        else
        {
            items.CopyTo(0, array, 0, index);
            array[index] = items[newLength];
            int offset = index + 1;
            items.CopyTo(offset, array, offset, newLength - offset);
        }
        return ImmutableCollectionsMarshal.AsImmutableArray(array);
    }

    private AsyncEventListener<TSender, TEventArgs> Start(AsyncEventHandler<TSender, TEventArgs> eventHandler)
    {
        AsyncEventListener<TSender, TEventArgs> listener = _factory.Create(eventHandler, _logger);
        listener.Start();
        listener.StoppingToken.Register(listener.Unsubscribe, this);
        return listener;
    }

    private static bool TryGet(ImmutableArray<AsyncEventListener<TSender, TEventArgs>> listeners, 
        AsyncEventHandler<TSender, TEventArgs> eventHandler, 
        [NotNullWhen(true)] out AsyncEventListener<TSender, TEventArgs>? result)
    {
        foreach (AsyncEventListener<TSender, TEventArgs> item in listeners)
        {
            if (ReferenceEquals(item.EventHandler, eventHandler))
            {
                result = item;
                return true;
            }
        }
        result = null;
        return false;
    }

    public ImmutableArray<AsyncEventListener<TSender, TEventArgs>>.Enumerator GetEnumerator()
    {
        return _listeners.GetEnumerator();
    }

    void ICollection<AsyncEventListener<TSender, TEventArgs>>.Add(AsyncEventListener<TSender, TEventArgs> item)
    {
        ImmutableArray<AsyncEventListener<TSender, TEventArgs>> currentListeners = _listeners, previousListeners;

        if (currentListeners.Contains(item))
        {
            return;
        }

        lock (_syncRoot)
        {
            (currentListeners, previousListeners) = (_listeners, currentListeners);
            if (currentListeners == previousListeners || !currentListeners.Contains(item))
            {
                _listeners = currentListeners.Add(item);
            }
        }
    }

    IEnumerator<AsyncEventListener<TSender, TEventArgs>> IEnumerable<AsyncEventListener<TSender, TEventArgs>>.GetEnumerator()
    {
        IEnumerable<AsyncEventListener<TSender, TEventArgs>> listeners = ImmutableCollectionsMarshal.AsArray(_listeners) ?? 
            Enumerable.Empty<AsyncEventListener<TSender, TEventArgs>>();
        return listeners.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<AsyncEventListener<TSender, TEventArgs>>)this).GetEnumerator();
    }
}
