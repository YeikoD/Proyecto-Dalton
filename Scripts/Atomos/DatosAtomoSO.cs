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
        [Header("--- DATOS DEL ELEMENTO ---")]
        public string nombreElemento = "Hidrógeno";
        public string simbolo = "H";
        [Tooltip("Masa atómica relativa que afectará las físicas (inercia y flotación).")]
        public float masaAtomica = 1.0f;
        public Sprite icono;
        [TextArea(2, 4)]
        public string descripcionTeorica = "Según Dalton, los átomos de un mismo elemento son idénticos en masa y propiedades.";


        // --- PROPIEDADES AUTOMATIZADAS POR LA TEORÍA DE DALTON ---
        // Estos valores se calculan solos en base a la masaAtómica y las constantes del Hidrógeno.
        // Al ser propiedades de solo lectura, no aparecen en el inspector y mantienen el asset limpio.
        public float amplitudMovimiento => 0.15f;
        public float velocidadMovimiento => 3.0f;
        public float velocidadRotacion => 5.0f;
        public bool usarInercia => true;
        public float agilidadBase => 25.0f;
        
        // La escala base del Hidrógeno es ~0.303. El resto crece según la raíz cúbica de su masa.
        public float escalaModelo => 0.3032686f * Mathf.Pow(masaAtomica, 1f / 3f);

        [Space(20)]
        [Header("--- CONFIGURACIÓN VISUAL ---")]
        
        [Header("Representación 3D")]
        [Tooltip("El prefab que se instanciará en el mundo.")]
        public GameObject prefabAtomo;

        [Header("Billboard y UI")]
        [Tooltip("Color del texto del elemento en el menú lateral y línea del callout.")]
        public Color colorTextoMenu = Color.white;
        public Color colorLinea = Color.white;
        
        [Space(10)]
        [Tooltip("Largo del subrayado horizontal debajo del nombre.")]
        public float largoSubrayado = 0.1f;
        [Tooltip("Cuánto baja la línea respecto al centro del texto (valor negativo).")]
        public float offsetVerticalCodo = -0.16f;
        [Tooltip("Altura de la línea vertical de cierre.")]
        public float alturaLineaVertical = -0.03f;
        [Tooltip("Grosor de la línea callout.")]
        public float anchoLinea = 0.015f;
        
        [Space(10)]
        [Tooltip("Inercia del giro del panel Billboard (0 = instantáneo, 0.99 = muy lento).")]
        [Range(0f, 0.99f)]
        public float inerciaBillboard = 0.85f;
        [Tooltip("Distancia máxima de la cámara para que el billboard sea visible.")]
        public float distanciaMaxima = 20f;
    }
}
