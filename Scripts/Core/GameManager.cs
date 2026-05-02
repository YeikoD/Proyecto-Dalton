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

        [Header("Configuración Global")]
        [SerializeField, Range(0f, 1f)] private float _volumenAtomosGlobal = 0.5f;
        [Tooltip("Sonido base que usarán todos los átomos de forma automática.")]
        public AudioClip SonidoBaseAtomos;
        
        public static float VolumenAtomosGlobal 
        {
            get => Instancia != null ? Instancia._volumenAtomosGlobal : 0.5f;
            set 
            {
                if (Instancia != null)
                {
                    Instancia._volumenAtomosGlobal = value;
                    LogN.Info($"SYS: Volumen Global de Átomos: {Mathf.RoundToInt(value * 100)}%");
                    OnVolumenAtomosCambiado?.Invoke(value);
                }
            }
        }
        
        [SerializeField, Range(0f, 1f)] private float _volumenUIGlobal = 0.8f;
        
        public static float VolumenUIGlobal 
        {
            get => Instancia != null ? Instancia._volumenUIGlobal : 0.8f;
            set 
            {
                if (Instancia != null)
                {
                    Instancia._volumenUIGlobal = value;
                    OnVolumenUICambiado?.Invoke(value);
                }
            }
        }

        public static event System.Action<float> OnVolumenAtomosCambiado;
        public static event System.Action<float> OnVolumenUICambiado;

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
