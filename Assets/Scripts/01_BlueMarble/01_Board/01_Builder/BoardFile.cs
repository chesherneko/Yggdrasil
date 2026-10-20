using System;
using System.Collections.Generic;
using System.IO;
using Jay.FileIO;
using UnityEngine;

namespace BlueMarble
{
    [Serializable]
    public class BoardFile
    {
        private static readonly string FILE_PATH 
            = Path.Combine(Application.streamingAssetsPath, "Json", "Board", "Board.json");
        
        public List<TileEntry> Entries;

        public void Save()
        {
            JsonWriter.Save(this, FILE_PATH);
            Debug.Log($"[BoardFile] Board saved to {FILE_PATH}. " +
                $"Saved data count: {Entries.Count}");
        }

        public static bool TryLoad(out BoardFile file)
        {
            file = null;

            if (File.Exists(FILE_PATH) == false)
            {
                Debug.LogError($"[BoardFile] Board file not found at {FILE_PATH}.");
                return false;
            }
            
            file = JsonReader.Load<BoardFile>(FILE_PATH);

            if (file == null) return false;
            if (file.IsValid() == false) return false;
            
            return true;
        }

        public bool IsValid()
        {
            if (Entries == null) return false;
            if (Entries.Count == 0) return false;
            
            return true;
        }
    }
}