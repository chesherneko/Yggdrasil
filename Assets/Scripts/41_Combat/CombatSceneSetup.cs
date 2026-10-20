using System.Collections.Generic;
using BlueMarble;
using BlueMarble.Server;
using Cysharp.Threading.Tasks;
using Player;
using UnityEngine;

namespace Combat
{
    /// <summary>
    /// Combat 씬의 최소 실행 구성을 만든다.
    /// 아트 작업 시 같은 이름의 오브젝트를 꾸며도 전투 컴포넌트는 그대로 재사용된다.
    /// </summary>
    [DisallowMultipleComponent]
    public class CombatSceneSetup : MonoBehaviour
    {
        [Header("Standalone Test")]
        [SerializeField] private bool runTestWhenOpenedDirectly = true;
        [SerializeField] private int testStartHp = 50;

        [Header("Player Visual")]
        [SerializeField] private RuntimeAnimatorController playerAnimatorController;

        private readonly List<Camera> disabledCameras = new();
        private readonly List<AudioListener> disabledListeners = new();

        private void Start()
        {
            SetupAsync().Forget();
        }

        private async UniTaskVoid SetupAsync()
        {
            CombatSystem combatSystem = CombatSystem.Instance;

            Transform systemsRoot = FindOrCreateRoot("=Combat Systems");
            Transform playersRoot = FindOrCreateRoot("=Players");
            Transform environmentRoot = FindOrCreateRoot("=Environment");
            Transform uiRoot = FindOrCreateRoot("=UI");

            Timer timer = GetOrAddComponent<Timer>(FindOrCreateChild(systemsRoot, "Combat Timer").gameObject);
            combatSystem.AttachTimer(timer);

            CombatUI combatUI = GetOrAddComponent<CombatUI>(uiRoot.gameObject);
            combatUI.Initialize(combatSystem, timer);
            combatSystem.AttachCombatUI(combatUI);

            BattleManager battleManager = BattleManager.Instance;
            if (battleManager.basicArrowPrefab == null)
                battleManager.basicArrowPrefab = Resources.Load<GameObject>("Equipment/Basic");

            CreateEnvironmentPlaceholders(environmentRoot);

            PlayerEntity leftPlayer = CreatePlayer(
                FindOrCreateChild(playersRoot, "Left Player"),
                new Vector3(-4f, -1.5f, 0f));
            PlayerEntity rightPlayer = CreatePlayer(
                FindOrCreateChild(playersRoot, "Right Player"),
                new Vector3(4f, -1.5f, 0f));

            DisableOtherSceneCameras();

            if (combatSystem.HasCombatAssignment)
            {
                SetupPlayers(
                    leftPlayer,
                    rightPlayer,
                    combatSystem.AttackerId,
                    combatSystem.DefenderId,
                    combatSystem.LocalPlayerId,
                    combatSystem.StartHp);

                combatSystem.NotifySceneReady();
                return;
            }

            if (!runTestWhenOpenedDirectly) return;

            const string localId = "LocalPlayer";
            const string remoteId = "RemotePlayer";
            SetupPlayers(leftPlayer, rightPlayer, localId, remoteId, localId, testStartHp);
            await combatSystem.StartTestCombat(localId, remoteId, localId, testStartHp);
        }

        private void OnDestroy()
        {
            foreach (Camera camera in disabledCameras)
            {
                if (camera != null)
                    camera.enabled = true;
            }

            foreach (AudioListener listener in disabledListeners)
            {
                if (listener != null)
                    listener.enabled = true;
            }
        }

        private PlayerEntity CreatePlayer(Transform playerTransform, Vector3 defaultPosition)
        {
            GameObject playerObject = playerTransform.gameObject;
            bool wasActive = playerObject.activeSelf;
            playerObject.SetActive(false);

            if (playerTransform.localPosition == Vector3.zero)
                playerTransform.localPosition = defaultPosition;

            CapsuleCollider2D collider = GetOrAddComponent<CapsuleCollider2D>(playerObject);
            collider.isTrigger = true;
            collider.size = new Vector2(1f, 2f);

            Rigidbody2D rigidbody = GetOrAddComponent<Rigidbody2D>(playerObject);
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
            rigidbody.gravityScale = 0f;
            rigidbody.freezeRotation = true;

            // 씬에 배치된 Visual의 위치와 크기를 그대로 사용한다.
            // 플레이어 루트에 렌더러를 추가하면 기존 Visual과 겹쳐 보이므로
            // 외형용 컴포넌트는 반드시 자식 Visual에만 둔다.
            Transform visualTransform = FindOrCreateChild(playerTransform, "Visual");
            GameObject visualObject = visualTransform.gameObject;
            SpriteRenderer visualRenderer = GetOrAddComponent<SpriteRenderer>(visualObject);
            visualRenderer.enabled = true;
            visualRenderer.color = Color.white;

            SpriteRenderer rootRenderer = playerObject.GetComponent<SpriteRenderer>();
            if (rootRenderer != null)
                rootRenderer.enabled = false;

            Animator rootAnimator = playerObject.GetComponent<Animator>();
            if (rootAnimator != null)
                rootAnimator.enabled = false;

            Animator animator = GetOrAddComponent<Animator>(visualObject);
            animator.enabled = true;
            if (playerAnimatorController != null)
                animator.runtimeAnimatorController = playerAnimatorController;

            CharacterController2D characterController = GetOrAddComponent<CharacterController2D>(playerObject);
            characterController.collisionLayer = 1 << 3;
            PlayerEntity entity = GetOrAddComponent<PlayerEntity>(playerObject);
            GetOrAddComponent<HurtBox>(playerObject);
            GetOrAddComponent<PlayerController>(playerObject);
            GetOrAddComponent<PlayerAnimator>(playerObject);
            EquipmentCaster caster = GetOrAddComponent<EquipmentCaster>(playerObject);

            Transform spawnPoint = FindOrCreateChild(playerTransform, "Spawn Point");
            if (spawnPoint.localPosition == Vector3.zero)
                spawnPoint.localPosition = new Vector3(0.75f, 0f, 0f);
            caster.spawnPoint = spawnPoint;

            playerObject.SetActive(wasActive);
            return entity;
        }

