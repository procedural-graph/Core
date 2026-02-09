// Copyright (c) 2026 William Brocklesby. All rights reserved.
// 
// This source code is the proprietary property of William Brocklesby.
// Use, distribution, or modification of this file is strictly prohibited 
// without express written permission.
//
// Internal Use Only.
using System.Diagnostics.CodeAnalysis;

namespace ProceduralGraph.Generic;

public abstract partial class Graph<TKey, TValue>
{
    private bool TryFindParent(TValue sceneMember, GraphEntity<TKey, TValue> rootEntity, [NotNullWhen(true)] out GraphEntity<TKey, TValue>? parentEntity)
    {
        TValue? parent = SceneMemberInfoProvider.GetParent(sceneMember);

        using var bfsEnumerator = new BreadthFirstSearchEnumerator<GraphEntity<TKey, TValue>>(rootEntity, GraphEntity<TKey, TValue>.EnqueueChildren);
        while (bfsEnumerator.MoveNext())
        {
            GraphEntity<TKey, TValue> current = bfsEnumerator.Current;

            if (current is not IProxyGraphNode<TValue> proxyNode)
            {
                continue;
            }

            TValue? currentParent = SceneMemberInfoProvider.GetParent(proxyNode.SceneMember);
            if (SceneMemberInfoProvider.Equals(currentParent, parent))
            {
                parentEntity = current;
                return true;
            }
        }

        parentEntity = null;
        return false;
    }
}
