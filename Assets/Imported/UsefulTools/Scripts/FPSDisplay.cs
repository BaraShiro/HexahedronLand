using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace UsefulTools
{
    [RequireComponent(typeof(Text))]
    public class FPSDisplay : MonoBehaviour
    {
        public float updateInterval = 0.5f;

        private float accumulatedTime = 0f;
        private int numberOfFrames = 0;
        private float timeLeft = 0;
        private float currentFPS = 0;
        private Text guiText;

        public float CurrentFPS => currentFPS;

        private void Start()
        {
            guiText = GetComponent<Text>();
            timeLeft = updateInterval;
        }

        private void Update()
        {
            timeLeft -= Time.deltaTime;
            accumulatedTime += Time.timeScale / Time.deltaTime;
            numberOfFrames++;

            if (timeLeft <= 0.0)
            {
                currentFPS = accumulatedTime / numberOfFrames;

                guiText.text = $"{currentFPS:F2} FPS ({1000.0f / currentFPS:F1} ms)";

                timeLeft = updateInterval;
                accumulatedTime = 0.0f;
                numberOfFrames = 0;
            }
        }
    }
}