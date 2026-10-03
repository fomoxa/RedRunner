// Copied by com.fomoxa.unity from its system models. Edit the package, not this file.
using System;
using Fomoxa;

namespace Fomoxa.Unity.Messaging
{
    [Network]
    [Codec("net")]
    [NetworkChannel("net", Channel.ReliableOrdered)]
    public class StateDelta
    {
        [Network("bytes")]
        [Codec("net")]
        public ReadOnlyMemory<byte> Data { get; set; }
    }
}
