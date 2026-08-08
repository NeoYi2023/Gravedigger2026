// Offline verification stubs: minimal UnityEngine surface used by the facing-stabilizer
// compile set (WarriorAnimView.DirIndexFromXZ copy + PushMapMonsterAgentView facing copy).
// Not part of the Unity build (lives in .scratch).
using System;

namespace UnityEngine
{
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3(0f, 0f, 0f);
        public float sqrMagnitude => x * x + y * y + z * z;
        public float magnitude => (float)Math.Sqrt(sqrMagnitude);
        public Vector3 normalized
        {
            get
            {
                var len = magnitude;
                return len > 1e-8f ? new Vector3(x / len, y / len, z / len) : new Vector3(0f, 0f, 0f);
            }
        }
        public override string ToString() => $"({x:F4}, {y:F4}, {z:F4})";
    }

    public static class Mathf
    {
        public const float Rad2Deg = 360f / (2f * (float)Math.PI);
        public const float Deg2Rad = (2f * (float)Math.PI) / 360f;
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
        public static float Abs(float f) => Math.Abs(f);
        public static float Sin(float f) => (float)Math.Sin(f);
        public static float Cos(float f) => (float)Math.Cos(f);

        // Unity Mathf.RoundToInt rounds half to even (banker's rounding).
        public static int RoundToInt(float f) => (int)Math.Round((double)f, MidpointRounding.ToEven);

        public static float DeltaAngle(float current, float target)
        {
            var delta = (target - current) % 360f;
            if (delta > 180f) delta -= 360f;
            if (delta < -180f) delta += 360f;
            return delta;
        }
    }
}
