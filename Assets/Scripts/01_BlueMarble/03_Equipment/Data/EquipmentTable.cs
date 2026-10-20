using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Utility.Data;

namespace BlueMarble
{
    public class EquipmentTable : DataTable<EquipmentDTO, EquipmentDef>
    {
        protected override string TablePath => "Equipment/equipments";

        public override async UniTask LoadAsync()
        {
            defs.Clear();
            List<EquipmentDTO> rows = await GetRows();

            for (int i = 0; i < rows.Count; i++)
            {
                EquipmentDTO dto = rows[i];

                EquipmentGrade grade = Enum.Parse<EquipmentGrade>(dto.Grade);
                AttackType attackType = Enum.Parse<AttackType>(dto.AttackType);

                defs[dto.Id] = new EquipmentDef(
                    dto.Id, dto.Code, dto.Name,
                    grade, attackType,
                    dto.BaseDamage, dto.Range, dto.ProjectileSpeed, dto.Radius,
                    dto.Cooldown, dto.MaxDurability, dto.Price, dto.Prefab, dto.IconKey);
            }
        }
    }
}