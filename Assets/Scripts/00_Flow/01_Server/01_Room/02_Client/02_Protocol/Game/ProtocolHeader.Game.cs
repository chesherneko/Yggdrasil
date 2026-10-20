namespace BlueMarble.Server
{
    public static partial class ProtocolHeader
    {
        #region CLIENT_TO_SERVER
    
        public const string SET_BOARD_READY = "setBoardReady";
    
        public const string ROLL_DICE = "rollDice";
        public const string UPDATE_ECONOMY = "updateEconomy";
        public const string DECLARE_BANKRUPTCY = "declareBankruptcy";

        public const string DRAW_GOLD_CARD = "drawGoldCard";
        public const string ADD_INCAPACITATION_COUNT = "addIncapacitationCount";
        public const string TURN_FINISHED = "turnFinished";
        public const string SET_EQUIPMENT = "setEquipment";
        public const string REMOVE_EQUIPMENT = "removeEquipment";
        public const string COMBAT_REQUEST = "combatRequest";
        public const string COMBAT_RESPONSE = "combatResponse";
        public const string COMBAT_READY = "combatReady";
        public const string SKILL_CAST = "skillCast";
        public const string COMBAT_HEALTH_REPORT = "combatHealthReport";

        #endregion

        #region BI_DIRECTIONAL

        public const string MOVE_USER_TO = "moveUserTo";
        public const string USER_MOVE_FINISHED = "userMoveFinished";
        public const string TILE_EFFECT_SYNC = "tileEffectSync";
        public const string UPDATE_TERRITORY = "updateTerritory";
        public const string APPLY_TREASURE = "applyTreasure";
        public const string TREASURE_RESOLVED = "treasureResolved";
        public const string PLAYER_ACTIVITY = "playerActivity";
        public const string COMBAT_POSITION = "combatPosition";
        public const string REVEAL_GOLD_CARD = "revealGoldCard";
        public const string GOLD_CARD_PRESENTATION_FINISHED = "goldCardPresentationFinished";

        #endregion

        #region SERVER_TO_CLIENT
    
        public const string TURN_STARTED = "turnStarted";
        public const string DICE_ROLLED = "diceRolled";
        public const string ECONOMY_UPDATED = "economyUpdated";
    
        public const string GOLD_CARD_DRAWN = "goldCardDrawn";
        public const string INVENTORY_UPDATED = "inventoryUpdated";
        public const string USER_MOVE_CANCELLED = "userMoveCancelled";
        public const string PLAYER_BANKRUPTED = "playerBankrupted";
    
        public const string GAME_ENDED = "gameEnded";
        public const string COMBAT_OFFER = "combatOffer";
        public const string COMBAT_CANCELLED = "combatCancelled";
        public const string COMBAT_ASSIGNMENT = "combatAssignment";
        public const string COMBAT_STARTED = "combatStarted";
        public const string COMBAT_HEALTH_UPDATED = "combatHealthUpdated";
        public const string COMBAT_RESULT = "combatResult";

        #endregion
    }
}
