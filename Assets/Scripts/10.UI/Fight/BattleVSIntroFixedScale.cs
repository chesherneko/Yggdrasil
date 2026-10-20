using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Utility;

namespace Combat
{
    public class BattleVSIntroFixedScale : MonoBehaviour
    {
        [Header("1. 순서대로 등장시킬 단어들 (GET, READY, BATTLE 등)")]
        public GameObject[] wordObjects;

        [Header("2. 단어 등장 연출 세팅")]
        [Tooltip("단어가 나타날 때의 연출 속도 (초)")]
        public float scaleDuration = 0.15f;
        [Tooltip("원본 크기 대비 튀어나오는 배율 (1.1 = 원본의 110% 크기로 튕김)")]
        public float punchScaleAmount = 1.15f;

        [Header("3. 카메라 & 이펙트 세팅")]
        public Camera mainCamera;
        public float shakeDuration = 0.4f;
        public float shakeMagnitude = 0.3f;
        public ParticleSystem finalVFX;
        public AudioSource audioSource;
        public AudioClip hitSFX;
        public AudioClip finalExplosionSFX;

        private Vector3 originalCamPos;
        private readonly List<Vector3> originalWordScales = new();

        // 외부(BattleIntroManager)에서 호출하는 진입점.
        // 연출이 완전히 끝날 때까지 await로 대기 가능
        [Button]
        public async UniTask PlayAsync()
        {
            PrepareCamera();
            PrepareWords();

            await PlaySequenceAsync();
        }

        private void PrepareCamera()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera != null) originalCamPos = mainCamera.transform.localPosition;
        }

        private void PrepareWords()
        {
            originalWordScales.Clear();

            foreach (GameObject word in wordObjects)
            {
                if (word == null)
                {
                    originalWordScales.Add(Vector3.one);
                    continue;
                }

                // 인스펙터에 설정된 원본 크기 저장 후, 연출 전이므로 크기 0으로 초기화
                originalWordScales.Add(word.transform.localScale);

                word.SetActive(true);
                word.transform.localScale = Vector3.zero;
            }
        }

        private async UniTask PlaySequenceAsync()
        {
            // 1단계: 단어를 하나씩 원본 크기 기준으로 Scale 연출
            for (int i = 0; i < wordObjects.Length; i++)
            {
                GameObject word = wordObjects[i];
                if (word == null) continue;

                Vector3 targetOriginalScale = originalWordScales[i];

                PunchScaleAsync(word.transform, targetOriginalScale).Forget();

                if (audioSource != null && hitSFX != null)
                    audioSource.PlayOneShot(hitSFX);

                CameraShakeAsync(0.1f, 0.1f).Forget();

                await UniTask.WaitForSeconds(0.35f);
            }

            // 2단계: 피날레
            if (finalVFX != null) finalVFX.Play();
            if (audioSource != null && finalExplosionSFX != null)
                audioSource.PlayOneShot(finalExplosionSFX);

            await CameraShakeAsync(shakeDuration, shakeMagnitude);

            await CustomUniTask.DelaySeconds(2.5f);
        }

        private async UniTask PunchScaleAsync(Transform targetTransform, Vector3 originalScale)
        {
            float elapsed = 0f;
            Vector3 maxScale = originalScale * punchScaleAmount;

            // 0에서 원본보다 살짝 큰 크기(maxScale)로 확대
            while (elapsed < scaleDuration)
            {
                elapsed += Time.deltaTime;
                float percent = elapsed / scaleDuration;
                targetTransform.localScale = Vector3.Lerp(Vector3.zero, maxScale, percent);
                await UniTask.Yield();
            }

            // maxScale에서 원래 크기(originalScale)로 정착
            elapsed = 0f;
            float returnDuration = scaleDuration * 0.5f;

            while (elapsed < returnDuration)
            {
                elapsed += Time.deltaTime;
                float percent = elapsed / returnDuration;
                targetTransform.localScale = Vector3.Lerp(maxScale, originalScale, percent);
                await UniTask.Yield();
            }

            targetTransform.localScale = originalScale;
        }

        private async UniTask CameraShakeAsync(float duration, float magnitude)
        {
            if (mainCamera == null) return;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;

                mainCamera.transform.localPosition = originalCamPos + new Vector3(x, y, 0);
                elapsed += Time.deltaTime;
                await UniTask.Yield();
            }

            mainCamera.transform.localPosition = originalCamPos;
        }
    }
}