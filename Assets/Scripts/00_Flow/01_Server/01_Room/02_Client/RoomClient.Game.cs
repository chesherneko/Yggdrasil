using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BlueMarble.Server
{
    public partial class RoomClient
    {
        [field: Title("Game States")]
        [field: ShowInInspector, ReadOnly] private string[] playerIds;

        [field: ShowInInspector, ReadOnly]
        private readonly Dictionary<string, EquipmentSlotState[]> inventories = new();

        [field: PropertySpace]
        [field: ShowInInspector, ReadOnly] public int RoundCount { get; private set; }
        [field: ShowInInspector, ReadOnly] public int MaxRoundCount { get; private set; }
        [field: ShowInInspector, ReadOnly] public int TurnId { get; private set; }
        [field: ShowInInspector, ReadOnly] public User CurrentTurnUser { get; private set; }
        [field: ShowInInspector, ReadOnly] public bool IsSpectating { get; private set; }

        public event Action OnGameStarted;

        public event Action<User, bool> OnTurnStarted;
        
        public event Action<User, int, int> OnDiceRolled;
        public event Action<UserEconomy[]> OnEconomyUpdated;
        public event Action<string, IReadOnlyList<EquipmentSlotState>> OnInventoryUpdated;
        public event Action<UpdateTerritoryMessage> OnTerritoryUpdated;
        
        public event Action<TileEffectSyncMessage> OnTileEffectSync;
        public event Action<User, int> OnGoldCardDrawn;
        public event Action<User> OnGoldCardRevealed;
        public event Action<int> OnGoldCardPresentationFinished;
        
        public event Action<MoveUserToMessage> OnUserMovedTo;
        public event Action<UserMoveFinishedMessage> OnUserMoveFinished;
        public event Action<UserMoveCancelledMessage> OnUserMoveCancelled;
        public event Action<string, PlayerActivityType> OnPlayerActivityChanged;
        public event Action<User> OnPlayerBankrupted;
        
        public event Action<GameEndedMessage> OnGameEnded;
        
        public IReadOnlyList<string> PlayerIds => playerIds;

        public IReadOnlyList<EquipmentSlotState> GetEquipmentStates(string userId)
        {
            return inventories.TryGetValue(userId, out EquipmentSlotState[] equipments)
                ? equipments
                : Array.Empty<EquipmentSlotState>();
        }

        private void RegisterGameHandlers()
        {
            RegisterCombatHandlers();
            RegisterHandler<GameStartedMessage>(ProtocolHeader.GAME_STARTED, HandleGameStarted);
            RegisterHandler<TurnStartedMessage>(ProtocolHeader.TURN_STARTED, HandleTurnStarted);
            RegisterHandler<DiceRolledMessage>(ProtocolHeader.DICE_ROLLED, HandleDiceRolled);
            RegisterHandler<TileEffectSyncMessage>(ProtocolHeader.TILE_EFFECT_SYNC, HandleTileEffectSync);
            
            RegisterHandler<GoldCardDrawnMessage>(ProtocolHeader.GOLD_CARD_DRAWN, HandleGoldCardDrawn);
            RegisterHandler<RevealGoldCardMessage>(ProtocolHeader.REVEAL_GOLD_CARD, HandleGoldCardRevealed);
            RegisterHandler<GoldCardPresentationFinishedMessage>(
                ProtocolHeader.GOLD_CARD_PRESENTATION_FINISHED,
                HandleGoldCardPresentationFinished);
            RegisterHandler<PlayerBankruptedMessage>(
                ProtocolHeader.PLAYER_BANKRUPTED,
                HandlePlayerBankrupted);
            RegisterHandler<GameEndedMessage>(ProtocolHeader.GAME_ENDED, HandleGameEnded);

            RegisterHandler<EconomyUpdatedMessage>(ProtocolHeader.ECONOMY_UPDATED, HandleEconomyUpdated);
            RegisterHandler<InventoryUpdatedMessage>(ProtocolHeader.INVENTORY_UPDATED, HandleInventoryUpdated);

            RegisterHandler<MoveUserToMessage>(ProtocolHeader.MOVE_USER_TO, HandleUserMovedTo);
            RegisterHandler<UserMoveFinishedMessage>(ProtocolHeader.USER_MOVE_FINISHED, HandleUserMoveFinished);
            RegisterHandler<UserMoveCancelledMessage>(ProtocolHeader.USER_MOVE_CANCELLED, HandleUserMoveCancelled);

            RegisterHandler<UpdateTerritoryMessage>(ProtocolHeader.UPDATE_TERRITORY, HandleTerritoryUpdated);
            RegisterHandler<ApplyTreasureMessage>(ProtocolHeader.APPLY_TREASURE, HandleApplyTreasure);
            RegisterHandler<TreasureResolvedMessage>(ProtocolHeader.TREASURE_RESOLVED, HandleTreasureResolved);
            RegisterHandler<PlayerActivityMessage>(ProtocolHeader.PLAYER_ACTIVITY, HandlePlayerActivity);
        }
        
        #region SEND_TO_SERVER

        public UniTask AnnounceBoardReady()
        {
            return SendAsync(new SetBoardReadyMessage());
        }

        public UniTask AnnounceTileEffectSync(TileEffectSyncMessage msg) => SendAsync(msg);

        public UniTask AnnounceTurnFinished(int turnId)
        {
            return SendAsync(new TurnFinishedMessage { TurnId = turnId });
        }

        public UniTask AnnounceUserMoveFinished(int turnId, long moveId)
        {
            return SendAsync(new UserMoveFinishedMessage
            {
                TurnId = turnId,
                MoveId = moveId
            });
        }

        public void RequestRollDice(int turnId)
        {
            SendAsync(new RollDiceMessage { TurnId = turnId }).Forget();
        }

        public void RequestDrawGoldCard(int turnId, int[] cardIds)
        {
            SendAsync(new DrawGoldCardMessage
            {
                TurnId = turnId,
                CardIds = cardIds
            }).Forget();
        }

        public UniTask RequestRevealGoldCard(int turnId)
        {
            return SendAsync(new RevealGoldCardMessage
            {
                TurnId = turnId
            });
        }

        public UniTask AnnounceGoldCardPresentationFinished(int turnId)
        {
            return SendAsync(new GoldCardPresentationFinishedMessage
            {
                TurnId = turnId
            });
        }

        public void RequestUpdateEconomy(params EconomyUpdate[] updates)
        {
            SendAsync(new UpdateEconomyMessage { Updates = updates }).Forget();
        }

        public UniTask RequestDeclareBankruptcy(string playerId)
        {
            return SendAsync(new DeclareBankruptcyMessage
            {
                PlayerId = playerId
            });
        }

        public UniTask RequestUpdateTerritory(string ownerId, int tileId, bool hasBuilding, bool hasLandMark)
        {
            return SendAsync(new UpdateTerritoryMessage
            {
                TileId = tileId, 
                OwnerId = ownerId, 
                HasBuilding = hasBuilding, 
                HasLandMark = hasLandMark
            });
        }

        public UniTask RequestAddIncapacitationCount(int count)
        {
            return SendAsync(new AddIncapacitationCountMessage { Count = count });
        }

        public void RequestSetEquipment(Equipment equipment)
        {
            SendAsync(new SetEquipmentMessage
            {
                InstanceId = equipment.InstanceId,
                EquipmentId = equipment.Def.Id,
                SlotIndex = equipment.SlotIndex
            }).Forget();
        }

        public void RequestRemoveEquipment(Equipment equipment)
        {
            SendAsync(new RemoveEquipmentMessage
            {
                InstanceId = equipment.InstanceId,
                SlotIndex = equipment.SlotIndex
            }).Forget();
        }

        public UniTask RequestMovePlayerTo(string playerId, int tileId, int turnId, string requestId)
        {
            return SendAsync(new MoveUserToMessage
            {
                UserId = playerId, 
                TileId = tileId, 
                TurnId = turnId, 
                RequestId = requestId
            });
        }

        public UniTask RequestApplyTreasure(
            string requestId,
            string targetId,
            GoldCardTreasureEffectType effectType,
            int equipmentId)
        {
            return SendAsync(new ApplyTreasureMessage
            {
                RequestId = requestId,
                TargetId = targetId,
                EffectType = effectType,
                EquipmentId = equipmentId
            });
        }

        public UniTask AnnounceTreasureResolved(string requestId)
        {
            return SendAsync(new TreasureResolvedMessage
            {
                RequestId = requestId
            });
        }

        public UniTask AnnouncePlayerActivity(PlayerActivityType activity)
        {
            return SendAsync(new PlayerActivityMessage
            {
                Activity = activity
            });
        }

        public void SetLocalPlayerActivity(PlayerActivityType activity)
        {
            OnPlayerActivityChanged?.Invoke(Me.Id, activity);
        }
        
        #endregion
        
        #region HANDLE_RECEIVED_MESSAGE
        
        private void HandleGameStarted(GameStartedMessage message)
        {
            playerIds = message.MemberIds;

            foreach (Equipment equipment in InventoryManager.Instance.GetAll())
                RequestSetEquipment(equipment);
            
            OnGameStarted?.Invoke();
            Debug.Log($"[RoomClient] Game started. Players: {playerIds.Length}");
        }

        private void HandleTurnStarted(TurnStartedMessage message)
        {
            User user = GetUser(message.PlayerId);
            
            RoundCount = message.RoundCount;
            MaxRoundCount = message.MaxRoundCount;
            TurnId = message.TurnId;
            
            CurrentTurnUser = user;
            
            OnTurnStarted?.Invoke(user, message.CanAct);
            Debug.Log($"[RoomClient] {user.Nickname}({user.Id}) turn started. Round: {RoundCount}, Turn Id: {TurnId}");
        }

        private void HandleDiceRolled(DiceRolledMessage message)
        {
            if (message.TurnId != TurnId) return;
            
            User user = GetUser(message.PlayerId);
            
            OnDiceRolled?.Invoke(user, message.D1, message.D2);
            Debug.Log($"[RoomClient] {user.Nickname}({user.Id}) rolled dice: {message.D1}, {message.D2}. " +
                      $"Turn Id: {message.TurnId}");
        }

        private void HandleTileEffectSync(TileEffectSyncMessage message)
        {
            if (message.TurnId != TurnId) return;

            OnTileEffectSync?.Invoke(message);
            Debug.Log("[RoomClient] Tile effect sync. " +
                $"Turn: {message.TurnId}, " +
                $"Move: {message.MoveId}, " +
                $"Step: {message.StepIndex}, " +
                $"Phase: {message.Phase}");
        }

        private void HandleGoldCardDrawn(GoldCardDrawnMessage message)
        {
            if (message.TurnId != TurnId) return;
            
            User user = GetUser(message.PlayerId);
            
            OnGoldCardDrawn?.Invoke(user, message.CardId);
            Debug.Log($"[RoomClient] {user.Nickname}({user.Id}) drew gold card: {message.CardId}. " +
                      $"Turn Id: {message.TurnId}");
        }

        private void HandleGoldCardRevealed(RevealGoldCardMessage message)
        {
            if (message.TurnId != TurnId) return;

            User user = GetUser(message.PlayerId);
            OnGoldCardRevealed?.Invoke(user);
        }

        private void HandleGoldCardPresentationFinished(GoldCardPresentationFinishedMessage message)
        {
            if (message.TurnId != TurnId) return;

            OnGoldCardPresentationFinished?.Invoke(message.TurnId);
        }

        private void HandleGameEnded(GameEndedMessage message)
        {
            CurrentTurnUser = null;
            
            OnGameEnded?.Invoke(message);
            Debug.Log("[RoomClient] Game ended");
        }

        private void HandlePlayerBankrupted(PlayerBankruptedMessage message)
        {
            User user = GetUser(message.Id);

            playerIds = Array.FindAll(playerIds, id => id != message.Id);

            if (message.Id == Me.Id)
                IsSpectating = true;

            OnPlayerBankrupted?.Invoke(user);

            if (CurrentTurnUser?.Id == message.Id)
                CurrentTurnUser = null;

            Debug.Log($"[RoomClient] {user.Nickname}({user.Id}) went bankrupt.");
        }

        private void HandleEconomyUpdated(EconomyUpdatedMessage message)
        {
            OnEconomyUpdated?.Invoke(message.Economies);
            Debug.Log("[RoomClient] Economy updated");
        }

        private void HandleInventoryUpdated(InventoryUpdatedMessage message)
        {
            EquipmentSlotState[] equipments = message.Equipments ?? Array.Empty<EquipmentSlotState>();
            inventories[message.UserId] = equipments;

            OnInventoryUpdated?.Invoke(message.UserId, equipments);
            Debug.Log($"[RoomClient] Inventory updated. User: {message.UserId}, Count: {equipments.Length}");
        }

        private void HandleTerritoryUpdated(UpdateTerritoryMessage message)
        {
            OnTerritoryUpdated?.Invoke(message);
            Debug.Log(
                $"[RoomClient] Territory updated. " +
                $"Tile: {message.TileId}, " +
                $"Owner: {message.OwnerId}, " +
                $"Building: {message.HasBuilding}, " +
                $"LandMark: {message.HasLandMark}");
        }

        private void HandleApplyTreasure(ApplyTreasureMessage message)
        {
            if (message.TargetId != Me.Id) return;

            ResolveTreasureAsync(message).Forget();
        }

        private async UniTaskVoid ResolveTreasureAsync(ApplyTreasureMessage message)
        {
            try
            {
                InventoryManager inventory = InventoryManager.Instance;

                if (message.EffectType == GoldCardTreasureEffectType.Gain)
                {
                    EquipmentDef equipment = DataManager.Instance.GetEquipmentDef(message.EquipmentId);
                    inventory.TryAddEquipment(equipment);

                    Debug.Log($"[RoomClient] Gain treasure. Equipment: {equipment.Name}");
                    await TreasureManager.Instance.PresentAsync(equipment);
                    return;
                }

                inventory.DestroyRandomEquipment();
                Debug.Log("[RoomClient] Lose treasure");
            }
            finally
            {
                await AnnounceTreasureResolved(message.RequestId);
            }
        }

        private void HandleTreasureResolved(TreasureResolvedMessage message)
        {
            TreasureManager.Instance.CompleteRequest(message.RequestId);
        }

        private void HandleUserMovedTo(MoveUserToMessage message)
        {
            if (message.TurnId != TurnId) return;

            OnUserMovedTo?.Invoke(message);
            Debug.Log(
                $"[RoomClient] User move requested. " +
                $"Turn: {message.TurnId}, " +
                $"Move: {message.MoveId}, " +
                $"User: {message.UserId}, " +
                $"Tile: {message.TileId}");
        }
        
        private void HandleUserMoveFinished(UserMoveFinishedMessage message)
        {
            OnUserMoveFinished?.Invoke(message);
            Debug.Log(
                $"[RoomClient] User move finished. " +
                $"Turn: {message.TurnId}, " +
                $"Move: {message.MoveId}");
        }

        private void HandleUserMoveCancelled(UserMoveCancelledMessage message)
        {
            OnUserMoveCancelled?.Invoke(message);
            Debug.Log(
                $"[RoomClient] User move cancelled. " +
                $"Turn: {message.TurnId}, " +
                $"Move: {message.MoveId}, " +
                $"User: {message.UserId}");
        }

        private void HandlePlayerActivity(PlayerActivityMessage message)
        {
            OnPlayerActivityChanged?.Invoke(message.PlayerId, message.Activity);
        }

        #endregion
    }
}
