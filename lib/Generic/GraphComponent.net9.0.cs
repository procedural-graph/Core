// Copyright (c) 2026 William Brocklesby. All rights reserved.
// 
// This source code is the proprietary property of William Brocklesby.
// Use, distribution, or modification of this file is strictly prohibited 
// without express written permission.
//
// Internal Use Only.
using System.Collections.Generic;

namespace ProceduralGraph.Generic;

public abstract partial class GraphComponent<TKey, TValue>
{
    IReadOnlyCollection<IGraphNode> IGraphNode.Descendants => [];
}
