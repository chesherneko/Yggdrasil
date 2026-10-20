using System;
using System.Collections.Generic;
using BlueMarble.Server;
using Cysharp.Threading.Tasks;
using Jay;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BlueMarble
{
    public class ProfileManager : SingletonBase<ProfileManager>
    {
        [Title("Components")]
        [SerializeField] private Profile[] profiles;
        
        private RoomClient Client => RoomClient.Instance;
        private EconomyManager economy;
        private TerritoryManager territory;

        private void Start()
        {
            economy = EconomyManager.Instance;
            territory = TerritoryManager.Instance;

            InitProfiles();
            RefreshGoldState();

            economy.OnUpdated += RefreshGoldState;
            territory.OnUpdated += RefreshGoldState;
            Client.OnInventoryUpdated += HandleInventoryUpdated;
            Client.OnPlayerActivityChanged += HandlePlayerActivityChanged;
            Client.OnPlayerBankrupted += HandlePlayerBankrupted;
            Client.OnLeft += HandlePlayerLeft;

            RefreshEquipmentStates();
        }

        private void OnDisable()
        {
            if (economy != null)
                economy.OnUpdated -= RefreshGoldState;

            if (territory != null)
                territory.OnUpdated -= RefreshGoldState;

            Client.OnInventoryUpdated -= HandleInventoryUpdated;
            Client.OnPlayerActivityChanged -= HandlePlayerActivityChanged;
            Client.OnPlayerBankrupted -= HandlePlayerBankrupted;
            Client.OnLeft -= HandlePlayerLeft;
        }

        private void InitProfiles()
        {
            int idx = 0;
            
            // 내 프로필은 무조건 0번 요소에 초기화
            InitProfile(profiles[idx++], Client.Me);
            
            foreach (User other in Client.Others.Values)
                InitProfile(profiles[idx++], other);

            void InitProfile(Profile profile, User user)
            {
                profile.Initialize(user);
                profile.gameObject.SetActive(true);
            }
        }

        private async void RefreshGoldState()
        {
            for (int i = 0; i < profiles.Length; i++)
            {
                Profile profile = profiles[i];
                
                if (profile.gameObject.activeSelf == false) continue;
                if (economy.TryGetGold(profile.Id, out int gold) == false) continue;
                
                profile.SetGold(gold);

                int territoryValue = await GetTerritoryValue(profile.Id);
                profile.SetTotalGold(gold + territoryValue);
            }
        }
        
        private async UniTask<int> GetTerritoryValue(string playerId)
        {
            BlueMarbleWorld world = BlueMarbleWorld.Instance;

            await UniTask.WaitUntil(world, _ => world.IsBoardReady);
            
            Board board = world.Board;
            List<Tile> territories = board.GetOccupiedTiles(playerId);

            if (territories.Count == 0) return 0;

            int total = 0;
            
            for (int i = 0; i < territories.Count; i++)
                total += territories[i].GetTotalValue();
            
            return total;
        }

        private void RefreshEquipmentStates()
        {
            for (int i = 0; i < profiles.Length; i++)
            {
                Profile profile = profiles[i];
                if (profile.gameObject.activeSelf == false) continue;

                profile.SetEquipmentIcons(Client.GetEquipmentStates(profile.Id));
            }
        }

        private void HandleInventoryUpdated(
            string playerId,
            IReadOnlyList<EquipmentSlotState> equipments)
        {
            for (int i = 0; i < profiles.Length; i++)
            {
                Profile profile = profiles[i];
                if (profile.gameObject.activeSelf == false) continue;
                if (profile.Id != playerId) continue;

                profile.SetEquipmentIcons(equipments);
                return;
            }
        }

        private void HandlePlayerActivityChanged(string playerId, PlayerActivityType activity)
        {
            for (int i = 0; i < profiles.Length; i++)
            {
                Profile profile = profiles[i];

                if (profile.gameObject.activeSelf == false) continue;
                if (profile.Id != playerId) continue;

                if (activity == PlayerActivityType.None)
                    profile.HideBubble();
                else if (activity == PlayerActivityType.Shop)
                    profile.ShowBubble("상점 둘러보는중...");
                else if (activity == PlayerActivityType.TerritoryPurchase)
                    profile.ShowBubble("영토 구매 검토중...");
                else if (activity == PlayerActivityType.Treasure)
                    profile.ShowBubble("보물 찾는중...");
                else if (activity == PlayerActivityType.Combat)
                    profile.ShowBubble("결투 준비중...");
                else if (activity == PlayerActivityType.PaymentProtection)
                    profile.ShowBubble("까방권 발동!");

                return;
            }
        }

        private void HandlePlayerLeft(User user)
        {
            for (int i = 0; i < profiles.Length; i++)
            {
                Profile profile = profiles[i];
                if (profile.gameObject.activeSelf == false) continue;
                if (profile.Id != user.Id) continue;

                profile.gameObject.SetActive(false);
                return;
            }
        }

        private void HandlePlayerBankrupted(User user)
        {
            for (int i = 0; i < profiles.Length; i++)
            {
                Profile profile = profiles[i];
                if (profile.gameObject.activeSelf == false) continue;
                if (profile.Id != user.Id) continue;

                profile.SetBankrupt();
                return;
            }
        }
    }
}
