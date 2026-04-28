using UnityEngine;
using UnityEngine.InputSystem;
using System;
using TMPro;

namespace ProyectoDalton.Atomos
{
    /// <summary>
    /// Muestra un panel de información flotante (Billboard) junto al átomo.
    /// Incluye una línea "L" tipo callout que va del átomo al texto y lo subraya.
    /// Aparece en Hover y se mantiene al hacer Click. Se oculta mientras se arrastra.
    /// </summary>
    [RequireComponent(typeof(ArrastrarAtomo))]
    [RequireComponent(typeof(LineRenderer))]
    public class BillboardAtomo : MonoBehaviour
    {
        [Header("Debug")]
        [Tooltip("TRUE: usa los valores del propio script (ideal para ajustar parámetros en Play Mode).\nFALSE: lee todo desde el ScriptableObject (modo producción).")]
        public bool modoDebug = false;
        [Header("Datos del Átomo")]
        [Tooltip("Arrastra aquí el ScriptableObject con los datos de este elemento.")]
        public DatosAtomoSO datos;

        [Header("Referencias UI")]
        [Tooltip("El objeto hijo que contiene todo el panel del Billboard.")]
        public GameObject panelBillboard;
        [Tooltip("El TextMeshPro donde se mostrará el nombre del elemento.")]
        public TMP_Text textoNombre;

        [Header("Línea de Puntero (Callout)")]
        [Tooltip("Largo del subrayado horizontal debajo del texto.")]
        public float largoSubrayado = 1.2f;
        [Tooltip("Cuánto baja el codo respecto al panel de texto.")]
        public float offsetVerticalCodo = -0.15f;
        [Tooltip("Altura de la línea vertical de cierre (ajustá hasta que coincida con el texto).")]
        public float alturaLineaVertical = 0.15f;
        public float anchoLinea = 0.015f;
        public Color colorLinea = new Color(1f, 1f, 1f, 0.7f);

        [Header("Inercia del Billboard")]
        [Tooltip("Qué tan lento gira el panel para seguir a la cámara. 0 = instantáneo, 1 = muy lento.")]
        [Range(0f, 0.99f)]
        public float inerciaBillboard = 0.85f;

        [Header("Visibilidad por Distancia")]
        [Tooltip("El billboard se apaga automáticamente si la cámara está más lejos que esto.")]
        public float distanciaMaxima = 20f;

        // Control de visibilidad
        private bool seleccionado = false;
        private bool enHover = false;
        private ArrastrarAtomo scriptArrastre;
        private Camera camaraPrincipal;
        private LineRenderer lr;

        // Selección global: solo un átomo puede estar "seleccionado" a la vez
        // Selección global: solo un átomo puede estar "seleccionado" a la vez
        private static BillboardAtomo atomoSeleccionadoActual;

        // --- EVENTOS (BAJO ACOPLAMIENTO) ---
        public static event Action<DatosAtomoSO, ArrastrarAtomo.InformacionCompuesto> OnAtomoSeleccionado;
        public static event Action OnAtomoDeseleccionado;
        public static event Action<DatosAtomoSO, bool> OnAtomoPresenciaCambiada; // bool: true=creado, false=destruido
        public static event Func<DatosAtomoSO, int> OnSolicitarNumeroInstancia;

        /// <summary>
        /// Deselecciona cualquier átomo que esté activo actualmente.
        /// Útil para limpiar la interfaz al abrir menús superiores.
        /// </summary>
        public static void DeseleccionarTodo()
        {
            if (atomoSeleccionadoActual != null)
            {
                atomoSeleccionadoActual.Deseleccionar();
            }
        }

        void Start()
        {
            scriptArrastre = GetComponent<ArrastrarAtomo>();
            camaraPrincipal = Camera.main;
            lr = GetComponent<LineRenderer>();

            // Suscribirse a cambios en la estructura para refrescar la UI en vivo
            ArrastrarAtomo.OnEstructuraCambiada += RefrescarUI;

            // Si modoDebug=false aplicamos la config del SO (modo producción).
            // Si modoDebug=true se usan los valores del propio script para ajuste en vivo.
            if (!modoDebug && datos != null)
            {
                // Notificamos a través de eventos (quien escuche, que responda con el número)
                int miNumero = 0;
                if (OnSolicitarNumeroInstancia != null)
                {
                    miNumero = OnSolicitarNumeroInstancia.Invoke(datos);
                }
                
                // Notificamos que hemos nacido
                OnAtomoPresenciaCambiada?.Invoke(datos, true);

                if (textoNombre != null) 
                {
                    string prefijo = datos.esElemento ? "ELEMENTO: " : "ATOMO: ";
                    textoNombre.text = $"#{miNumero} {prefijo}{datos.nombreElemento.ToUpper()}";
                }

                largoSubrayado      = datos.largoSubrayado;
                offsetVerticalCodo  = datos.offsetVerticalCodo;
                alturaLineaVertical = datos.alturaLineaVertical;
                anchoLinea          = datos.anchoLinea;
                colorLinea          = datos.colorLinea;
                inerciaBillboard    = datos.inerciaBillboard;
                distanciaMaxima     = datos.distanciaMaxima;
            }
            else if (datos != null && textoNombre != null)
            {
                // En debug igual mostramos el nombre del SO
                string prefijo = datos.esElemento ? "ELEMENTO: " : "ATOMO: ";
                textoNombre.text = prefijo + datos.nombreElemento.ToUpper();
            }

            ConfigurarLineRenderer();

            // Empezamos ocultos
            if (panelBillboard != null) panelBillboard.SetActive(false);
            lr.enabled = false;
        }

