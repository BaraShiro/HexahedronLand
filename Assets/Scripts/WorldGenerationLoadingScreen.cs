using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldGenerationLoadingScreen : MonoBehaviour
{
    public Canvas loadingCanvas;
    public TextMeshProUGUI detailedLoadingText;
    public Slider loadingBar;

    [SerializeField] private int numberOfGenerationSteps = 12;

    private int logLength = 0;

    private void Start()
    {
        World.OnWorldGenerationFinish += HideLoadingCanvas;
        detailedLoadingText.SetText("");
        loadingBar.value = 0;
    }

    private void Update()
    {
        if (logLength < WorldGenerationLogger.GetLogLength())
        {
            logLength = WorldGenerationLogger.GetLogLength();
            detailedLoadingText.SetText(WorldGenerationLogger.GetFormattedLog());
            loadingBar.value = Mathf.Min((float) logLength / numberOfGenerationSteps, 1f);
        }
    }
    
    private void HideLoadingCanvas(object sender, EventArgs e)
    {
        StartCoroutine(InactivateCanvas());
        
        World.OnWorldGenerationFinish -= HideLoadingCanvas;
    }

    private IEnumerator InactivateCanvas()
    {
        yield return new WaitForSeconds(0.5f);
        
        if (loadingCanvas)
        {
            loadingCanvas.gameObject.SetActive(false);
        }
    }
}

