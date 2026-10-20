using System;
using UnityEngine;

namespace Combat
{
    // 대결 제한 시간 관리하는 컴포넌트
    public class Timer : MonoBehaviour
    {
        [Header("Settings")]
        public float duration = 30f;

        public float RemainingTime { get; private set; }
        public bool  IsRunning     { get; private set; } = false;
        
        public event Action OnTimeUp;

        public void StartTimer()
        {
            RemainingTime = duration;
            IsRunning = true;
        }

        public void StopTimer()
        {
            IsRunning = false;
        }

        private void Update()
        {
            if (!IsRunning) return;

            RemainingTime -= Time.deltaTime;

            if (RemainingTime <= 0f)
            {
                RemainingTime = 0f;
                IsRunning = false;
                OnTimeUp?.Invoke();
            }
        }
    }
}
