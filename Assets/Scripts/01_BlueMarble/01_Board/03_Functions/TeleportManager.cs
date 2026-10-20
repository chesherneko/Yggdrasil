using System;
using System.Net;
using BlueMarble.Server;
using Cysharp.Threading.Tasks;
using Jay;
using Sirenix.OdinInspector;
using Study.Utilities;
using UnityEngine;

namespace BlueMarble
{
    public class TeleportManager : SingletonBase<TeleportManager>
    {
        [Title("Components")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private RectTransform travelMask;

        private RoomClient Client => RoomClient.Instance;
        private BlueMarbleWorld World => BlueMarbleWorld.Instance;
        
        public async UniTask MoveToSelected(string playerId)
        {
            Board board = World.Board;

            if (PlayerRegistry.TryGetPlayer(playerId, out Player player) == false)
            {
                Debug.Log($"[TeleportManager] Cannot find player: {playerId}");
                return;
            }
            
            travelMask.gameObject.SetActive(true);

            try
            {
                while (true)
                {
                    await UniTask.Yield(
                        PlayerLoopTiming.Update,
                        cancellationToken: destroyCancellationToken);

                    if (PlayerRegistry.IsRegistered(playerId) == false) return;
                    if (SimpleInput.GetMouseButtonDown(0) == false) continue;

                    Vector2 mousePos = SimpleInput.GetMousePosition();
                    Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
                    worldPos.z = 0;

                    if (board.TryGetTile(worldPos, out Tile tile) == false) continue;
                    if (board.GetTile(player.Position) == tile) continue;

                    travelMask.gameObject.SetActive(false);
                    if (await MoveToAsync(playerId, tile)) break;
                    travelMask.gameObject.SetActive(true);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                travelMask.gameObject.SetActive(false);
            }
        }
        
        public async UniTask<bool> MoveToAsync(string playerId, int tileIdx)
        {
            return 
                World.Board.TryGetTile(tileIdx, out Tile tile) && 
                await MoveToAsync(playerId, tile);
        }
        
        public async UniTask<bool> MoveToAsync(string playerId, Tile tile)
        {
            if (PlayerRegistry.IsRegistered(playerId) == false)
            {
                Debug.Log($"[TeleportManager] Cannot find player: {playerId}");
                return false;
            }
            
            Board board = World.Board;
                
            int tileIdx = board.GetTileIndex(tile);

            string requestId = Guid.NewGuid().ToString("N");
            UniTaskCompletionSource<bool> completionSource = new();

            Client.OnUserMoveFinished += HandleMoveFinished;
            Client.OnUserMoveCancelled += HandleMoveCancelled;
            Client.OnLeft += HandlePlayerLeft;

            try
            {
                await Client.RequestMovePlayerTo(playerId, tileIdx, Client.TurnId, requestId);
                return await completionSource.Task.AttachExternalCancellation(
                    destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            finally
            {
                Client.OnUserMoveFinished -= HandleMoveFinished;
                Client.OnUserMoveCancelled -= HandleMoveCancelled;
                Client.OnLeft -= HandlePlayerLeft;
            }
                
            void HandleMoveFinished(UserMoveFinishedMessage msg)
            {
                if (msg.RequestId != requestId) return;
                completionSource.TrySetResult(true);
            }

            void HandleMoveCancelled(UserMoveCancelledMessage msg)
            {
                if (msg.RequestId != requestId) return;
                completionSource.TrySetResult(false);
            }

            void HandlePlayerLeft(User user)
            {
                if (user.Id != playerId) return;
                completionSource.TrySetResult(false);
            }
        }
    }
}
