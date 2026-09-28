using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("CONFIG")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject camPlayer;
    [SerializeField] private Animator animPlayer;
    [SerializeField] private GameObject habilitiesPlayer;
    [SerializeField] private GameObject configCanvas;

    private bool pause;
    private HabilidadesManager habilidadesManager;

    private void Start()
    {
        Application.targetFrameRate = 60;
        habilidadesManager = habilitiesPlayer.GetComponent<HabilidadesManager>();
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasReleasedThisFrame)
        {
            if (pause) Reanudar();
            else Pause();
        }
    }

    public void Pause()
    {
        pause = true;
        pauseMenu.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        camPlayer.GetComponent<Camera>().enabled = false;
        habilidadesManager.enabled = false;
        animPlayer.speed = 0f;
        InventoryUIManager.Instance.SetPausa(true);
        AudioListener.pause = true;
        Time.timeScale = 0;
    }

    public void Reanudar()
    {
        pause = false;
        pauseMenu.SetActive(false);
        configCanvas.SetActive(false);

        Time.timeScale = 1;
        camPlayer.GetComponent<Camera>().enabled = true;
        habilidadesManager.enabled = true;
        animPlayer.speed = 1f;
        InventoryUIManager.Instance.SetPausa(false);
        AudioListener.pause = false;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void MENU()
    {
        pause = false;
        Time.timeScale = 1;
        AudioListener.pause = false; 
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene("MAIN MENU");
    }

    //extra cambio de escenas
    public void CambioEscena(string escena)
    {
        pause = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        SceneManager.LoadScene(escena);
    }

}
