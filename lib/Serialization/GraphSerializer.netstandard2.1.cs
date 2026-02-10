using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ProceduralGraph.Generic
{
    public partial struct GraphSerializer
    {
        private partial class GraphSerializationContext
        {
            public ChannelWriter<Task<object>> Writer { get; }
            public int count;
            public GraphSerializationContext(ChannelWriter<Task<object>> writer)
            {
                Writer = writer ?? throw new ArgumentNullException(nameof(writer));
            }
        }
    }
}
