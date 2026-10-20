using UnityEngine;
using BlueMarble;

namespace Combat
{
    public interface IAttackStrategy
    {
        void Execute(Vector3 spawnPoint, float direction, int damage, string attackerName, string castId, GameObject prefab, EquipmentDef def = null);
    }
    
    // 기본 공격 (포물선 발사)
    public class BasicAttackStrategy : IAttackStrategy
    {
        public void Execute(Vector3 spawnPoint, float direction, int damage, string attackerName, string castId, GameObject prefab, EquipmentDef def = null)
        {
            if (prefab != null)
            {
                GameObject instance = Object.Instantiate(prefab, spawnPoint, Quaternion.identity);
                if (instance.TryGetComponent<AnimatedProjectile>(out AnimatedProjectile projectile))
                {
                    // Debug.Log($"{spawnPoint}");
                    projectile.Launch(direction, damage, attackerName, castId);
                }
            }
        }
    }

    // 장비 스킬들 (발동 로그만 출력)
    public class StraightAttackStrategy : IAttackStrategy
    {
        public void Execute(Vector3 spawnPoint, float direction, int damage, string attackerName, string castId, GameObject prefab, EquipmentDef def = null)
        {
            if (prefab != null)
            {
                Debug.Log("[Straight]");
                GameObject instance = Object.Instantiate(prefab, spawnPoint, Quaternion.identity);
                if (instance.TryGetComponent<AnimatedProjectile>(out AnimatedProjectile projectile))
                {
                    projectile.Launch(direction, damage, attackerName, castId);
                }
            }
        }
    }
    
    public class ParabolaAttackStrategy : IAttackStrategy
    {
        public void Execute(Vector3 spawnPoint, float direction, int damage, string attackerName, string castId, GameObject prefab, EquipmentDef def = null)
        {
            if (prefab != null)
            {
                Debug.Log("[Parabola]");
                GameObject instance = Object.Instantiate(prefab, spawnPoint, Quaternion.identity);
                if (instance.TryGetComponent<AnimatedProjectile>(out AnimatedProjectile projectile))
                {
                    projectile.Launch(direction, damage, attackerName, castId);
                }
            }
        }
    }
    
    public class AreaAttackStrategy : IAttackStrategy
    {
        public void Execute(Vector3 spawnPoint, float direction, int damage, string attackerName, string castId, GameObject prefab, EquipmentDef def = null)
        {
            if (prefab != null)
            {
                Debug.Log("[Area]");
                GameObject instance = Object.Instantiate(prefab, spawnPoint, Quaternion.identity);
                if (instance.TryGetComponent<AnimatedProjectile>(out AnimatedProjectile projectile))
                {
                    projectile.Launch(direction, damage, attackerName, castId);
                }
            }
        }
    }
}
