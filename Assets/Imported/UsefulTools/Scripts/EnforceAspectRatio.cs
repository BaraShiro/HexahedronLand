using System;
using UnityEngine;


namespace UsefulTools
{
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Image Effects/Enforce Aspect Ratio")]
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class EnforceAspectRatio : MonoBehaviour
    {
        [Serializable]
        public struct AspectRatio
        {
            [Min(1f)]
            public float widthUnits;
            [Min(1f)]
            public float heightUnits;
        }
        
        [Header("Aspect Ratio")]
        [Space]
        
        [SerializeField, Tooltip("Enforce")]
        private bool enforce = true;

        [SerializeField]
        [ContextMenuItem("16:9", "SixteenToNine")]
        [ContextMenuItem("9:16", "NineToSixteen")]
        [ContextMenuItem("4:3", "FourToThree")]
        [ContextMenuItem("3:4", "ThreeToFour")]
        private AspectRatio aspectRatio = new AspectRatio {widthUnits = 16f, heightUnits = 9f};

        private int currentScreenHeight = 1;
        private int currentScreenWidth = 1;

        private Camera cam;

        private Camera Cam
        {
            get
            {
                if (!cam) 
                    cam = GetComponent<Camera>();
                
                return cam;
            }
        }

        public bool Enforce
        {
            get => enforce;
            set
            {
                enforce = value;
                AdjustCameraAspectRatio();
            }
        }

        private bool ResolutionChanged => currentScreenHeight != Screen.height || currentScreenWidth != Screen.width;

        private void OnValidate()
        {
            AdjustCameraAspectRatio();
        }
        
        private void Start()
        {
            cam = GetComponent<Camera>();
            GetCurrentResolution();
            AdjustCameraAspectRatio();
        }

        private void Update()
        {
            if (ResolutionChanged)
            {
                GetCurrentResolution();
                AdjustCameraAspectRatio();
            }
        }
        
        private void GetCurrentResolution()
        {
            currentScreenHeight = Screen.height;
            currentScreenWidth = Screen.width;
        }

        private void AdjustCameraAspectRatio()
        {
            if (enforce)
            {
                float desiredAspectRatio = aspectRatio.widthUnits / aspectRatio.heightUnits;
                float currentAspectRatio = (float) currentScreenWidth / (float) currentScreenHeight;
            
                // Pillarbox the image
                if (currentAspectRatio > desiredAspectRatio) 
                {
                    float percentage = 1 - desiredAspectRatio / currentAspectRatio;
                    Cam.rect = new Rect(percentage / 2f, 0f, 1f - percentage, 1f);
                }
                // Letterbox the image
                else
                {
                    float percentage = 1f - currentAspectRatio / desiredAspectRatio;
                    Cam.rect = new Rect(0f, percentage / 2f, 1f, 1f - percentage);
                }
            }
            else
            {
                Cam.rect = new Rect(0f, 0f, 1f, 1f);
            }
        }
        
        private void SixteenToNine()
        {
            aspectRatio = new AspectRatio {widthUnits = 16f, heightUnits = 9f};
            AdjustCameraAspectRatio();
        }
        
        private void NineToSixteen()
        {
            aspectRatio = new AspectRatio {widthUnits = 9f, heightUnits = 16f};
            AdjustCameraAspectRatio();
        }
        
        private void FourToThree()
        {
            aspectRatio = new AspectRatio {widthUnits = 4f, heightUnits = 3f};
            AdjustCameraAspectRatio();
        }
        
        private void ThreeToFour()
        {
            aspectRatio = new AspectRatio {widthUnits = 3f, heightUnits = 4f};
            AdjustCameraAspectRatio();
        }
    }
}
