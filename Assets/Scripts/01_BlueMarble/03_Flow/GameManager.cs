using System;
using BlueMarble.Server;
using Combat;
using Cysharp.Threading.Tasks;
using Jay;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utility;

namespace BlueMarble
{
    public class GameManager : SingletonBase<GameManager>
    {
        [field: Title("References")]
        [field: SerializeField] public PlayerManager Players { get; private set; }
        [field: SerializeField] public TurnManager Turn { get; private set; }

        [Title("Round")]
        [SerializeField] private TMP_Text roundText;

        [Title("Winner")]
        [SerializeField] private GameObject winnerPanel;
        [SerializeField] private TMP_Text winnerText;
        [SerializeField] private TMP_Text winnerGoldText;

        private RoomClient Client => RoomClient.Instance;
        
        private BlueMarbleWorld World => BlueMarbleWorld.Instance;
        private PlayerManager Player => PlayerManager.Instance;

        private bool isFinishingGame;
        
        private async UniTaskVoid Start()
        {
            if (Client.Me == null) return;

            Players.Initialize(Client.PlayerIds, Client.Me.Id);

            Client.OnTurnStarted += HandleTurnStarted;
            Client.OnDiceRolled += HandleDiceRolled;
            Client.OnGameEnded += HandleGameEnded;
            Client.OnPlayerBankrupted += HandlePlayerBankrupted;
            Client.OnLeft += HandlePlayerLeft;

            Client.OnUserMovedTo += HandleUserMovedTo;
            Client.OnUserMoveCancelled += HandleUserMoveCancelled;
            
            Turn.OnDiceRollRequested += HandleDiceRollDiceRequested;

            // 도중에 연결이 끊겼다거나, 기타 등등의 이유로
            // 게임이 시작되었는데 다른 플레이어가 없을 경우
            if (Client.Others.Count <= 0)
            {
                HandleGameEnded(new GameEndedMessage
                {
                    WinnerId = Client.Me.Id
                });
                return;
            }

            await UniTask.WaitUntil(
                this,
                self => World.IsBoardReady,
                cancellationToken: destroyCancellationToken);

            await Client.AnnounceBoardReady();
        }

        private void OnDisable()
        {
            Players.CancelAllMoves();

            Client.OnTurnStarted -= HandleTurnStarted;
            Client.OnDiceRolled -= HandleDiceRolled;
            Client.OnGameEnded -= HandleGameEnded;
            Client.OnPlayerBankrupted -= HandlePlayerBankrupted;
            Client.OnLeft -= HandlePlayerLeft;

            Client.OnUserMovedTo -= HandleUserMovedTo;
            Client.OnUserMoveCancelled -= HandleUserMoveCancelled;
            
            Turn.OnDiceRollRequested -= HandleDiceRollDiceRequested;
        }

        private void HandleTurnStarted(User user, bool canAct)
        {
            Players.CancelAllMoves();
            TileEffectSyncManager.Instance.CancelAll();

            Debug.Log("[GameManager] " +
                $"Round {Client.RoundCount}, " +
                $"Turn {Client.TurnId}, " +
                $"Player {user.Nickname}({user.Id})");

            roundText.SetText($"{Client.RoundCount}/{Client.MaxRoundCount}");
            Players.SetCurrentTurn(user.Id);

            if (canAct == false)
            {
                Client.AnnounceTurnFinished(Client.TurnId);
                return;
            }
            
            bool isMyTurn = user.Id == Client.Me.Id;
            Turn.BeginTurn(isMyTurn);
        }

        private void HandleDiceRollDiceRequested()
        {
            User currentUser = Client.CurrentTurnUser;

            if (currentUser == null) return;
            if (currentUser.Id != Client.Me.Id) return;
            
            Client.RequestRollDice(Client.TurnId);
        }

        private void HandleDiceRolled(User user, int d1, int d2)
        {
            int turnId = Client.TurnId;
            ResolveTurnAsync(user.Id, d1, d2, turnId).Forget();
        }
        
        private void HandleGameEnded(GameEndedMessage message)
        {
            if (isFinishingGame) return;
            isFinishingGame = true;

            Players.CancelAllMoves();
            TileEffectSyncManager.Instance.CancelAll();

            FinishGameAsync(message).Forget();
        }

