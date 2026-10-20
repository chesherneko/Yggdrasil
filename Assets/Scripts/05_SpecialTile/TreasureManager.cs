using BlueMarble.Server;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BlueMarble
{
    /// <summary>
    /// E5 보물창고 효과를 처리하는 매니저.
    /// DropService(가중 랜덤 뽑기)와 InventoryManager(지급)를 연결한다.
    /// TreasureEffect에서 Instance로 호출한다.
    /// </summary>
    public class TreasureManager
    {
        public static TreasureManager Instance { get; } = new TreasureManager();

        private readonly Dictionary<string, UniTaskCompletionSource> waiters = new();
        private TreasureRewardPopupController presenter;

        private TreasureManager()
        {
        }

        /// <summary>
        /// 보물창고 UI 팝업(presenter)을 등록한다. 씬 진입 시 팝업 프리팹 쪽에서 1회 호출한다.
        /// </summary>
        public void SetPresenter(TreasureRewardPopupController presenter)
        {
            this.presenter = presenter;
        }

        public void ClearPresenter(TreasureRewardPopupController presenter)
        {
            if (this.presenter == presenter)
                this.presenter = null;
        }

        /// <summary>
        /// 보물창고 드롭 테이블에서 장비 하나를 뽑아 플레이어 인벤토리에 지급한다.
        /// 인벤토리가 가득 찬 경우 InventoryManager가 자동으로 교체 대기 상태로 전환한다.
        /// 서버가 대상 플레이어에게 지급을 중계하고, 대상의 연출/교체 결정이 끝날 때까지 기다린다.
        /// </summary>
        /// <param name="playerId">지급 대상 플레이어</param>
        public async UniTask GrantAsync(string playerId)
        {
            EquipmentDef def = DropService.DrawEquipment(DropType.Treasure);

            await RequestEffectAndWaitAsync(
                playerId,
                GoldCardTreasureEffectType.Gain,
                def.Id);

            Debug.Log($"[TreasureManager] {playerId}: 보물창고에서 {def.Name}({def.Grade}) 획득");
        }

        public UniTask GrantFromGoldCardAsync(string[] targetIds)
        {
            UniTask[] tasks = new UniTask[targetIds.Length];

            for (int i = 0; i < targetIds.Length; i++)
            {
                string targetId = targetIds[i];
                EquipmentDef equipment = DropService.DrawEquipment(DropType.GoldenKey);

                tasks[i] = RequestEffectAndWaitAsync(
                    targetId,
                    GoldCardTreasureEffectType.Gain,
                    equipment.Id);

                Debug.Log($"[TreasureManager] Gold Card: Gain {equipment.Name} to {targetId}");
            }

            return UniTask.WhenAll(tasks);
        }

        public UniTask LoseFromGoldCardAsync(string[] targetIds)
        {
            UniTask[] tasks = new UniTask[targetIds.Length];

            for (int i = 0; i < targetIds.Length; i++)
            {
                tasks[i] = RequestEffectAndWaitAsync(
                    targetIds[i],
                    GoldCardTreasureEffectType.Lose,
                    -1);
            }

            return UniTask.WhenAll(tasks);
        }

        public UniTask PresentAsync(EquipmentDef reward)
        {
            if (presenter != null)
                return presenter.ShowRewardAsync(reward);

            Debug.LogError("[TreasureManager] 보상 UI가 등록되지 않았습니다.");
            return UniTask.CompletedTask;
        }

        public void CompleteRequest(string requestId)
        {
            if (waiters.TryGetValue(requestId, out UniTaskCompletionSource waiter))
                waiter.TrySetResult();
        }

        public void Reset()
        {
            List<UniTaskCompletionSource> pendingWaiters = new(waiters.Values);

            waiters.Clear();
            presenter = null;

            for (int i = 0; i < pendingWaiters.Count; i++)
                pendingWaiters[i].TrySetResult();
        }

        private async UniTask RequestEffectAndWaitAsync(
            string targetId,
            GoldCardTreasureEffectType effectType,
            int equipmentId)
        {
            string requestId = Guid.NewGuid().ToString("N");
            var waiter = new UniTaskCompletionSource();

            waiters.Add(requestId, waiter);

            try
            {
                await RoomClient.Instance.RequestApplyTreasure(
                    requestId,
                    targetId,
                    effectType,
                    equipmentId);

                await waiter.Task;
            }
            finally
            {
                waiters.Remove(requestId);
            }
        }
    }
}
