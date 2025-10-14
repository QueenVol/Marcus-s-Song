using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void SongOne()
    {
        SceneManager.LoadScene("SongOne");
    }

    public void SongTwo()
    {
        SceneManager.LoadScene("SongTwo");
    }
}
