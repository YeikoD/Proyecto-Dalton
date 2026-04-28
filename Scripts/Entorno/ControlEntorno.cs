using UnityEngine;
using ProyectoDalton.Interfaz;

namespace ProyectoDalton.Entorno
{
    /// <summary>
    /// Gestiona el inicio y estado de la simulación.
    /// </summary>
    public class ControlEntorno : MonoBehaviour
    {
        void Start()
        {
            // Secuencia de inicio con acción de carga
            LogN.Carga("Iniciando Simulador Atómico", 3.0f);
            LogN.Info("Cargando base de datos de Dalton...");
            LogN.Info("Entorno listo.");
        }
    }
}
