using System;
using System.Collections.Generic;
using BlueMarble.Server;
using Cysharp.Threading.Tasks;
using Jay;
using UnityEngine;
using Utility;

namespace BlueMarble
{
    public class EconomyManager : SingletonBase<EconomyManager>
    {
        private readonly Dictionary<string, int> golds = new();
        private readonly HashSet<string> paymentProtectionUsedPlayerIds = new();

        public event Action OnUpdated;
        
        private RoomClient Client => RoomClient.Instance;

        private void Start()
        {
            Client.OnEconomyUpdated += HandleEconomyUpdated;
        }

        private void OnDisable()
        {
            Client.OnEconomyUpdated -= HandleEconomyUpdated;
        }

        private void HandleEconomyUpdated(UserEconomy[] players)
        {
            golds.Clear();

            for (int i = 0; i < players.Length; i++)
            {
                UserEconomy economy = players[i];
                golds[economy.UserId] = economy.Gold;
            }
            
            OnUpdated?.Invoke();
        }

        public bool TryGetGold(string playerId, out int gold) 
            => golds.TryGetValue(playerId, out gold);
        
        #region GAIN_OR_SPEND_GOLD

        private void UpdateGoldTo(string playerId, int amount)
        {
            Client.RequestUpdateEconomy(new EconomyUpdate
            {
                UserId = playerId,
                Amount = amount
            });
            
            Debug.Log($"[EconomyManager] Update {amount} gold to {playerId}");
        }
        
        private async UniTask UpdateGoldToAsync(string playerId, int amount)
        {
            if (TryGetGold(playerId, out int currentGold) == false)
                return;

            int expectedGold = currentGold + amount;
            UniTaskCompletionSource completionSource = new();

            OnUpdated += HandleUpdated;

            try
            {
                UpdateGoldTo(playerId, amount);
                await completionSource.Task;
            }
            finally
            {
                OnUpdated -= HandleUpdated;
            }

            void HandleUpdated()
            {
                if (TryGetGold(playerId, out int updatedGold) == false)
                    return;

                if (updatedGold != expectedGold)
                    return;

                completionSource.TrySetResult();
            }
        }

        public UniTask GainGoldTo(string playerId, int amount) => UpdateGoldToAsync(playerId, amount);
        public UniTask LoseGoldTo(string playerId, int amount) => UpdateGoldToAsync(playerId, -amount);

        public async UniTask<bool> PayMandatoryAsync(
            string payerId,
            int amount,
            string recipientId = null)
        {
            if (amount <= 0) return true;
            if (TryGetGold(payerId, out int currentGold) == false) return false;

            bool canUsePaymentProtection =
                currentGold < amount &&
                string.IsNullOrEmpty(recipientId) == false &&
                payerId == Client.Me.Id &&
                paymentProtectionUsedPlayerIds.Contains(payerId) == false &&
                GetTotalAssets(payerId, currentGold) >= amount;

            if (canUsePaymentProtection)
            {
                paymentProtectionUsedPlayerIds.Add(payerId);
                await GainGoldTo(recipientId, amount);
                await PresentPaymentProtectionAsync();
                return true;
            }

            if (currentGold < amount)
            {
                await TerritoryManager.Instance.LiquidateForDebtAsync(payerId, amount);

                if (TryGetGold(payerId, out currentGold) == false)
                    return false;
            }

            int paidAmount = Mathf.Min(currentGold, amount);
            if (paidAmount > 0)
            {
                await LoseGoldTo(payerId, paidAmount);

                if (string.IsNullOrEmpty(recipientId) == false)
                    await GainGoldTo(recipientId, paidAmount);
            }

            if (paidAmount >= amount) return true;

            await Client.RequestDeclareBankruptcy(payerId);
            return false;
        }

        private static long GetTotalAssets(string playerId, int currentGold)
        {
            long totalAssets = currentGold;
            List<Tile> territories = BlueMarbleWorld.Instance.Board.GetOccupiedTiles(playerId);

            for (int i = 0; i < territories.Count; i++)
                totalAssets += territories[i].GetTotalValue();

            return totalAssets;
        }

        private async UniTask PresentPaymentProtectionAsync()
        {
            Client.SetLocalPlayerActivity(PlayerActivityType.PaymentProtection);

            try
            {
                await Client.AnnouncePlayerActivity(PlayerActivityType.PaymentProtection);
                await CustomUniTask.DelaySeconds(1.5f);
            }
            finally
            {
                Client.SetLocalPlayerActivity(PlayerActivityType.None);
                await Client.AnnouncePlayerActivity(PlayerActivityType.None);
            }
        }

        #endregion


    }
}
