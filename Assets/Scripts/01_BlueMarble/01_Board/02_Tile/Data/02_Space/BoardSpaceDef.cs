using UnityEngine;

namespace BlueMarble
{
    public enum TileEffectType
    {
        Territory,
        Salary,
        Prison,
        Treasure,
        GoldCard,
        Teleport,
        Shop
    }
    
    public class BoardSpaceDef
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public int Price { get; private set; }

        public TileEffectType EffectType { get; private set; }
        public bool IsActiveOnPass { get; private set; }

        public Sprite Icon { get; private set; }
        public Color Color { get; private set; }
        
        public BoardSpaceDef(int id, string name, int price, 
            TileEffectType effectType, bool isActiveOnPass, Sprite icon, Color color)
        {
            Id = id;
            Name = name;
            Price = price;

            EffectType = effectType;
            IsActiveOnPass = isActiveOnPass;
            
            Icon = icon;
            Color = color;
        }
    }
}