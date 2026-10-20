using Sirenix.OdinInspector;
using UnityEngine;

namespace BlueMarble
{
    public class BoardLoader : MonoBehaviour
    {
        [Title("Settings")]
        [SerializeField] private Transform root;
        [SerializeField] private Transform startPoint;
        [SerializeField] private TileDirection startDir;
        [SerializeField] private Vector2 offset = new(0.0625f, 0.0625f);
        
        public BoardData Board { get; private set; }
        public Transform BoardRoot => root;

        public bool TryLoad(out BoardData board)
        {
            board = null;
            
            if (Board != null)
            {
                board = Board;
                return true;
            }

            if (BoardFile.TryLoad(out BoardFile file) == false)
                return false;

            board = file.SpawnBoard(root, startPoint, startDir, offset);
            Debug.Log($"[BoardLoader] Board is loaded. Tile count: {board.TileCount}");

            Board = board;
            return true;
        }
    }
}