using ProceduralGraph.Collections;
using ProceduralGraph.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph.Generic;

internal sealed class CollectionChangeEventHandler<T> : IDisposable, IAsyncDisposable
{
    private readonly AsyncEventSubscription<ItemEventArgs<T>> _subscription;
    private readonly Action<T> _onItemAdded;
    private readonly Action<T> _onItemRemoved;

    public CollectionChangeEventHandler(AsyncEventManager<ItemEventArgs<T>> eventManager, Action<T> onItemAdded, Action<T> onItemRemoved)
    {
        ThrowHelpers.ThrowIfNull(eventManager);
        _subscription = eventManager.Subscribe(OnCollectionChangedAsync);

        _onItemAdded = onItemAdded ?? throw new ArgumentNullException(nameof(onItemAdded));
        _onItemRemoved = onItemRemoved ?? throw new ArgumentNullException(nameof(onItemRemoved));
    }

    public void Dispose() => _subscription.Dispose();

    public ValueTask DisposeAsync() => _subscription.DisposeAsync();

    private async ValueTask OnCollectionChangedAsync(ItemEventArgs<T> value, CancellationToken cancellationToken)
    {
        switch (value.ChangeType)
        {
            case ItemChangeType.Added: _onItemAdded(value.Item); break;
            case ItemChangeType.Removed: _onItemRemoved(value.Item); break;
            default: throw new InvalidOperationException("Unknown change type.");
        }
    }
}
