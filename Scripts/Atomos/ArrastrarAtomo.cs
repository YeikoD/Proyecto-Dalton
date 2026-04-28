using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using ProyectoDalton.Entorno;

namespace ProyectoDalton.Atomos
{
    [RequireComponent(typeof(Collider))]
    public class ArrastrarAtomo : MonoBehaviour
    {
        // --- ESTRUCTURA PARA EL ANÁLISIS MATEMÁTICO (DALTON) ---
        public struct InformacionCompuesto
        {
            public float masaTotal;
            public string formula;
            public string desgloseComposicion; // Ej: "H: 2.0u (11%) | O: 16.0u (89%)"
            public bool esCompuesto;
        }

        public static event System.Action OnEstructuraCambiada;
        public static event System.Action<InformacionCompuesto> OnEnlaceCreado;
        public static event System.Action<InformacionCompuesto> OnEnlaceRoto;
        [Header("Configuración de Grilla")]
        [Tooltip("Si está activo, el átomo se moverá a saltos, encajando perfectamente en los cuadros de tu suelo.")]
        public bool ajustarAGrilla = true;
        public float tamanoGrilla = 1.0f;

        [Header("Límites del Mapa")]
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

        // Edad del átomo para desempatar dominancia en compuestos
        public float tiempoCreacion { get; private set; }

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
            tiempoCreacion = Time.time;
            flotacionScript = GetComponentInChildren<AtomoFlotante>();
            billboardScript = GetComponentInChildren<BillboardAtomo>();
            camaraPrincipal = Camera.main;

            // Auto-detectamos el radio basado en el SphereCollider y la escala
            SphereCollider col = GetComponent<SphereCollider>();
            if (col == null) col = GetComponentInChildren<SphereCollider>();
            
            if (col != null)
            {
                // El radio real es el radio del collider multiplicado por la escala mayor
                radioAtomo = col.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
            }
        }

        void Start()
        {
            // Ya inicializado en Awake
        }

