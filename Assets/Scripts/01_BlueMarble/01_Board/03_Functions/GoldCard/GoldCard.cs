using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DG.Tweening;
using Study.Utilities;
using TMPro;
using UnityEngine.InputSystem;

namespace BlueMarble
{
    public class GoldCard : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private SpriteRenderer iconRenderer;
        [SerializeField] private TMP_Text descriptionText;
        
        [Header("Flip VFX")]
        [SerializeField] private ParticleSystem preFlipVfx;         // 뒤집기 시작 시 재생 
        [SerializeField] private ParticleSystem durationFlipVfx;    // 뒤집을 때 재생 
        [SerializeField] private ParticleSystem resultFlipVfx;      // 뒤집기 완료 시 재생 

        [Header("Flip Settings")]
        [SerializeField] private float flipDuration = 0.25f;

        private bool clickable = false;
        private Tween flipTween;

        public GoldCardDef Def { get; private set; }
        public event Action Clicked;

        private void OnDisable()
        {
            Clear(); // 오브젝트 비활성화 시 트윈/파티클 정리 (씬 전환, 팝업 닫힘 등)
        }

        public void Initialize(GoldCardDef def)
        {
            Def = def;
            iconRenderer.sprite = def.Icon == null ? iconRenderer.sprite : def.Icon;
            descriptionText.text = def.Description;

            ResetVisual();
        }
        
        /// <summary>
        /// 팝업이 닫히거나 카드가 재사용될 때 외부(팝업 컨트롤러)에서 호출.
        /// 파티클 잔상 없이 완전히 초기화한다.
        /// </summary>
        public void ResetVisual()
        {
            Clear();
            PlayVfx(preFlipVfx);
        }

        public void SetClickable(bool value)
        {
            clickable = value;
        }

        public void OnClicked()
        {
            if (clickable == false) return;

            clickable = false;
            Clicked?.Invoke();
        }

        public async UniTask FlipAsync()
        {
            StopVfx(preFlipVfx);
            PlayVfx(durationFlipVfx);

            flipTween = transform.DORotate(new Vector3(0, 180f, 0), flipDuration);

            await flipTween.ToUniTask();

            StopVfx(durationFlipVfx);
            PlayVfx(resultFlipVfx);
        }

        private void PlayVfx(ParticleSystem vfx)
        {
            if (vfx == null) return;
            vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            vfx.Play(true);
        }

        private void StopVfx(ParticleSystem vfx)
        {
            if (vfx == null) return;
            vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Clear()
        {
            flipTween?.Kill();

            clickable = false;
            transform.rotation = Quaternion.identity;
            
            StopVfx(preFlipVfx);
            StopVfx(resultFlipVfx);
            StopVfx(durationFlipVfx);
        }
    }
}
