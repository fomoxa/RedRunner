using System.Collections.Generic;
using Fomoxa.Networking.Rapier;
using Fomoxa.Networking.Simulation;

namespace RedRunner.Simulation
{
    public enum Surface
    {
        None,
        Ground,
        Water,
        Saw,
        Spike,
        Mace,
    }

    public interface ISurfaces
    {
        Surface SurfaceOf(RapierCollider collider, out float top);
    }

    public static class RunnerContacts
    {
        public const float SpikeMargin = 0.2f;

        public static bool Grounded(RapierWorld2D world, BodyHandle body, int sensor, ISurfaces surfaces, List<RapierCollider> scratch)
        {
            scratch.Clear();
            world.Touching(RapierCollider.OfBody(body, sensor), scratch);
            foreach (RapierCollider other in scratch)
            {
                if (surfaces.SurfaceOf(other, out _) == Surface.Ground)
                {
                    return true;
                }
            }

            return false;
        }

        public static Surface Hazard(RapierWorld2D world, BodyHandle body, int colliders, float bottom, ISurfaces surfaces, List<RapierCollider> scratch)
        {
            scratch.Clear();
            for (int index = 0; index < colliders; index++)
            {
                world.Touching(RapierCollider.OfBody(body, index), scratch);
            }

            foreach (RapierCollider other in scratch)
            {
                Surface surface = surfaces.SurfaceOf(other, out float top);
                switch (surface)
                {
                    case Surface.Water:
                    case Surface.Saw:
                    case Surface.Mace:
                        return surface;
                    case Surface.Spike when bottom >= top - SpikeMargin:
                        return surface;
                }
            }

            return Surface.None;
        }
    }
}
