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
                // El handle abre el menú al hacer clic
                _menuHandle.RegisterCallback<ClickEvent>(evt => AlternarMenu());
            }

            var botonCerrar = root.Q<Button>("BotonCerrarMenu");
            if (botonCerrar != null)
            {
                botonCerrar.RegisterCallback<ClickEvent>(evt => AlternarMenu());
            }

            PoblarMenu();
        }

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null && 
                UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame)
            {
                AlternarMenu();
            }
        }

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

        private void PoblarMenu()
        {
            if (_listaContenedor == null) return;
            _listaContenedor.Clear();

            foreach (var datos in listaAtomosBase)
            {
                CrearBotonAtomo(datos);
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

            boton.Add(icono);
            boton.Add(nombre);

            // Registramos el botón para poder bloquearlo luego
            if (!_registroAtomos.ContainsKey(datos.nombreElemento))
            {
                _registroAtomos.Add(datos.nombreElemento, (0, boton));
            }

            // Evento Click
            boton.RegisterCallback<ClickEvent>(evt => {
                // Verificación de límite antes de instanciar
                if (_registroAtomos.TryGetValue(datos.nombreElemento, out var registro) && registro.count >= MAX_ATOMOS_POR_TIPO)
                {
                    LogN.Info($"<color=orange>Límite alcanzado:</color> No puedes tener más de {MAX_ATOMOS_POR_TIPO} átomos de {datos.nombreElemento}.");
                    return;
                }
                InstanciarAtomoEnEscena(datos);
            });

            _listaContenedor.Add(boton);
        }

        public static int NotificarPresenciaAtomo(DatosAtomoSO datos, bool presente)
        {
            if (_instancia == null || datos == null) return 0;

            if (_instancia._registroAtomos.TryGetValue(datos.nombreElemento, out var registro))
            {
                // Actualizamos el contador
                int nuevoContador = presente ? registro.count + 1 : registro.count - 1;
                nuevoContador = Mathf.Max(0, nuevoContador);
                
                _instancia._registroAtomos[datos.nombreElemento] = (nuevoContador, registro.button);

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

                return nuevoContador;
            }

            return 0;
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

            // 4. Lanzamiento inicial y límites físicos
            var arrastre = nuevoAtomo.GetComponentInChildren<ArrastrarAtomo>();
            if (arrastre != null)
            {
                // Intentamos buscar la configuración de límites de la cámara para replicarla
                var camRTS = cam.GetComponent<ProyectoDalton.Camara.CamaraRTS>();
                if (camRTS != null)
                {
                    arrastre.limiteFisico = camRTS.limiteFisico;
                }

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
