using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class LoginPage : MonoBehaviour
{
    [SerializeField] Button loginButton;
    [SerializeField] Button backButton;
    [SerializeField] TMP_InputField passInput;
    [SerializeField] TMP_InputField idInput;
    [SerializeField] TMP_Text errorMessage;
    [SerializeField] GameObject popupPanel;
    string pass = "12345678";
    string id = "baongocngo2211"; 
    
    void Start()
    {
        backButton.onClick.AddListener(ExitApp);
        loginButton.onClick.AddListener(ValidateLogin);
        popupPanel.SetActive(false);
        loginButton.onClick.AddListener(ValidateLogin);
    }

    void ExitApp()
    {
        Application.Quit();
    }

    void ValidateLogin()
    {
        string enteredId = idInput.text.Trim();
        string enteredPass = passInput.text.Trim();
        if(enteredId == "" || enteredPass == "")
        {
            PopUpMessage("Please enter ID and Password!", Color.red);
        }
        else if (enteredId == id && enteredPass == pass){
            PopUpMessage ("Login Succesful!", Color.green);
            StartCoroutine(LoadSceneAfterDelay("ChooseMethod", 1f));
        }
        else if(enteredId != id ){
            PopUpMessage ("Wrong ID!", Color.red);
        }
        else if(enteredPass != pass){
            PopUpMessage ("Wrong Password!", Color.red); 
        }
        else{
            PopUpMessage ("Wrong Password and ID!", Color.red);  
        }
    }

    void PopUpMessage(string message, Color messageColor){
        errorMessage.color = messageColor;
        errorMessage.text = message;
        popupPanel.SetActive(true);
        Invoke("HidePopup", 2f);

    }

    void HidePopup(){
        popupPanel.SetActive(false);
    }

    IEnumerator LoadSceneAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);  
        SceneManager.LoadScene(sceneName);       
    }
}