        /// <summary>
        /// Copia los valores actuales del script al ScriptableObject para guardarlos.
        /// Uso: Ajustá los valores en ModoDebug durante Play Mode, luego click derecho
        /// sobre el componente → "Guardar valores en SO".
        /// </summary>
        [ContextMenu("Guardar valores actuales en SO")]
        private void GuardarValoresEnSO()
        {
            if (datos == null)
            {
                return;
            }

            datos.largoSubrayado      = largoSubrayado;
            datos.offsetVerticalCodo  = offsetVerticalCodo;
            datos.alturaLineaVertical = alturaLineaVertical;
            datos.anchoLinea          = anchoLinea;
            datos.colorLinea          = colorLinea;
            datos.inerciaBillboard    = inerciaBillboard;
            datos.distanciaMaxima     = distanciaMaxima;

            // Marcamos el SO como modificado para que Unity lo guarde en el asset
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(datos);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        void Update()
        {
            ActualizarVisibilidad();
        }

        void LateUpdate()
        {
            if (panelBillboard == null || !panelBillboard.activeSelf) return;

            // 1. Rotación Billboard con inercia suavizada:
            // Calculamos la rotación objetivo (mirando a la cámara) y hacemos Slerp hacia ella.
            // 'inerciaBillboard' controla qué tan perezosamente sigue al objetivo:
            // cerca de 0 = instantáneo, cerca de 1 = muy lento y fluido.
            Vector3 direccionACamara = panelBillboard.transform.position - camaraPrincipal.transform.position;
            if (direccionACamara != Vector3.zero)
            {
                Quaternion rotacionObjetivo = Quaternion.LookRotation(
                    direccionACamara,
                    camaraPrincipal.transform.up
                );
                panelBillboard.transform.rotation = Quaternion.Slerp(
                    panelBillboard.transform.rotation,
                    rotacionObjetivo,
                    1f - inerciaBillboard
                );
            }

            // 2. Actualizar línea callout en L
            ActualizarLineaCallout();
        }

        private void ConfigurarLineRenderer()
        {
            lr.useWorldSpace = true;
            lr.positionCount = 3;
            lr.startWidth = anchoLinea;
            lr.endWidth = anchoLinea;
            lr.numCornerVertices = 4;
            lr.numCapVertices = 4;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = colorLinea;
            lr.endColor = colorLinea;
        }

        private void ActualizarLineaCallout()
        {
            if (textoNombre == null) return;

            RectTransform rt = textoNombre.rectTransform;
            Transform panel  = panelBillboard.transform;

            // Usamos el espacio LOCAL del panel para TODAS las direcciones.
            // El panel ya apunta hacia la cámara (LookRotation en LateUpdate),
            // así que su right/up coinciden exactamente con los ejes visuales de la cámara.
            // Esto garantiza que texto y línea siempre estén en el mismo plano,
            // sin importar el ángulo de la cámara (lateral, top-down, desde abajo, etc).
            Vector3 panelRight = panel.right;
            Vector3 panelUp    = panel.up;   // "arriba" visual en la vista de la cámara

            // Ancho real del texto en unidades de mundo
            float anchoTextoMundo = textoNombre.preferredWidth * rt.lossyScale.x;

            // Centro del texto en mundo
            Vector3 centroTexto = rt.position;

            // ¿El texto está a la derecha o izquierda del átomo en el plano del panel?
            Vector3 dirAtomoATexto = centroTexto - transform.position;
            float signo = Vector3.Dot(dirAtomoATexto, panelRight) >= 0f ? 1f : -1f;

            // Nivel del subrayado: bajamos en el espacio del panel (panel.up negativo = abajo visual)
            // offsetVerticalCodo debe ser negativo (ej: -0.15) para ir debajo del texto
            Vector3 nivelSubrayado = centroTexto + panelUp * offsetVerticalCodo;

            // Codo = borde de ENTRADA donde llega la diagonal
            Vector3 codo = nivelSubrayado - panelRight * (anchoTextoMundo * 0.5f * signo);

            // Extremo = borde de SALIDA hasta la última letra + margen
            Vector3 extremo = nivelSubrayado + panelRight * (anchoTextoMundo * 0.5f * signo)
                              + panelRight * largoSubrayado * signo;

            // Línea vertical de cierre: sube desde el extremo del subrayado
            // 'alturaLineaVertical' se ajusta directamente desde el Inspector.
            Vector3 cimaCierre = extremo + panelUp * (alturaLineaVertical + Mathf.Abs(offsetVerticalCodo));

            lr.positionCount = 4;
            lr.SetPosition(0, transform.position); // átomo
            lr.SetPosition(1, codo);               // inicio del subrayado
            lr.SetPosition(2, extremo);            // fin del subrayado
            lr.SetPosition(3, cimaCierre);         // línea vertical de cierre → cima del texto
        }

        private void ActualizarVisibilidad()
        {
            // Prioridad 0: Distancia máxima → siempre oculto si está lejos
            if (Vector3.Distance(transform.position, camaraPrincipal.transform.position) > distanciaMaxima)
            {
                MostrarPanel(false);
                return;
            }

            // Prioridad 1: Selección y Hover
            if (Mouse.current != null)
            {
                // Si el input está bloqueado por el GameManager (ej. menú de pausa), no permitimos interactuar
                if (ProyectoDalton.Core.GameManager.Instancia != null && ProyectoDalton.Core.GameManager.Instancia.BloquearInput)
                {
                    enHover = false;
                    return;
                }

                Ray rayo = camaraPrincipal.ScreenPointToRay(Mouse.current.position.ReadValue());
                bool ratonEncima = Physics.Raycast(rayo, out RaycastHit hit) && hit.collider.gameObject == this.gameObject;
                enHover = ratonEncima;

                if (enHover && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    Seleccionar();
                }
            }

            // Prioridad 2: Arrastre → siempre oculto el panel, pero mantenemos el estado de selección
            if (scriptArrastre != null && scriptArrastre.EstaSiendoArrastrado)
            {
                MostrarPanel(false);
                return;
            }

            // Prioridad 3: Seleccionado → siempre visible
            if (seleccionado)
            {
                MostrarPanel(true);
                // Click fuera = deseleccionar
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    Ray rayo = camaraPrincipal.ScreenPointToRay(Mouse.current.position.ReadValue());
                    if (!Physics.Raycast(rayo, out RaycastHit hit) || hit.collider.gameObject != this.gameObject)
                        Deseleccionar();
                }
                return;
            }

            // Prioridad 4: Hover normal
            MostrarPanel(enHover);
        }

