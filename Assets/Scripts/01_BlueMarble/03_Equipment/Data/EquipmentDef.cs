namespace BlueMarble
{
    public enum EquipmentGrade
    {
        Common,
        Rare,
        Legendary
    }
    
    public enum AttackType
    {
        Straight,   // 직선
        Parabola,   // 포물선
        Area        // 범위
    }
    
    public class EquipmentDef
    {
        public int Id { get; }
      
        public string Code { get; }
        public string Name { get; }
        
        public EquipmentGrade Grade { get; } 
        public AttackType AttackType { get; } // 공격 타입

        public float BaseDamage { get; }
        public float Range { get; } // 사거리
        public float ProjectileSpeed { get; } // 속도
        public float Radius { get; } // 범위

        public float Cooldown { get; }
        public int MaxDurability { get; } // 최대 내구도
        public int Price { get; } // 가격

        public string PrefabPath { get; } // 프리팹 경로
        public string IconKey { get; } // 아이콘 이미지

        public EquipmentDef(int id, string code, string name,
            EquipmentGrade grade, AttackType attackType,
            float baseDamage, float range, float projectileSpeed, float radius,
            float cooldown, int maxDurability, int price, string prefabPath, string iconKey)

        {
            Id = id;
            Code = code;
            Name = name;
            Grade = grade;
            AttackType = attackType;
            BaseDamage = baseDamage;
            Range = range;
            ProjectileSpeed = projectileSpeed;
            Radius = radius;
            Cooldown = cooldown;
            MaxDurability = maxDurability;
            Price = price;
            PrefabPath = prefabPath;
            IconKey = iconKey;
        }

    }
}