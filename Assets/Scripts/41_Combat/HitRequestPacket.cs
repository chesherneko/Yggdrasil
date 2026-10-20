using System;

namespace Combat
{
    [Serializable]
    public class HitRequestPacket
    {
        public string CombatId;
        public string CastId;
        public string AttackerId;
        public string TargetId;
        public int Damage;
    }
}
