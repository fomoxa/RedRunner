using System;
using System.Numerics;

namespace RedRunner.Simulation
{
    public static class Smoothing
    {
        public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            smoothTime = Math.Max(0.0001f, smoothTime);
            float omega = 2f / smoothTime;
            float x = omega * deltaTime;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            float change = current - target;
            float originalTo = target;
            float maxChange = maxSpeed * smoothTime;
            change = Math.Clamp(change, -maxChange, maxChange);
            target = current - change;
            float temp = (currentVelocity + omega * change) * deltaTime;
            currentVelocity = (currentVelocity - omega * temp) * exp;
            float output = target + (change + temp) * exp;
            if (originalTo - current > 0f == output > originalTo)
            {
                output = originalTo;
                currentVelocity = (output - originalTo) / deltaTime;
            }

            return output;
        }

        public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            smoothTime = Math.Max(0.0001f, smoothTime);
            float omega = 2f / smoothTime;
            float x = omega * deltaTime;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            float changeX = current.X - target.X;
            float changeY = current.Y - target.Y;
            float changeZ = current.Z - target.Z;
            Vector3 originalTo = target;
            float maxChange = maxSpeed * smoothTime;
            float maxChangeSquared = maxChange * maxChange;
            float squared = changeX * changeX + changeY * changeY + changeZ * changeZ;
            if (squared > maxChangeSquared)
            {
                float magnitude = (float)Math.Sqrt(squared);
                changeX = changeX / magnitude * maxChange;
                changeY = changeY / magnitude * maxChange;
                changeZ = changeZ / magnitude * maxChange;
            }

            target.X = current.X - changeX;
            target.Y = current.Y - changeY;
            target.Z = current.Z - changeZ;
            float tempX = (currentVelocity.X + omega * changeX) * deltaTime;
            float tempY = (currentVelocity.Y + omega * changeY) * deltaTime;
            float tempZ = (currentVelocity.Z + omega * changeZ) * deltaTime;
            currentVelocity.X = (currentVelocity.X - omega * tempX) * exp;
            currentVelocity.Y = (currentVelocity.Y - omega * tempY) * exp;
            currentVelocity.Z = (currentVelocity.Z - omega * tempZ) * exp;
            float outputX = target.X + (changeX + tempX) * exp;
            float outputY = target.Y + (changeY + tempY) * exp;
            float outputZ = target.Z + (changeZ + tempZ) * exp;
            float towardX = originalTo.X - current.X;
            float towardY = originalTo.Y - current.Y;
            float towardZ = originalTo.Z - current.Z;
            float pastX = outputX - originalTo.X;
            float pastY = outputY - originalTo.Y;
            float pastZ = outputZ - originalTo.Z;
            if (towardX * pastX + towardY * pastY + towardZ * pastZ > 0f)
            {
                outputX = originalTo.X;
                outputY = originalTo.Y;
                outputZ = originalTo.Z;
                currentVelocity.X = (outputX - originalTo.X) / deltaTime;
                currentVelocity.Y = (outputY - originalTo.Y) / deltaTime;
                currentVelocity.Z = (outputZ - originalTo.Z) / deltaTime;
            }

            return new Vector3(outputX, outputY, outputZ);
        }

        public static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDistanceDelta)
        {
            float towardX = target.X - current.X;
            float towardY = target.Y - current.Y;
            float towardZ = target.Z - current.Z;
            float squared = towardX * towardX + towardY * towardY + towardZ * towardZ;
            if (squared == 0f || (maxDistanceDelta >= 0f && squared <= maxDistanceDelta * maxDistanceDelta))
            {
                return target;
            }

            float distance = (float)Math.Sqrt(squared);
            return new Vector3(current.X + towardX / distance * maxDistanceDelta, current.Y + towardY / distance * maxDistanceDelta, current.Z + towardZ / distance * maxDistanceDelta);
        }

        public static Vector3 Lerp(Vector3 from, Vector3 to, float amount)
        {
            amount = Math.Clamp(amount, 0f, 1f);
            return new Vector3(from.X + (to.X - from.X) * amount, from.Y + (to.Y - from.Y) * amount, from.Z + (to.Z - from.Z) * amount);
        }

        public static float SquaredDistance(Vector3 from, Vector3 to)
        {
            float x = from.X - to.X;
            float y = from.Y - to.Y;
            float z = from.Z - to.Z;
            return x * x + y * y + z * z;
        }
    }
}
