using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using ProyectoDalton.Atomos;

namespace ProyectoDalton.Interfaz
{
    /// <summary>
    /// Gestiona el menú lateral derecho para la instanciación de átomos.
    /// </summary>
    public class MenuAtomosUI : MonoBehaviour
    {
        private static MenuAtomosUI _instancia;
        
        [Header("Configuración")]
        [SerializeField] private List<DatosAtomoSO> listaAtomosBase;
        
        private VisualElement _menuRaiz;
        private VisualElement _menuHandle;
        private ScrollView _listaContenedor;
        private Button _botonCerrar;
        private bool _estaVisible = false;

        // Diccionario para rastrear cuántos átomos hay de cada tipo y su botón correspondiente
        private Dictionary<string, (int count, VisualElement button)> _registroAtomos = new Dictionary<string, (int count, VisualElement button)>();
        private const int MAX_ATOMOS_POR_TIPO = 20;

        private void Awake()
        {
            _instancia = this;
        }

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _menuRaiz = root.Q<VisualElement>("MenuRaiz");
            _menuHandle = root.Q<VisualElement>("MenuHandle");
            _listaContenedor = root.Q<ScrollView>("ListaAtomos");

            if (_menuRaiz != null)
            {
                // Desactivado por defecto con animación hacia la derecha
                _menuRaiz.AddToClassList("panel--hidden-right");
                _estaVisible = false;
            }

            if (_menuHandle != null)
            {
                // El handle abre el menú al hacer clic, pero solo si el input no está bloqueado
                _menuHandle.RegisterCallback<ClickEvent>(evt => {
                    if (ProyectoDalton.Core.GameManager.Instancia != null && ProyectoDalton.Core.GameManager.Instancia.BloquearInput) return;
                    AlternarMenu();
                });
            }

            _botonCerrar = root.Q<Button>("BotonCerrarMenu");
            if (_botonCerrar != null)
            {
                _botonCerrar.clicked += CerrarMenu;
            }

            PoblarMenu();

            // Suscribirse a los eventos del Átomo y Entorno
            BillboardAtomo.OnAtomoPresenciaCambiada += ManejarPresenciaAtomo;
            BillboardAtomo.OnSolicitarNumeroInstancia += ManejarSolicitudNumero;
            ProyectoDalton.Entorno.ControlEntorno.OnVisualFusionDisparado += ActivarEfectoFusion;
            ProyectoDalton.Entorno.ControlEntorno.OnEstructuraRota += ActivarEfectoRuptura;
        }

        private void OnDisable()
        {
            if (_botonCerrar != null)
                _botonCerrar.clicked -= CerrarMenu;

            // Desuscribirse
            BillboardAtomo.OnAtomoPresenciaCambiada -= ManejarPresenciaAtomo;
            BillboardAtomo.OnSolicitarNumeroInstancia -= ManejarSolicitudNumero;
            ProyectoDalton.Entorno.ControlEntorno.OnVisualFusionDisparado -= ActivarEfectoFusion;
            ProyectoDalton.Entorno.ControlEntorno.OnEstructuraRota -= ActivarEfectoRuptura;
        }

        private void ActivarEfectoFusion(float duracion)
        {
            StopAllCoroutines();
            LimpiarEfectosBorde();
            StartCoroutine(SecuenciaColorBorde(duracion, "fusion"));
        }

        private void ActivarEfectoRuptura(ProyectoDalton.Atomos.ArrastrarAtomo.InformacionCompuesto info)
        {
            StopAllCoroutines();
            LimpiarEfectosBorde();
            StartCoroutine(SecuenciaColorBorde(2.0f, "ruptura"));
        }

        private void LimpiarEfectosBorde()
        {
            if (_menuRaiz == null) return;
            _menuRaiz.RemoveFromClassList("panel-base--fusion");
            _menuRaiz.RemoveFromClassList("panel-base--ruptura");
        }

