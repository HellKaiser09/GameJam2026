using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Menu de pausa HECHO A MANO en el Canvas. Este script solo hace la logica:
//  - Esc (o P) muestra / esconde el panel
//  - congela el juego y la musica, y los regresa al continuar
//  - funciones para los botones: Resume (Continuar), Restart (Reintentar), Quit (Salir)
//
// Ponlo en el objeto Canvas (NO en el panel: el panel se apaga y el script dejaria de escuchar Esc).
public class PauseMenu : MonoBehaviour
{
    public static bool Paused { get; private set; }

    [Header("Tu menu hecho a mano")]
    [SerializeField, Tooltip("El panel de pausa (PauseMenu)")] GameObject panel;
    [SerializeField, Tooltip("Boton que queda seleccionado al pausar (Continuar)")] Button firstButton;

    [Header("Se buscan solos si los dejas vacios")]
    [SerializeField] FightManager fight;
    [SerializeField] BeatClock beatClock;

    bool stoppedMusic;
    float prevTimeScale = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Paused = false; }

    void Awake()
    {
        if (fight == null) fight = FindAnyObjectByType<FightManager>();
        if (beatClock == null) beatClock = FindAnyObjectByType<BeatClock>();
        if (panel != null) panel.SetActive(false); // empieza escondido
    }

    void OnDisable() { Paused = false; }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (Paused) Resume();
            else if (fight == null || !fight.Over) Pause();   // no se pausa al ganar / perder
        }
    }

    public void Pause()
    {
        if (panel == null) { Debug.LogWarning("PauseMenu: falta arrastrar el Panel"); return; }
        Paused = true;
        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;                                   // congela el juego
        stoppedMusic = beatClock != null && beatClock.Started && !beatClock.IsStopped;
        if (stoppedMusic) beatClock.PauseNow();                // congela la musica
        panel.SetActive(true);
        if (firstButton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
    }

    // Boton CONTINUAR
    public void Resume()
    {
        Paused = false;
        Time.timeScale = prevTimeScale <= 0f ? 1f : prevTimeScale;
        if (stoppedMusic) beatClock.Resume();                  // la musica sigue en ritmo
        if (panel != null) panel.SetActive(false);
    }

    // Boton REINTENTAR
    public void Restart()
    {
        Paused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Boton SALIR
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
