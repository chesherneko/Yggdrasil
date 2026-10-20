using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FantasyGarden.Utility
{
    public static class RandomExtensions
    {
        /// <summary>
        /// 목록에서 무작위로 값을 반환
        /// </summary>
        public static T GetRandom<T>(this IReadOnlyList<T> items)
        {
            int randIdx = Random.Range(0, items.Count);
            return items[randIdx];
        }

        /// <summary>
        /// 목록에서 조건을 만족하는 무작위 값을 반환
        /// </summary>
        public static T GetRandom<T>(this IReadOnlyList<T> items, Func<T, bool> predicate)
        {
            int idx = -1;
            int matchCount = 0;

            for (int i = 0; i < items.Count; i++)
            {
                T item = items[i];
                if (predicate(item) == false) continue;

                matchCount++;
                if (Random.Range(0, matchCount) == 0) idx = i;
            }

            return items[idx];
        }
        
        #region WEIGHTED_RANDOM

        /// <summary>
        /// 가중치에 따라 무작위 인덱스를 반환
        /// </summary>
        public static int GetWeightedRandom(this IReadOnlyList<float> weights)
        {
            float total = weights.Sum();

            if (total <= 0f)
            {
                Debug.LogError("[RandomExtensions] Total weight must be greater than zero.");
                return -1;
            }

            float randomPoint = Random.value * total;

            for (int i = 0; i < weights.Count; i++)
            {
                randomPoint -= weights[i];
                if (randomPoint < 0) return i;
            }
            
            return weights.Count - 1;
        }

        /// <summary>
        /// 가중치에 따라 목록에서 무작위로 값을 반환
        /// </summary>
        public static T GetWeightedRandom<T>(this IReadOnlyList<T> items, IReadOnlyList<float> weights)
        {
            int randIdx = GetWeightedRandom(weights);
            return items[randIdx];
        }
            
        #endregion
        
        #region DISTINCT_RANDOM

        /// <summary>
        /// 목록에서 중복된 값과 제외할 값을 제외하고 무작위로 값을 반환
        /// </summary>
        public static T GetDistinctRandom<T>(this IReadOnlyList<T> items, IReadOnlyList<T> excluded = null)
        {
            T result = default;
            
            HashSet<T> seen = new();

            // 제외할 후보는 미리 HashSet에 추가
            if (excluded != null)
            {
                for (int i = 0; i < excluded.Count; i++)
                    seen.Add(excluded[i]);
            }
            
            int count = 0;
            
            for (int i = 0; i < items.Count; i++)
            {
                T item = items[i];

                // HashSet에 이미 추가된 (중복된) 값일 경우, 스킵
                if (seen.Add(item) == false) continue;

                count++;
                if (Random.Range(0, count) == 0) result = item;
            }

            return result;
        }

        /// <summary>
        /// 목록에서 중복된 값과 제외할 값을 제외하고 무작위로 값을 일정 개수 반환
        /// </summary>
        public static T[] GetDistinctRandoms<T>(this IReadOnlyList<T> items, int count, IReadOnlyList<T> excluded = null)
        {
            // 뽑아야 할 값의 개수가 0 이하일 경우
            if (count <= 0) return Array.Empty<T>(); 
            
            HashSet<T> seen = excluded == null
                ? new HashSet<T>()
                : new HashSet<T>(excluded);

            List<T> candidates = new();

            for (int i = 0; i < items.Count; i++)
            {
                T item = items[i];
                
                // HashSet에 추가된 (중복되지 않은) 값만 후보군에 등록
                if (seen.Add(item)) candidates.Add(item);
            }
            
            // 뽑아야 할 수가, 후보군 수보다 적을 경우
            if (count > candidates.Count) return Array.Empty<T>();

            T[] result = new T[count];

            for (int i = 0; i < count; i++)
            {
                int randIdx = Random.Range(i, candidates.Count);
                
                T temp = candidates[i];
                
                // 뽑은 값을 제외하기 위해, i번째 인덱스와 randIdx의 값을 교대
                candidates[i] = candidates[randIdx];
                candidates[randIdx] = temp;
                
                // 뽑은 랜덤 값을 결과에 반영
                result[i] = candidates[i];
            }
            
            return result;
        }
        
        #endregion
    }
}
