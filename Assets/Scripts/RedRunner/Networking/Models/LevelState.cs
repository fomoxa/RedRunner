using Fomoxa;
using Fomoxa.Networking;

namespace RedRunner.Networking.Models
{
    [Network]
    [Codec("net")]
    [NetworkChannel("net", Channel.ReliableOrdered)]
    public class LevelState
    {
        [Network("u32")]
        [Codec("net")]
        public uint Seed { get; set; }
    }
}
