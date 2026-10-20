using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Jay;
using Sirenix.OdinInspector;

namespace BlueMarble
{
    public class BlueMarbleWorld : SingletonBase<BlueMarbleWorld>
    {
        [field: Title("Components")]
        [field: SerializeField] public BoardLoader BoardLoader { get; private set; }
        [field: ShowInInspector, ReadOnly] public BoardData BoardData { get; private set; }
        
        public Board Board { get; private set; }

        public bool IsBoardReady => Board != null;
        
        private DataManager Data => DataManager.Instance;

        private async UniTaskVoid Start()
        {
            await Data.EnsureDataLoaded();

            if (BoardLoader.TryLoad(out BoardData loaded) == false)
            {
                Debug.LogError($"[BlueMarble.World] Failed to load board.");
                return;
            }

            BoardData = loaded;
            
            Transform root = BoardLoader.BoardRoot;
            Board = BoardData.ToBoard(root);
        }
    }
}