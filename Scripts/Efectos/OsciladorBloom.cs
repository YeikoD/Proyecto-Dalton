using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // Necesario para URP

namespace ProyectoDalton.Efectos
{
    /// <summary>
    /// Hace que el efecto Bloom "respire" de forma orgánica.
    /// Completamente libre de errores del Inspector (Bugfix Unity 6).
    /// </summary>
    public class OsciladorBloom : MonoBehaviour
    {
        [Header("Configuración de Latido (Bloom)")]
        [Tooltip("La intensidad más baja que tendrá el brillo.")]
        public float intensidadMinima = 0.5f;
        [Tooltip("El pico máximo de brillo.")]
        public float intensidadMaxima = 2.0f;
        [Tooltip("Qué tan rápido respira el brillo.")]
        public float velocidadRespiracion = 1.5f;

        private Volume volumenTemporal;
        private Bloom efectoBloomVirtual;

        void Start()
        {
            // Para evitar el temido error "SerializedObjectNotCreatableException" del Editor de Unity,
            // no debemos modificar el Volume original que tienes seleccionado en el Inspector.
            // La forma correcta en URP es crear un Volumen "Fantasma" en memoria con máxima prioridad.

            // 1. Creamos un objeto hijo invisible
            GameObject objetoFantasma = new GameObject("BloomOscilador_Fantasma");
            objetoFantasma.transform.SetParent(this.transform);
            // Lo ocultamos en la jerarquía para no ensuciar (opcional)
            objetoFantasma.hideFlags = HideFlags.HideAndDontSave;

            // 2. Le ponemos un componente Volume
            volumenTemporal = objetoFantasma.AddComponent<Volume>();
            volumenTemporal.isGlobal = true;
            volumenTemporal.priority = 999; // Prioridad altísima para que sobreescriba tu Bloom base

            // 3. Le creamos un perfil virgen en la memoria RAM
            VolumeProfile perfilVirtual = ScriptableObject.CreateInstance<VolumeProfile>();
            volumenTemporal.sharedProfile = perfilVirtual;

            // 4. Le inyectamos SOLO el efecto Bloom
            efectoBloomVirtual = perfilVirtual.Add<Bloom>(true);
            
            // Le decimos que SOLO queremos que tome el control de la "Intensidad", 
            // el color y el resto de cosas las seguirá tomando de tu perfil original.
            efectoBloomVirtual.intensity.overrideState = true;
            efectoBloomVirtual.intensity.value = intensidadMinima;
        }

        void Update()
        {
            if (efectoBloomVirtual == null) return;

            // Generamos la onda de respiración suave (de 0 a 1)
            float ondaRespiracion = (Mathf.Sin(Time.time * velocidadRespiracion) + 1f) * 0.5f;
            
            // Modificamos solo el Bloom virtual
            efectoBloomVirtual.intensity.value = Mathf.Lerp(intensidadMinima, intensidadMaxima, ondaRespiracion);
        }

        void OnDestroy()
        {
            // Limpieza perfecta: destruimos el objeto fantasma y su perfil de la memoria
            // para no dejar basura ("memory leaks") cuando cambias de escena o detienes el juego.
            if (volumenTemporal != null)
            {
                if (volumenTemporal.sharedProfile != null)
                {
                    Destroy(volumenTemporal.sharedProfile);
                }
                Destroy(volumenTemporal.gameObject);
            }
        }
    }
}
