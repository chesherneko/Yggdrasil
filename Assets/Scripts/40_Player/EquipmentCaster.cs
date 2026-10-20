using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using BlueMarble;
using Combat;

namespace Player
{
    public class EquipmentCaster : MonoBehaviour
    {
        [Header("References")]
        public Transform spawnPoint;
        
        [Header("Basic Attack Settings")]
        public float basicAttackInterval = 1.3f;
        public int baseDamage = 1;
        public string attackerName = "Player";

        [Header("Facing")]
        [SerializeField, Tooltip("오른쪽: 1, 왼쪽: -1")]
        private float facingDirection = 1f;

        public float FacingDirection => facingDirection;

        private float nextAttackTime = 0f;
        
        // false 이면 움직이지 않음
        public bool isLocalPlayer = true;

        private CancellationTokenSource attackCts;
        private PlayerController playerController;
        private PlayerAnimator playerAnimator;
        private PlayerEntity myEntity;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            playerAnimator = GetComponent<PlayerAnimator>();
            myEntity = GetComponent<PlayerEntity>();
            SetFacingDirection(facingDirection);
        }

        public void SetFacingDirection(float direction)
        {
            facingDirection = direction < 0f ? -1f : 1f;
            if (playerAnimator == null)
                playerAnimator = GetComponent<PlayerAnimator>();

            playerAnimator?.SetFacingDirection(facingDirection);

            // 발사 위치도 캐릭터가 바라보는 쪽으로 맞춘다.
            if (spawnPoint != null)
            {
                Vector3 localPosition = spawnPoint.localPosition;
                localPosition.x = Mathf.Abs(localPosition.x) * facingDirection;
                spawnPoint.localPosition = localPosition;
            }
        }

        private void OnEnable()
        {
            SetLocalPlayer(isLocalPlayer);
        }

        private void OnDisable()
        {
            StopAutoAttack();
        }

        public void SetLocalPlayer(bool value)
        {
            isLocalPlayer = value;

            if (isActiveAndEnabled == false) return;

            if (isLocalPlayer)
            {
                if (attackCts != null) return;

                attackCts = new CancellationTokenSource();
                AutoAttackRoutine(attackCts.Token).Forget();
            }
            else
            {
                StopAutoAttack();
            }
        }

        private void StopAutoAttack()
        {
            attackCts?.Cancel();
            attackCts?.Dispose();
            attackCts = null;
        }

        private async UniTaskVoid AutoAttackRoutine(CancellationToken token)
        {
            try
            {
                while (true)
                {
                    bool canAttack = isLocalPlayer
                                     && CombatSystem.Instance.IsCombatActive
                                     && !playerController.IsMoving 
                                     && !playerController.IsCastingSkill
                                     && !playerController.IsActionLocked;

                    if (canAttack && Time.time >= nextAttackTime)
                    {
                        SkillCastMessage msg = new SkillCastMessage 
                        {
                            CasterId = myEntity.OwnerId,
                            IsBasicAttack = true,
                            EquipmentId = 0
                        };

                        BattleManager.Instance.CastLocal(msg);
                        // Debug.Log($"[네트워크 전송] 기본 공격 발사 요청!");

                        nextAttackTime = Time.time + basicAttackInterval;
                    }
                    
                    if (playerController.IsMoving)
                    {
                        nextAttackTime = Mathf.Max(nextAttackTime, Time.time + 0.1f);
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
                }
            }
            catch (System.OperationCanceledException) { }
        }

        public void ActivateEquipment(EquipmentDef def)
        {
            SkillCastMessage msg = new SkillCastMessage
            {
                CasterId = myEntity.OwnerId,
                IsBasicAttack = false,
                EquipmentId = def.Id
            };
    
            BattleManager.Instance.CastLocal(msg);
            Debug.Log($"[로컬 전송] {def.Name} 스킬 발사 요청!");

            nextAttackTime = Time.time + basicAttackInterval;
        }
    }
}
