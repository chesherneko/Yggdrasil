using UnityEngine;
using Player;
using Cysharp.Threading.Tasks;

namespace Combat
{
    public class CombatTestSetup : MonoBehaviour
    {
        [Header("플레이어 본체 연결")]
        public PlayerEntity localPlayer;
        public PlayerEntity remotePlayer;

        [Header("하드코딩 세팅값")]
        public string localId = "LocalPlayer";
        public string remoteId = "RemotePlayer";
        public int startHp = 50;

        [Header("테스트 조작 설정")]
        [SerializeField, InspectorName("오른쪽 플레이어로 테스트")]
        private bool playAsRightPlayer = false;

        private void Start()
        {
            CombatSystem combatSystem = CombatSystem.Instance;

            if (combatSystem.HasCombatAssignment)
            {
                SetupPlayers(
                    combatSystem.AttackerId,
                    combatSystem.DefenderId,
                    combatSystem.LocalPlayerId,
                    combatSystem.StartHp);
                return;
            }

            string testLocalPlayerId = playAsRightPlayer ? remoteId : localId;
            SetupPlayers(localId, remoteId, testLocalPlayerId, startHp);

            Debug.Log("[CombatTestSetup] 플레이어 초기화 및 권한 세팅 완료. 전투 시작!");

            combatSystem.StartTestCombat(localId, remoteId, testLocalPlayerId, startHp).Forget();
        }

        private void SetupPlayers(
            string attackerId,
            string defenderId,
            string localPlayerId,
            int hp)
        {
            SetupPlayer(localPlayer, attackerId, localPlayerId, hp, 1f);
            SetupPlayer(remotePlayer, defenderId, localPlayerId, hp, -1f);
        }

        private static void SetupPlayer(
            PlayerEntity player,
            string ownerId,
            string localPlayerId,
            int hp,
            float facingDirection)
        {
            if (player == null) return;

            bool isMine = ownerId == localPlayerId;
            player.Init(ownerId, hp);

            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller != null)
                controller.SetLocalPlayer(isMine);

            EquipmentCaster caster = player.GetComponent<EquipmentCaster>();
            if (caster != null)
            {
                caster.SetLocalPlayer(isMine);
                caster.SetFacingDirection(facingDirection);
            }
        }
    }
}
