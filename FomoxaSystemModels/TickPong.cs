// Copied by com.fomoxa.unity from its system models. Edit the package, not this file.
using Fomoxa;

namespace Fomoxa.Unity.Messaging
{
    [Network]
    [Codec("net")]
    public class TickPong
    {
        [Network("u32")]
        [Codec("net")]
        public uint ClientTime { get; set; }

        [Network("u32")]
        [Codec("net")]
        public uint ServerTick { get; set; }

        [Network("i16")]
        [Codec("net")]
        public short InputLead { get; set; }
    }
}
