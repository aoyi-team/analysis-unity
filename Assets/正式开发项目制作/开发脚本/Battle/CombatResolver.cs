using System.Collections.Generic;
using FixMath;
using UnityEngine;

public static class CombatResolver
{
    public static int ApplyPointAttack(
        _playerInfo attacker,
        IEnumerable<_playerInfo> players,
        Vector2 target,
        Fixed64 damage,
        Fixed64 radius)
    {
        if (attacker == null || players == null || attacker.IsDead) return 0;
        if (damage <= Fixed64.Zero || radius < Fixed64.Zero) return 0;

        Fixed64 radiusSqr = radius * radius;
        int hitCount = 0;

        foreach (_playerInfo player in players)
        {
            if (player == null || player == attacker || player.IsDead) continue;
            if (player.TeamId == attacker.TeamId) continue;

            FixedVector2 delta = player._currLogicPos - new FixedVector2(target);
            if (delta.SqrMagnitude > radiusSqr) continue;

            player.TakeDamage(damage);
            hitCount++;
        }

        return hitCount;
    }
}
