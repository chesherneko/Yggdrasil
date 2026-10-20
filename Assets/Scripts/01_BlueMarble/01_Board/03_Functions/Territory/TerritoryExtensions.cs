namespace BlueMarble
{
    public static class TerritoryExtensions
    {
        private static GameSettings Settings => GameSettings.Instance;

        public static int GetLandPrice(this Tile tile)
        {
            return tile.SpaceDef.Price;
        }

        public static int GetBuildingPrice(this Tile tile)
        {
            return ToGold(tile.GetLandPrice() * Settings.BuildingPriceMultiplier);
        }

        public static int GetLandmarkPrice(this Tile tile)
        {
            return ToGold(tile.GetLandPrice() * Settings.LandmarkPriceMultiplier);
        }

        public static int GetMinimumPurchasePrice(this Tile tile)
        {
            TileState state = tile.State;

            if (state.IsOccupied == false) return tile.GetLandPrice();
            if (state.HasBuilding == false) return tile.GetBuildingPrice();
            if (state.HasLandMark == false) return tile.GetLandmarkPrice();

            return 0;
        }

        public static int GetPurchasePrice(this Tile tile, TerritoryBill bill)
        {
            int totalPrice = 0;

            if (bill.IsBuyTile) totalPrice += tile.GetLandPrice();
            if (bill.IsBuyBuilding) totalPrice += tile.GetBuildingPrice();
            if (bill.IsBuyLandMark) totalPrice += tile.GetLandmarkPrice();

            return totalPrice;
        }

        public static int GetTotalValue(this Tile tile)
        {
            TileState state = tile.State;
            int totalValue = tile.GetLandPrice();

            if (state.HasBuilding || state.HasLandMark)
                totalValue += tile.GetBuildingPrice();

            if (state.HasLandMark)
                totalValue += tile.GetLandmarkPrice();

            return totalValue;
        }

        public static int GetBankruptcySaleValue(this Tile tile)
        {
            return ToGold(tile.GetTotalValue() * 0.8f);
        }

        public static int GetTollAmount(this Tile tile)
        {
            return ToGold(tile.GetTotalValue() * GetTollRate(tile.State));
        }

        private static float GetTollRate(TileState state)
        {
            if (state.HasLandMark) return Settings.LandmarkTollRate;
            if (state.HasBuilding) return Settings.BuildingTollRate;

            return Settings.LandTollRate;
        }

        private static int ToGold(float value)
        {
            return UnityEngine.Mathf.RoundToInt(value);
        }
    }
}
