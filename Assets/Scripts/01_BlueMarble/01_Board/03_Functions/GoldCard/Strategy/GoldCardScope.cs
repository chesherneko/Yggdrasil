using System.Collections.Generic;
using System.Linq;
using BlueMarble.Server;
using FantasyGarden.Utility;
using UnityEngine;

namespace BlueMarble
{
    public interface IGoldCardScope
    {
        string[] GetTargetIds(string playerId);
    }

    public class SelfScope : IGoldCardScope
    {
        public string[] GetTargetIds(string playerId) => new[] { playerId };
    }
    
    public class AllScope : IGoldCardScope
    {
        public string[] GetTargetIds(string _) => RoomClient.Instance.PlayerIds.ToArray();
    }
    
    public class ExceptSelfScope : IGoldCardScope
    {
        public string[] GetTargetIds(string playerId)
        {
            RoomClient client = RoomClient.Instance;
            IReadOnlyList<string> playerIds = client.PlayerIds;

            string[] targets = new string[playerIds.Count - 1];

            int targetIndex = 0;

            for (int i = 0; i < playerIds.Count; i++)
            {
                string id = playerIds[i];
                if (id == playerId) continue;

                targets[targetIndex++] = id;
            }

            return targets;
        }
    }
    
    public class RandomScope : IGoldCardScope
    {
        public string[] GetTargetIds(string _)
        {
            RoomClient client = RoomClient.Instance;
            IReadOnlyList<string> playerIds = client.PlayerIds;

            int randCount = Random.Range(1, playerIds.Count);
            return playerIds.GetDistinctRandoms(randCount);
        }
    }
}