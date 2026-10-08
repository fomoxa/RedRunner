using System;
using System.Numerics;
using Fomoxa.Networking.Simulation;

namespace RedRunner.Simulation
{
    public readonly struct RunnerSettings
    {
        public RunnerSettings(float runSpeed, float maxRunSpeed, float runSmoothTime, float jumpStrength, float rollForce)
        {
            RunSpeed = runSpeed;
            MaxRunSpeed = maxRunSpeed;
            RunSmoothTime = runSmoothTime;
            JumpStrength = jumpStrength;
            RollForce = rollForce;
        }

        public float RunSpeed { get; }

        public float MaxRunSpeed { get; }

        public float RunSmoothTime { get; }

        public float JumpStrength { get; }

        public float RollForce { get; }
    }

    [Flags]
    public enum RunnerActions
    {
        None = 0,
        Jumped = 1,
        Rolled = 2,
    }

    public sealed class RunnerMotion
    {
        public float RunSmoothVelocity { get; set; }

        public int Facing { get; set; } = 1;

        public RunnerActions Simulate(PhysicsBody2D body, in RunnerSettings settings, float move, bool jump, bool roll, bool grounded, bool dead, float deltaTime)
        {
            float speed = Math.Abs(body.Velocity.X);
            float runSpeed = settings.RunSpeed;
            if (speed >= settings.RunSpeed)
            {
                float smoothVelocity = RunSmoothVelocity;
                runSpeed = Smoothing.SmoothDamp(speed, settings.MaxRunSpeed, ref smoothVelocity, settings.RunSmoothTime, float.PositiveInfinity, deltaTime);
                RunSmoothVelocity = smoothVelocity;
            }

            if (dead)
            {
                return RunnerActions.None;
            }

            Vector2 velocity = body.Velocity;
            velocity.X = runSpeed * move;
            body.Velocity = velocity;
            if (move > 0f)
            {
                Facing = 1;
            }
            else if (move < 0f)
            {
                Facing = -1;
            }

            RunnerActions actions = RunnerActions.None;
            if (jump && grounded)
            {
                velocity = body.Velocity;
                velocity.Y = settings.JumpStrength;
                body.Velocity = velocity;
                actions |= RunnerActions.Jumped;
            }

            if (roll)
            {
                body.AddForce(new Vector2(Facing * settings.RollForce, 0f));
                actions |= RunnerActions.Rolled;
            }

            return actions;
        }
    }
}
