using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlueMarble
{
    public static class PlayerRegistry
    {
        private static List<Player> all = new();
        
        public static IReadOnlyList<Player> All => all;
        public static int Count => all.Count;

        public static event Action OnUpdated;

        public static void Register(Player player)
        {
            all.Add(player);
            OnUpdated?.Invoke();
        }
        
        public static void Unregister(Player player)
        {
            all.Remove(player);
            OnUpdated?.Invoke();
        }
        
        public static bool IsRegistered(string playerId) => all.Exists(p => p.Id == playerId);

        public static Player GetPlayer(string playerId)
        {
            for (int i = 0; i < all.Count; i++)
            {
                if (playerId == all[i].Id)
                    return all[i];
            }

            Debug.LogError($"[PlayerRegistry] Player not found: {playerId}");
            return null;
        }

        public static bool TryGetPlayer(string id, out Player result)
        {
            result = GetPlayer(id);
            return result;
        }
    }
}