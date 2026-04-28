using UnityEngine;
using ProyectoDalton.Interfaz;

namespace ProyectoDalton.Core
{
    /// <summary>
    /// Gestiona el estado global del juego y los bloqueos de sistemas.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instancia { get; private set; }

        [Header("Estados de Bloqueo")]
        [SerializeField] private bool bloquearInput = false;
        [SerializeField] private bool bloquearCamara = false;

        public bool BloquearInput 
        { 
            get => bloquearInput; 
            set => bloquearInput = value; 
        }

        public bool BloquearCamara 
        { 
            get => bloquearCamara; 
            set => bloquearCamara = value; 
        }

        private void Awake()
        {
            if (Instancia == null)
            {
                Instancia = this;
                // No destruimos entre escenas para mantener persistencia si fuera necesario
                // DontDestroyOnLoad(gameObject); 
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Al iniciar, bloqueamos todo por defecto para dar paso al Menú Principal
            SetEstadoBloqueoMenu(true);
        }

        /// <summary>
        /// Activa o desactiva todos los bloqueos típicos de un menú.
        /// </summary>
        public void SetEstadoBloqueoMenu(bool bloqueado)
        {
            BloquearInput = bloqueado;
            BloquearCamara = bloqueado;
            
            // LogN.Info(bloqueado ? "<color=yellow>GameManager:</color> Interacción bloqueada por menú." : "<color=yellow>GameManager:</color> Interacción liberada.");
        }

        /// <summary>
        /// Alterna el estado del bloqueo de input.
        /// </summary>
        public void AlternarBloqueoInput() => BloquearInput = !BloquearInput;

        /// <summary>
        /// Alterna el estado del bloqueo de cámara.
        /// </summary>
        public void AlternarBloqueoCamara() => BloquearCamara = !BloquearCamara;
    }
}
