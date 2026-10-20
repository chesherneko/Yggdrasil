using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(Collider2D))]
    public class CharacterController2D : MonoBehaviour
    {
        [Header("Settings")]
        public float        speed               = 4.0f;
        public float        skinWidth           = 0.02f;
        public LayerMask    collisionLayer;

        [Header("Dodge Settings")]
        public float        dodgeSpeedBonus     = 2.0f;
        public float        dodgeDuration       = 0.5f;
        public float dodgeCooldown = 1.5f;

        private float nextDodgeTime;
        
        private Collider2D  col;
        
        private Vector3 externalDelta       = Vector3.zero;   
        private float   moveInput           = 0.0f;

        private CancellationTokenSource dodgeCts;

        public bool IsInvincible { get; private set; } = false;


        #region Unity Methods

        private void Awake()
        {
            col = GetComponent<Collider2D>();
        }

        private void Update()
        {
            Vector3 desireMove =
                new Vector3(moveInput * speed, 0) * Time.deltaTime
                + externalDelta;

            externalDelta = Vector3.zero;

            Vector3 moveVector = Vector3.zero;
            moveVector.x = ResolveAxisMovement(desireMove.x, Vector3.right);
            
            transform.position += moveVector;
        }

        #endregion

        #region Public Methods (명령 통로)

        public void SetMoveInput(float horizontal)
        {
            moveInput = Mathf.Clamp(horizontal, -1.0f, 1.0f);
        }

        public void AddExternalMovement(Vector3 delta)
        {
            externalDelta += delta;
        }

        #endregion

        #region Private Methods (운동 파이프라인)

        private float ResolveAxisMovement(float move, Vector3 axis)
        {
            if (move == 0.0f) return 0.0f;

            Vector3 dir = (move > 0.0f ? axis : -axis);
            float distance = Mathf.Abs(move);
            Bounds box = GetCastBox();
            
            RaycastHit2D[] hits = Physics2D.BoxCastAll(
                box.center, box.size, 0.0f, dir, distance, collisionLayer);

            float allowedDistance = distance;
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null || hit.collider == col) continue;

                Bounds obstacle = hit.collider.bounds;

                // 바닥도 벽과 같은 충돌 레이어를 사용한다. 바닥처럼 세로로
                // 맞닿기만 한 콜라이더는 수평 이동을 막지 않는다.
                float verticalOverlap =
                    Mathf.Min(box.max.y, obstacle.max.y) -
                    Mathf.Max(box.min.y, obstacle.min.y);
                if (verticalOverlap <= skinWidth) continue;

                // 이미 접촉한 벽에서 멀어지는 입력은 허용한다.
                float obstacleDirection = obstacle.center.x - box.center.x;
                if (obstacleDirection * dir.x <= 0f) continue;

                float hitDistance = Mathf.Max(0f, hit.distance - skinWidth);
                allowedDistance = Mathf.Min(allowedDistance, hitDistance);
            }

            return Mathf.Sign(move) * allowedDistance;
        }

        private Bounds GetCastBox()
        {
            Bounds box = col.bounds; 

            Vector3 size = box.size;
            size.x -= skinWidth * 2;
            size.y -= skinWidth * 2;
            box.size = size;

            return box;
        }

        #endregion

        #region Dodge Pipeline

        public async UniTaskVoid Dodge(float direction)
        {
            // 회피 동작 중이거나 쿨타임이면 사용 불가
            if (IsInvincible || Time.time < nextDodgeTime)
                return;

            // 회피를 시작한 시점을 기준으로 쿨타임 적용
            nextDodgeTime = Time.time + dodgeCooldown;
            IsInvincible = true;

            dodgeCts?.Cancel();
            dodgeCts?.Dispose();
            dodgeCts = new CancellationTokenSource();

            float dir = Mathf.Abs(direction) > 0.01f
                ? Mathf.Sign(direction)
                : 0.0f;

            float elapsed = 0.0f;

            try
            {
                while (elapsed < dodgeDuration)
                {
                    if (dir != 0.0f)
                    {
                        AddExternalMovement(
                            Vector3.right * dir *
                            (speed + dodgeSpeedBonus) *
                            Time.deltaTime
                        );
                    }

                    elapsed += Time.deltaTime;

                    await UniTask.Yield(
                        PlayerLoopTiming.Update,
                        cancellationToken: dodgeCts.Token
                    );
                }
            }
            catch (System.OperationCanceledException)
            {
            }
            finally
            {
                IsInvincible = false;
                dodgeCts?.Dispose();
                dodgeCts = null;
            }
        }

        #endregion
    }
}
