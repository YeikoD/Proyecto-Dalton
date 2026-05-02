using UnityEngine;
using System.Collections.Generic;
using ProyectoDalton.Atomos;

namespace ProyectoDalton.Entorno
{
    /// <summary>
    /// Gestor centralizado que reconoce compuestos históricos basados en la fórmula generada.
    /// Sistema de bajo acoplamiento que observa los cambios en la materia.
    /// </summary>
    public class GestorCompuestosDalton : MonoBehaviour
    {
        public static GestorCompuestosDalton Instancia { get; private set; }

        [Header("Base de Datos Histórica")]
        [Tooltip("Lista de ScriptableObjects con las fórmulas de Dalton.")]
        public List<CompuestoDaltonSO> compuestosEspeciales = new List<CompuestoDaltonSO>();

        public delegate void CompuestoDaltonHandler(CompuestoDaltonSO compuesto);
        public static event CompuestoDaltonHandler OnCompuestoEspecialFormado;

        void Awake()
        {
            if (Instancia == null)
            {
                Instancia = this;
                ProyectoDalton.Interfaz.LogN.Info("GestorCompuestosDalton: Sistema iniciado y listo.");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void OnEnable()
        {
            // Observamos cuando se crea un enlace para analizar si es especial
            ArrastrarAtomo.OnEnlaceCreado += AnalizarNuevaEstructura;
        }

        void OnDisable()
        {
            ArrastrarAtomo.OnEnlaceCreado -= AnalizarNuevaEstructura;
        }

        private void AnalizarNuevaEstructura(ArrastrarAtomo.InformacionCompuesto info)
        {
            CompuestoDaltonSO especial = BuscarCompuestoPorFormula(info.formula);
            if (especial != null)
            {
                ProyectoDalton.Interfaz.LogN.Info($"¡COMPUESTO DE DALTON DETECTADO: {especial.nombreCompuesto}! ({especial.formulaHistorica})");
                
                // Disparamos el evento para que UI y otros sistemas reaccionen
                OnCompuestoEspecialFormado?.Invoke(especial);
            }
        }

        /// <summary>
        /// Busca si una fórmula string coincide con algún compuesto registrado.
        /// </summary>
        public CompuestoDaltonSO BuscarCompuestoPorFormula(string formula)
        {
            if (string.IsNullOrEmpty(formula)) return null;

            foreach (var so in compuestosEspeciales)
            {
                if (so != null && so.formulaHistorica == formula)
                {
                    return so;
                }
            }
            
            // ProyectoDalton.Interfaz.LogN.Info($"Gestor: No hay coincidencia para '{formula}' en la base de datos.");
            return null;
        }
    }
}
