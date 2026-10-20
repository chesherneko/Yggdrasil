using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Utility.Data;

namespace BlueMarble
{
    public class DropTable : DataTable<DropDTO, DropDef>
    {
        protected override string TablePath => "Equipment/drop";

        public override async UniTask LoadAsync()
        {
            defs.Clear();
            List<DropDTO> rows = await GetRows();

            for (int i = 0; i < rows.Count; i++)
            {
                DropDTO dto = rows[i];

                DropType type = Enum.Parse<DropType>(dto.Type);
                EquipmentGrade grade = Enum.Parse<EquipmentGrade>(dto.Grade);
                
                defs[dto.Id] = new DropDef(dto.Id, grade, type, dto.probability );
            }
        }
    }
}