        private async UniTaskVoid FinishGameAsync(GameEndedMessage message)
        {
            Players.ResetTurnVisuals();
            Turn.SetInputEnabled(false);
            Debug.Log($"[GameManager] Game Ended.");

            CombatSystem combat = CombatSystem.Instance;
            if (combat.IsSceneLoading ||
                combat.HasCombatAssignment ||
                SceneManager.GetSceneByName("Combat").isLoaded)
            {
                await UniTask.WaitUntil(
                    this,
                    _ => combat.IsSceneLoading == false &&
                         combat.HasCombatAssignment == false &&
                         SceneManager.GetSceneByName("Combat").isLoaded == false,
                    cancellationToken: destroyCancellationToken);
            }

            if (World.IsBoardReady == false)
            {
                await UniTask.WaitUntil(
                    this,
                    _ => World.IsBoardReady,
                    cancellationToken: destroyCancellationToken);
            }

            string winnerId = string.IsNullOrEmpty(message.WinnerId)
                ? GetRoundWinnerId()
                : message.WinnerId;

            if (string.IsNullOrEmpty(winnerId) == false)
            {
                User winner = Client.GetUser(winnerId);
                string winnerNickname = winner.Nickname;
                int totalAssets = GetTotalAssets(winnerId);
                
                winnerPanel.SetActive(true);
                winnerText.SetText($"{winnerNickname}({winnerId}) 승리!!");
                winnerGoldText.SetText($"총 자산: {totalAssets:N0} 골드");

                await CustomUniTask.DelaySeconds(5f);
            }

            await Client.LeaveAsync();
        }

        private string GetRoundWinnerId()
        {
            string winnerId = null;
            int highestAssets = int.MinValue;

            for (int i = 0; i < Client.PlayerIds.Count; i++)
            {
                string playerId = Client.PlayerIds[i];
                int totalAssets = GetTotalAssets(playerId);

                if (totalAssets <= highestAssets) continue;

                winnerId = playerId;
                highestAssets = totalAssets;
            }

            return winnerId;
        }

        private int GetTotalAssets(string playerId)
        {
            int totalAssets = 0;

            if (EconomyManager.Instance.TryGetGold(playerId, out int gold))
                totalAssets += gold;

            var territories = World.Board.GetOccupiedTiles(playerId);
            for (int i = 0; i < territories.Count; i++)
                totalAssets += territories[i].GetTotalValue();

            return totalAssets;
        }

        private void HandlePlayerLeft(User user)
        {
            bool wasCurrentPlayer = Client.CurrentTurnUser?.Id == user.Id;

            Players.Remove(user.Id);

            if (wasCurrentPlayer)
            {
                Players.CancelAllMoves();
                TileEffectSyncManager.Instance.CancelAll();
            }
        }

        private void HandlePlayerBankrupted(User user)
        {
            Players.Remove(user.Id);

            if (user.Id == Client.Me.Id)
                Turn.SetInputEnabled(false);
        }

        private void HandleUserMovedTo(MoveUserToMessage message)
        {
            ResolveUserMovedToAsync(message).Forget();
        }

        private void HandleUserMoveCancelled(UserMoveCancelledMessage message)
        {
            Players.CancelMove(message.MoveId);
        }
        
        private async UniTaskVoid ResolveTurnAsync(string playerId, int d1, int d2, int turnId)
        {
            // 1. 주사위 결과 연출
            await Turn.ShowDiceResultAsync(d1, d2);
            
            // 2. 이동 및 타일 효과 처리
            bool completed = await Player.MoveBy(playerId, d1 + d2, turnId);
            if (completed == false) return;
            
            // 4. 현재 클라이언트의 턴이 끝났음을 서버에 보고
            await Client.AnnounceTurnFinished(turnId);
        }

        private async UniTaskVoid ResolveUserMovedToAsync(MoveUserToMessage message)
        {
            bool completed = await Players.MoveTo(
                message.UserId,
                message.TileId,
                message.TurnId,
                message.MoveId);

            if (completed == false) return;

            await Client.AnnounceUserMoveFinished(
                message.TurnId,
                message.MoveId);
        }
    }
}