        private void Seleccionar()
        {
            // Si ya está seleccionado, no repetimos la lógica para evitar spam en consola
            if (seleccionado) return;

            if (atomoSeleccionadoActual != null && atomoSeleccionadoActual != this)
                atomoSeleccionadoActual.Deseleccionar();

            seleccionado = true;
            atomoSeleccionadoActual = this;

            // Log a la consola y mostrar detalle UI
            if (datos != null)
            {
                ArrastrarAtomo.InformacionCompuesto info = scriptArrastre != null ? 
                    scriptArrastre.ObtenerInformacionCompuesto() : 
                    new ArrastrarAtomo.InformacionCompuesto { masaTotal = datos.masaAtomica, esCompuesto = false };

                ProyectoDalton.Interfaz.LogN.Info($"Elemento seleccionado: {datos.nombreElemento} ({datos.simbolo}) | Masa: {info.masaTotal:F1}u");
                
                // Disparamos el evento con toda la matemática calculada
                OnAtomoSeleccionado?.Invoke(datos, info);
            }
        }

        private void Deseleccionar()
        {
            if (seleccionado)
            {
                ProyectoDalton.Interfaz.LogN.Info("Elemento deseleccionado.");
                OnAtomoDeseleccionado?.Invoke();
            }
            
            seleccionado = false;
            if (atomoSeleccionadoActual == this)
                atomoSeleccionadoActual = null;
        }

        private void MostrarPanel(bool mostrar)
        {
            if (panelBillboard != null && panelBillboard.activeSelf != mostrar)
                panelBillboard.SetActive(mostrar);

            if (lr != null && lr.enabled != mostrar)
                lr.enabled = mostrar;
        }

        private void RefrescarUI()
        {
            // Solo si este átomo es el que el usuario está viendo en el panel de detalles
            if (seleccionado)
            {
                // Re-calculamos y disparamos el evento (esto actualizará la masa y fórmula en vivo)
                if (datos != null)
                {
                    ArrastrarAtomo.InformacionCompuesto info = scriptArrastre != null ? 
                        scriptArrastre.ObtenerInformacionCompuesto() : 
                        new ArrastrarAtomo.InformacionCompuesto { masaTotal = datos.masaAtomica, esCompuesto = false };

                    OnAtomoSeleccionado?.Invoke(datos, info);
                }
            }
        }

        void OnDisable()
        {
            Deseleccionar();
            ArrastrarAtomo.OnEstructuraCambiada -= RefrescarUI;
        }

        void OnDestroy()
        {
            // Notificamos que nos vamos
            if (!modoDebug && datos != null)
            {
                OnAtomoPresenciaCambiada?.Invoke(datos, false);
            }
        }
    }
}
