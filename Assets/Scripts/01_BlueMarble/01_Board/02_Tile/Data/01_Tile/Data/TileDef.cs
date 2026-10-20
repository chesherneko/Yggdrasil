namespace BlueMarble
{
    public class TileDef
    {
        public int Id { get; private set; }
        public TileType Type { get; private set; }
        public TileDirection Dir { get; private set; }

        public Tile Prefab { get; private set; }

        public TileDef(int id, TileType type, TileDirection dir, Tile prefab)
        {
            Id = id;
            Type = type;
            Dir = dir;
            Prefab = prefab;
        }
    }
}