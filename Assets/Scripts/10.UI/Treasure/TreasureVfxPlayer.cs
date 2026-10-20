using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;

namespace BlueMarble
{
    public class TreasureVfxPlayer : MonoBehaviour
    {
        [Serializable]
        private struct GradeVfxEntry
        {
            public EquipmentGrade Grade;
            public ParticleSystem Prefab;
        }

        [SerializeField, ListDrawerSettings(ShowFoldout = false)]
        private List<GradeVfxEntry> vfxByGrade = new();

        [SerializeField] private Transform spawnAnchor;

        public async UniTask PlayAsync(EquipmentGrade grade)
        {
            Debug.Log($"[TreasureVfxPlayer] PlayAsync 시작. grade={grade}, " +
                      $"spawnAnchor={(spawnAnchor == null ? "NULL" : spawnAnchor.name)}, " +
                      $"vfxByGrade.Count={vfxByGrade?.Count.ToString() ?? "NULL LIST"}");

            if (spawnAnchor == null)
            {
                Debug.LogError("[TreasureVfxPlayer] spawnAnchor가 null입니다. Inspector에서 다시 연결해주세요.");
                return;
            }

            if (vfxByGrade == null || vfxByGrade.Count == 0)
            {
                Debug.LogError("[TreasureVfxPlayer] vfxByGrade 리스트가 비어있습니다. 항목을 추가해주세요.");
                return;
            }

            ParticleSystem prefab = vfxByGrade.FirstOrDefault(e => e.Grade == grade).Prefab;

            if (prefab == null)
            {
                Debug.Log($"[TreasureVfxPlayer] {grade} 등급 VFX가 없어 기본값으로 대체합니다.");
                prefab = vfxByGrade.FirstOrDefault().Prefab;
            }

            if (prefab == null)
            {
                Debug.LogError("[TreasureVfxPlayer] 등록된 VFX 프리팹이 없습니다. vfxByGrade의 Prefab 필드를 확인해주세요.");
                return;
            }

            Debug.Log($"[TreasureVfxPlayer] {prefab.name} 인스턴스화 시도");

            ParticleSystem instance = Instantiate(prefab, spawnAnchor.position, Quaternion.identity);
            instance.Play();

            float duration = instance.main.duration + instance.main.startLifetime.constantMax;
            const float maxWaitSeconds = 3f; // 루프형 프리팹이 잘못 들어와도 팝업이 무한 대기하지 않도록 상한선
            duration = Mathf.Min(duration, maxWaitSeconds);

            Debug.Log($"[TreasureVfxPlayer] 재생 시작. {duration}초 후 정리");

            await UniTask.Delay(TimeSpan.FromSeconds(duration));

            Destroy(instance.gameObject);
        }
    }
}