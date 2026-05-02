using UnityEngine;
using System.Collections.Generic;

namespace ProyectoDalton.Atomos
{


    /// <summary>
    /// Define un compuesto químico histórico o especial basado en el modelo de Dalton.
    /// </summary>
    [CreateAssetMenu(fileName = "Nuevo Compuesto Dalton", menuName = "Proyecto Dalton/Compuesto Especial")]
    public class CompuestoDaltonSO : ScriptableObject
    {
        [Header("Identidad del Compuesto")]
        public string nombreCompuesto = "Agua";
        public string formulaHistorica = "HO";
        
        [TextArea(3, 6)]
        public string descripcion;
        
        [Header("Visuales")]
        public Texture2D icono;
        public Color colorRepresentativo = Color.cyan;

        [Header("Receta / Requisitos")]
        [Tooltip("Símbolos necesarios para que se forme este compuesto. Ej: H, O")]
        public string[] simbolosNecesarios;
    }
}
