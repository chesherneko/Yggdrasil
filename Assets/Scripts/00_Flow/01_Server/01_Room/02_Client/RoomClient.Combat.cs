using Combat;
using Cysharp.Threading.Tasks;

namespace BlueMarble.Server
{
    public partial class RoomClient
    {
        private void RegisterCombatHandlers()
        {
            RegisterHandler<CombatOfferMessage>(ProtocolHeader.COMBAT_OFFER, HandleCombatOffer);
            RegisterHandler<CombatCancelledMessage>(ProtocolHeader.COMBAT_CANCELLED, HandleCombatCancelled);
            RegisterHandler<CombatAssignmentMessage>(ProtocolHeader.COMBAT_ASSIGNMENT, HandleCombatAssignment);
            RegisterHandler<CombatStartedMessage>(ProtocolHeader.COMBAT_STARTED, HandleCombatStarted);
            RegisterHandler<CombatPositionMessage>(ProtocolHeader.COMBAT_POSITION, HandleCombatPosition);
            RegisterHandler<SkillCastMessage>(ProtocolHeader.SKILL_CAST, HandleSkillCast);
            RegisterHandler<CombatHealthUpdatedMessage>(
                ProtocolHeader.COMBAT_HEALTH_UPDATED,
                HandleCombatHealthUpdated);
            RegisterHandler<CombatResultMessage>(ProtocolHeader.COMBAT_RESULT, HandleCombatResult);
        }

        public UniTask RequestCombat(CombatRequestMessage message) => SendAsync(message);

        public UniTask RespondCombat(string combatId, bool accepted)
        {
            return SendAsync(new CombatResponseMessage
            {
                CombatId = combatId,
                Accepted = accepted
            });
        }

        public UniTask AnnounceCombatReady(string combatId)
        {
            return SendAsync(new CombatReadyMessage { CombatId = combatId });
        }

        public void RequestSkillCast(SkillCastMessage message)
        {
            SendAsync(message).Forget();
        }

        public void ReportCombatPosition(string combatId, float x)
        {
            SendAsync(new CombatPositionMessage
            {
                CombatId = combatId,
                X = x
            }).Forget();
        }

        public void ReportCombatHealth(string combatId, int remainingHp)
        {
            SendAsync(new CombatHealthReportMessage
            {
                CombatId = combatId,
                RemainingHp = remainingHp
            }).Forget();
        }

        private static void HandleCombatOffer(CombatOfferMessage message)
        {
            CombatSystem.Instance.ReceiveCombatOffer(message);
        }

        private static void HandleCombatCancelled(CombatCancelledMessage message)
        {
            CombatSystem.Instance.CancelCombatFromNetwork(message);
        }

        private static void HandleCombatAssignment(CombatAssignmentMessage message)
        {
            CombatSystem.Instance.PrepareCombatFromNetwork(message);
        }

        private static void HandleCombatStarted(CombatStartedMessage message)
        {
            CombatSystem.Instance.BeginCombatFromNetwork(message).Forget();
        }

        private static void HandleCombatPosition(CombatPositionMessage message)
        {
            CombatSystem.Instance.ApplyPositionFromNetwork(message);
        }

        private static void HandleSkillCast(SkillCastMessage message)
        {
            BattleManager.Instance.ExecuteSkillFromNetwork(message);
        }

        private static void HandleCombatHealthUpdated(CombatHealthUpdatedMessage message)
        {
            CombatSystem.Instance.ApplyHealthFromNetwork(message);
        }

        private static void HandleCombatResult(CombatResultMessage message)
        {
            CombatSystem.Instance.CompleteCombatFromNetwork(message);
        }
    }
}