        private System.Collections.IEnumerator SecuenciaColorBorde(float duracion, string tipo)
        {
            if (_menuRaiz == null) yield break;

            _menuRaiz.AddToClassList($"panel-base--{tipo}");
            yield return new WaitForSeconds(duracion);
            _menuRaiz.RemoveFromClassList($"panel-base--{tipo}");
        }

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null && 
                UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame)
            {
                // Solo permitimos abrir el menú si el input no está bloqueado (ej. por un menú principal superior)
                if (ProyectoDalton.Core.GameManager.Instancia != null && ProyectoDalton.Core.GameManager.Instancia.BloquearInput) return;
                
                AlternarMenu();
            }
        }

        public static MenuAtomosUI Instancia => _instancia;

        public void AlternarMenu()
        {
            if (_menuRaiz == null) return;

            _estaVisible = !_estaVisible;
            
            if (_estaVisible)
            {
                _menuRaiz.RemoveFromClassList("panel--hidden-right");
                _menuHandle?.AddToClassList("menu-handle--hidden");
            }
            else
            {
                _menuRaiz.AddToClassList("panel--hidden-right");
                _menuHandle?.RemoveFromClassList("menu-handle--hidden");
            }
        }

        /// <summary>
        /// Cierra el menú de átomos si está abierto.
        /// </summary>
        public void CerrarMenu()
        {
            if (!_estaVisible || _menuRaiz == null) return;
            _estaVisible = false;
            _menuRaiz.AddToClassList("panel--hidden-right");
            // No restauramos el handle aquí porque MenuPrincipal lo controlará
        }

        private void PoblarMenu()
        {
            if (_listaContenedor == null) return;
            _listaContenedor.Clear();
            _registroAtomos.Clear();

            foreach (var datos in listaAtomosBase)
            {
                DatosAtomoSO d = datos;
                CrearBotonAtomo(d);
            }
        }

        private void CrearBotonAtomo(DatosAtomoSO datos)
        {
            // Creamos el botón como un VisualElement contenedor
            VisualElement boton = new VisualElement();
            boton.AddToClassList("boton-atomo");

            // Icono
            VisualElement icono = new VisualElement();
            icono.AddToClassList("boton-atomo__icono");
            
            if (datos.icono != null)
            {
                icono.style.backgroundImage = new StyleBackground(datos.icono);
            }
            
            // Texto
            string prefijo = datos.esElemento ? "ELEMENTO: " : "ATOMO: ";
            Label nombre = new Label($"{prefijo}{datos.nombreElemento.ToUpper()}");
            nombre.AddToClassList("boton-atomo__texto");
            nombre.AddToClassList("text-base");
            nombre.AddToClassList("text-bold");
            nombre.style.color = datos.colorTextoMenu;

            // --- Botón de Borrado (X) ---
            Label btnBorrar = new Label("✕");
            btnBorrar.AddToClassList("boton-atomo__borrar");
            btnBorrar.tooltip = $"Eliminar todos los átomos de {datos.nombreElemento}";
            
            btnBorrar.RegisterCallback<ClickEvent>(evt => {
                evt.StopPropagation(); // Evita que se instancie un nuevo átomo al borrar
                BorrarTodosLosAtomos(datos);
            });
            
            boton.Add(icono);
            boton.Add(nombre);
            boton.Add(btnBorrar);

            // Registramos el botón para poder bloquearlo luego
            if (!_registroAtomos.ContainsKey(datos.nombreElemento))
            {
                _registroAtomos.Add(datos.nombreElemento, (0, boton));
            }

            // Evento Click Principal (Instanciar)
            boton.RegisterCallback<ClickEvent>(evt => {
                // Verificación de límite antes de instanciar
                if (_registroAtomos.TryGetValue(datos.nombreElemento, out var registro) && registro.count >= MAX_ATOMOS_POR_TIPO)
                {
                    return;
                }
                InstanciarAtomoEnEscena(datos);
            });

            _listaContenedor.Add(boton);
        }

        private void BorrarTodosLosAtomos(DatosAtomoSO datos)
        {
            // Buscamos todos los BillboardAtomo en la escena
            BillboardAtomo[] todos = Object.FindObjectsByType<BillboardAtomo>(FindObjectsSortMode.None);
            int cantidadBorrados = 0;

            foreach (var b in todos)
            {
                if (b.datos == datos)
                {
                    Destroy(b.gameObject);
                    cantidadBorrados++;
                }
            }

            if (cantidadBorrados > 0)
                LogN.Info($"<color=red>Limpieza:</color> Se han eliminado {cantidadBorrados} átomos de {datos.nombreElemento}.");
        }

        private int ManejarSolicitudNumero(DatosAtomoSO datos)
        {
            if (datos == null) return 0;
            if (_registroAtomos.TryGetValue(datos.nombreElemento, out var registro))
            {
                // Devolvemos el número que le tocaría (contador actual + 1)
                return registro.count + 1;
            }
            return 1;
        }

        private void ManejarPresenciaAtomo(DatosAtomoSO datos, bool presente)
        {
            if (datos == null) return;

            if (_registroAtomos.TryGetValue(datos.nombreElemento, out var registro))
            {
                // Actualizamos el contador
                int nuevoContador = presente ? registro.count + 1 : registro.count - 1;
                nuevoContador = Mathf.Max(0, nuevoContador);
                
                _registroAtomos[datos.nombreElemento] = (nuevoContador, registro.button);

                // Actualizamos el estado visual del botón
                if (nuevoContador >= MAX_ATOMOS_POR_TIPO)
                {
                    LogN.Alerta($"LÍMITE ALCANZADO: {datos.nombreElemento} (20/20). Botón bloqueado.");
                    registro.button.style.opacity = 0.3f;
                    registro.button.pickingMode = PickingMode.Ignore;
                }
                else
                {
                    if (presente)
                    {
                        int restantes = MAX_ATOMOS_POR_TIPO - nuevoContador;
                        LogN.Info($"{datos.nombreElemento}: {nuevoContador}/20. Espacio restante: {restantes}.");
                    }
                    
                    registro.button.style.opacity = 1.0f;
                    registro.button.pickingMode = PickingMode.Position;
                }
            }
        }

        private void InstanciarAtomoEnEscena(DatosAtomoSO datos)
        {
            if (datos.prefabAtomo == null)
            {
                LogN.Info($"<color=red>Error:</color> El elemento {datos.nombreElemento} no tiene un prefab asignado.");
                return;
            }

            Camera cam = Camera.main;
            if (cam == null) return;

            // 1. Posición: Centro de la cámara + pequeño offset hacia adelante
            Vector3 spawnPos = cam.transform.position + cam.transform.forward * 2f;
            
            // 2. Instanciación
            GameObject nuevoAtomo = Instantiate(datos.prefabAtomo, spawnPos, Quaternion.identity);
            nuevoAtomo.name = $"Atomo_{datos.nombreElemento}";

            // 3. Configuración de Componentes
            // Asignamos los datos al Billboard
            var billboard = nuevoAtomo.GetComponentInChildren<BillboardAtomo>();
            if (billboard != null)
            {
                billboard.datos = datos;
                billboard.modoDebug = false; // Forzamos modo producción para que lea del SO
            }

            // Asignamos la masa a la flotación
            var flotacion = nuevoAtomo.GetComponentInChildren<AtomoFlotante>();
            if (flotacion != null)
            {
                flotacion.masaAtomica = datos.masaAtomica;
            }

            // 4. Lanzamiento inicial
            var arrastre = nuevoAtomo.GetComponentInChildren<ArrastrarAtomo>();
            if (arrastre != null)
            {
                // Fuerza de lanzamiento
                float fuerzaLanzamiento = 15f; // Un poco más de fuerza para que se note
                arrastre.Lanzar(cam.transform.forward * fuerzaLanzamiento);
            }

            LogN.Info($"Simulando {datos.nombreElemento} ({datos.simbolo})");
            
            // Cerramos el menú para apreciar la simulación
            AlternarMenu();
        }

        public static void AgregarAtomoAlMenu(DatosAtomoSO datos)
        {
            _instancia?.CrearBotonAtomo(datos);
        }
    }
}
