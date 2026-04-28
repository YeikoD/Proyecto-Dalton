using UnityEngine;

namespace ProyectoDalton.Atomos
{
    /// <summary>
    /// Base de datos para almacenar la teoría y propiedades de un átomo.
    /// Permite crear "Cajas de datos" en el editor (Click derecho > Create > Dalton > Datos de Átomo).
    /// </summary>
    [CreateAssetMenu(fileName = "NuevoAtomo", menuName = "Dalton/Datos de Átomo")]
    public class DatosAtomoSO : ScriptableObject
    {
        [Header("Teoría de Dalton")]
        public string nombreElemento = "Hidrógeno";
        public bool esElemento = false;
        [TextArea(2, 4)]
        public string descripcionTeorica = "Según Dalton, los átomos de un mismo elemento son idénticos en masa y propiedades.";
        [TextArea(2, 4)]
        public string nota = "Nota adicional...";

        [Header("Propiedades Físicas")]
        public string simbolo = "H";
        public Sprite icono;
        [Tooltip("Masa atómica relativa que afectará las físicas (inercia y flotación).")]
        public float masaAtomica = 1.0f;

        [Header("Representación 3D")]
        [Tooltip("El prefab que se instanciará en el mundo.")]
        public GameObject prefabAtomo;

        // ─────────────────────────────────────────────────────────────────────
        [Space(20)]
        [Header("Configuración Visual del Billboard")]
        [Tooltip("Largo del subrayado horizontal debajo del nombre.")]
        public float largoSubrayado = 0.1f;
        [Tooltip("Cuánto baja la línea respecto al centro del texto (valor negativo).")]
        public float offsetVerticalCodo = -0.16f;
        [Tooltip("Altura de la línea vertical de cierre.")]
        public float alturaLineaVertical = -0.03f;
        [Tooltip("Grosor de la línea callout.")]
        public float anchoLinea = 0.015f;
        [Tooltip("Color de la línea callout.")]
        public Color colorLinea = Color.white;
        [Tooltip("Color del texto del elemento en el menú lateral.")]
        public Color colorTextoMenu = Color.white;
        [Tooltip("Inercia del giro del panel Billboard (0 = instantáneo, 0.99 = muy lento).")]
        [Range(0f, 0.99f)]
        public float inerciaBillboard = 0.85f;
        [Tooltip("Distancia máxima de la cámara para que el billboard sea visible.")]
        public float distanciaMaxima = 20f;
    }
}
