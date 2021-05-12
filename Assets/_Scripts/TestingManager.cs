using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; 
using UnityEngine.SceneManagement;

public class TestingManager : Singleton<TestingManager>
{
    public TMP_Text interactText;

    public List<Door> doors;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            LoadScene("Menu");

        if(Input.GetKeyDown(KeyCode.F))
            Screen.fullScreen = !Screen.fullScreen;

        if(doors.Count > 0)
            HandleInteract();
    }

    public void HandleInteract()
    {
        foreach (Door door in doors)
        {
            if (door.playerInRange)
            {
                interactText.text = "[E] to open/close door";

                return;
            }
        }

        interactText.text = "";
    }


    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Quit()
    {
        Application.Quit();
    }
}
