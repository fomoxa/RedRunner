using Fomoxa;

namespace RedRunner.Networking.Models
{
    [Network]
    [Codec("net")]
    public class PlayerMotion
    {
        [Network("f32")]
        [Codec("net")]
        public float X { get; set; }

        [Network("f32")]
        [Codec("net")]
        public float Y { get; set; }

        [Network("f32")]
        [Codec("net")]
        public float VelocityX { get; set; }

        [Network("f32")]
        [Codec("net")]
        public float VelocityY { get; set; }

        [Network("f32")]
        [Codec("net")]
        public float RunSmoothVelocity { get; set; }
    }
}
