// Copied by com.fomoxa.unity from its system models. Edit the package, not this file.
using Fomoxa;

namespace Fomoxa.Unity.Messaging
{
    [Network]
    [Codec("net")]
    [NetworkChannel("net", Channel.ReliableOrdered)]
    public class ObjectDespawn
    {
        [Network("u32")]
        [Codec("net")]
        public uint ObjectId { get; set; }
    }
}
