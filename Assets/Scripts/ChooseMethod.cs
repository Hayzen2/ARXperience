using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class ChooseMethod : MonoBehaviour
{
    [SerializeField] Button backButton;
    [SerializeField] Button scanImage;
    [SerializeField] Button scanSurface;
    void Start()
    {      
        backButton.onClick.AddListener(LogOut);
        scanImage.onClick.AddListener(ArImage);
        scanSurface.onClick.AddListener(ArSurface);
        
    }
    void LogOut(){
        StartCoroutine(LoadSceneAfterDelay("LoginPage", 1f));
    }
    void ArImage(){
        StartCoroutine(LoadSceneAfterDelay("ARImage", 1f));
    }
    void ArSurface(){
        StartCoroutine(LoadSceneAfterDelay("ARSurface", 1f));
    }

    IEnumerator LoadSceneAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }
}
