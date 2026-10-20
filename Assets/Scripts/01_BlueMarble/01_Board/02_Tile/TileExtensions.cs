using System;
using UnityEngine;

namespace BlueMarble
{
    public static class TileExtensions
    {
        public static Vector3 CalculateDistFrom(this Tile tile, Tile origin, TileDirection dir, Vector3 offset)
        {
            SpriteRenderer sr = tile.MainRenderer;
            SpriteRenderer osr = origin.MainRenderer;

            Vector3 move = dir switch
            {
                TileDirection.South => Vector3.left,
                TileDirection.West => Vector3.up,
                TileDirection.North => Vector3.right,
                TileDirection.East => Vector3.down,
                _ => throw new ArgumentOutOfRangeException()
            };
            Vector3 dist = osr.bounds.extents + sr.bounds.extents - offset;
            
            return osr.transform.position + Vector3.Scale(move, dist);
        }
    }
}