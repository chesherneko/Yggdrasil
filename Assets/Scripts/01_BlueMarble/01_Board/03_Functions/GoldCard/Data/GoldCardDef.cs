using UnityEngine;

namespace BlueMarble
{
    public enum GoldCardPatternType
    {
        GainGold,
        LoseGold,
        GainTreasure,
        LoseTreasure,
        GoTo,
    }

    public enum GoldCardScopeType
    {
        Self,
        All,
        ExceptSelf,
        Random,
    }
    
    public class GoldCardDef
    {
        public int Id { get; private set; }
        public string Description { get; private set; }
        public Sprite Icon { get; private set; }
        
        public GoldCardPatternType PatternType { get; private set; }
        public GoldCardScopeType ScopeType { get; private set; }
        
        public int GoldAmount { get; private set; }
        public int DropTableId { get; private set; } // 이건 나중에 수정
        public int TileId { get; private set; }

        public GoldCardDef(int id, string description, Sprite icon, 
            GoldCardPatternType patternType, GoldCardScopeType scopeType, int goldAmount, int dropTableId, int tileId)
        {
            Id = id;
            Description = description;
            Icon = icon;
            
            PatternType = patternType;
            ScopeType = scopeType;
            
            GoldAmount = goldAmount;
            DropTableId = dropTableId;
            TileId = tileId;
        }
    }
}