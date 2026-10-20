using Cysharp.Threading.Tasks;
using BlueMarble;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace BlueMarble
{
    public class Tile : MonoBehaviour
    {
        [field: Title("Components")]
        [field: SerializeField] public SpriteRenderer MainRenderer { get; private set; }
        [field: SerializeField] public TMP_Text AreaName { get; private set; }
        [field: SerializeField] public SpriteRenderer AreaIconRenderer { get; private set; }
        [field: SerializeField] public SpriteRenderer RegionRenderer { get; private set; }
        [SerializeField] private Transform territoryVisualRoot;

        public TileDef Def { get; private set; }
        public BoardSpaceDef SpaceDef { get; private set; }

        public TileState State { get; private set; } = new(); // 타일 상태

        private ITileEffect effect;
        private GameObject territoryVisual;

        public void Initialize(TileDef def, BoardSpaceDef spaceDef)
        {
            Def = def;
            SpaceDef = spaceDef;

            State.Clear();

            effect = TileEffectFactory.Create(SpaceDef.EffectType);
        }

        public void UpdateVisuals()
        {
            AreaName.SetText(SpaceDef.Name);
            AreaIconRenderer.sprite = SpaceDef.Icon;
            if (RegionRenderer != null) RegionRenderer.color = SpaceDef.Color;
        }

        public void SetTerritoryVisual(GameObject prefab, Color color)
        {
            if (territoryVisual != null)
            {
                territoryVisual.SetActive(false);
                Destroy(territoryVisual);
                territoryVisual = null;
            }

            if (prefab == null || territoryVisualRoot == null) return;

            territoryVisual = Instantiate(prefab, territoryVisualRoot);
            territoryVisual.transform.localPosition = Vector3.zero;
            territoryVisual.transform.localRotation = Quaternion.identity;
            territoryVisual.transform.localScale = Vector3.one;

            if (territoryVisualRoot.lossyScale.y < 0f)
            {
                Vector3 scale = territoryVisual.transform.localScale;
                scale.y *= -1f;
                territoryVisual.transform.localScale = scale;
            }

            SpriteRenderer[] renderers =
                territoryVisual.GetComponentsInChildren<SpriteRenderer>();

            for (int i = 0; i < renderers.Length; i++)
            {
                Color rendererColor = color;
                rendererColor.a *= renderers[i].color.a;
                renderers[i].color = rendererColor;
            }
        }

        public async UniTask ExecuteEffect(string playerId)
        {
            await effect.Apply(playerId, this);
        }
    }
}
