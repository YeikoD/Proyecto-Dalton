using UnityEngine;

namespace ProyectoDalton.Interfaz
{
    /// <summary>
    /// Gestiona los sonidos de la interfaz globalmente.
    /// Para usar: GestorAudioUI.ReproducirClick();
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class GestorAudioUI : MonoBehaviour
    {
        public static GestorAudioUI Instancia { get; private set; }

        [Header("Efectos de Sonido UI")]
        public AudioClip sonidoButtonClick;
        public AudioClip sonidoButtonAlt;
        public AudioClip sonidoNotify;

        private AudioSource _audioSource;

        private void Awake()
        {
            if (Instancia == null)
            {
                Instancia = this;
                _audioSource = GetComponent<AudioSource>();
                
                // Asegurar configuración 2D global pura
                _audioSource.spatialBlend = 0f;
                _audioSource.playOnAwake = false;
                _audioSource.loop = false;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            ProyectoDalton.Core.GameManager.OnVolumenUICambiado += ActualizarVolumen;
            ActualizarVolumen(ProyectoDalton.Core.GameManager.VolumenUIGlobal);
        }

        private void OnDisable()
        {
            ProyectoDalton.Core.GameManager.OnVolumenUICambiado -= ActualizarVolumen;
        }

        private void ActualizarVolumen(float nuevoVolumen)
        {
            if (_audioSource != null) _audioSource.volume = nuevoVolumen;
        }

        // --- MÉTODOS PÚBLICOS ESTÁTICOS PARA FÁCIL ACCESO ---

        public static void ReproducirClick()
        {
            if (Instancia != null && Instancia.sonidoButtonClick != null)
            {
                Instancia._audioSource.PlayOneShot(Instancia.sonidoButtonClick, ProyectoDalton.Core.GameManager.VolumenUIGlobal);
            }
        }

        public static void ReproducirAlt()
        {
            if (Instancia != null && Instancia.sonidoButtonAlt != null)
            {
                Instancia._audioSource.PlayOneShot(Instancia.sonidoButtonAlt, ProyectoDalton.Core.GameManager.VolumenUIGlobal);
            }
        }

        public static void ReproducirNotificacion()
        {
            if (Instancia != null && Instancia.sonidoNotify != null)
            {
                Instancia._audioSource.PlayOneShot(Instancia.sonidoNotify, ProyectoDalton.Core.GameManager.VolumenUIGlobal);
            }
        }
    }
}
