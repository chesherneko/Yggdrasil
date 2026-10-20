using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Jay.FileIO;

namespace Utility.Data
{
    public abstract class DataTable<DTO, Def>
    {
        protected abstract string TablePath { get; }

        protected readonly Dictionary<int, Def> defs = new();
        
        public int Count => defs.Count;
        
        public Def Get(int id) => defs[id];
        public IReadOnlyCollection<int> GetIds() => defs.Keys;
        public IReadOnlyCollection<Def> GetDefs() => defs.Values;

        public abstract UniTask LoadAsync();

        protected async UniTask<List<DTO>> GetRows()
            => await TSVReader.ReadTableAsync<DTO>(TablePath, true);
    }
}