namespace BlueMarble
{
    /// <summary>
    /// EquipmentDef에는 별도의 "효과 설명" 텍스트가 없기 때문에,
    /// AttackType에 따라 원본 스탯(BaseDamage, Range, ProjectileSpeed, Radius)을
    /// 조합해서 사람이 읽는 문자열로 만들어주는 헬퍼.
    /// </summary>
    public static class EquipmentDisplayFormatter
    {
        public static string GetEffectText(EquipmentDef def)
        {
            switch (def.AttackType)
            {
                case AttackType.Straight:
                    return $"직선 공격 · 피해 {def.BaseDamage:0} · 사거리 {def.Range:0.#}";

                case AttackType.Parabola:
                    return $"포물선 공격 · 피해 {def.BaseDamage:0} · 사거리 {def.Range:0.#} · 속도 {def.ProjectileSpeed:0.#}";

                case AttackType.Area:
                    return $"범위 공격 · 피해 {def.BaseDamage:0} · 범위 {def.Radius:0.#}";

                default:
                    return string.Empty;
            }
        }

        public static string GetCooldownText(EquipmentDef def) => $"쿨타임 {def.Cooldown:0.0}초";

        public static string GetDurabilityText(EquipmentDef def) => $"내구도 {def.MaxDurability}";

        public static string GetPriceText(EquipmentDef def) => $"{def.Price}";

        public static string GetPriceText(int price) => $"{price}";
    }
}