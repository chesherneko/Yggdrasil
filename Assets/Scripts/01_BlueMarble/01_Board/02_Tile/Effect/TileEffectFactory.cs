namespace BlueMarble
{
    public static class TileEffectFactory
    {
        public static ITileEffect Create(TileEffectType type)
        {
            return type switch
            {
                TileEffectType.Territory => new TerritoryEffect(),
                TileEffectType.Salary => new SalaryEffect(),
                TileEffectType.Prison => new PrisonEffect(),
                TileEffectType.Treasure => new TreasureEffect(),
                TileEffectType.GoldCard => new GoldCardEffect(),
                TileEffectType.Teleport => new TeleportEffect(),
                TileEffectType.Shop => new ShopEffect(),
                _ => throw new System.NotImplementedException()
            };
        }
    }
}