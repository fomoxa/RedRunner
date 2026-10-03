using Fomoxa;

namespace RedRunner.Networking.Models
{
    [Network]
    [Codec("net")]
    public class PlayerInput
    {
        [Network("f32")]
        [Codec("net")]
        public float Move { get; set; }

        [Network("bool")]
        [Codec("net")]
        public bool Jump { get; set; }

        [Network("bool")]
        [Codec("net")]
        public bool Roll { get; set; }
    }
}
