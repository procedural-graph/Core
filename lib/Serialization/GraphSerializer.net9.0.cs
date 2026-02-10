using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ProceduralGraph.Generic;

public partial struct GraphSerializer
{
    private partial class GraphSerializationContext(ChannelWriter<Task<object>> writer)
    {
        public ChannelWriter<Task<object>> Writer { get; } = writer ?? throw new ArgumentNullException(nameof(writer));
        public int count;
    }
}
