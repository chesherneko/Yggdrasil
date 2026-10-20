using System;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BlueMarble
{
    [Serializable]
    public class Dice
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3[] faceRotations; // 주사위 6면의 회전값

        private Vector3 landingPosition; // 착지 위치

        public void Initialize()
        {
            landingPosition = target.localPosition;
        }

        public void SetActive(bool isActive)
        {
            target.gameObject.SetActive(isActive);
            
            // 주사위 오브젝트가 비활성화 되었을 때, 눈금을 1로 돌림
            if (isActive == false)
                target.localEulerAngles = faceRotations[0];
        }

        public Sequence CreateRoll(int value, float jumpPower, float duration)
        {
            Vector3 faceRotation = faceRotations[value - 1];
            
            Vector3 rollingRotation = 
                faceRotation + 
                new Vector3(GetRandomAxis(), GetRandomAxis(), GetRandomAxis());

            target.localPosition = landingPosition;
            target.gameObject.SetActive(true);

            Sequence sequence = DOTween.Sequence();
            sequence
                .Join(target.DOLocalJump(landingPosition, jumpPower, 1, duration))
                .Join(target
                    .DOLocalRotate(rollingRotation, duration, RotateMode.FastBeyond360)
                    .SetEase(Ease.OutCubic))
                .AppendCallback(() =>
                {
                    target.localPosition = landingPosition;
                    target.localEulerAngles = faceRotation;
                })
                .Append(target.DOPunchScale(Vector3.one * 0.08f, 0.12f, 1, 0.3f));

            return sequence;

            static float GetRandomAxis() => 360f * Random.Range(1, 2);
        }
    }
}