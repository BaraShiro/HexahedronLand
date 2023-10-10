using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UsefulTools.LoadingScene;

public class MainMenuButton : MonoBehaviour
{
    public void Load(string scene)
    {
        LoadingSceneController.LoadScene(scene);
    }
}
