using Combat;
using UnityEngine;

namespace Player
{
    public class PlayerEntity : CombatEntity
    {
        private bool isDead;

        public override void Init(string playerId, int maxHp)
        {
            base.Init(playerId, maxHp);
            isDead = false;
        }

        public override void TakeDamage(int damage)
        {
            if (isDead) return;

            CurrentHp -= damage;
            
            Debug.Log($"[PlayerEntity] {OwnerId} 맞음.");

            if (CurrentHp <= 0)
            {
                CurrentHp = 0;
                isDead = true;
                Die();
            }
        }

        private void Die()
        {
            Debug.Log($"[PlayerEntity] {OwnerId} 사망");

            CombatSystem.Instance.OnPlayerDied(OwnerId);
        }
    }
}
