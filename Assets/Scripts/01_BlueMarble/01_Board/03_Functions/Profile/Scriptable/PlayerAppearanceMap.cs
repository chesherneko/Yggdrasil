using System;
using UnityEngine;

namespace BlueMarble
{
    [Serializable]
    public class PlayerAppearance
    {
        [field: SerializeField] public Sprite Portrait { get; private set; }
        [field: SerializeField] public Color Color { get; private set; }
    }
    
    [CreateAssetMenu(fileName = "Appearance", menuName = "Board/Profile/Appearance", order = 0)]
    public class PlayerAppearanceMap : ScriptableObject
    {
        private const string FILE_PATH = "Board/PlayerAppearance";
        
        private static PlayerAppearanceMap instance;
        public static PlayerAppearanceMap Instance
        {
            get
            {
                if (instance == null)
                    instance = Resources.Load<PlayerAppearanceMap>(FILE_PATH);
                
                return instance;
            }
        }

        [SerializeField] private PlayerAppearance[] appearances;
        
        public PlayerAppearance GetAppearance(int idx) => appearances[idx];
    }
}

