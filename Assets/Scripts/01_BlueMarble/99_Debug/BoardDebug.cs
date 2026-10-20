using BlueMarble.Server;
using Cysharp.Threading.Tasks;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlueMarble
{
    public class BoardDebug : MonoBehaviour
    {
        private bool isRunning;

        private RoomClient Client => RoomClient.Instance;
        private GameManager Game => GameManager.Instance;

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Key.F1.GetKeyDown())
                ExecuteAsync().Forget();
#endif
        }

        private async UniTaskVoid ExecuteAsync()
        {
            if (isRunning) return;

            User currentUser = Client.CurrentTurnUser;

            // 현재 턴 플레이어의 클라이언트에서만 사용할 수 있다.
            if (currentUser == null) return;
            if (currentUser.Id != Client.Me.Id) return;

            // 주사위를 굴리기 전, 행동 가능한 상태에서만 허용한다.
            if (Game.Turn.CanRoll == false) return;

            int turnId = Client.TurnId;
            isRunning = true;

            // 텔레포트 실행 중 주사위 입력을 막는다.
            Game.Turn.SetInputEnabled(false);

            try
            {
                await TeleportManager.Instance.MoveToSelected(Client.Me.Id);

                // 텔레포트 도중 턴이 바뀌었다면 이전 턴 종료를 보내지 않는다.
                if (Client.TurnId != turnId) return;

                await Client.AnnounceTurnFinished(turnId);
            }
            finally
            {
                isRunning = false;
            }
        }
    }
}