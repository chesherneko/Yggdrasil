using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueMarble.Server
{
    public class RoomLobbySlot : MonoBehaviour
    {
        [field: Title("Components")]
        [field: SerializeField] public TMP_Text NicknameTMP { get; private set; }
        [field: SerializeField] public GameObject ReadyLabel { get; private set; }

        [field: SerializeField] public string UserId { get; private set; }

        public void Initialize(User user)
        {
            UserId = user.Id;
            NicknameTMP.SetText($"{user.Nickname}({user.Id})");
            ReadyLabel.SetActive(user.IsReady);
        }
    }
}