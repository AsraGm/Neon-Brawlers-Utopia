using UnityEngine;

[CreateAssetMenu(fileName = "Nueva_Mision", menuName = "Inventario/Mision Data")]
public class MisionData : ScriptableObject
{
    [Header("Información de la Misión")]
    [Tooltip("ID único de esta misión")]
    public int misionID;

    [Header("Textos UI")]
    [Tooltip("Texto que aparece en OBJETIVO")]
    [TextArea(2, 4)]
    public string textoObjetivo;

    [Tooltip("Texto que aparece en QUÉ HACER")]
    [TextArea(2, 4)]
    public string textoQueHacer;

    [Header("Condición de Completado")]
    [Tooltip("ID del item que se debe recolectar para completar")]
    public string itemRequeridoID;

    [Header("Siguiente Misión")]
    [Tooltip("ID de la siguiente misión (-1 si es la última)")]
    public int siguienteMisionID = -1;
}