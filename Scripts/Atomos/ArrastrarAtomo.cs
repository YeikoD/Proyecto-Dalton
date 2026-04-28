using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace ProyectoDalton.Atomos
{
    [RequireComponent(typeof(Collider))]
    public class ArrastrarAtomo : MonoBehaviour
    {
        [Header("Configuración de Grilla")]
        [Tooltip("Si está activo, el átomo se moverá a saltos, encajando perfectamente en los cuadros de tu suelo.")]
        public bool ajustarAGrilla = true;
        public float tamanoGrilla = 1.0f;

        [Header("Límites del Mapa")]
        [Tooltip("Asigna aquí la Nebulosa (SphereCollider) para que los átomos no se escapen al lanzarlos.")]
        public Collider limiteFisico;
        [Tooltip("Radio del átomo para que rebote antes de que la malla atraviese la pared.")]
        public float radioAtomo = 0.5f;
        [Tooltip("Altura del suelo de tu grilla. El átomo nunca bajará más que esto.")]
        public float alturaSuelo = 0f;
        [Tooltip("Capa (Layer) donde están los átomos para que choquen entre sí.")]
        public LayerMask capaAtomos;

        [Header("Físicas de Arrastre")]
        [Tooltip("Activa la inercia física. El átomo se sentirá pesado y le costará seguir el cursor.")]
        public bool usarInercia = true;
        [Tooltip("Agilidad base del arrastre. Átomos más pesados tendrán menos agilidad que esta.")]
        public float agilidadBase = 15.0f;
        [Tooltip("Qué tan rápido se acerca o aleja el átomo al usar la rueda del ratón.")]
        public float velocidadAcercamiento = 3.0f;

        // Bandera global para avisarle a la cámara que estamos ocupados arrastrando algo
        public static bool AlgunAtomoArrastrado { get; private set; }

        // Propiedad para saber si ESTE átomo en específico está siendo arrastrado
        public bool EstaSiendoArrastrado { get; private set; }

        private Vector3 offsetRaton;
        private float distanciaZ;
        private bool deslizandosePorInercia = false;
        private Camera camaraPrincipal;
        
        private AtomoFlotante flotacionScript;
        private BillboardAtomo billboardScript;
        
        // Esta variable la usa SmoothDamp internamente para calcular la fuerza de inercia
        private Vector3 velocidadActualFriccion;

        /// <summary>
        /// Lanza el átomo con una fuerza inicial, aprovechando el sistema de inercia existente.
        /// </summary>
        public void Lanzar(Vector3 velocidadInicial)
        {
            if (flotacionScript != null)
                flotacionScript.PausarFlotacion(true);

            deslizandosePorInercia = true;
            velocidadActualFriccion = velocidadInicial;
        }

        void Awake()
        {
            flotacionScript = GetComponentInChildren<AtomoFlotante>();
            billboardScript = GetComponentInChildren<BillboardAtomo>();
            camaraPrincipal = Camera.main;
        }

        void Start()
        {
            // Ya inicializado en Awake
        }

        void Update()
        {
            if (Mouse.current == null) return;

            // 0. IGNORAR CLIC SI ESTÁ SOBRE UI (Evita atrapar el átomo al instanciarlo desde el botón)
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) 
            {
                // Si ya lo estamos arrastrando, seguimos procesando, 
                // pero si no, no permitimos empezar un arrastre sobre la interfaz.
                if (!EstaSiendoArrastrado) return;
            }

            // 1. CLICK INICIAL
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 posicionRatonPantalla = Mouse.current.position.ReadValue();
                Ray rayo = camaraPrincipal.ScreenPointToRay(posicionRatonPantalla);
                
                if (Physics.Raycast(rayo, out RaycastHit hit))
                {
                    if (hit.collider.gameObject == this.gameObject)
                    {
                        EstaSiendoArrastrado = true;
                        AlgunAtomoArrastrado = true; // Avisamos a la cámara
                        deslizandosePorInercia = false; // Cancelamos cualquier inercia si lo atrapamos en el aire
                        
                        distanciaZ = camaraPrincipal.WorldToScreenPoint(transform.position).z;
                        offsetRaton = transform.position - ObtenerPosicionRatonEn3D();

                        // Reseteamos la velocidad si lo agarramos de cero
                        velocidadActualFriccion = Vector3.zero;

                        if (flotacionScript != null)
                            flotacionScript.PausarFlotacion(true);
                    }
                }
            }

            // 2. MANTENER PRESIONADO (Arrastre con Inercia)
            if (EstaSiendoArrastrado && Mouse.current.leftButton.isPressed)
            {
                // Acercar o alejar el átomo con la rueda del ratón
                float scroll = Mouse.current.scroll.y.ReadValue();
                if (scroll != 0)
                {
                    distanciaZ += Mathf.Sign(scroll) * velocidadAcercamiento;
                    // Evitamos que atraviese la cámara o se vaya al infinito
                    distanciaZ = Mathf.Clamp(distanciaZ, 2f, 100f);
                }

                // Este es el punto a donde "queremos" que vaya el átomo
                Vector3 posicionObjetivo = ObtenerPosicionRatonEn3D() + offsetRaton;

                // --- LÓGICA DE TIRONEAR (TEARING) ---
                // Si somos hijos de otro átomo, comprobamos si el usuario está tirando con fuerza
                if (transform.parent != null)
                {
                    float distanciaAlPadre = Vector3.Distance(posicionObjetivo, transform.parent.position);
                    // Si el usuario intenta alejar el átomo más de 2.5 veces su radio, se rompe el enlace
                    if (distanciaAlPadre > radioAtomo * 2.5f)
                    {
                        ProyectoDalton.Interfaz.LogN.Info($"<color=orange>Enlace roto:</color> {billboardScript.datos.nombreElemento} se ha separado.");
                        transform.SetParent(null);
                        // Al soltarse, el AtomoFlotante necesita saber su nueva ancla en el mundo
                        if (flotacionScript != null)
                            flotacionScript.FijarNuevaPosicion(transform.localPosition);
                    }
                    else
                    {
                        // Mientras no se rompa, el átomo "se resiste" y no se mueve de su sitio de enlace
                        // (Opcional: podrías moverlo un poquito para dar feedback de tensión)
                        return; 
                    }
                }

                if (ajustarAGrilla && tamanoGrilla > 0)
                {
                    posicionObjetivo.x = Mathf.Round(posicionObjetivo.x / tamanoGrilla) * tamanoGrilla;
                    posicionObjetivo.z = Mathf.Round(posicionObjetivo.z / tamanoGrilla) * tamanoGrilla;
                    posicionObjetivo.y = transform.position.y;
                }

                if (usarInercia)
                {
                    // Leemos el peso TOTAL del compuesto (Teoría de Dalton)
                    float masa = ObtenerMasaTotal();
                    
                    // Calculamos el "Tiempo de Suavizado". A mayor masa, mayor tiempo tarda en alcanzar al ratón (Inercia/Ease-in).
                    float tSuavizado = Mathf.Clamp(Mathf.Sqrt(masa) / agilidadBase, 0.05f, 2.0f);
                    
                    // SmoothDamp actúa como un resorte o elástico físico. Arrastra el objeto con inercia real.
                    transform.position = Vector3.SmoothDamp(transform.position, posicionObjetivo, ref velocidadActualFriccion, tSuavizado);
                }
                else
                {
                    transform.position = posicionObjetivo; // Movimiento instantáneo (sin inercia)
                }
            }

            // 3. SOLTAR EL CLICK
            if (EstaSiendoArrastrado && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                EstaSiendoArrastrado = false;
                
                if (usarInercia)
                {
                    // Al soltarlo, si tiene inercia, dejamos que siga resbalando un poco
                    deslizandosePorInercia = true;
                }
                else
                {
                    // Si no tiene inercia, se frena en seco de inmediato
                    TerminarArrastreYFijar();
                }
            }

            // 4. DESLIZAMIENTO POST-ARRASTRE (Sensación de soltado)
            if (deslizandosePorInercia)
            {
                float masa = flotacionScript != null ? flotacionScript.masaAtomica : 1.0f;
                float factorFriccion = 4f / Mathf.Max(Mathf.Sqrt(masa), 0.5f);
                
                velocidadActualFriccion = Vector3.Lerp(velocidadActualFriccion, Vector3.zero, Time.deltaTime * factorFriccion);
                transform.position += velocidadActualFriccion * Time.deltaTime;

                if (velocidadActualFriccion.magnitude < 0.3f)
                {
                    TerminarArrastreYFijar();
                }
            }
        }

        void LateUpdate()
        {
            // 5. ASEGURAR QUE NO SE SALGA DEL MAPA
            // Lo ponemos en LateUpdate para que actúe DESPUÉS de que AtomoFlotante mueva el átomo.
            // Así la animación de levitación nunca podrá empujar el átomo dentro de la pared.
            LimitarPosicionFisica();
        }

        private void LimitarPosicionFisica()
        {
            // 1. Límite Esférico (Nebulosa)
            if (limiteFisico != null)
            {
                if (limiteFisico is SphereCollider esfera)
                {
                    Vector3 centroEsfera = esfera.transform.TransformPoint(esfera.center);
                    float radioRealEsfera = esfera.radius * Mathf.Max(esfera.transform.lossyScale.x, esfera.transform.lossyScale.y, esfera.transform.lossyScale.z);
                    
                    float radioPermitido = radioRealEsfera - radioAtomo;
                    float distanciaActual = Vector3.Distance(transform.position, centroEsfera);

                    if (distanciaActual > radioPermitido)
                    {
                        // Lo empujamos de vuelta hacia adentro
                        Vector3 direccionDesdeCentro = (transform.position - centroEsfera).normalized;
                        transform.position = centroEsfera + direccionDesdeCentro * radioPermitido;
                        
                        // Si choca contra la pared mientras resbala por inercia, se frena un poco (absorbe el impacto)
                        if (deslizandosePorInercia)
                        {
                            velocidadActualFriccion *= 0.8f; 
                        }
                    }
                }
                else
                {
                    transform.position = limiteFisico.ClosestPoint(transform.position);
                }
            }

            // 2. Límite de Suelo (Plano Y)
            if (transform.position.y < alturaSuelo + radioAtomo)
            {
                Vector3 posicionCorregida = transform.position;
                posicionCorregida.y = alturaSuelo + radioAtomo;
                transform.position = posicionCorregida;

                // Si cae de golpe contra el suelo, matamos el rebote vertical
                if (deslizandosePorInercia)
                {
                    velocidadActualFriccion.y = 0f;
                }
            }

            // 3. Choque entre Átomos (Teoría de Dalton)
            Collider[] vecinos = Physics.OverlapSphere(transform.position, radioAtomo * 1.5f, capaAtomos);
            foreach (var vecino in vecinos)
            {
                if (vecino.gameObject == this.gameObject) continue;

                if (vecino.TryGetComponent<ArrastrarAtomo>(out var scriptVecino))
                {
                    // Identificamos si son del mismo elemento
                    bool mismoTipo = false;
                    if (billboardScript != null && billboardScript.datos != null && 
                        scriptVecino.billboardScript != null && scriptVecino.billboardScript.datos != null)
                    {
                        mismoTipo = billboardScript.datos.nombreElemento == scriptVecino.billboardScript.datos.nombreElemento;
                    }

                    Vector3 direccionDesdeVecino = transform.position - vecino.transform.position;
                    float distancia = direccionDesdeVecino.magnitude;
                    float radioVecino = scriptVecino.radioAtomo;
                    
                    float distanciaMinima = radioAtomo + radioVecino;

                    if (mismoTipo)
                    {
                        // DALTON: Átomos iguales se repelen (10% de margen)
                        distanciaMinima *= 1.1f;

                        if (distancia < distanciaMinima * 1.05f && !EstaSiendoArrastrado && !scriptVecino.EstaSiendoArrastrado)
                        {
                            // Ahora también se "pegan" pero manteniendo el margen de repulsión
                            UnirseA(scriptVecino, distanciaMinima);
                        }
                        else if (distancia < distanciaMinima)
                        {
                            Vector3 normalColision = distancia > 0.001f ? direccionDesdeVecino.normalized : Vector3.up;
                            transform.position = vecino.transform.position + normalColision * distanciaMinima;

                            if (deslizandosePorInercia)
                            {
                                velocidadActualFriccion = Vector3.Reflect(velocidadActualFriccion, normalColision) * 0.6f;
                            }
                        }
                    }
                    else
                    {
                        // DALTON: Átomos diferentes se unen (Combinación Química)
                        if (distancia < distanciaMinima * 1.05f && !EstaSiendoArrastrado && !scriptVecino.EstaSiendoArrastrado)
                        {
                            UnirseA(scriptVecino, distanciaMinima);
                        }
                        else if (distancia < distanciaMinima)
                        {
                            Vector3 normalColision = direccionDesdeVecino.normalized;
                            transform.position = vecino.transform.position + normalColision * distanciaMinima;
                        }
                    }
                }
            }
        }

        private void UnirseA(ArrastrarAtomo otroAtomo, float distanciaDeUnion)
        {
            if (transform.parent == otroAtomo.transform || otroAtomo.transform.parent == transform) return;

            ProyectoDalton.Interfaz.LogN.Info($"<color=cyan>Enlace:</color> {billboardScript.datos.nombreElemento} + {otroAtomo.billboardScript.datos.nombreElemento}");
            
            Vector3 dir = (transform.position - otroAtomo.transform.position).normalized;
            if (dir == Vector3.zero) dir = Vector3.up;

            transform.position = otroAtomo.transform.position + dir * distanciaDeUnion;
            transform.SetParent(otroAtomo.transform);
            
            // NO desactivamos el script, para poder detectar cuando el usuario intente "tironear"
            deslizandosePorInercia = false;
            velocidadActualFriccion = Vector3.zero;

            if (flotacionScript != null)
            {
                flotacionScript.FijarNuevaPosicion(transform.localPosition);
            }
        }

        private void TerminarArrastreYFijar()
        {
            EstaSiendoArrastrado = false;
            AlgunAtomoArrastrado = false; // Liberamos la cámara
            deslizandosePorInercia = false;
            velocidadActualFriccion = Vector3.zero;

            // Si usamos grilla, forzamos que al detenerse quede perfectamente alineado
            if (ajustarAGrilla && tamanoGrilla > 0)
            {
                Vector3 posicionFinalPrecisa = transform.position;
                posicionFinalPrecisa.x = Mathf.Round(posicionFinalPrecisa.x / tamanoGrilla) * tamanoGrilla;
                posicionFinalPrecisa.z = Mathf.Round(posicionFinalPrecisa.z / tamanoGrilla) * tamanoGrilla;
                transform.position = posicionFinalPrecisa;
            }

            // Le avisamos al script de Flotación (vida) que esta es su nueva casa y lo despertamos
            if (flotacionScript != null)
            {
                flotacionScript.FijarNuevaPosicion(transform.localPosition);
                flotacionScript.PausarFlotacion(false);
            }

            // Log de posición final
            if (ajustarAGrilla)
            {
                Vector3 pos = transform.position;
                ProyectoDalton.Interfaz.LogN.Info($"Posición fijada: [{pos.x:F1}, {pos.z:F1}]");
            }
        }

        public float ObtenerMasaTotal()
        {
            // Buscamos la raíz del compuesto (el padre de todos)
            Transform raiz = transform;
            while (raiz.parent != null && raiz.parent.GetComponent<ArrastrarAtomo>() != null)
            {
                raiz = raiz.parent;
            }

            // Sumamos las masas de todos los átomos en esta estructura
            float masaTotal = 0;
            AtomoFlotante[] atomos = raiz.GetComponentsInChildren<AtomoFlotante>();
            foreach (var atomo in atomos)
            {
                masaTotal += atomo.masaAtomica;
            }

            return masaTotal;
        }

        private Vector3 ObtenerPosicionRatonEn3D()
        {
            Vector3 posicionPantalla = Mouse.current.position.ReadValue();
            posicionPantalla.z = distanciaZ;
            return camaraPrincipal.ScreenToWorldPoint(posicionPantalla);
        }

        void OnDisable()
        {
            // Seguro por si se desactiva el objeto mientras lo arrastras
            if (EstaSiendoArrastrado)
            {
                AlgunAtomoArrastrado = false;
            }
        }
    }
}
