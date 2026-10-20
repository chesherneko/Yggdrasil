using Cysharp.Threading.Tasks;
using Jay;
using BlueMarble.Server;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BlueMarble
{
    public class ShopManager : SingletonBase<ShopManager>
    {
        [Title("References")]
        [SerializeField] private ShopUIController ui;

        private RoomClient Client => RoomClient.Instance;

        public async UniTask OpenAsync(string playerId)
        {
            await Client.AnnouncePlayerActivity(PlayerActivityType.Shop);

            try
            {
                await ui.OpenShopAsync(playerId);
            }
            finally
            {
                await Client.AnnouncePlayerActivity(PlayerActivityType.None);
            }
        }
    }
}
