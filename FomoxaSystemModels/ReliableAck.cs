// Copied by com.fomoxa.unity from its system models. Edit the package, not this file.
using Fomoxa;

namespace Fomoxa.Unity.Messaging
{
    [Network]
    [Codec("net")]
    public class ReliableAck
    {
        [Network("u16")]
        [Codec("net")]
        public ushort Next { get; set; }

        [Network("u32")]
        [Codec("net")]
        public uint Received { get; set; }
    }
}
