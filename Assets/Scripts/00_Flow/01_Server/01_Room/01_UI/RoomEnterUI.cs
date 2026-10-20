using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BlueMarble.Server
{
    public class RoomEnterUI : MonoBehaviour
    {
        [Title("Components")]
        [SerializeField] private TMP_InputField nicknameInput;
        [SerializeField] private TMP_InputField roomCodeInput;
        [SerializeField] private Button enterButton;
        [SerializeField] private GameObject maskImage;
        
        [Title("Settings")]
        [SerializeField] private string roomSceneName = "Room";

        [Title("States")]
        [ShowInInspector, ReadOnly] private string nickname;
        [ShowInInspector, ReadOnly] private string roomCode;

        private int roomCodeLength;

        private bool CanEnter
        {
            get
            {
                if (nickname.IsNullOrWhitespace()) return false;
                if (roomCode.IsNullOrWhitespace()) return false;
                if (roomCode.Length < roomCodeLength) return false;
                return true; // 위의 조건을 모두 넘겼다면 입장 가능
            }
        }
        
        private RoomClient Client => RoomClient.Instance;

        private void Awake()
        {
            roomCodeLength = roomCodeInput.characterLimit;
        }

        private void Start()
        {
            nicknameInput.onEndEdit.AddListener(OnNicknameChanged);
            roomCodeInput.onEndEdit.AddListener(OnRoomCodeChanged);
            enterButton.onClick.AddListener(OnEnterButtonClicked);
            
            maskImage.SetActive(false); // 혹시 모르니까
        }

        private void OnNicknameChanged(string value)
        {
            string result = value.IsNullOrWhitespace() ? string.Empty : value.Trim();
            
            nickname = result;
            nicknameInput.SetTextWithoutNotify(result);
            
            enterButton.interactable = CanEnter;
        }

        private void OnRoomCodeChanged(string value)
        {
            string result = value.IsNullOrWhitespace() || value.Length < roomCodeLength
                ? string.Empty
                : value;
            
            roomCode = result;
            roomCodeInput.SetTextWithoutNotify(result);
            
            enterButton.interactable = CanEnter;
        }

        private async void OnEnterButtonClicked()
        {
            if (CanEnter == false) return;
            
            maskImage.SetActive(true);
            Client.Join(nickname, roomCode);
            
            await WaitForRoomJoinAsync();
        }

        private async UniTask WaitForRoomJoinAsync()
        {
            while (true)
            {
                if (Client.Me != null)
                {
                    _ = SceneManager.LoadSceneAsync(roomSceneName);
                    return;
                }

                if (Client.IsBusy == false)
                {
                    maskImage.SetActive(false);
                    enterButton.interactable = CanEnter;
                    return;
                }
                
                await UniTask.Yield();
            }
        }
    }
}