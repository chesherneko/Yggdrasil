using System.Collections.Generic;
using BlueMarble.Server;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BlueMarble
{
    public interface IGoldCardPattern
    {
        UniTask Apply(string[] targetIds, int goldAmount, int dropTableId, int tileId);
    }

    public class GainGoldPattern : IGoldCardPattern
    {
        public UniTask Apply(string[] targetIds, int goldAmount, int _, int tileId)
        {
            EconomyManager economy = EconomyManager.Instance;
            
            List<UniTask> tasks = new();
            
            for (int i = 0; i < targetIds.Length; i++)
            {
                string targetId = targetIds[i];
                tasks.Add(economy.GainGoldTo(targetId, goldAmount));
                
                Debug.Log($"[GoldCardPattern] Gain {goldAmount} gold to {targetId}");
            }

            return UniTask.WhenAll(tasks);
        }
    }

    public class LoseGoldPattern : IGoldCardPattern
    {
        public async UniTask Apply(string[] targetIds, int goldAmount, int _, int tileId)
        {
            EconomyManager economy = EconomyManager.Instance;

            for (int i = 0; i < targetIds.Length; i++)
            {
                string targetId = targetIds[i];
                if (economy.TryGetGold(targetId, out int currentGold) == false) continue;

                int lossAmount = Mathf.Min(currentGold, goldAmount);
                if (lossAmount <= 0) continue;

                await economy.LoseGoldTo(targetId, lossAmount);
                
                Debug.Log($"[GoldCardPattern] Lose {lossAmount} gold to {targetId}");
            }
        }
    }

    public class GainTreasurePattern : IGoldCardPattern
    {
        public UniTask Apply(string[] targetIds, int _, int __, int tileId)
        {
            return TreasureManager.Instance.GrantFromGoldCardAsync(targetIds);
        }
    }

    public class LoseTreasurePattern : IGoldCardPattern
    {
        public UniTask Apply(string[] targetIds, int _, int __, int tileId)
        {
            return TreasureManager.Instance.LoseFromGoldCardAsync(targetIds);
        }
    }

    public class GoToPattern : IGoldCardPattern
    {
        public async UniTask Apply(string[] targetIds, int _, int __, int tileId)
        {
            TeleportManager teleport = TeleportManager.Instance;
            
            List<UniTask<bool>> tasks = new();
            
            for (int i = 0; i < targetIds.Length; i++)
            {
                string targetId = targetIds[i];
                tasks.Add(teleport.MoveToAsync(targetId, tileId));
            }

            await UniTask.WhenAll(tasks);
        }
    }
}
