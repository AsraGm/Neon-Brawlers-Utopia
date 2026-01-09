using UnityEngine;
using UnityEngine.SceneManagement;

public class MAINMENU : MonoBehaviour
{
    public void CREDITS()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("CREDITS");
    }
    public void ARTGALLERY()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("ART GALLERY");
    }
    public void MENU()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MAIN MENU");
    }
    public void QUIT()
    {
        Application.Quit();
    }
}
