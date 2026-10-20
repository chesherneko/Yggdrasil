using System;
using System.Collections.Generic;
using System.Linq;
using BlueMarble.Server;
using Cysharp.Threading.Tasks;
using Jay;
using Sirenix.OdinInspector;
using UnityEngine;
using Utility;

namespace BlueMarble
{
    public class GoldCardManager : SingletonBase<GoldCardManager>
    {
        [Title("Components")]
        [SerializeField] private GoldCard card;
        
        private readonly Dictionary<GoldCardScopeType, IGoldCardScope> scopes = new();
        private readonly Dictionary<GoldCardPatternType, IGoldCardPattern> patterns = new();

        private UniTaskCompletionSource<GoldCardDef> drawWaiter;
        private UniTaskCompletionSource presentationWaiter;

        private string activePlayerId;
        private int activeTurnId = -1;
        
        private DataManager Data => DataManager.Instance;
        private RoomClient Client => RoomClient.Instance;

        #region INITIALIZER
        
        protected override void OnInitialize()
        {
            PopulateByEnum(scopes, GoldCardScopeFactory.Create);
            PopulateByEnum(patterns, GoldCardPatternFactory.Create);

            Client.OnGoldCardDrawn += HandleGoldCardDrawn;
            Client.OnGoldCardRevealed += HandleGoldCardRevealed;
            Client.OnGoldCardPresentationFinished += HandleGoldCardPresentationFinished;
            Client.OnLeft += HandlePlayerLeft;

            card.Clicked += HandleCardClicked;
            card.gameObject.SetActive(false);
        }

        protected override void OnDispose()
        {
            Client.OnGoldCardDrawn -= HandleGoldCardDrawn;
            Client.OnGoldCardRevealed -= HandleGoldCardRevealed;
            Client.OnGoldCardPresentationFinished -= HandleGoldCardPresentationFinished;
            Client.OnLeft -= HandlePlayerLeft;

            card.Clicked -= HandleCardClicked;
        }
        
        private static void PopulateByEnum<TEnum, TValue>(
            Dictionary<TEnum, TValue> dict, Func<TEnum, TValue> factory) 
            where TEnum : struct, Enum
        {
            TEnum[] values = (TEnum[])Enum.GetValues(typeof(TEnum));

            for (int i = 0; i < values.Length; i++)
            {
                TEnum value = values[i];
                dict[value] = factory(value);
            }
        }
        
        #endregion
        
        public async UniTask DrawAndUseAsync(string playerId)
        {
            await Data.EnsureDataLoaded();

            GoldCardDef drawn = await WaitUntilCardDrawnAsync(playerId);
            await WaitUntilCardAnimationFinishedAsync();

            await ApplyCardEffectAsync(playerId, drawn);
        }

        private async UniTask<GoldCardDef> WaitUntilCardDrawnAsync(string playerId)
        {
            drawWaiter = new UniTaskCompletionSource<GoldCardDef>();

            GoldCardDef drawn;

            try
            {
                int[] cardIds = Data.GoldCards.GetIds().ToArray();
                Client.RequestDrawGoldCard(Client.TurnId, cardIds);

                drawn = await drawWaiter.Task;
            }
            finally
            {
                drawWaiter = null;
            }

            Debug.Log($"[GoldCardManager] {playerId} drew Gold Card " +
                $"{drawn.Id}: {drawn.Description}");

            return drawn;
        }

        private UniTask WaitUntilCardAnimationFinishedAsync()
        {
            return presentationWaiter.Task;
        }

        private void HandleGoldCardDrawn(User player, int cardId)
        {
            GoldCardDef drawn = Data.GetGoldCardDef(cardId);

            activePlayerId = player.Id;
            activeTurnId = Client.TurnId;
            presentationWaiter = new UniTaskCompletionSource();

            card.gameObject.SetActive(true);
            card.Initialize(drawn);
            card.SetClickable(player.Id == Client.Me.Id);

            if (player.Id == Client.Me.Id)
                drawWaiter?.TrySetResult(drawn);
        }

        private void HandleCardClicked()
        {
            if (activePlayerId != Client.Me.Id) return;

            Client.RequestRevealGoldCard(activeTurnId).Forget();
        }

        private void HandleGoldCardRevealed(User player)
        {
            if (player.Id != activePlayerId) return;

            card.SetClickable(false);
            PlayPresentationAsync(activeTurnId).Forget();
        }

        private async UniTaskVoid PlayPresentationAsync(int turnId)
        {
            try
            {
                await card.FlipAsync();
                await CustomUniTask.DelaySeconds(2f);
            }
            finally
            {
                card.gameObject.SetActive(false);
                await Client.AnnounceGoldCardPresentationFinished(turnId);
            }
        }

        private void HandleGoldCardPresentationFinished(int turnId)
        {
            if (turnId != activeTurnId) return;

            card.gameObject.SetActive(false);
            activePlayerId = null;
            activeTurnId = -1;

            UniTaskCompletionSource waiter = presentationWaiter;
            presentationWaiter = null;

            waiter?.TrySetResult();
        }

        private void HandlePlayerLeft(User user)
        {
            if (user.Id != activePlayerId) return;

            HandleGoldCardPresentationFinished(activeTurnId);
        }

        private UniTask ApplyCardEffectAsync(string playerId, GoldCardDef drawn)
        {
            IGoldCardScope scope = scopes[drawn.ScopeType];
            string[] targetIds = scope.GetTargetIds(playerId);

            IGoldCardPattern pattern = patterns[drawn.PatternType];

            return pattern.Apply(
                targetIds,
                drawn.GoldAmount,
                drawn.DropTableId,
                drawn.TileId);
        }
    }
}
