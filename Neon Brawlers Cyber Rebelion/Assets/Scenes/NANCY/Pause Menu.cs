using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("CONFIG")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject camPlayer;
    [SerializeField] private GameObject habilitiesPlayer;
    [SerializeField] private GameObject configCanvas;

    private bool pause;
    private CinemachineBrain camara;
    private HabilidadesManager habilidadesManager;

    private void Start()
    {
        camara = camPlayer.GetComponent<CinemachineBrain>();
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
            camara.enabled = false;
            habilidadesManager.enabled = false;
            InventoryUIManager.Instance.SetPausa(true);
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = 1;
            camara.enabled = true;
            habilidadesManager.enabled = true;
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

}