        void Update()
        {
            if (Mouse.current == null) return;

            // Si el input está bloqueado por el GameManager, no procesamos arrastres
            if (ProyectoDalton.Core.GameManager.Instancia != null && ProyectoDalton.Core.GameManager.Instancia.BloquearInput)
            {
                if (EstaSiendoArrastrado) TerminarArrastreYFijar();
                return;
            }

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
                        
                        // Capturamos la info de la estructura ANTES de separar por completo (opcional) o DESPUÉS
                        // Usualmente la ruptura se comunica para saber qué quedó solo
                        InformacionCompuesto infoRuptura = ObtenerInformacionCompuesto();

                        transform.SetParent(null);
                        
                        // Al soltarse, el AtomoFlotante necesita saber su nueva ancla en el mundo
                        if (flotacionScript != null)
                            flotacionScript.FijarNuevaPosicion(transform.localPosition);

                        // NOTIFICAMOS QUE LA ESTRUCTURA CAMBIÓ (Ruptura)
                        OnEstructuraCambiada?.Invoke();
                        OnEnlaceRoto?.Invoke(infoRuptura);
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
            // Obtenemos el límite centralizado desde el ControlEntorno
            Collider limiteGlobal = ControlEntorno.Instancia != null ? ControlEntorno.Instancia.limiteFisico : null;
            Transform miRoot = transform.root;

            // 1. Límite Esférico (Nebulosa)
            if (limiteGlobal != null)
            {
                if (limiteGlobal is SphereCollider esfera)
                {
                    Vector3 centroEsfera = esfera.transform.TransformPoint(esfera.center);
                    float radioRealEsfera = esfera.radius * Mathf.Max(esfera.transform.lossyScale.x, esfera.transform.lossyScale.y, esfera.transform.lossyScale.z);
                    
                    float radioPermitido = radioRealEsfera - radioAtomo;
                    float distanciaActual = Vector3.Distance(transform.position, centroEsfera);

                    if (distanciaActual > radioPermitido)
                    {
                        // Lo empujamos de vuelta hacia adentro, pero moviendo toda la estructura (root)
                        Vector3 direccionDesdeCentro = (transform.position - centroEsfera).normalized;
                        Vector3 posicionDeseada = centroEsfera + direccionDesdeCentro * radioPermitido;
                        Vector3 desplazamiento = posicionDeseada - transform.position;
                        
                        miRoot.position += desplazamiento;
                        
                        // Si choca contra la pared mientras resbala por inercia, se frena un poco (absorbe el impacto)
                        if (deslizandosePorInercia)
                        {
                            velocidadActualFriccion *= 0.8f; 
                        }
                    }
                }
                else
                {
                    Vector3 puntoCercano = limiteGlobal.ClosestPoint(transform.position);
                    if (puntoCercano != transform.position)
                    {
                        Vector3 desplazamiento = puntoCercano - transform.position;
                        miRoot.position += desplazamiento;
                    }
                }
            }

            // 2. Límite de Suelo (Plano Y)
            if (transform.position.y < alturaSuelo + radioAtomo)
            {
                float dy = (alturaSuelo + radioAtomo) - transform.position.y;
                miRoot.position += Vector3.up * dy;

                // Si cae de golpe contra el suelo, matamos el rebote vertical
                if (deslizandosePorInercia)
                {
                    velocidadActualFriccion.y = 0f;
                }
            }

            // 3. Choque entre Átomos (Teoría de Dalton)
            // Aumentamos el rango de detección (3.0f) para permitir el magnetismo desde más lejos
            Collider[] vecinos = Physics.OverlapSphere(transform.position, radioAtomo * 3.0f, capaAtomos);
            foreach (var vecino in vecinos)
            {
                if (vecino.gameObject == this.gameObject) continue;

                if (vecino.TryGetComponent<ArrastrarAtomo>(out var scriptVecino))
                {
                    // Ignorar colisiones y magnetismo si ya pertenecen al mismo compuesto
                    if (miRoot == scriptVecino.transform.root) continue;

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
                    
                    // Añadimos un pequeño margen de seguridad (1%) para evitar que se queden pegados por precisión
                    float distanciaMinima = (radioAtomo + radioVecino) * 1.01f;

                    if (mismoTipo)
                    {
                        // DALTON: Átomos iguales se repelen (10% de margen)
                        distanciaMinima *= 1.1f;

                        if (distancia < distanciaMinima * 1.05f && !EstaSiendoArrastrado && !scriptVecino.EstaSiendoArrastrado)
                        {
                            UnirseA(scriptVecino, distanciaMinima);
                        }
                        else if (distancia < distanciaMinima)
                        {
                            Vector3 normalColision = distancia > 0.001f ? direccionDesdeVecino.normalized : Vector3.up;
                            Vector3 posicionDeseada = vecino.transform.position + normalColision * distanciaMinima;
                            miRoot.position += (posicionDeseada - transform.position);

                            if (deslizandosePorInercia)
                            {
                                velocidadActualFriccion = Vector3.Reflect(velocidadActualFriccion, normalColision) * 0.6f;
                            }
                        }
                    }
                    else
                    {
                        // DALTON: Átomos diferentes se unen (Combinación Química)
                        float factorMagnetismo = 2.2f;
                        float rangoAtraccion = distanciaMinima * factorMagnetismo;

                        if (distancia < rangoAtraccion)
                        {
                            float tAtraccion = 1.0f - (distancia / rangoAtraccion);
                            tAtraccion = tAtraccion * tAtraccion;

                            Vector3 normalColision = distancia > 0.001f ? direccionDesdeVecino.normalized : Vector3.up;
                            Vector3 puntoUnionIdeal = vecino.transform.position + normalColision * distanciaMinima;
                            
                            float intensidad = EstaSiendoArrastrado ? 3.0f : 10.0f;
                            
                            // Aplicamos el desplazamiento basado en Lerp al root
                            Vector3 nuevaPos = Vector3.Lerp(transform.position, puntoUnionIdeal, Time.deltaTime * tAtraccion * intensidad);
                            miRoot.position += (nuevaPos - transform.position);

                            if (distancia < distanciaMinima * 1.2f && !EstaSiendoArrastrado && !scriptVecino.EstaSiendoArrastrado)
                            {
                                UnirseA(scriptVecino, distanciaMinima);
                            }
                        }

                        // Colisión de seguridad (Hard Collision)
                        if (distancia < distanciaMinima)
                        {
                            Vector3 normalColision = distancia > 0.001f ? direccionDesdeVecino.normalized : Vector3.up;
                            Vector3 posicionDeseada = vecino.transform.position + normalColision * distanciaMinima;
                            miRoot.position += (posicionDeseada - transform.position);
                        }
                    }
                }
            }
        }

