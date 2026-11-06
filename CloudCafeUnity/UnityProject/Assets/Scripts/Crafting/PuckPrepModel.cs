using System;

namespace CloudCafe.Crafting
{
    public sealed class PuckPrepModel
    {
        public const float RequiredRotationDegrees = 270f;
        public const float RequiredTampDepth = .85f;
        public const float RequiredFirmSeconds = .25f;

        public float Rotation { get; private set; }
        public float Depth { get; private set; }
        public bool Leveled => Rotation >= RequiredRotationDegrees;
        public bool Complete { get; private set; }
        public float LevelProgress => Math.Max(0f, Math.Min(1f, Rotation / RequiredRotationDegrees));
        public float TampHoldProgress => Complete ? 1f :
            Math.Max(0f, Math.Min(1f, firmTime / RequiredFirmSeconds));
        public float FirmTime => firmTime;

        private bool leveling, tamping;
        private float firmTime;

        public void BeginLevel()
        {
            if (!Complete) leveling = true;
        }

        public void Rotate(float degrees)
        {
            if (leveling && !float.IsNaN(degrees) && !float.IsInfinity(degrees))
                Rotation = Math.Min(RequiredRotationDegrees, Rotation + Math.Abs(degrees));
        }

        public void EndLevel() => leveling = false;

        public bool BeginTamp()
        {
            if (!Leveled || Complete) return false;
            tamping = true;
            firmTime = 0f;
            return true;
        }

        public void Press(float depth, float dt)
        {
            if (!tamping || float.IsNaN(depth) || float.IsInfinity(depth)) return;
            Depth = Math.Max(0f, Math.Min(1f, depth));
            if (!float.IsNaN(dt) && !float.IsInfinity(dt) && dt > 0f)
            {
                if (Depth >= RequiredTampDepth) firmTime += Math.Min(.05f, dt);
                else firmTime = 0f;
            }
        }

        public void Release()
        {
            if (tamping && firmTime >= RequiredFirmSeconds) Complete = true;
            tamping = false;
            Depth = firmTime = 0f;
        }

        public void Reset()
        {
            Rotation = Depth = firmTime = 0f;
            leveling = tamping = Complete = false;
        }
    }
}
