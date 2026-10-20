using System.Collections.Generic;
using System.Threading;
using BlueMarble.Server;
using Cysharp.Threading.Tasks;
using Jay;

namespace BlueMarble
{
    public class TileEffectSyncManager : SingletonBase<TileEffectSyncManager>
    {
        private readonly Dictionary<string, UniTaskCompletionSource> waiters = new();
        private readonly HashSet<string> completed = new();

        private RoomClient Client => RoomClient.Instance;
        
        private void OnEnable()
        {
            Client.OnTileEffectSync += HandleSync;
        }

        private void OnDisable()
        {
            Client.OnTileEffectSync -= HandleSync;
            CancelAll();
        }

        public void CancelAll()
        {
            foreach (UniTaskCompletionSource source in waiters.Values)
                source.TrySetResult();

            waiters.Clear();
            completed.Clear();
        }

        public async UniTask ExecuteAsync(string playerId, Tile tile, 
            int turnId, long moveId, int stepIndex, bool isOwner,
            CancellationToken cancellationToken)
        {
            TileEffectSyncMessage readyMessage = new TileEffectSyncMessage
            {
                TurnId = turnId,
                MoveId = moveId,
                StepIndex = stepIndex,
                Phase = TileEffectSyncPhase.Ready
            };

            await AnnounceAndWaitAsync(readyMessage, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            TileEffectSyncMessage resolvedMessage = new TileEffectSyncMessage
            {
                TurnId = turnId,
                MoveId = moveId,
                StepIndex = stepIndex,
                Phase = TileEffectSyncPhase.Resolved
            };

            if (isOwner)
            {
                await tile.ExecuteEffect(playerId);
                cancellationToken.ThrowIfCancellationRequested();
                await AnnounceAndWaitAsync(resolvedMessage, cancellationToken);
            }
            else await WaitAsync(resolvedMessage, cancellationToken);
        }

        private async UniTask WaitAsync(
            TileEffectSyncMessage msg,
            CancellationToken cancellationToken)
        {
            string key = CreateKey(msg);

            // 서버 응답이 빨랐을 경우
            if (completed.Remove(key))
                return;

            if (waiters.TryGetValue(key, out UniTaskCompletionSource source) == false)
            {
                source = new UniTaskCompletionSource();
                waiters.Add(key, source);
            }

            try
            {
                await source.Task.AttachExternalCancellation(cancellationToken);
            }
            finally
            {
                if (cancellationToken.IsCancellationRequested &&
                    waiters.TryGetValue(key, out UniTaskCompletionSource current) &&
                    ReferenceEquals(current, source))
                {
                    waiters.Remove(key);
                    completed.Remove(key);
                }
            }
        }

        private async UniTask AnnounceAndWaitAsync(
            TileEffectSyncMessage msg,
            CancellationToken cancellationToken)
        {
            UniTask waitTask = WaitAsync(msg, cancellationToken);

            await Client.AnnounceTileEffectSync(msg);
            await waitTask;
        }

        private void HandleSync(TileEffectSyncMessage msg)
        {
            string key = CreateKey(msg);

            if (waiters.Remove(key, out UniTaskCompletionSource source))
            {
                source.TrySetResult();
                return;
            }
            
            completed.Add(key);
        }

        private static string CreateKey(TileEffectSyncMessage msg)
            => $"{msg.TurnId}:{msg.MoveId}:{msg.StepIndex}:{msg.Phase}";
    }
}
