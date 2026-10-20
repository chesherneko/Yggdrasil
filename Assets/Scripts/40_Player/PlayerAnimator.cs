using System;
using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAnimator : MonoBehaviour
    {
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int AttackHash = Animator.StringToHash("Attack");

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Attack Timing")]
        [SerializeField] private string attackStateName = "Attack";
        [SerializeField, Range(0f, 1f)] private float attackReleaseNormalizedTime = 0.65f;

        [Header("Death")]
        [SerializeField] private string deathStateName = "Death";

        private PlayerController playerController;
        private bool hasIsMovingParameter;
        private bool hasAttackParameter;
        private bool isAttackPlaying;
        private bool attackStateEntered;
        private bool attackReleased;
        private Action pendingAttackRelease;
        private bool isDeathPlaying;
        private bool deathStateEntered;
        private bool deathCompleted;
        private Action pendingDeathComplete;

        public bool IsAttackPlaying => isAttackPlaying;
        public bool IsDeathPlaying => isDeathPlaying;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);

            if (spriteRenderer == null)
            {
                spriteRenderer = animator != null
                    ? animator.GetComponent<SpriteRenderer>()
                    : GetComponentInChildren<SpriteRenderer>(true);
            }

            CacheAnimatorParameters();
        }

        private void Update()
        {
            if (animator == null || playerController == null) return;

            if (isDeathPlaying)
            {
                if (hasIsMovingParameter)
                    animator.SetBool(IsMovingHash, false);

                UpdateDeathTiming();
                return;
            }

            if (hasIsMovingParameter)
                animator.SetBool(IsMovingHash, playerController.IsMoving);

            // 이동 중에는 실제 이동 입력 방향을 바라본다.
            // 공격 중에는 BattleManager가 지정한 공격 방향을 유지한다.
            if (!isAttackPlaying && Mathf.Abs(playerController.MoveInput) > 0.01f)
                SetFacingDirection(playerController.MoveInput);

            UpdateAttackTiming();
        }

        public bool PlayAttack(Action onRelease)
        {
            if (animator == null || !hasAttackParameter || isAttackPlaying || isDeathPlaying)
                return false;

            isAttackPlaying = true;
            attackStateEntered = false;
            attackReleased = false;
            pendingAttackRelease = onRelease;
            playerController?.SetActionLocked(true);

            animator.ResetTrigger(AttackHash);
            animator.SetTrigger(AttackHash);
            return true;
        }

        public bool PlayDeath(Action onComplete)
        {
            if (animator == null || isDeathPlaying)
                return false;

            // 사망하면 진행 중이던 공격은 취소하고 죽음 상태를 최우선으로 재생한다.
            pendingAttackRelease = null;
            isAttackPlaying = false;
            attackStateEntered = false;
            attackReleased = false;

            isDeathPlaying = true;
            deathStateEntered = false;
            deathCompleted = false;
            pendingDeathComplete = onComplete;

            playerController?.SetActionLocked(true);

            if (hasIsMovingParameter)
                animator.SetBool(IsMovingHash, false);

            if (hasAttackParameter)
                animator.ResetTrigger(AttackHash);

            animator.Play(deathStateName, 0, 0f);
            return true;
        }

        public void SetFacingDirection(float direction)
        {
            if (spriteRenderer == null) return;

            spriteRenderer.flipX = direction < 0f;
        }

        private void CacheAnimatorParameters()
        {
            hasIsMovingParameter = false;
            hasAttackParameter = false;

            if (animator == null || animator.runtimeAnimatorController == null) return;

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.nameHash == IsMovingHash &&
                    parameter.type == AnimatorControllerParameterType.Bool)
                {
                    hasIsMovingParameter = true;
                }
                else if (parameter.nameHash == AttackHash &&
                         parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    hasAttackParameter = true;
                }
            }
        }

        private void UpdateAttackTiming()
        {
            if (!isAttackPlaying) return;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(attackStateName))
            {
                attackStateEntered = true;

                if (!attackReleased &&
                    state.normalizedTime >= attackReleaseNormalizedTime)
                {
                    ReleaseAttack();
                }

                if (state.normalizedTime >= 1f)
                    FinishAttack();

                return;
            }

            if (attackStateEntered)
                FinishAttack();
        }

        private void UpdateDeathTiming()
        {
            if (deathCompleted) return;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(deathStateName))
            {
                deathStateEntered = true;

                if (state.normalizedTime >= 1f)
                    FinishDeath();

                return;
            }

            if (deathStateEntered)
                FinishDeath();
        }

        private void FinishDeath()
        {
            if (deathCompleted) return;

            deathCompleted = true;
            Action callback = pendingDeathComplete;
            pendingDeathComplete = null;
            callback?.Invoke();
        }

        private void ReleaseAttack()
        {
            if (attackReleased) return;

            attackReleased = true;
            Action callback = pendingAttackRelease;
            pendingAttackRelease = null;
            callback?.Invoke();
        }

        private void FinishAttack()
        {
            // 상태가 예상보다 일찍 끝나더라도 공격 자체가 사라지지는 않게 한다.
            ReleaseAttack();

            isAttackPlaying = false;
            attackStateEntered = false;
            playerController?.SetActionLocked(false);
        }

        private void OnDisable()
        {
            pendingAttackRelease = null;
            isAttackPlaying = false;
            attackStateEntered = false;
            attackReleased = false;

            pendingDeathComplete = null;
            deathStateEntered = false;

            if (!isDeathPlaying)
                playerController?.SetActionLocked(false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);

            if (spriteRenderer == null)
            {
                spriteRenderer = animator != null
                    ? animator.GetComponent<SpriteRenderer>()
                    : GetComponentInChildren<SpriteRenderer>(true);
            }
        }
#endif
    }
}
