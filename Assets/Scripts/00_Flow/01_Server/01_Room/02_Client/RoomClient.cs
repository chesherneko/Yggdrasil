using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Jay;
using Newtonsoft.Json;
using NUnit.Framework.Api;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace BlueMarble.Server
{
    public partial class RoomClient : SingletonBase<RoomClient>
    {
        [Title("Settings")]
        [SerializeField] private string url = "ws://34.50.63.204:5005/room";

        [field: Title("Client States")]
        [field: ShowInInspector, ReadOnly] public bool IsBusy { get; private set; } = false;
        
        [field: Title("Room Info")]
        [field: ShowInInspector, ReadOnly] public string RoomCode { get; private set; }
        [field: ShowInInspector, ReadOnly] public string Nickname { get; private set; }

        [field: Title("My Info")]
        [field: ShowInInspector, ReadOnly] public User Me { get; private set; }

        [field: Title("Others Info")]
        [field: ShowInInspector, ReadOnly] private readonly Dictionary<string, User> others = new();
        
        private RoomConnection connection;

        private readonly Dictionary<string, Action<string>> messageHandlers = new();
        
        public IReadOnlyDictionary<string, User> Others => others;

        protected override void OnInitialize()
        {
            Application.runInBackground = true;

            if (IsInitialized == false)
            {
                RegisterLobbyHandlers();
                RegisterGameHandlers();
            }
        }

        #region LIFECYCLE

        public void Join(string nickname, string roomCode)
        {
            if (IsBusy) return;

            ClearRoom();

            Nickname = nickname;
            RoomCode = roomCode;
            
            RunAsync().Forget();
        }

        private async UniTaskVoid RunAsync()
        {
            IsBusy = true;

            RoomConnection activeConnection = new RoomConnection();
            connection = activeConnection;
            
            string uriStr = $"{url}?code={UnityWebRequest.EscapeURL(RoomCode)}";

            try
            {
                await activeConnection.ConnectAsync(new Uri(uriStr), destroyCancellationToken);
                
                Debug.Log($"[RoomClient] Connected to \"{uriStr}\"");

                await SendAsync(new HelloMessage { Nickname = Nickname });
                await activeConnection.ReceiveLoopAsync(HandleReceived, destroyCancellationToken);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Debug.LogError($"[RoomClient] {ex}"); }
            finally
            {
                activeConnection.Dispose();

                if (connection == activeConnection)
                    connection = null;

                ClearRoom();
                IsBusy = false;
                ReturnToRoomEntryAsync().Forget();
            }
        }

        public async UniTask LeaveAsync()
        {
            RoomConnection activeConnection = connection;
            if (activeConnection == null) return;

            await activeConnection.CloseAsync(destroyCancellationToken);
            await UniTask.WaitUntil(this, self => self.IsBusy == false);
            
            Debug.Log("[RoomClient] Disconnected");
        }

        private async UniTaskVoid ReturnToRoomEntryAsync()
        {
            const string sceneName = "Room Enter";
            if (SceneManager.GetActiveScene().name == sceneName) return;

            await SceneManager.LoadSceneAsync(sceneName);
        }

        #endregion
        
        #region SEND_AND_RECEIVE_MESSAGE

        private async UniTask SendAsync(object message)
        {
            if (connection == null) return;
            
            string json = JsonConvert.SerializeObject(message);
            await connection.SendAsync(json, destroyCancellationToken);
        }

        private void HandleReceived(string json)
        {
            string type = JsonConvert.DeserializeObject<MessageHeader>(json).Type;
            
            if (type == null ||
                messageHandlers.TryGetValue(type, out Action<string> handler) == false)
            {
                Debug.LogWarning($"[RoomClient] Unknown message type: {type}");
                return;
            }

            handler?.Invoke(json);
        }
        
        private void RegisterHandler<T>(string type, Action<T> handler)
        {
            messageHandlers.Add(type, json =>
            {
                T msg = JsonConvert.DeserializeObject<T>(json);
                handler?.Invoke(msg);
            });
        }
        
        #endregion
        
        public User GetUser(string id) => id == Me.Id ? Me : others[id];
        
        private void ClearRoom()
        {
            TreasureManager.Instance.Reset();
            InventoryManager.Instance.Reset();

            Nickname = null;
            RoomCode = null;

            Me = null;
            others.Clear();
            playerIds = null;
            inventories.Clear();
            IsSpectating = false;
        }
    }
}
