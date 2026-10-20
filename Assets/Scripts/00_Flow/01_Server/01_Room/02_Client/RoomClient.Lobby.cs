using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BlueMarble.Server
{
    public partial class RoomClient
    {
        public event Action<User> OnJoined;
        public event Action<User> OnLeft;
        public event Action<User, bool> OnSetGameReady; // Room에서 준비 상태를 변경했을 때
        
        private void RegisterLobbyHandlers()
        {
            RegisterHandler<WelcomeMessage>(ProtocolHeader.WELCOME, HandleWelcome);
            RegisterHandler<JoinMessage>(ProtocolHeader.JOIN, HandleJoin);
            RegisterHandler<LeaveMessage>(ProtocolHeader.LEAVE, HandleLeave);
            RegisterHandler<GameReadySetMessage>(ProtocolHeader.GAME_READY_SET, HandleGameReadySet);
        }
        
        #region REQUEST_METHODS

        public void RequestReady(bool isReady)
        {
            SetGameReadyMessage msg = new SetGameReadyMessage
            {
                IsReady = isReady
            };
            
            SendAsync(msg).Forget();
        }

        public void RequestStart()
        {
            // 방장만 시작 기능 이용 가능
            if (Me is not { IsHost: true }) return;
            
            SendAsync(new StartGameMessage()).Forget();
        }
        
        #endregion
        
        #region HANDLE_RECEIVED_MESSAGE

        private void HandleWelcome(WelcomeMessage message)
        {
            Me = message.User;
            RoomCode = message.RoomCode;
            
            // 현재 방에 있던 다른 유저들 등록
            foreach (User user in message.Users)
                others[user.Id] = user;

            Debug.Log($"[RoomClient] Room joined: {RoomCode}. " +
                      $"My ID: {Me.Id}, Participants: {others.Count + 1}");
        }

        private void HandleJoin(JoinMessage message)
        {
            User user = message.User;
            others[user.Id] = user;
            
            OnJoined?.Invoke(user);
            Debug.Log($"[RoomClient] {user.Nickname}({user.Id}) joined.");
        }

        private void HandleLeave(LeaveMessage message)
        {
            inventories.Remove(message.Id);
            if (playerIds != null)
                playerIds = Array.FindAll(playerIds, id => id != message.Id);

            // Remove(key, out value) => TryRemove랑 같음 (의미적으로)
            // Dictionary에서 key에 해당하는 요소를 삭제하는데 성공했는지를 반환해줌
            if (others.Remove(message.Id, out User user) == false)
                return;

            if (string.IsNullOrEmpty(message.NewHostId) == false)
            {
                if (message.NewHostId == Me.Id)
                    Me.IsHost = true;
                else if (others.TryGetValue(message.NewHostId, out User newHost))
                    newHost.IsHost = true;
            }
            
            OnLeft?.Invoke(user);

            if (CurrentTurnUser?.Id == message.Id)
                CurrentTurnUser = null;

            Debug.Log($"[RoomClient] {user.Nickname}({user.Id}) left.");
        }

        private void HandleGameReadySet(GameReadySetMessage message)
        {
            User user = GetUser(message.Id);
            user.IsReady = message.IsReady;
            
            OnSetGameReady?.Invoke(user, user.IsReady);
            Debug.Log($"[RoomClient] {user.Nickname}({user.Id}) is ready: {user.IsReady}");
        }
        
        #endregion
    }
}
