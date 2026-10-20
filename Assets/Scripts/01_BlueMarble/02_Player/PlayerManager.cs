using System.Collections.Generic;
using System;
using System.Threading;
using BlueMarble.Server;
using Cysharp.Threading.Tasks;
using Jay;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BlueMarble
{
    public class PlayerManager : SingletonBase<PlayerManager>
    {
        private class ActiveMove
        {
            public string PlayerId;
            public CancellationTokenSource Cancellation;
        }

        [Title("Components")]
        [SerializeField] private Player[] players;

        private readonly Dictionary<long, ActiveMove> activeMoves = new();

        private RoomClient Client => RoomClient.Instance;
        private TileEffectSyncManager Sync => TileEffectSyncManager.Instance;
        
        public void Initialize(IReadOnlyList<string> ids, string myId)
        {
            if (ids.Count > players.Length)
            {
                Debug.LogError("[PlayerManager] Player count is greater than dummy count.");
                return;
            }

            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                User user = Client.GetUser(id);
                Player player = players[i];
                
                player.Initialize(id, user.PlayerNumber);
                player.gameObject.SetActive(true);
            }
        }

        public void SetCurrentTurn(string currentPlayerId)
        {
            for (int i = 0; i < players.Length; i++)
            {
                Player player = players[i];

                if (player.gameObject.activeSelf == false) continue;

                player.SetTurnActive(player.Id == currentPlayerId);
            }
        }

        public void ResetTurnVisuals()
        {
            for (int i = 0; i < players.Length; i++)
            {
                Player player = players[i];

                if (player.gameObject.activeSelf == false) continue;

                player.ResetTurnVisual();
            }
        }

        public void Remove(string playerId)
        {
            CancelMoves(playerId);

            for (int i = 0; i < players.Length; i++)
            {
                Player player = players[i];
                if (player.gameObject.activeSelf == false) continue;
                if (player.Id != playerId) continue;

                player.gameObject.SetActive(false);
                return;
            }
        }

        public async UniTask<bool> MoveBy(string playerId, int steps, int turnId)
        {
            if (TryGetPlayer(playerId, out Player player) == false) return false;

            const long moveId = 0;
            ActiveMove move = BeginMove(moveId, playerId);

            try
            {
                bool isOwner = Client.Me.Id == playerId;

                for (int i = 0; i < steps; i++)
                {
                    move.Cancellation.Token.ThrowIfCancellationRequested();
                    if (player.gameObject.activeSelf == false) return false;

                    Tile reached = await player.MoveNext(move.Cancellation.Token);
                    move.Cancellation.Token.ThrowIfCancellationRequested();

                    bool isActiveEffect =
                        i == steps - 1 ||                // 도착 타일이거나
                        reached.SpaceDef.IsActiveOnPass; // 통과 시에도 효과를 발동해야하거나

                    if (isActiveEffect == false) continue;

                    await Sync.ExecuteAsync(
                        playerId, reached, turnId,
                        moveId, stepIndex: i, isOwner,
                        move.Cancellation.Token);
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            finally
            {
                EndMove(moveId, move);
            }
        }

        public async UniTask<bool> MoveTo(string playerId, int tileIdx, int turnId, long moveId)
        {
            if (TryGetPlayer(playerId, out Player player) == false) return false;

            ActiveMove move = BeginMove(moveId, playerId);

            try
            {
                bool isMe = Client.Me.Id == playerId;
                Board board = BlueMarbleWorld.Instance.Board;
                int stepIdx = 0;

                while (true)
                {
                    move.Cancellation.Token.ThrowIfCancellationRequested();
                    if (player.gameObject.activeSelf == false) return false;

                    Tile reached = await player.MoveNext(move.Cancellation.Token);
                    move.Cancellation.Token.ThrowIfCancellationRequested();

                    bool isLanding = board.GetTileIndex(reached) == tileIdx;
                    bool isActiveEffect = isLanding || reached.SpaceDef.IsActiveOnPass;

                    if (isActiveEffect)
                    {
                        await Sync.ExecuteAsync(
                            playerId, reached, turnId,
                            moveId, stepIdx, isMe,
                            move.Cancellation.Token);
                    }

                    if (isLanding) return true;

                    stepIdx++;
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            finally
            {
                EndMove(moveId, move);
            }
        }

        public void CancelMove(long moveId)
        {
            if (activeMoves.Remove(moveId, out ActiveMove move))
                move.Cancellation.Cancel();
        }

        public void CancelMoves(string playerId)
        {
            List<long> moveIds = new();

            foreach ((long moveId, ActiveMove move) in activeMoves)
            {
                if (move.PlayerId == playerId)
                    moveIds.Add(moveId);
            }

            for (int i = 0; i < moveIds.Count; i++)
                CancelMove(moveIds[i]);
        }

        public void CancelAllMoves()
        {
            List<long> moveIds = new(activeMoves.Keys);

            for (int i = 0; i < moveIds.Count; i++)
                CancelMove(moveIds[i]);
        }

        private ActiveMove BeginMove(long moveId, string playerId)
        {
            CancelMove(moveId);

            ActiveMove move = new ActiveMove
            {
                PlayerId = playerId,
                Cancellation = new CancellationTokenSource()
            };

            activeMoves.Add(moveId, move);
            return move;
        }

        private void EndMove(long moveId, ActiveMove move)
        {
            if (activeMoves.TryGetValue(moveId, out ActiveMove current) &&
                ReferenceEquals(current, move))
            {
                activeMoves.Remove(moveId);
            }

            move.Cancellation.Dispose();
        }

        private bool TryGetPlayer(string playerId, out Player result)
        {
            if (PlayerRegistry.TryGetPlayer(playerId, out result)) return true;
            
            Debug.LogError($"[TurnManager] Player not found: {playerId}");
            return false;
        }
    }
}
