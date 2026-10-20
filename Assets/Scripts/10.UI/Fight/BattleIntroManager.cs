using Cysharp.Threading.Tasks;
using Jay;
using UnityEngine;

namespace Combat
{
    public class BattleIntroManager : SingletonBase<BattleIntroManager>
    {
        // Combat 씬의 BattleVSIntroFixedScale이 Awake/OnEnable에서 자기 자신을 등록
        [SerializeField] private BattleVSIntroFixedScale currentIntro;

        public void Register(BattleVSIntroFixedScale intro)
        {
            currentIntro = intro;
        }

        public void Unregister(BattleVSIntroFixedScale intro)
        {
            if (currentIntro == intro)
                currentIntro = null;
        }

        // 외부(CombatSystem 등)에서 호출하는 진입점.
        // Combat 씬이 로드되고 인트로 오브젝트가 등록된 상태에서 호출되어야 함.
        public async UniTask PlayAsync()
        {
            if (currentIntro == null)
            {
                Debug.LogWarning("[BattleIntroManager] 등록된 배틀 인트로가 없습니다.");
                return;
            }

            currentIntro.gameObject.SetActive(true);

            await currentIntro.PlayAsync();

            currentIntro.gameObject.SetActive(false);
        }
    }
}