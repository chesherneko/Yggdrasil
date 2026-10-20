using Player;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Combat
{
    public abstract class CombatEntity : MonoBehaviour
    {
        [field: SerializeField] public string OwnerId { get; private set; }
        [ShowInInspector, ReadOnly] private CharacterController2D controller;
        
        public int CurrentHp { get; protected set; }
        public int MaxHp { get; protected set; }

        public bool IsInvincible => controller.IsInvincible;

        public virtual void Init(string playerId, int maxHp)
        {
            OwnerId = playerId;
            CurrentHp = maxHp;
            MaxHp = maxHp;
            
            controller = GetComponent<CharacterController2D>();

            if (CombatSystem.Instance != null)
            {
                CombatSystem.Instance.RegisterEntity(this);
            }
        }

        public void ApplyHealthSnapshot(int currentHp)
        {
            CurrentHp = Mathf.Clamp(currentHp, 0, MaxHp);
        }

        protected virtual void OnDisable()
        {
            if (CombatSystem.IsInitialized)
                CombatSystem.Instance.UnregisterEntity(this);
        }

        public abstract void TakeDamage(int damage);
    }
}
