using UnityEngine;
using ProyectoDalton.Interfaz;

namespace ProyectoDalton.Entorno
{
    /// <summary>
    /// Gestiona el inicio y estado de la simulación.
    /// </summary>
    public class ControlEntorno : MonoBehaviour
    {
        public static ControlEntorno Instancia { get; private set; }

        [Header("Límites")]
        [Tooltip("Límite físico global para todos los átomos (ej. la Nebulosa).")]
        public Collider limiteFisico;

        [Header("Configuración de Fusión")]
        [SerializeField] private float duracionEfecto = 2.0f;
        
        [Header("Base de Datos Histórica")]
        [Tooltip("Agrega aquí los CompuestoDaltonSO (ej. Agua HO) para que el simulador los reconozca al unir átomos.")]
        public System.Collections.Generic.List<ProyectoDalton.Atomos.CompuestoDaltonSO> compuestosEspeciales = new System.Collections.Generic.List<ProyectoDalton.Atomos.CompuestoDaltonSO>();

        void Awake()
        {
            if (Instancia == null)
            {
                Instancia = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public static event System.Action<float> OnVisualFusionDisparado;
        public static event System.Action<ProyectoDalton.Atomos.ArrastrarAtomo.InformacionCompuesto> OnCompuestoFormado;
        public static event System.Action<ProyectoDalton.Atomos.ArrastrarAtomo.InformacionCompuesto> OnCohesionFormada;
        public static event System.Action<ProyectoDalton.Atomos.ArrastrarAtomo.InformacionCompuesto> OnEstructuraRota;

        void Start()
        {
            // Nos suscribimos a los eventos de creación y ruptura de enlaces
            ProyectoDalton.Atomos.ArrastrarAtomo.OnEnlaceCreado += DispararEfectoFusion;
            ProyectoDalton.Atomos.ArrastrarAtomo.OnEnlaceRoto += DispararEfectoRuptura;
        }

        private void DispararEfectoFusion(ProyectoDalton.Atomos.ArrastrarAtomo.InformacionCompuesto info)
        {
            // Avisamos a todos los sistemas (Cielo y UI) para que reaccionen visualmente
            OnVisualFusionDisparado?.Invoke(duracionEfecto);

            // Si es un compuesto químico real (elementos diferentes), lanzamos la notificación
            if (info.esCompuesto)
            {
                ProyectoDalton.Interfaz.LogN.Info($"! COMPUESTO: {info.formula} ({info.masaTotal:F1} u)");
                OnCompuestoFormado?.Invoke(info);
            }
            else
            {
                // Si son del mismo elemento, es cohesión (Dalton)
                ProyectoDalton.Interfaz.LogN.Info($"! COHESIÓN: {info.formula} ({info.masaTotal:F1} u)");
                OnCohesionFormada?.Invoke(info);
            }
        }

        private void DispararEfectoRuptura(ProyectoDalton.Atomos.ArrastrarAtomo.InformacionCompuesto info)
        {
            // Opcional: Podrías disparar un efecto visual diferente aquí (como un flash azul o gris)
            // Por ahora, notificamos la ruptura
            string tipo = info.esCompuesto ? "Compuesto disuelto" : "Cohesión rota";
            ProyectoDalton.Interfaz.LogN.Info($"! ESTRUCTURA ROTA: {info.formula} desvinculado");
            
            OnEstructuraRota?.Invoke(info);
        }

        private void OnDestroy()
        {
            ProyectoDalton.Atomos.ArrastrarAtomo.OnEnlaceCreado -= DispararEfectoFusion;
            ProyectoDalton.Atomos.ArrastrarAtomo.OnEnlaceRoto -= DispararEfectoRuptura;
        }
    }
}
