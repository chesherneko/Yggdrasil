using UnityEngine;

namespace BlueMarble
{
    [CreateAssetMenu(fileName = "SpaceIcons", menuName = "Board/Space/IconMap", order = 0)]
    public class BoardSpaceIconMap : ScriptableObject
    {
        private const string FILE_PATH = "Board/SpaceIcons";
        
        private static BoardSpaceIconMap instance;
        public static BoardSpaceIconMap Instance
        {
            get
            {
                if (instance == null)
                    instance = Resources.Load<BoardSpaceIconMap>(FILE_PATH);

                return instance;
            }
        }

        [SerializeField] private Sprite[] icons;
        
        public Sprite GetIcon(int idx)
        {
            if (idx < 0 || idx >= icons.Length)
            {
                Debug.LogError($"[BoardSpaceIconMap] Idx({idx}) is out of range");
                return null;
            }
            return icons[idx];
        }
    }
}