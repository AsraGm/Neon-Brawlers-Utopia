using Unity.Cinemachine;
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
            Pause();
        }
    }

    public void Pause()
    {
        pause = !pause;
        pauseMenu.SetActive(pause);

        if (pause)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            camPlayer.gameObject.GetComponent<Camera>().enabled = false;
            habilidadesManager.enabled = false;
            animPlayer.speed = 0f;
            InventoryUIManager.Instance.SetPausa(true);
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = 1;
            camPlayer.gameObject.GetComponent<Camera>().enabled = true;
            habilidadesManager.enabled = true;
            animPlayer.speed = 1f;
            configCanvas.SetActive(false);
            InventoryUIManager.Instance.SetPausa(false);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void MENU()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("MAIN MENU");
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    //extra cambio de escenas
    public void CambioEscena(string escena)
    {
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        pause = false;

        SceneManager.LoadScene(escena);
    }

}
