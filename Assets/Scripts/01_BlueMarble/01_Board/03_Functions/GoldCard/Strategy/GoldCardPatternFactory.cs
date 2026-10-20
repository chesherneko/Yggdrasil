namespace BlueMarble
{
    public static class GoldCardPatternFactory
    {
        public static IGoldCardPattern Create(GoldCardPatternType patternType)
        {
            return patternType switch
            {
                GoldCardPatternType.GainGold => new GainGoldPattern(),
                GoldCardPatternType.LoseGold => new LoseGoldPattern(),
                GoldCardPatternType.GainTreasure => new GainTreasurePattern(),
                GoldCardPatternType.LoseTreasure => new LoseTreasurePattern(),
                GoldCardPatternType.GoTo => new GoToPattern(),
                _ => throw new System.NotImplementedException()
            };
        }
    }
}