        private void UnirseA(ArrastrarAtomo otroAtomo, float distanciaDeUnion)
        {
            if (transform.root == otroAtomo.transform.root) return;

            ArrastrarAtomo rootThis = transform.root.GetComponent<ArrastrarAtomo>();
            ArrastrarAtomo rootOtro = otroAtomo.transform.root.GetComponent<ArrastrarAtomo>();

            // Determinar la raíz dominante (padre)
            float masaThis = rootThis.ObtenerMasaTotal();
            float masaOtro = rootOtro.ObtenerMasaTotal();

            bool thisIsDominant = masaThis > masaOtro || (masaThis == masaOtro && rootThis.tiempoCreacion < rootOtro.tiempoCreacion);

            ArrastrarAtomo dominanteLocal, subordinadoLocal, dominanteRoot, subordinadoRoot;
            
            if (thisIsDominant)
            {
                dominanteLocal = this;
                subordinadoLocal = otroAtomo;
                dominanteRoot = rootThis;
                subordinadoRoot = rootOtro;
            }
            else
            {
                dominanteLocal = otroAtomo;
                subordinadoLocal = this;
                dominanteRoot = rootOtro;
                subordinadoRoot = rootThis;
            }

            ProyectoDalton.Interfaz.LogN.Info($"<color=cyan>Enlace:</color> {subordinadoRoot.billboardScript.datos.nombreElemento} se une a {dominanteRoot.billboardScript.datos.nombreElemento}");
            
            Vector3 dir = (subordinadoLocal.transform.position - dominanteLocal.transform.position).normalized;
            if (dir == Vector3.zero) dir = Vector3.up;

            // Calculamos la posición donde debe quedar físicamente el átomo subordinado local
            Vector3 targetSubordinadoPos = dominanteLocal.transform.position + dir * distanciaDeUnion;
            
            // Movemos toda la raíz del subordinado para que el subordinado local quede en su sitio
            Vector3 offset = targetSubordinadoPos - subordinadoLocal.transform.position;
            subordinadoRoot.transform.position += offset;

            // Anclamos la raíz subordinada a la local dominante
            subordinadoRoot.transform.SetParent(dominanteLocal.transform);
            
            subordinadoRoot.deslizandosePorInercia = false;
            subordinadoRoot.velocidadActualFriccion = Vector3.zero;

            // Al unirse, todos los átomos de la rama subordinada deben pausar su flotación independiente
            // para que toda la molécula se mueva orgánicamente guiada solo por la raíz dominante.
            AtomoFlotante[] flotantesSubordinados = subordinadoRoot.GetComponentsInChildren<AtomoFlotante>();
            foreach(var f in flotantesSubordinados) 
            {
                // Al ser hijos, su posición local cambia, hay que re-anclarla
                f.FijarNuevaPosicion(f.transform.localPosition);
                f.PausarFlotacion(true);
            }

            // Si el dominante local (no la raíz) era también hijo, ya estaba pausado. 
            // La única raíz activa es dominanteRoot.

            // NOTIFICAMOS QUE LA ESTRUCTURA CAMBIÓ (Unión)
            OnEstructuraCambiada?.Invoke();
            OnEnlaceCreado?.Invoke(dominanteRoot.ObtenerInformacionCompuesto());
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

            // Le avisamos al script de Flotación (vida) que esta es su nueva casa.
            // SOLO despertamos si es la raíz del compuesto (no es hijo de nadie más).
            if (flotacionScript != null)
            {
                flotacionScript.FijarNuevaPosicion(transform.localPosition);
                if (transform.parent == null)
                {
                    flotacionScript.PausarFlotacion(false);
                }
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
            return ObtenerInformacionCompuesto().masaTotal;
        }

        public InformacionCompuesto ObtenerInformacionCompuesto()
        {
            // 1. Buscamos la raíz del compuesto
            Transform raiz = transform;
            while (raiz.parent != null && raiz.parent.GetComponent<ArrastrarAtomo>() != null)
            {
                raiz = raiz.parent;
            }

            // 2. Recolectamos todos los átomos de la estructura
            ArrastrarAtomo[] todosLosAtomos = raiz.GetComponentsInChildren<ArrastrarAtomo>();
            
            float masaAcumulada = 0;
            System.Collections.Generic.Dictionary<string, (int count, float masaTotalElemento, string simbolo)> conteoElementos = 
                new System.Collections.Generic.Dictionary<string, (int, float, string)>();

            foreach (var atomo in todosLosAtomos)
            {
                float masaAtomo = atomo.flotacionScript != null ? atomo.flotacionScript.masaAtomica : 1.0f;
                masaAcumulada += masaAtomo;
                
                // Usamos las referencias cacheadas en lugar de buscar componentes
                var bb = atomo.billboardScript;
                if (bb != null && bb.datos != null)
                {
                    string nombre = bb.datos.nombreElemento;
                    string simbolo = bb.datos.simbolo;

                    if (conteoElementos.ContainsKey(nombre))
                    {
                        var data = conteoElementos[nombre];
                        conteoElementos[nombre] = (data.count + 1, data.masaTotalElemento + masaAtomo, simbolo);
                    }
                    else
                    {
                        conteoElementos.Add(nombre, (1, masaAtomo, simbolo));
                    }
                }
            }

            // 3. Generar Fórmula y Desglose (Matemática de Dalton)
            string formulaGenerada = "";
            string desgloseText = "";
            
            foreach (var elem in conteoElementos)
            {
                // Fórmula: Simbolo + Cantidad (si es > 1)
                formulaGenerada += elem.Value.simbolo + (elem.Value.count > 1 ? elem.Value.count.ToString() : "");

                // Porcentajes: (Masa Elemento / Masa Total) * 100
                float porcentaje = (elem.Value.masaTotalElemento / masaAcumulada) * 100f;
                desgloseText += $"{elem.Value.simbolo}: {porcentaje:F0}% | ";
            }

            // Limpiamos el último separador del desglose
            if (desgloseText.Length > 3) desgloseText = desgloseText.Substring(0, desgloseText.Length - 3);

            return new InformacionCompuesto
            {
                masaTotal = masaAcumulada,
                formula = formulaGenerada,
                desgloseComposicion = desgloseText,
                esCompuesto = conteoElementos.Count > 1
            };
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
