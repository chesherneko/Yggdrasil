using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Jay;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utility;

namespace BlueMarble
{
    public class DiceManager : SingletonBase<DiceManager>
    {
        [Title("Dices")]
        [SerializeField] private Dice[] dices;

        [Title("UI")]
        [SerializeField] private Button rollButton;
        [SerializeField] private TMP_Text diceText;
        [SerializeField] private GameObject sfxObject;

        [Title("Settings")]
        [SerializeField] private float jumpPower = 8f;
        [SerializeField] private float jumpDuration = 0.5f;

        public event Action OnRollButtonClicked;

        protected override void OnInitialize()
        {
            for (int i = 0; i < dices.Length; i++)
            {
                dices[i].Initialize();
                dices[i].SetActive(false);
            }

            rollButton.gameObject.SetActive(false);
            rollButton.onClick.AddListener(HandleRollButtonClicked);
        }
        
        public void SetRollButtonActive(bool isActive) => rollButton.gameObject.SetActive(isActive);

        public void ShowForTurn(bool showButton)
        {
            Debug.Log($"[DiceManager] ShowForTurn. ShowButton: {showButton}");

            SetActiveDices(true);
            SetRollButtonActive(showButton);
        }
        
        public async UniTask RollAsync(int d1, int d2)
        {
            sfxObject.SetActive(true);
            
            Sequence r1 = dices[0].CreateRoll(d1, jumpPower, jumpDuration);
            Sequence r2 = dices[1].CreateRoll(d2, jumpPower, jumpDuration);
            
            Sequence sequence = DOTween.Sequence();
            
            await sequence
                .Join(r1)
                .Join(r2)
                .ToUniTask(TweenCancelBehaviour.Complete);
            
            diceText.SetText($"{d1 + d2}");
            diceText.gameObject.SetActive(true);
            
            await CustomUniTask.DelaySeconds(1.5f);

            SetActiveDices(false);
            sfxObject.SetActive(false);
            diceText.gameObject.SetActive(false);
        }

        private void HandleRollButtonClicked()
        {
            SetRollButtonActive(false);
            OnRollButtonClicked?.Invoke();
        }

        private void SetActiveDices(bool isActive)
        {
            for (int i = 0; i < dices.Length; i++)
                dices[i].SetActive(isActive);
        }
    }
}