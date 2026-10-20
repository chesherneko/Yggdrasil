using System;
using System.Linq;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BlueMarble.Server
{
    public class RoomLobbyUI : MonoBehaviour
    {
        [Title("Settings")]
        [SerializeField] private string readyStr = "준비 완료"; // 준비 상태에 띄울 글자
        [SerializeField] private string notReadyStr = "준비"; // 준비가 아닌 상태에서 띄울 글자
        
        [Title("Components")]
        [SerializeField] private RoomLobbySlot[] slots;
        [SerializeField] private Button readyButton;
        [SerializeField] private TMP_Text readyText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button leaveButton;
        [SerializeField] private GameObject mask;

        private RoomClient Client => RoomClient.Instance;
        
        #region UNITY_EVENTS
        
        private void Start()
        {
            // 첫 슬롯은 무조건 자신
            ActiveSlot(Client.Me);
            
            // 이후 슬롯에 이미 접속중인 플레이어 표시
            foreach (var user in Client.Others.Values)
                ActiveSlot(user);
            
            readyButton.onClick.AddListener(OnReadyButtonClicked);
            leaveButton.onClick.AddListener(OnExitButtonClicked);
            startButton.onClick.AddListener(OnStartButtonClicked);
            
            Client.OnJoined += ActiveSlot;
            Client.OnJoined += SetStartButton;
            
            Client.OnLeft += DeactiveSlot;
            Client.OnLeft += SetStartButton;
            
            Client.OnSetGameReady += SetReadyButton;
            Client.OnSetGameReady += SetStartButton;

            Client.OnGameStarted += StartUserGame;
        }

        private void OnDisable()
        {
            Client.OnJoined -= ActiveSlot;
            Client.OnJoined -= SetStartButton;
            
            Client.OnLeft -= DeactiveSlot;
            Client.OnLeft -= SetStartButton;
            
            Client.OnSetGameReady -= SetReadyButton;
            Client.OnSetGameReady -= SetStartButton;
            
            Client.OnGameStarted -= StartUserGame;
        }
        
        #endregion

        #region BUTTON_EVENTS

        private void OnReadyButtonClicked()
        {
            if (Client.Me == null) return;
            
            bool isReady = !Client.Me.IsReady;
            Client.RequestReady(isReady);
        }

        private async void OnExitButtonClicked()
        {
            mask.SetActive(true);
            
            await Client.LeaveAsync();
        }

        private void OnStartButtonClicked()
        {
            Client.RequestStart();
        }

        #endregion
        
        #region CLIENT_EVENTS
        
        private void ActiveSlot(User user)
        {
            RoomLobbySlot slot = slots
                .FirstOrDefault(s => s.gameObject.activeSelf == false);
            
            slot.Initialize(user);
            slot.gameObject.SetActive(true);
        }

        private void DeactiveSlot(User user)
        {
            if (TryFindSlot(user, out var slot))
                slot.gameObject.SetActive(false);
        }

        private void SetReadyButton(User user, bool isReady)
        {
            if (TryFindSlot(user, out var slot) == false)
                return;
            
            slot.ReadyLabel.SetActive(isReady);
            
            // 내 클라이언트만 글씨 변경
            if (user.Id == Client.Me.Id)
                readyText.SetText(isReady ? readyStr : notReadyStr);
        }

        private void SetStartButton(User user)
        {
            bool canStart =
                Client.Me is { IsHost: true } &&
                Client.Others.Count + 1 >= 2 &&
                Client.Me.IsReady &&
                Client.Others.Values.All(player => player.IsReady);

            startButton.gameObject.SetActive(canStart);
        }
        private void SetStartButton(User user, bool _) => SetStartButton(user);

        private void StartUserGame()
        {
            mask.SetActive(true);
            SceneManager.LoadSceneAsync("Board");
        }
        
        #endregion
        
        private bool TryFindSlot(User user, out RoomLobbySlot find)
        {
            find = slots.FirstOrDefault(s => s.UserId == user.Id);

            bool isFind = find != null;
            if (isFind == false) Debug.LogError($"[RoomLobbyUI] Slot not found for {user.Nickname}({user.Id})");

            return isFind;
        }
    }
}
