using System;
using System.Collections.Generic;
using BlueMarble;
using BlueMarble.Server;
using Cysharp.Threading.Tasks;
using Jay;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Combat
{
    public class CombatSystem : SingletonBase<CombatSystem>
    {
        private const string CombatSceneName = "Combat";

        [Header("References")]
        public Timer combatTimer;

        [Header("Damage Multiplier")]
        public float multiplierIncreaseStartTime = 10f;
        public float multiplierIncreaseInterval = 5f;
        public float multiplierIncreaseAmount = 1f;

        public float DamageMultiplier { get; private set; } = 1f;
        public bool IsCombatActive { get; private set; }
        public bool IsSceneLoading { get; private set; }

        public string CombatId { get; private set; }
        public string AttackerId { get; private set; }
        public string DefenderId { get; private set; }
        public string LocalPlayerId { get; private set; }
        public int StartHp { get; private set; } = 50;
        public float DurationSeconds { get; private set; } = 30f;
        public bool IsTestCombat { get; private set; }

        public bool HasCombatAssignment =>
            !string.IsNullOrEmpty(CombatId) &&
            !string.IsNullOrEmpty(AttackerId) &&
            !string.IsNullOrEmpty(DefenderId) &&
            !string.IsNullOrEmpty(LocalPlayerId);

        public event Action<CombatAssignmentMessage> OnCombatAssigned;
        public event Action<CombatResultMessage> OnCombatEnded;

        private float elapsedTime;
        private float nextMultiplierIncreaseTime;
        private string requestId;
        private bool isFinishing;
        private CombatUI combatUI;

        private UniTaskCompletionSource<CombatOutcome> combatResult;
        private readonly HashSet<string> appliedHitIds = new();
        private readonly Dictionary<Collider2D, HurtBox> hurtBoxes = new();
        private readonly Dictionary<string, CombatEntity> entities = new();

        protected override void Awake()
        {
            if (instance != null && instance != this)
            {
                instance.AttachTimer(combatTimer);
                instance.multiplierIncreaseStartTime = multiplierIncreaseStartTime;
                instance.multiplierIncreaseInterval = multiplierIncreaseInterval;
                instance.multiplierIncreaseAmount = multiplierIncreaseAmount;
            }

            base.Awake();
        }

        [Button]
        public void CombatStart()
        {
            StartCombatRuntime();
        }

        private void Update()
        {
            if (!IsCombatActive) return;

            elapsedTime += Time.deltaTime;

            if (elapsedTime < nextMultiplierIncreaseTime) return;

            DamageMultiplier += multiplierIncreaseAmount;
            nextMultiplierIncreaseTime = elapsedTime + multiplierIncreaseInterval;
            Debug.Log($"[CombatSystem] 데미지 배율 증가: x{DamageMultiplier}");
        }

        public async UniTask<CombatOutcome> StartCombat(string attackerId, Tile tile)
        {
            if (tile == null || string.IsNullOrEmpty(tile.State.OwnerId))
            {
                Debug.LogError("[CombatSystem] 대전 상대 정보가 없습니다.");
                return CombatOutcome.RejectedByDefender;
            }

            RoomClient client = RoomClient.Instance;

            requestId = Guid.NewGuid().ToString("N");
            AttackerId = attackerId;
            DefenderId = tile.State.OwnerId;
            LocalPlayerId = client.Me.Id;
            combatResult = new UniTaskCompletionSource<CombatOutcome>();

            int tileId = BlueMarbleWorld.Instance.Board.GetTileIndex(tile);

            await client.RequestCombat(new CombatRequestMessage
            {
                RequestId = requestId,
                TurnId = client.TurnId,
                TileId = tileId,
                DefenderId = DefenderId
            });

            Debug.Log($"[CombatSystem] 대전 요청: {AttackerId} vs {DefenderId}");
            return await combatResult.Task;
        }

        public void ReceiveCombatOffer(CombatOfferMessage offer)
        {
            ReceiveCombatOfferAsync(offer).Forget();
        }

        private async UniTaskVoid ReceiveCombatOfferAsync(CombatOfferMessage offer)
        {
            if (offer == null || RoomClient.Instance.Me.Id != offer.DefenderId) return;

            FightUI fightUI = FightUI.Instance;
            bool accepted = fightUI != null && await fightUI.ShowAsync(
                FightUIMode.Response,
                offer.AttackerId,
                offer.DefenderId,
                offer.RejectionChance);

            await RoomClient.Instance.RespondCombat(offer.CombatId, accepted);
        }

        public void CancelCombatFromNetwork(CombatCancelledMessage message)
        {
            if (message == null) return;
            if (!string.IsNullOrEmpty(requestId) && message.RequestId != requestId) return;

            FightUI.Instance?.Hide();
            combatResult?.TrySetResult(CombatOutcome.RejectedByDefender);
            ResetAssignment();

            Debug.Log($"[CombatSystem] 대전 취소: {message.Reason}");
        }

        public void PrepareCombatFromNetwork(CombatAssignmentMessage assignment)
        {
            if (assignment == null ||
                string.IsNullOrEmpty(assignment.CombatId) ||
                string.IsNullOrEmpty(assignment.AttackerId) ||
                string.IsNullOrEmpty(assignment.DefenderId))
            {
                Debug.LogError("[CombatSystem] 올바르지 않은 대전 배정 정보입니다.");
                return;
            }

            string myId = RoomClient.Instance.Me.Id;
            CombatId = assignment.CombatId;
            requestId = assignment.RequestId;
            AttackerId = assignment.AttackerId;
            DefenderId = assignment.DefenderId;
            LocalPlayerId = myId;
            StartHp = assignment.StartHp > 0 ? assignment.StartHp : 50;
            DurationSeconds = assignment.DurationSeconds > 0f ? assignment.DurationSeconds : 30f;
            IsTestCombat = assignment.IsTest;

            if (combatTimer != null)
                combatTimer.duration = DurationSeconds;

            combatResult ??= new UniTaskCompletionSource<CombatOutcome>();
            OnCombatAssigned?.Invoke(assignment);

            LoadCombatSceneAsync().Forget();
        }

        private async UniTaskVoid LoadCombatSceneAsync()
        {
            IsSceneLoading = true;

            try
            {
                Scene scene = SceneManager.GetSceneByName(CombatSceneName);

                if (!scene.isLoaded)
                    await SceneManager.LoadSceneAsync(CombatSceneName, LoadSceneMode.Additive);

                if (HasCombatAssignment) return;

                scene = SceneManager.GetSceneByName(CombatSceneName);
                if (scene.isLoaded)
                    await SceneManager.UnloadSceneAsync(scene);
            }
            finally
            {
                IsSceneLoading = false;
            }
        }

        public void NotifySceneReady()
        {
            if (IsTestCombat || string.IsNullOrEmpty(CombatId)) return;
            RoomClient.Instance.AnnounceCombatReady(CombatId).Forget();
        }

        public async UniTaskVoid BeginCombatFromNetwork(CombatStartedMessage message)
        {
            if (message == null || message.CombatId != CombatId) return;
            await BattleIntroManager.Instance.PlayAsync();   
            StartCombatRuntime();
        }
        
        public void CompleteCombatFromNetwork(CombatResultMessage result)
        {
            if (result == null || string.IsNullOrEmpty(result.WinnerId)) return;
            if (result.CombatId != CombatId) return;

            FinishCombatAsync(result).Forget();
        }

        private async UniTaskVoid FinishCombatAsync(CombatResultMessage result)
        {
            if (isFinishing) return;
            isFinishing = true;

            IsCombatActive = false;
            StopCombatTimer();

            if (LocalPlayerId == AttackerId || LocalPlayerId == DefenderId)
            {
                InventoryManager.Instance.ResetCooldownAll();
                InventoryManager.Instance.LossDurabilityAll();
            }

            if (TryGetEntity(result.LoserId, out CombatEntity loserEntity))
            {
                loserEntity.ApplyHealthSnapshot(0);
                loserEntity.GetComponent<Player.PlayerAnimator>()?.PlayDeath(null);
            }

            if (TryGetEntity(result.WinnerId, out CombatEntity winnerEntity))
                winnerEntity.GetComponent<Player.PlayerController>()?.SetActionLocked(true);

            combatUI?.ShowWinner($"{GetPlayerDisplayName(result.WinnerId)}({result.WinnerId})");
            await UniTask.Delay(3000, ignoreTimeScale: true);

            OnCombatEnded?.Invoke(result);

            Scene scene = SceneManager.GetSceneByName(CombatSceneName);
            if (scene.isLoaded)
                await SceneManager.UnloadSceneAsync(scene);

            CombatOutcome outcome = result.WinnerId == AttackerId
                ? CombatOutcome.AttackerWin
                : CombatOutcome.DefenderWin;

            UniTaskCompletionSource<CombatOutcome> resultSource = combatResult;
            ResetAssignment();
            resultSource?.TrySetResult(outcome);

            Debug.Log($"[CombatSystem] 대전 종료. 승자: {result.WinnerId}");
        }

        public void ApplyHealthFromNetwork(CombatHealthUpdatedMessage message)
        {
            if (message == null || message.CombatId != CombatId) return;
            if (message.PlayerId == LocalPlayerId) return;

            if (TryGetEntity(message.PlayerId, out CombatEntity entity))
                entity.ApplyHealthSnapshot(message.RemainingHp);
        }

        public void ReportLocalPosition(float x)
        {
            if (!IsCombatActive || IsTestCombat) return;

            RoomClient.Instance.ReportCombatPosition(CombatId, x);
        }

        public void ApplyPositionFromNetwork(CombatPositionMessage message)
        {
            if (message == null || message.CombatId != CombatId) return;
            if (message.PlayerId == LocalPlayerId) return;

            if (TryGetEntity(message.PlayerId, out CombatEntity entity))
                entity.GetComponent<Player.PlayerController>()?.SetNetworkPosition(message.X);
        }

        public bool TryApplyLocalHit(string targetId, int damage, string castId)
        {
            if (!IsCombatActive) return false;
            if (!IsTestCombat && targetId != LocalPlayerId) return false;

            string hitId = $"{castId}:{targetId}";
            if (!string.IsNullOrEmpty(castId) && !appliedHitIds.Add(hitId))
                return false;

            if (!TryGetEntity(targetId, out CombatEntity entity)) return false;
            if (entity.IsInvincible) return false;

            entity.TakeDamage(damage);

            if (!IsTestCombat && entity.CurrentHp > 0)
                RoomClient.Instance.ReportCombatHealth(CombatId, entity.CurrentHp);

            return true;
        }

        public void OnPlayerDied(string deadPlayerId)
        {
            if (!IsCombatActive) return;

            if (!IsTestCombat)
            {
                if (deadPlayerId == LocalPlayerId)
                    RoomClient.Instance.ReportCombatHealth(CombatId, 0);
                return;
            }

            string winnerId = deadPlayerId == AttackerId ? DefenderId : AttackerId;
            EndTestCombat(winnerId);
        }

        public void AddHurtBox(Collider2D collider, HurtBox hurtBox)
        {
            if (collider != null)
                hurtBoxes[collider] = hurtBox;
        }

        public void RemoveHurtBox(Collider2D collider)
        {
            if (collider != null)
                hurtBoxes.Remove(collider);
        }

        public HurtBox GetHurtBoxOrNull(Collider2D collider)
        {
            hurtBoxes.TryGetValue(collider, out HurtBox hurtBox);
            return hurtBox;
        }

        public void RegisterEntity(CombatEntity entity)
        {
            if (entity != null && !string.IsNullOrEmpty(entity.OwnerId))
                entities[entity.OwnerId] = entity;
        }

        public void UnregisterEntity(CombatEntity entity)
        {
            if (entity == null || string.IsNullOrEmpty(entity.OwnerId)) return;

            if (entities.TryGetValue(entity.OwnerId, out CombatEntity registered) && registered == entity)
                entities.Remove(entity.OwnerId);
        }

        public bool TryGetEntity(string playerId, out CombatEntity entity)
        {
            if (entities.TryGetValue(playerId, out entity) && entity != null)
                return true;

            entities.Remove(playerId);
            entity = null;
            return false;
        }

        public void AttachTimer(Timer timer)
        {
            if (timer == null) return;

            combatTimer = timer;
            combatTimer.duration = DurationSeconds;
        }

        public void AttachCombatUI(CombatUI ui)
        {
            combatUI = ui;
        }

        public async UniTask<bool> StartTestCombat(
            string testAttackerId,
            string testDefenderId,
            string testLocalPlayerId = null,
            int testStartHp = 50)
        {
            combatResult = new UniTaskCompletionSource<CombatOutcome>();
            CombatId = "TestCombat";
            AttackerId = testAttackerId;
            DefenderId = testDefenderId;
            LocalPlayerId = string.IsNullOrEmpty(testLocalPlayerId)
                ? testAttackerId
                : testLocalPlayerId;
            StartHp = testStartHp;
            DurationSeconds = combatTimer != null ? combatTimer.duration : 30f;
            IsTestCombat = true;

            StartCombatRuntime();

            CombatOutcome result = await combatResult.Task;
            Debug.Log($"[테스트 전투 종료] 공격자 승리 여부: {result == CombatOutcome.AttackerWin}");
            return result == CombatOutcome.AttackerWin;
        }

        private void StartCombatRuntime()
        {
            DamageMultiplier = 1f;
            elapsedTime = 0f;
            nextMultiplierIncreaseTime = multiplierIncreaseStartTime;
            appliedHitIds.Clear();
            BattleManager.Instance.ClearCombatState();
            IsCombatActive = true;
            StartCombatTimer();

            Debug.Log(
                $"[CombatSystem] 대전 시작: Left={AttackerId}, Right={DefenderId}, " +
                $"Me={LocalPlayerId}");
        }

        private void HandleTimeUp()
        {
            if (!IsTestCombat) return;

            TryGetEntity(AttackerId, out CombatEntity attacker);
            TryGetEntity(DefenderId, out CombatEntity defender);

            float attackerRatio = attacker != null ? (float)attacker.CurrentHp / attacker.MaxHp : 0f;
            float defenderRatio = defender != null ? (float)defender.CurrentHp / defender.MaxHp : 0f;
            string winnerId = attackerRatio > defenderRatio ? AttackerId : DefenderId;

            EndTestCombat(winnerId);
        }

        private void EndTestCombat(string winnerId)
        {
            if (!IsCombatActive) return;

            IsCombatActive = false;
            StopCombatTimer();

            CombatResultMessage result = new CombatResultMessage
            {
                CombatId = CombatId,
                WinnerId = winnerId,
                LoserId = winnerId == AttackerId ? DefenderId : AttackerId
            };

            OnCombatEnded?.Invoke(result);
            combatResult?.TrySetResult(
                winnerId == AttackerId ? CombatOutcome.AttackerWin : CombatOutcome.DefenderWin);
        }

        private void StartCombatTimer()
        {
            if (combatTimer == null) return;

            combatTimer.OnTimeUp -= HandleTimeUp;
            combatTimer.OnTimeUp += HandleTimeUp;
            combatTimer.StartTimer();
        }

        private void StopCombatTimer()
        {
            if (combatTimer == null) return;

            combatTimer.StopTimer();
            combatTimer.OnTimeUp -= HandleTimeUp;
        }

        private void ResetAssignment()
        {
            StopCombatTimer();
            FightUI.Instance?.Hide();
            BattleManager.Instance.ClearCombatState();

            IsCombatActive = false;
            IsTestCombat = false;
            CombatId = null;
            AttackerId = null;
            DefenderId = null;
            LocalPlayerId = null;
            DurationSeconds = 30f;
            requestId = null;
            combatResult = null;
            combatUI = null;
            isFinishing = false;
            appliedHitIds.Clear();
            hurtBoxes.Clear();
            entities.Clear();
        }

        private static string GetPlayerDisplayName(string playerId)
        {
            if (!RoomClient.IsInitialized || RoomClient.Instance.Me == null)
                return playerId;

            RoomClient client = RoomClient.Instance;

            if (client.Me.Id == playerId)
                return string.IsNullOrWhiteSpace(client.Me.Nickname)
                    ? playerId
                    : client.Me.Nickname;

            if (client.Others.TryGetValue(playerId, out User user) &&
                !string.IsNullOrWhiteSpace(user.Nickname))
            {
                return user.Nickname;
            }

            return playerId;
        }
    }
}
