using System;

namespace BlueMarble
{
    public static class GoldCardScopeFactory
    {
        public static IGoldCardScope Create(GoldCardScopeType scopeType)
        {
            return scopeType switch
            {
                GoldCardScopeType.Self => new SelfScope(),
                GoldCardScopeType.All => new AllScope(),
                GoldCardScopeType.ExceptSelf => new ExceptSelfScope(),
                GoldCardScopeType.Random => new RandomScope(),
                _ => throw new NotImplementedException()
            };
        }
    }
}