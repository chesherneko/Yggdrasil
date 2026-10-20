using BlueMarble;
using UnityEngine;

namespace Combat
{
    [RequireComponent(typeof(Animator))]
    public class AnimatedProjectile : MonoBehaviour
    {
        private Animator animator;
        private bool travelFinished;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void Launch(float direction, int damageAmount, string attackerId, string castId)
        {
            // 회전 애니메이션은 유지하고, 루트의 X축만 반전해 발사 방향을 맞춘다.
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (direction < 0f ? -1f : 1f);
            transform.localScale = scale;

            HitBox hitScript = GetComponentInChildren<HitBox>();
            if (hitScript != null)
            {
                hitScript.Init(damageAmount, attackerId, castId);
            }
        }

        private void Update()
        {
            if (travelFinished || animator == null || !animator.enabled) return;

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            if (stateInfo.normalizedTime >= 1.0f)
            {
                travelFinished = true;
                Destroy(gameObject);
            }
        }
    }
}
