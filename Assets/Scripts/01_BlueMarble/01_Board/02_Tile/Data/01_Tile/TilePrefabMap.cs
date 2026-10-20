using System;
using UnityEngine;

namespace BlueMarble
{
    [CreateAssetMenu(fileName = "TilePrefabs", menuName = "Board/Tile/PrefabMap", order = 0)]
    public class TilePrefabMap : ScriptableObject
    {
        [Serializable]
        private struct TilePrefab
        {
            public TileType Type;
            public TileDirection Dir;
            public Tile Prefab;
        }

        private const string FILE_PATH = "Board/TilePrefabs";
        
        private static TilePrefabMap instance;
        public static TilePrefabMap Instance
        {
            get
            {
                if (instance == null)
                    instance = Resources.Load<TilePrefabMap>(FILE_PATH);

                return instance;
            }
        }

        [SerializeField] private TilePrefab[] prefabs;

        public Tile GetPrefab(int id)
        {
            if (id < 0 || id >= prefabs.Length)
            {
                Debug.LogError($"[TilePrefabMap] Idx({id}) is out of range");
                return null;
            }
            return prefabs[id].Prefab;
        }

        public Tile GetPrefab(TileType type, TileDirection dir)
        {
            foreach (var prefab in prefabs)
            {
                if (prefab.Type != type) continue;
                if (prefab.Dir != dir) continue;
                
                return prefab.Prefab;
            }
            
            Debug.LogError($"[TilePrefabMap] Tile({type}, {dir}) is not found");
            return null;
        }
    }
}