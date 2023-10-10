using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UsefulTools.LoadingScene
{
    public class MissingResourceException : Exception
    {
        public MissingResourceException(string message) : base(message) {}
    }

    public class FadeToBlack : MonoBehaviour
    {
        private static FadeToBlack _instance;

        private static FadeToBlack Instance
        {
            get
            {
                if (_instance == null)
                {
                    FadeToBlack fadeToBlackPrefab = Resources.Load<FadeToBlack>("FadeToBlackCanvas");
                    if (fadeToBlackPrefab)
                    {
                        _instance = Instantiate(fadeToBlackPrefab);
                    }
                    else
                    {
                        throw new MissingResourceException("Failed to load resource! Make sure the 'FadeToBlackCanvas' prefab is located in the resource folder.");
                    }
                }

                return _instance;
            }
        }

        private Animator fadeToBlackAnimator;

        private static readonly int toBlack = Animator.StringToHash("ToBlack");
        private static readonly int fromBlack = Animator.StringToHash("FromBlack");

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Setup();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Setup()
        {
            _instance = this;
            fadeToBlackAnimator = GetComponentInChildren<Animator>();
            DontDestroyOnLoad(this);
        }

        public static void ToBlack()
        {
            Instance.fadeToBlackAnimator.SetTrigger(toBlack);
        }

        public static void FromBlack()
        {
            Instance.fadeToBlackAnimator.SetTrigger(fromBlack);
        }
        
        public static IEnumerator WaitForEndOfAnimation()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForSecondsRealtime(Instance.fadeToBlackAnimator.GetCurrentAnimatorStateInfo(0).length);
        }
    }
}