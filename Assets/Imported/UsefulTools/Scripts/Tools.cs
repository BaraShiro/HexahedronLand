using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UsefulTools
{
    public static class Tools
    {
        private static Camera _camera;
        public static Camera MainCamera => _camera ??= Camera.main;
        
        private static readonly Dictionary<float, WaitForSeconds> WaitDictionary = new Dictionary<float, WaitForSeconds>();

        // https://www.youtube.com/watch?v=JOABOQMurZo&t=65s
        public static WaitForSeconds GetWaitForSeconds(float time)
        {
            if (WaitDictionary.TryGetValue(time, out WaitForSeconds wait)) return wait;

            WaitDictionary[time] = new WaitForSeconds(time);
            return WaitDictionary[time];
        }

        // https://www.youtube.com/watch?v=JOABOQMurZo&t=210s
        public static Vector3 GetWorldPositionOfCanvasElement(RectTransform element)
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(element, element.position, MainCamera, out Vector3 result);
            return result;
        }
        
        public static float ClampAngle(float angle, float min, float max)
        {
            angle = NormalizeAngle(angle);
            if (angle > 180)
            {
                angle -= 360;
            }
            else if (angle < -180)
            {
                angle += 360;
            }
 
            min = NormalizeAngle(min);
            if (min > 180)
            {
                min -= 360;
            }
            else if (min < -180)
            {
                min += 360;
            }
 
            max = NormalizeAngle(max);
            if (max > 180)
            {
                max -= 360;
            }
            else if (max < -180)
            {
                max += 360;
            }
 
            return Mathf.Clamp(angle, min, max);
        }
        
        public static float NormalizeAngle(float angle)
        {
            while (angle > 360)
            {
                angle -= 360;
            }
            
            while (angle < 0)
            {
                angle += 360;
            }
            
            return angle;
        }
    }
}