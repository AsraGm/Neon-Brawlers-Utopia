using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class TransitionDeath : MonoBehaviour
{
    public static TransitionDeath Instance { get; private set; }

    [Header("Fade")]
    [SerializeField] private Animator animatorFade;

    [Header("Glitch")]
    [SerializeField] private ScriptableRendererFeature glitchRenderFeature;
    [SerializeField] private Material materialGlitch;
    [SerializeField] private float valorMaximoNoise = 1f;
    [SerializeField] private float valorMaximoGlitch = 1f;
    [SerializeField] private float duracionGlitchIn = 0.6f;
    [SerializeField] private float duracionGlitchOut = 0.6f;

    [Header("Tiempos de espera")]
    [Tooltip("Tiempo de espera del fade in")]
    [SerializeField] private float fadeInEspera = 0.6f;
    [Tooltip("Tiempo de espera del fade out")]
    [SerializeField] private float fadeOutEspera = 1f;
    [Tooltip("Tiempo de espera del glitch out")]
    [SerializeField] private float glitchOutEspera = 0.75f;

    private static readonly int idNoise = Shader.PropertyToID("_Noise_Amount");
    private static readonly int idGlitch = Shader.PropertyToID("_Glitch_Strength");

    private Coroutine glitchActual;
    private Coroutine secuenciaActual;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (animatorFade != null)
        {
            animatorFade.SetBool("Fade", false);
        }

        if (materialGlitch != null)
        {
            materialGlitch.SetFloat(idNoise, 0f);
            materialGlitch.SetFloat(idGlitch, 0f);
        }

        if (glitchRenderFeature != null)
        {
            glitchRenderFeature.SetActive(false);
        }
    }

    public void IniciarGlitchLuegoFade()
    {
        if (secuenciaActual != null) StopCoroutine(secuenciaActual);
        secuenciaActual = StartCoroutine(GlitchLuegoFade());
    }

    private IEnumerator GlitchLuegoFade()
    {
        IniciarGlitch();
        yield return new WaitForSeconds(fadeInEspera);

        if (animatorFade != null) animatorFade.SetBool("Fade", true);

        secuenciaActual = null;
    }

    public void TerminarFadeLuegoGlitch()
    {
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(fadeOutEspera);

        if (secuenciaActual != null) StopCoroutine(secuenciaActual);
        secuenciaActual = StartCoroutine(FadeLuegoGlitch());
    }

    private IEnumerator FadeLuegoGlitch()
    {
        if (animatorFade != null) animatorFade.SetBool("Fade", false);

        yield return new WaitForSeconds(glitchOutEspera);

        TerminarGlitch();
        secuenciaActual = null;
    }

    #region Glitch
    public void IniciarGlitch()
    {
        if (glitchActual != null) StopCoroutine(glitchActual);
        if (glitchRenderFeature != null) glitchRenderFeature.SetActive(true);
        glitchActual = StartCoroutine(Glitch(valorMaximoNoise, valorMaximoGlitch, duracionGlitchIn));
    }

    public void TerminarGlitch()
    {
        if (glitchActual != null) StopCoroutine(glitchActual);
        glitchActual = StartCoroutine(Glitch(0f, 0f, duracionGlitchOut));
    }

    private IEnumerator Glitch(float noiseObjetivo, float glitchObjetivo, float duracion)
    {
        if (materialGlitch == null) yield break;

        float noiseInicial = materialGlitch.GetFloat(idNoise);
        float glitchInicial = materialGlitch.GetFloat(idGlitch);

        if (duracion <= 0f)
        {
            materialGlitch.SetFloat(idNoise, noiseObjetivo);
            materialGlitch.SetFloat(idGlitch, glitchObjetivo);
        }
        else
        {
            float t = 0f;
            while (t < duracion)
            {
                t += Time.deltaTime;
                float p = t / duracion;
                materialGlitch.SetFloat(idNoise, Mathf.Lerp(noiseInicial, noiseObjetivo, p));
                materialGlitch.SetFloat(idGlitch, Mathf.Lerp(glitchInicial, glitchObjetivo, p));
                yield return null;
            }

            materialGlitch.SetFloat(idNoise, noiseObjetivo);
            materialGlitch.SetFloat(idGlitch, glitchObjetivo);
        }

        if (noiseObjetivo <= 0f && glitchObjetivo <= 0f && glitchRenderFeature != null)
        {
            glitchRenderFeature.SetActive(false);
        }
    }
    #endregion Glitch
}