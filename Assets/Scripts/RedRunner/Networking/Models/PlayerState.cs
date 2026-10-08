using Fomoxa;
using Fomoxa.Networking;

namespace RedRunner.Networking.Models
{
    [Network]
    [Codec("net")]
    [NetworkChannel("net", Channel.ReliableOrdered)]
    public class PlayerState
    {
        [Network("bool")]
        [Codec("net")]
        public bool Dead { get; set; }

        [Network("u8")]
        [Codec("net")]
        public byte Cause { get; set; }
    }
}
