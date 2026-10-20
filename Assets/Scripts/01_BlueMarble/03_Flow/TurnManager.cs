using System;
using System.Threading;
using BlueMarble.Server;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
using Utility;

namespace BlueMarble
{
    public class TurnManager : MonoBehaviour
    {
        [field: Title("States")]
        [field: ShowInInspector, ReadOnly] public bool CanRoll { get; private set; }

        public event Action OnDiceRollRequested;
        
        private RoomClient Client => RoomClient.Instance;
        
        private DiceManager dice;

        private void Awake()
        {
            dice = DiceManager.Instance;
            dice.OnRollButtonClicked += RequestRollDice;
        }

        private void OnDisable()
        {
            if (dice != null)
                dice.OnRollButtonClicked -= RequestRollDice;
        }
        
        private void RequestRollDice()
        {
            if (CanRoll == false) return;

            CanRoll = false;
            dice.SetRollButtonActive(false);
            
            OnDiceRollRequested?.Invoke();
        }

        public void SetInputEnabled(bool enabled)
        {
            CanRoll = enabled;
            dice.SetRollButtonActive(enabled);
        }

        public void BeginTurn(bool isMyTurn)
        {
            Debug.Log($"[TurnManager] BeginTurn. IsMyTurn: {isMyTurn}");
            
            CanRoll = isMyTurn;
            dice.ShowForTurn(isMyTurn);
        }

        public async UniTask ShowDiceResultAsync(int d1, int d2)
        {
            CanRoll = false;
            dice.SetRollButtonActive(false);

            await dice.RollAsync(d1, d2);
        }
    }
}
