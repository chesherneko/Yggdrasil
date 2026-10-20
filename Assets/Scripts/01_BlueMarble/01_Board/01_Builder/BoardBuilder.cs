using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace BlueMarble
{
    public class BoardBuilder : MonoBehaviour
    {
        [Title("Components")]
        [SerializeField] private BoardData boardData;
        
        [Title("Settings")]
        [SerializeField] private Transform boardRoot;
        [SerializeField] private Transform startPoint;

        [Space]
        [SerializeField] private Vector2 offset;

        [Space]
        [SerializeField, FolderPath] private string prefabFolderPath;

        [Title("Readonly")]
        [ShowInInspector] private TileDirection currentDir = TileDirection.South;
        [ShowInInspector] private readonly List<Tile> createdList = new();
        
        private DataManager Data => DataManager.Instance;

        private async UniTaskVoid Start()
        {
            await Data.EnsureDataLoaded();

            Tile created = boardRoot.CreateTile(TileType.Corner, TileDirection.All, 0);
            created.transform.position = startPoint.position;
        }
        
        #region BOARD

        private void BuildBoard(BoardFile file)
        {
            ClearBoard();
            
            List<TileEntry> entries = file.Entries;

            // 첫 번째 타일은 이미 생성되어있으므로, Build 과정에 포함시키지 않음
            for (int i = 1; i < entries.Count; i++)
            {
                TileEntry entry = entries[i];
                
                TileDef def = Data.GetTileDef(entry.TileId);
                BoardSpaceDef spaceDef = Data.GetBoardSpaceDef(entry.SpaceId);

                CreateTile(def.Type, spaceDef.Id);
            }
        }

        public void ClearBoard()
        {
            for (int i = createdList.Count - 1; i >= 0; i--)
            {
                if (RemoveLastTile(false) == false)
                    break;
            }
        }
        
        #endregion
        
        #region TILE
        
        public void CreateTile(TileType type, int spaceId)
        {
            // CreateTile에서 CreatedList에 생성한 타일을 즉시 Add하므로
            // 가장 최근 타일을 미리 캐싱해두고 위치 계산에 사용해야 함
            Tile lastCreated = createdList[^1];
            
            Tile created = boardRoot.CreateTile(type, currentDir, spaceId);
            created.transform.position = created.CalculateDistFrom(lastCreated, currentDir, offset);

            // 코너 타일이 설치되었다면 다음 방향으로 전환
            if (type == TileType.Corner) currentDir++;
        }

        public bool RemoveLastTile(bool isPrintLog = true)
        {
            // 출발점 타일은 지우지 못하도록 Early Return
            if (createdList.Count == 1)
            {
                if (isPrintLog) Debug.LogWarning("Cannot remove start tile.");
                return false;
            }
            
            Tile lastCreated = createdList[^1];
            
            // 지울 타일이 코너 타일이면, 타일 생성 방향을 이전으로 되돌림
            if (lastCreated.Def.Type == TileType.Corner)
                currentDir--;
            
            createdList.Remove(lastCreated);
            Destroy(lastCreated.gameObject);

            return true;
        }

        #endregion
        
        #region SAVE_OR_LOAD_BOARD

        public void SaveBoard()
        {
            boardData.ImportFrom(createdList);
            boardData.ToFile().Save();
            
            SaveBoardAsPrefab();
            
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }

        public void LoadBoard()
        {
            if (BoardFile.TryLoad(out BoardFile file) == false)
                return;

            BuildBoard(file);
            Debug.Log($"[BoardBuilder] Successfully loaded board: {file.Entries.Count} tiles.");
        }

        public void SaveBoardAsPrefab()
        {
#if UNITY_EDITOR
            if (boardData == null)
            {
                Debug.LogError("[BoardBuilder] Make sure that boardData is assigned.");
                return;
            }

            string prefabPath = $"{prefabFolderPath}/Board.prefab";

            GameObject pref = PrefabUtility
                .SaveAsPrefabAsset(boardData.gameObject, prefabPath, out bool success);

            if (success == false || pref == null)
            {
                Debug.LogError("[BoardBuilder] Failed to save board as prefab.");
                return;
            }
            
            AssetDatabase.Refresh();
            Debug.Log($"[BoardBuilder] Successfully saved board as prefab: {prefabPath}");
#endif
        }
        
        #endregion
    }
}