using System.IO;
using UnityEngine;

namespace BlueMarble
{
    [System.Serializable]
    public class GameSettings
    {
        public int SalaryAmount;
        public int PrisonActionDisabledTurnCount;
        public float BuildingPriceMultiplier;
        public float LandmarkPriceMultiplier;
        public float LandTollRate;
        public float BuildingTollRate;
        public float LandmarkTollRate;

        private static GameSettings instance;
        public static GameSettings Instance
        {
            get
            {
                instance ??= Load();
                return instance;
            }
        }

        private static GameSettings Load()
        {
            string path = Path.Combine(
                Application.streamingAssetsPath,
                "Json",
                "config.json");

            return JsonUtility.FromJson<GameSettings>(File.ReadAllText(path));
        }
    }
}
