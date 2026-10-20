using UnityEngine;

namespace Combat
{
    public class HurtBox : MonoBehaviour
    {
        [field: SerializeField] public CombatEntity Owner { get; private set; }

        private Collider2D col2D;

        private void Awake()
        {
            Owner = GetComponent<CombatEntity>();
            col2D = GetComponent<Collider2D>();
        }

        private void OnEnable()
        {
            CombatSystem.Instance.AddHurtBox(col2D, this);
        }

        private void OnDisable()
        {
            CombatSystem.Instance.RemoveHurtBox(col2D);
        }
    }
}