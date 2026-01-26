using UnityEngine;

[CreateAssetMenu(fileName = "Nuevo_Item", menuName = "Inventario/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Información General")]
    [Tooltip("ID único del item (ej: tarjeta_roja, bateria_01)")]
    public string itemID;

    [Tooltip("Nombre para mostrar en Base de Datos")]
    public string nombreDisplay;

    [Header("Visual")]
    [Tooltip("Sprite/PNG que se muestra en el slot")]
    public Sprite iconoItem;

    [Header("Tipo de Item")]
    [Tooltip("Selecciona en qué tab(s) aparece este item")]
    public TipoItem tipo = TipoItem.Item_Normal;

    [Header("Información Lore (solo para Base de Datos)")]
    [TextArea(3, 10)]
    [Tooltip("Texto que se muestra al seleccionar en Base de Datos")]
    public string descripcionLore;

    [Tooltip("Audio log que se reproduce al seleccionar")]
    public AudioClip audioLore;
}

public enum TipoItem
{
    Item_Normal,      // Solo aparece en tab LLAVES
    Item_Lore,        // Solo aparece en tab BdD
}