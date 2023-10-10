using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace UsefulTools
{
    public static class Extensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            Random random = new Random();
            int n = list.Count;
            for (int i = 0; i < n; i++)
            {
                int j = random.Next(i, n);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // https://www.youtube.com/watch?v=JOABOQMurZo&t=291s
        public static void DeleteChildren(this GameObject gameObject)
        {
            foreach (Transform child in gameObject.transform)
            {
                Object.Destroy(child.gameObject);
            }
        }

        public static void ZeroOutXZ(this Quaternion quaternion)
        {
            // Quaternion black magic
            quaternion[0] = 0f; // Zero out rotation X
            quaternion[2] = 0f; // Zero out rotation Z
        }
        
        public static bool Contains(this LayerMask mask, int layer)
        {
            return mask == (mask | (1 << layer));
        }
    }
}
