using UnityEngine;

namespace Combat
{
    [RequireComponent(typeof(Collider2D))]
    public class HitBox : MonoBehaviour
    {
        private int damage;
        private string attackerId;
        private string castId;
        private bool hasHit;

        public void Init(int damageAmount, string ownerId, string attackCastId)
        {
            damage = damageAmount;
            attackerId = ownerId;
            castId = attackCastId;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (hasHit) return;

            HurtBox targetHurtBox = CombatSystem.Instance.GetHurtBoxOrNull(collision);
            if (targetHurtBox == null || targetHurtBox.Owner == null) return;
            if (targetHurtBox.Owner.OwnerId == attackerId) return;

            if (!CombatSystem.Instance.TryApplyLocalHit(
                    targetHurtBox.Owner.OwnerId,
                    damage,
                    castId)) return;

            hasHit = true;
            GetComponent<Collider2D>().enabled = false;
        }
    }
}
