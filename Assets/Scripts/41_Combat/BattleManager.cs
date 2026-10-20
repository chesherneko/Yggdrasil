using System;
using System.Collections.Generic;
using BlueMarble;
using BlueMarble.Server;
using Jay;
using Player;
using UnityEngine;

namespace Combat
{
    public class SkillCastMessage
    {
        public string Type { get; set; } = ProtocolHeader.SKILL_CAST;
        public string CombatId { get; set; }
        public string CastId { get; set; }
        public string CasterId { get; set; }
        public bool IsBasicAttack { get; set; }
        public int EquipmentId { get; set; }
    }

    public class BattleManager : SingletonBase<BattleManager>
    {
        private Dictionary<AttackType, IAttackStrategy> strategies;
        private readonly HashSet<string> executedCastIds = new();

        public event Action<SkillCastMessage> OnSkillCastRequested;

        public GameObject basicArrowPrefab;
        private readonly IAttackStrategy basicAttackStrategy = new BasicAttackStrategy();

        public EquipmentTable equipmentTable;

        protected override void Awake()
        {
            if (instance != null && instance != this && basicArrowPrefab != null)
                instance.basicArrowPrefab = basicArrowPrefab;

            base.Awake();
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            strategies = new Dictionary<AttackType, IAttackStrategy>
            {
                { AttackType.Straight, new StraightAttackStrategy() },
                { AttackType.Parabola, new ParabolaAttackStrategy() },
                { AttackType.Area, new AreaAttackStrategy() }
            };

            equipmentTable = new EquipmentTable();
            LoadTableAsync();
        }

        private async void LoadTableAsync()
        {
            await equipmentTable.LoadAsync();
        }

        public void CastLocal(SkillCastMessage msg)
        {
            if (msg == null) return;

            if (string.IsNullOrEmpty(msg.CombatId))
                msg.CombatId = CombatSystem.Instance.CombatId;

            if (string.IsNullOrEmpty(msg.CastId))
                msg.CastId = Guid.NewGuid().ToString("N");

            if (CombatSystem.Instance.IsTestCombat)
            {
                ExecuteSkill(msg);
                return;
            }

            OnSkillCastRequested?.Invoke(msg);
            RoomClient.Instance.RequestSkillCast(msg);
        }

        public void ExecuteSkillFromNetwork(SkillCastMessage msg)
        {
            ExecuteSkill(msg);
        }

        public void ClearCombatState()
        {
            executedCastIds.Clear();
        }

        private void ExecuteSkill(SkillCastMessage msg)
        {
            if (msg == null) return;

            CombatSystem combatSystem = CombatSystem.Instance;
            if (!combatSystem.IsCombatActive) return;
            if (msg.CombatId != combatSystem.CombatId) return;

            if (!string.IsNullOrEmpty(msg.CastId) && !executedCastIds.Add(msg.CastId))
                return;

            if (!combatSystem.TryGetEntity(msg.CasterId, out CombatEntity entity)) return;

            EquipmentCaster caster = entity.GetComponent<EquipmentCaster>();
            if (caster == null || caster.spawnPoint == null) return;

            PlayerAnimator playerAnimator = entity.GetComponent<PlayerAnimator>();
            playerAnimator?.SetFacingDirection(caster.FacingDirection);

            // 서버에서 동일한 공격 메시지를 받은 각 클라이언트가 같은 애니메이션을
            // 재생하고, 활시위를 놓는 시점에 실제 투사체를 생성한다.
            if (playerAnimator != null &&
                playerAnimator.PlayAttack(() => SpawnSkill(msg)))
            {
                return;
            }

            SpawnSkill(msg);
        }

        private void SpawnSkill(SkillCastMessage msg)
        {
            CombatSystem combatSystem = CombatSystem.Instance;
            if (!combatSystem.IsCombatActive) return;
            if (!combatSystem.TryGetEntity(msg.CasterId, out CombatEntity entity)) return;

            EquipmentCaster caster = entity.GetComponent<EquipmentCaster>();
            if (caster == null || caster.spawnPoint == null) return;

            Vector3 spawnPoint = caster.spawnPoint.position;
            float direction = caster.FacingDirection;

            if (msg.IsBasicAttack)
            {
                int finalDamage = Mathf.RoundToInt(5 * combatSystem.DamageMultiplier);

                basicAttackStrategy.Execute(
                    spawnPoint,
                    direction,
                    finalDamage,
                    msg.CasterId,
                    msg.CastId,
                    basicArrowPrefab);
                return;
            }

            EquipmentDef def = equipmentTable.Get(msg.EquipmentId);
            if (def == null) return;

            GameObject prefab = Resources.Load<GameObject>(def.PrefabPath.Trim());
            if (prefab == null)
            {
                Debug.LogError($"[BattleManager] {def.PrefabPath} 프리팹을 찾을 수 없습니다.");
                return;
            }

            if (!strategies.TryGetValue(def.AttackType, out IAttackStrategy strategy)) return;

            int equipmentDamage = Mathf.RoundToInt(def.BaseDamage * combatSystem.DamageMultiplier);
            strategy.Execute(
                spawnPoint,
                direction,
                equipmentDamage,
                msg.CasterId,
                msg.CastId,
                prefab,
                def);
        }
    }
}
