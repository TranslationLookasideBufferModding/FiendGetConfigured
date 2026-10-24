using System;

namespace FiendGetConfigured
{
    internal sealed class Settings
    {
        internal float WalkSpeed = 3f;
        internal float StalkSpeed = 1f;
        internal float ChaseSpeed = 5.5f;
        internal float RageSpeed = 7f;
        internal int Damage = 25;
        internal int ExecutionHealthThreshold = 25;
        internal float AttackDistance = 2f;
        internal float AttackCooldownSeconds = 2f;
        internal float KnockbackForce = 15f;
        internal bool RequireLineOfSight = true;
        internal float DetectionRange = 30f;
        internal float FieldOfViewDegrees = 140f;
        internal float TargetMemorySeconds = 8f;
        internal bool TeleportEnabled = false;
        internal bool ApparatusRageEnabled = false;
        internal bool TemporaryRageEnabled = false;
        internal bool BreakDoorsEnabled = true;
        internal bool BreakerBoxDestructionEnabled = true;
        internal bool LateGameBoostEnabled = false;
        internal int LateGameStartHour = 15;
        internal int LateGameMultiplier = 2;
    }

    internal static class Policy
    {
        internal static bool CanRemember(float now, float lastSeen, float duration)
        {
            return duration > 0f && now >= lastSeen && now - lastSeen <= duration;
        }

        internal static bool IsInRange(float squaredDistance, float range)
        {
            return !float.IsNaN(squaredDistance) && squaredDistance >= 0f && squaredDistance <= range * range;
        }

        internal static bool CanStartAttack(float now, ref float nextAttack, float cooldown)
        {
            if (now < nextAttack) return false;
            nextAttack = now + cooldown;
            return true;
        }

        internal static float Speed(Settings s, float requested, int state, bool stalking, bool rage, int factor)
        {
            if (requested <= 0f) return requested;
            float speed = stalking ? s.StalkSpeed : state == 2 ? (rage ? s.RageSpeed : s.ChaseSpeed) : s.WalkSpeed;
            return speed * (s.LateGameBoostEnabled ? Math.Max(1, factor) : 1);
        }
    }
}
