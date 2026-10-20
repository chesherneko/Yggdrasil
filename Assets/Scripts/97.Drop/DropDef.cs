namespace BlueMarble
{
    public enum DropType
    {
        Treasure,
        GoldenKey,
        BattleReward
    }

    
    public class DropDef
    {
        public int Id { get; }
        
        public EquipmentGrade Grade { get; } 
        
        public DropType Type { get; }
        
        public float Probability { get; }
        
        public DropDef(int id, EquipmentGrade grade, DropType type, float probability)
        {
            Id = id;
            Grade = grade;
            Type = type;
            Probability = probability;
        }
        
        
    }
    
    
    
}

