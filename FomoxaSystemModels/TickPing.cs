// Copied by com.fomoxa.unity from its system models. Edit the package, not this file.
using Fomoxa;

namespace Fomoxa.Unity.Messaging
{
    [Network]
    [Codec("net")]
    public class TickPing
    {
        [Network("u32")]
        [Codec("net")]
        public uint ClientTime { get; set; }

        [Network("u16")]
        [Codec("net")]
        public ushort Rtt { get; set; }
    }
}
