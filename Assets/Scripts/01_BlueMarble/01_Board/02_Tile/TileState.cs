namespace BlueMarble
{
    public class TileState
    {
        public string OwnerId { get; set; } = string.Empty; // -1이면 주인 없음
        
        public bool HasBuilding { get; set; } = false;
        public bool HasLandMark { get; set; } = false;
        
        public bool IsOccupied => !string.IsNullOrEmpty(OwnerId);

        public void Clear()
        {
            OwnerId = string.Empty;
            HasBuilding = false;
            HasLandMark = false;
        }
    }
}