        private static void SetupPlayers(
            PlayerEntity leftPlayer,
            PlayerEntity rightPlayer,
            string attackerId,
            string defenderId,
            string localPlayerId,
            int hp)
        {
            SetupPlayer(
                leftPlayer,
                attackerId,
                attackerId == localPlayerId,
                hp,
                1f,
                GetPlayerNumber(attackerId, 0));
            SetupPlayer(
                rightPlayer,
                defenderId,
                defenderId == localPlayerId,
                hp,
                -1f,
                GetPlayerNumber(defenderId, 1));
        }

        private static void SetupPlayer(
            PlayerEntity player,
            string ownerId,
            bool isLocal,
            int hp,
            float facingDirection,
            int playerNumber)
        {
            player.Init(ownerId, hp);
            ApplyAppearance(player, playerNumber);

            PlayerController controller = player.GetComponent<PlayerController>();
            controller.SetLocalPlayer(isLocal);

            Transform cursor = player.transform.Find("Cursor");
            if (cursor != null)
                cursor.gameObject.SetActive(isLocal);

            EquipmentCaster caster = player.GetComponent<EquipmentCaster>();
            caster.SetFacingDirection(facingDirection);
            caster.SetLocalPlayer(isLocal);
        }

        private static int GetPlayerNumber(string playerId, int fallback)
        {
            if (!RoomClient.IsInitialized || RoomClient.Instance.Me == null)
                return fallback;

            RoomClient client = RoomClient.Instance;
            if (client.Me.Id == playerId)
                return client.Me.PlayerNumber;

            return client.Others.TryGetValue(playerId, out User user)
                ? user.PlayerNumber
                : fallback;
        }

        private static void ApplyAppearance(PlayerEntity player, int playerNumber)
        {
            PlayerAppearanceMap appearanceMap = PlayerAppearanceMap.Instance;
            if (appearanceMap == null)
            {
                Debug.LogError("[CombatSceneSetup] PlayerAppearanceMap을 불러올 수 없습니다.");
                return;
            }

            SpriteRenderer visualRenderer = player.transform
                .Find("Visual")?
                .GetComponent<SpriteRenderer>();
            if (visualRenderer == null)
            {
                Debug.LogError($"[CombatSceneSetup] {player.OwnerId}의 Visual SpriteRenderer가 없습니다.");
                return;
            }

            PlayerAppearance appearance = appearanceMap.GetAppearance(playerNumber);
            visualRenderer.color = appearance.Color;
        }

        private void DisableOtherSceneCameras()
        {
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Camera camera in cameras)
            {
                if (camera.gameObject.scene == gameObject.scene || !camera.enabled) continue;
                camera.enabled = false;
                disabledCameras.Add(camera);
            }

            AudioListener[] listeners = FindObjectsByType<AudioListener>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            foreach (AudioListener listener in listeners)
            {
                if (listener.gameObject.scene == gameObject.scene || !listener.enabled) continue;
                listener.enabled = false;
                disabledListeners.Add(listener);
            }
        }

        private static void CreateEnvironmentPlaceholders(Transform environmentRoot)
        {
            CreateBoundary(environmentRoot, "Ground", new Vector3(0f, -3f, 0f), new Vector2(18f, 1f));
            CreateBoundary(environmentRoot, "Left Wall", new Vector3(-9f, 0f, 0f), new Vector2(1f, 7f));
            CreateBoundary(environmentRoot, "Right Wall", new Vector3(9f, 0f, 0f), new Vector2(1f, 7f));
        }

        private static void CreateBoundary(
            Transform root,
            string name,
            Vector3 defaultPosition,
            Vector2 defaultSize)
        {
            Transform boundary = FindOrCreateChild(root, name);
            boundary.gameObject.layer = 3;
            if (boundary.localPosition == Vector3.zero)
                boundary.localPosition = defaultPosition;

            BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(boundary.gameObject);
            collider.size = defaultSize;
        }

        private Transform FindOrCreateRoot(string name)
        {
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                if (root.name == name)
                    return root.transform;
            }

            GameObject created = new GameObject(name);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(created, gameObject.scene);
            return created.transform;
        }

        private static Transform FindOrCreateChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) return child;

            GameObject created = new GameObject(name);
            created.transform.SetParent(parent, false);
            return created.transform;
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
    }
}
