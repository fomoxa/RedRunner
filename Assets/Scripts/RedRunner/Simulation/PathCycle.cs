using System;
using System.Collections.Generic;
using System.Numerics;

namespace RedRunner.Simulation
{
    public enum PathMove
    {
        MoveTowards,
        Lerp,
        SmoothDamp,
        Acceleration,
    }

    public readonly struct PathStop
    {
        public PathStop(Vector3 position, PathMove move, float speed, float smoothTime, float acceleration, float maxSpeed, float delay)
        {
            Position = position;
            Move = move;
            Speed = speed;
            SmoothTime = smoothTime;
            Acceleration = acceleration;
            MaxSpeed = maxSpeed;
            Delay = delay;
        }

        public Vector3 Position { get; }

        public PathMove Move { get; }

        public float Speed { get; }

        public float SmoothTime { get; }

        public float Acceleration { get; }

        public float MaxSpeed { get; }

        public float Delay { get; }
    }

    public static class PathCycle
    {
        private const float MaxSeconds = 600f;

        public static List<Vector3> Build(IReadOnlyList<PathStop> stops, bool continueToStart, float step, float tickDelta, Func<int, float> delayOf)
        {
            var cycle = new List<Vector3>();
            if (stops.Count == 0)
            {
                return cycle;
            }

            int index = 0;
            int direction = 1;
            Vector3 position = stops[0].Position;
            if (stops.Count < 2)
            {
                cycle.Add(position);
                return cycle;
            }

            Vector3 smoothVelocity = Vector3.Zero;
            float overTimeSpeed = 0f;
            int waitTicks = 0;
            bool left = false;
            int limit = (int)Math.Ceiling(MaxSeconds / tickDelta);
            while (cycle.Count < limit)
            {
                cycle.Add(position);
                if (waitTicks > 0)
                {
                    waitTicks--;
                    if (waitTicks == 0)
                    {
                        overTimeSpeed = 0f;
                        Advance(stops.Count, continueToStart, ref index, ref direction);
                        left |= index != 0;
                    }

                    continue;
                }

                PathStop target = stops[index];
                Vector3 destination = target.Position;
                switch (target.Move)
                {
                    case PathMove.MoveTowards:
                        position = Smoothing.MoveTowards(position, destination, tickDelta * target.Speed);
                        break;
                    case PathMove.Lerp:
                        position = Smoothing.Lerp(position, destination, tickDelta * target.Speed);
                        break;
                    case PathMove.SmoothDamp:
                        position = Smoothing.SmoothDamp(position, destination, ref smoothVelocity, target.SmoothTime, float.PositiveInfinity, tickDelta);
                        break;
                    case PathMove.Acceleration:
                        position = Smoothing.MoveTowards(position, destination, overTimeSpeed);
                        overTimeSpeed = Math.Min(overTimeSpeed + target.Acceleration, target.MaxSpeed);
                        break;
                }

                if (Smoothing.SquaredDistance(position, destination) < step * step)
                {
                    position = destination;
                    smoothVelocity = Vector3.Zero;
                    if (left && index == 0)
                    {
                        return cycle;
                    }

                    waitTicks = Math.Max(1, (int)Math.Ceiling(delayOf(index) / tickDelta));
                }
            }

            return cycle;
        }

        public static Vector3 PositionAt(IReadOnlyList<Vector3> cycle, uint tick) => cycle[(int)(tick % (uint)cycle.Count)];

        private static void Advance(int count, bool continueToStart, ref int index, ref int direction)
        {
            if (index <= 0)
            {
                direction = 1;
            }
            else if (index >= count - 1)
            {
                direction = -1;
            }

            if (index == count - 1 && continueToStart)
            {
                index = 0;
            }
            else
            {
                index += direction;
            }
        }
    }
}
