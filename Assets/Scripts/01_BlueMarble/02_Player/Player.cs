using BlueMarble;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using DG.Tweening;
using Jay;
using Sirenix.OdinInspector;

namespace BlueMarble
{
    public class Player : MonoBehaviour
    {
        [field: ShowInInspector, ReadOnly] public string Id { get; private set; }
        [field: ShowInInspector, ReadOnly] public int PlayerNumber { get; private set; }

        [Title("Components")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private GameObject turnMark;

        [Title("Settings")]
        [SerializeField] private float jumpPower = 1f;
        [SerializeField] private float jumpDuration = 0.5f;
        [SerializeField] private AudioClip jumpSound;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private float volumeScale = 0.5f;
        
        [field: Title("States")]
        [field: ShowInInspector, ReadOnly] public int CurrentTileIndex { get; private set; }
        [field: ShowInInspector, ReadOnly] public int ActionDisabledTurnCount { get; set; } = 0;

        public Vector3 Position => transform.position;
        
        private BlueMarbleWorld World => BlueMarbleWorld.Instance;
        
        public void Initialize(string id, int playerNumber)
        {
            Id = id;
            PlayerNumber = playerNumber;

            PlayerAppearance appearance = PlayerAppearanceMap.Instance
                .GetAppearance(playerNumber);

            bodyRenderer.color = appearance.Color;
            ResetTurnVisual();
            name = $"Player {playerNumber} ({id})";
            
            PlayerRegistry.Register(this);
        }

        public void SetTurnActive(bool isActive)
        {
            turnMark.SetActive(isActive);
            SetBodyAlpha(isActive ? 1f : 0.5f);
        }

        public void ResetTurnVisual()
        {
            turnMark.SetActive(false);
            SetBodyAlpha(1f);
        }

        private void SetBodyAlpha(float alpha)
        {
            Color color = bodyRenderer.color;
            color.a = alpha;
            bodyRenderer.color = color;
        }

        public void OnDisable()
        {
            PlayerRegistry.Unregister(this);
        }
        
        public async UniTask<Tile> MoveNext(CancellationToken cancellationToken)
        {
            await UniTask.WaitUntil(
                this,
                self => self.World.IsBoardReady,
                cancellationToken: cancellationToken);

            Board board = World.Board;
            
            // 다음 목적지 설정
            CurrentTileIndex += 1;
                
            // 만약 다음 목적지가 보드의 끝이었다면, 시작점으로 목표 변경
            if (CurrentTileIndex > board.MaxTileIndex)
                CurrentTileIndex = 0;
            
            Tile goal = board.GetTile(CurrentTileIndex);
            
            audioSource.PlayOneShot(jumpSound, volumeScale);
            await transform
                .DOJump(goal.transform.position, jumpPower, 1, jumpDuration)
                .SetEase(Ease.OutCubic)
                .ToUniTask(
                    TweenCancelBehaviour.CompleteAndCancelAwait,
                    cancellationToken: cancellationToken);

            return goal;
        }
    }
}
