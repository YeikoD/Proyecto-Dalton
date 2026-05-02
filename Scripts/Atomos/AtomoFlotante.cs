using UnityEngine;

namespace ProyectoDalton.Atomos
{
    /// <summary>
    /// Controla el movimiento orgánico de flotación y rotación de un átomo.
    /// Alto nivel de encapsulamiento: no depende de otros sistemas y usa coordenadas locales.
    /// </summary>
    public class AtomoFlotante : MonoBehaviour
    {
        [Header("Propiedades Físicas")]
        [Tooltip("La masa del elemento (Ej: Hidrógeno=1, Oxígeno=16). Átomos más pesados se moverán más lento.")]
        [Min(0.1f)]
        public float masaAtomica = 1.0f; 

        [Header("Configuración de Flotación (Valores para masa 1)")]
        public float amplitudMovimiento = 0.5f;
        public float velocidadMovimiento = 1.0f;
        public float velocidadRotacion = 30.0f;

        private Vector3 posicionInicial;
        private BillboardAtomo _billboardCache;
        private ArrastrarAtomo _arrastreCache;
        
        // "Semillas" para que múltiples átomos no se muevan exactamente igual y parezca caótico
        private float offsetTiempoX;
        private float offsetTiempoY;
        private float offsetTiempoZ;
        
        // Hacia dónde rotará este átomo específico
        private Vector3 ejeDeRotacion;

        // --- METODOS PÚBLICOS PARA INTERACCIÓN ---
        public void FijarNuevaPosicion(Vector3 nuevaPos)
        {
            posicionInicial = nuevaPos;
        }

        public void PausarFlotacion(bool pausar)
        {
            // Apaga el Update temporalmente para que no pelee con el arrastre del ratón
            this.enabled = !pausar;
        }

        void Start()
        {
            _billboardCache = GetComponent<BillboardAtomo>();
            _arrastreCache = GetComponent<ArrastrarAtomo>();
            // Guardamos su ancla original. Al usar localPosition, puedes poner el átomo
            // dentro de otro objeto (como un contenedor) y flotará en relación a ese contenedor.
            posicionInicial = transform.localPosition;

            // Damos valores aleatorios de inicio para que cada instancia sea única
            offsetTiempoX = Random.Range(0f, 100f);
            offsetTiempoY = Random.Range(0f, 100f);
            offsetTiempoZ = Random.Range(0f, 100f);

            // Elegimos un eje de rotación al azar en 3D
            ejeDeRotacion = Random.onUnitSphere;

            // --- AUDIO DINÁMICO ---
            ConfigurarAudioDinámico();
        }

        private AudioSource _audioCentral;

        private void ConfigurarAudioDinámico()
        {
            if (ProyectoDalton.Core.GameManager.Instancia == null) return;

            AudioClip clipCentral = ProyectoDalton.Core.GameManager.Instancia.SonidoBaseAtomos;
            if (clipCentral == null) return;

            _audioCentral = gameObject.AddComponent<AudioSource>();
            _audioCentral.clip = clipCentral;
            _audioCentral.playOnAwake = true;
            _audioCentral.loop = true;
            _audioCentral.spatialBlend = 1f; // 100% 3D
            
            // Calculamos el tamaño basado en la escala (aplicada por el Configurador)
            float tamanoEsfera = transform.localScale.x;
            _audioCentral.minDistance = tamanoEsfera * 1.8f; // Volumen máximo hasta un 80% más allá del átomo
            _audioCentral.maxDistance = tamanoEsfera * 4.3f; // Rango de atenuación expandido un 20% extra
            
            _audioCentral.rolloffMode = AudioRolloffMode.Linear;
            _audioCentral.volume = ProyectoDalton.Core.GameManager.VolumenAtomosGlobal;
            
            _audioCentral.Play();

            // Suscribirse a cambios de volumen
            ProyectoDalton.Core.GameManager.OnVolumenAtomosCambiado += ActualizarVolumen;
        }

        private void OnDestroy()
        {
            ProyectoDalton.Core.GameManager.OnVolumenAtomosCambiado -= ActualizarVolumen;
        }

        private void ActualizarVolumen(float vol)
        {
            if (_audioCentral != null) _audioCentral.volume = vol;
        }

        void Update()
        {
            // 1. Sincronización desde el ScriptableObject (Single Source of Truth)
            if (_billboardCache != null && !_billboardCache.modoDebug && _billboardCache.datos != null)
            {
                masaAtomica = _billboardCache.datos.masaAtomica;
                amplitudMovimiento = _billboardCache.datos.amplitudMovimiento;
                velocidadMovimiento = _billboardCache.datos.velocidadMovimiento;
                velocidadRotacion = _billboardCache.datos.velocidadRotacion;
            }

            // Usamos la masa TOTAL del compuesto para la vibración térmica
            float masaEfectiva = _arrastreCache != null ? _arrastreCache.ObtenerMasaTotal() : masaAtomica;

            // La magia física: A mayor masa, el átomo es más "torpe". 
            // Dividimos por la raíz cuadrada de la masa para que la curva de lentitud sea más suave 
            float factorAgilidad = 1f / Mathf.Max(Mathf.Sqrt(masaEfectiva), 0.5f);

            float velocidadReal = velocidadMovimiento * factorAgilidad;
            float amplitudReal = amplitudMovimiento * factorAgilidad;

            // 1. MOVIMIENTO: Efecto de flotación que siempre vuelve al centro
            // Usamos funciones de onda (Seno) con velocidades ligeramente distintas para crear un patrón orgánico en 3D
            float movX = Mathf.Sin((Time.time + offsetTiempoX) * velocidadReal * 0.9f) * amplitudReal;
            float movY = Mathf.Sin((Time.time + offsetTiempoY) * velocidadReal * 1.1f) * amplitudReal;
            float movZ = Mathf.Sin((Time.time + offsetTiempoZ) * velocidadReal * 1.3f) * amplitudReal;

            // Siempre se aplica desde la posición original, garantizando que NUNCA se aleje indefinidamente
            transform.localPosition = posicionInicial + new Vector3(movX, movY, movZ);

            // 2. ROTACIÓN: Gira sobre su propio eje lentamente
            float rotacionReal = velocidadRotacion * factorAgilidad;
            transform.Rotate(ejeDeRotacion, rotacionReal * Time.deltaTime, Space.World);
        }
    }
}
