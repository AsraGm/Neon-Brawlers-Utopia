using UnityEngine;
using UnityEngine.UI;

public class ExposureUi : MonoBehaviour
{
    [SerializeField] private PlayerExposure playerExposure;
    [SerializeField] private RectTransform[] picos;
    [SerializeField] private Image[] picosImages; 
    [SerializeField] private float velocidadSuavizado = 8f;
    [SerializeField] private Gradient colorPorNivel;

    [SerializeField] private float velocidadOscilacion = 4f;
    [SerializeField] private float varietyOscilacion = 0.3f;
    [Tooltip("Entre mas expuesto, mas rapido")]
    [SerializeField] private bool oscilacionEscalaConNivel = true;

    private float[] alturaActual;
    private float[] faseOffset;

    void Start()
    {
        alturaActual = new float[picos.Length];
        faseOffset = new float[picos.Length];

        for (int i = 0; i < faseOffset.Length; i++)
        {
            faseOffset[i] = Random.Range(0f, Mathf.PI * 2f);
        }
    }

    void LateUpdate()
    {
        float nivel = playerExposure.ExposureLevel;
        bool visible = nivel > 0.03f;

        float velocidadActual = oscilacionEscalaConNivel
            ? Mathf.Lerp(velocidadOscilacion * 0.5f, velocidadOscilacion * 1.5f, nivel)
            : velocidadOscilacion;

        for (int i = 0; i < picos.Length; i++)
        {
            alturaActual[i] = Mathf.Lerp(alturaActual[i], nivel, Time.deltaTime * velocidadSuavizado);

            float onda = Mathf.Sin(Time.time * velocidadActual + faseOffset[i]) * 0.5f + 0.5f;

            float factorOscilacion = Mathf.Lerp(1f - varietyOscilacion, 1f, onda);
            float escalaFinal = alturaActual[i] * factorOscilacion;

            Vector3 escala = picos[i].localScale;
            escala.y = Mathf.Max(0.05f, escalaFinal);
            picos[i].localScale = escala;

            if (picosImages != null && picosImages.Length > i && picosImages[i] != null)
            {
                picosImages[i].enabled = visible;
                if (visible)
                {
                    picosImages[i].color = colorPorNivel.Evaluate(nivel);
                }
            }
        }
    }
}
