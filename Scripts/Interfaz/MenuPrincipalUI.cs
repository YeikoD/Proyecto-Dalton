using UnityEngine;
using UnityEngine.UIElements;
using ProyectoDalton.Core;
using ProyectoDalton.Entorno;
using System.Collections;
using System.Collections.Generic;

namespace ProyectoDalton.Interfaz
{
    /// <summary>
    /// Gestiona la pantalla de inicio y el menú de pausa de "Dalton Visualizer".
    /// Refactorizado para mejorar la mantenibilidad y reducir el acoplamiento.
    /// </summary>
    public class MenuPrincipalUI : MonoBehaviour
    {
        #region Constantes Estéticas
        private const string TITULO_PROYECTO = "Proyecto de Química";
        private const string DESARROLLADOR = "Desarrollado por Anderson Correa";
        private readonly string[] INFO_PROYECTO = {
            TITULO_PROYECTO,
            "Liceo Nº 45 - Victor Bersanelli",
            "MODULO 3-3 - Plan 2009",
            "Prof: Federico Rodrigues"
        };
        #endregion

        #region Referencias UI
        private VisualElement _raiz;
        private VisualElement _logoTitulo;
        private VisualElement _logosContenedor;
        private VisualElement _cuadroInfo;
        private VisualElement _menuHandle;
        private Label _labelCreditos;
        private List<Label> _labelsInfo = new List<Label>();

        private Button _botonIniciar;
        private Button _botonVolver;
        private Button _botonLimpiar;
        private Button _botonSalir;
        private Button _botonAjustes;
        #endregion

        private bool _yaIniciado = false;

        private void OnEnable()
        {
            VincularElementos();
            ConfiguracionInicial();
            
            // Iniciamos la coreografía de entrada
            StartCoroutine(AnimarEntradaMenu());
        }

        private void VincularElementos()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            
            // Contenedores
            _raiz = root.Q<VisualElement>("MenuPrincipalRaiz");
            _logoTitulo = root.Q<VisualElement>(className: "titulo-logo");
            _logosContenedor = root.Q<VisualElement>(className: "logos-contenedor");
            _cuadroInfo = root.Q<VisualElement>(className: "cuadro-informacion");
            _menuHandle = root.Q<VisualElement>("MenuHandle");
            
            // Textos
            _labelCreditos = root.Q<Label>(className: "texto-creditos");
            if (_cuadroInfo != null)
            {
                _labelsInfo.Clear();
                _cuadroInfo.Query<Label>().ForEach(lbl => _labelsInfo.Add(lbl));
            }

            // Botones
            _botonIniciar = root.Q<Button>("BotonIniciar");
            _botonVolver = root.Q<Button>("BotonVolver");
            _botonLimpiar = root.Q<Button>("BotonLimpiar");
            _botonSalir = root.Q<Button>("BotonSalir");
            _botonAjustes = root.Q<Button>("BotonAjustes");

            // Eventos
            if (_botonIniciar != null) _botonIniciar.clicked += IniciarSimulacion;
            if (_botonVolver != null)  _botonVolver.clicked += IniciarSimulacion;
            if (_botonLimpiar != null) _botonLimpiar.clicked += LimpiarEscena;
            if (_botonSalir != null)   _botonSalir.clicked += SalirDelSimulador;

            if (_raiz != null) _raiz.RegisterCallback<TransitionEndEvent>(OnTransitionEnd);
        }

        private void ConfiguracionInicial()
        {
            // Limpiar textos para evitar parpadeo antes de las animaciones
            if (_labelCreditos != null) _labelCreditos.text = "";
            foreach (var lbl in _labelsInfo) lbl.text = "";

            // Estado inicial de botones (todo oculto físicamente)
            ActualizarVisibilidadBotones(false, instantaneo: true);
        }

        private void Update()
        {
            HandleEscKey();
        }

        private void HandleEscKey()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null && 
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (!_yaIniciado) return;

                if (GameManager.Instancia != null)
                {
                    if (GameManager.Instancia.BloquearInput) IniciarSimulacion();
                    else MostrarMenu();
                }
            }
        }

        #region Animaciones y Secuencias

        private IEnumerator AnimarEntradaMenu()
        {
            // 1. Logo Principal
            if (_logoTitulo != null)
            {
                yield return new WaitForSeconds(0.3f);
                _logoTitulo.RemoveFromClassList("titulo-logo--hidden");
            }

            yield return new WaitForSeconds(1.2f);

            // 2. Cuadro de Información
            if (_cuadroInfo != null)
            {
                _cuadroInfo.RemoveFromClassList("cuadro-informacion--hidden");
                for (int i = 0; i < Mathf.Min(_labelsInfo.Count, INFO_PROYECTO.Length); i++)
                {
                    yield return StartCoroutine(TypewriterEffect(_labelsInfo[i], INFO_PROYECTO[i], 0.03f));
                    yield return new WaitForSeconds(0.1f);
                }
            }

            // 3. Logos de Compañía
            if (_logosContenedor != null) _logosContenedor.RemoveFromClassList("logos-contenedor--hidden");
            yield return new WaitForSeconds(0.4f);

            // 4. Créditos
            if (_labelCreditos != null)
            {
                _labelCreditos.RemoveFromClassList("texto-creditos--hidden");
                yield return StartCoroutine(TypewriterEffect(_labelCreditos, DESARROLLADOR, 0.04f));
            }

            // 5. Botones y Ajustes
            ActualizarVisibilidadBotones(true);
            
            if (_botonAjustes != null)
            {
                yield return new WaitForSeconds(0.2f);
                _botonAjustes.RemoveFromClassList("boton-ajustes--hidden");
            }

            // 6. Bucle de vida (Glitch/Typewriter)
            if (_labelsInfo.Count > 0) StartCoroutine(LifeLoop(_labelsInfo[0], TITULO_PROYECTO));
        }

        private void MostrarMenu()
        {
            GameManager.Instancia?.SetEstadoBloqueoMenu(true);
            MenuAtomosUI.Instancia?.CerrarMenu();
            ProyectoDalton.Atomos.BillboardAtomo.DeseleccionarTodo();
            _menuHandle?.AddToClassList("menu-handle--hidden");

            if (_raiz != null)
            {
                StopAllCoroutines();
                _raiz.style.display = DisplayStyle.Flex;
                ConsolaUI.Ocultar();
                
                // Mostrar menú de pausa instantáneamente
                StartCoroutine(SequencePauseInstant());
            }
            
            LogN.Info("SYS: Regresando al Menú Principal");
        }

        private IEnumerator SequencePauseInstant()
        {
            yield return null; // Frame de seguridad para UI Toolkit
            _raiz.RemoveFromClassList("menu-principal--hidden");

            // Visibilidad instantánea de todos los elementos
            _logoTitulo?.RemoveFromClassList("titulo-logo--hidden");
            _cuadroInfo?.RemoveFromClassList("cuadro-informacion--hidden");
            _logosContenedor?.RemoveFromClassList("logos-contenedor--hidden");
            _labelCreditos?.RemoveFromClassList("texto-creditos--hidden");
            _botonAjustes?.RemoveFromClassList("boton-ajustes--hidden");

            // Restaurar textos instantáneamente
            for (int i = 0; i < Mathf.Min(_labelsInfo.Count, INFO_PROYECTO.Length); i++)
                _labelsInfo[i].text = INFO_PROYECTO[i];
            
            if (_labelCreditos != null) _labelCreditos.text = DESARROLLADOR;

            ActualizarVisibilidadBotones(true, instantaneo: true);

            if (_labelsInfo.Count > 0) StartCoroutine(LifeLoop(_labelsInfo[0], TITULO_PROYECTO));
        }

        #endregion

        #region Lógica de Negocio

        private void IniciarSimulacion()
        {
            StopAllCoroutines();
            if (!_yaIniciado)
            {
                StartCoroutine(SecuenciaInicioReal());
                _yaIniciado = true;
            }
            else
            {
                StartCoroutine(SecuenciaReanudarRapida());
            }
        }

        private IEnumerator SecuenciaReanudarRapida()
        {
            _raiz?.AddToClassList("menu-principal--hidden");
            GameManager.Instancia?.SetEstadoBloqueoMenu(false);
            ActualizarVisibilidadBotones(false, instantaneo: true);
            _menuHandle?.RemoveFromClassList("menu-handle--hidden");
            
            ConsolaUI.Mostrar();
            LogN.Info("SYS: Simulación reanudada");
            yield break;
        }

        private IEnumerator SecuenciaInicioReal()
        {
            _raiz?.AddToClassList("menu-principal--hidden");
            ActualizarVisibilidadBotones(false, instantaneo: true);

            ConsolaUI.Mostrar();
            LogN.Info("SYS: Iniciando motores de simulación molecular...");
            
            while (ConsolaUI.EstaOcupada) yield return null;
            yield return new WaitForSecondsRealtime(0.3f); 

            // Sincronización de Grilla
            if (GrillaVisual.Instancia != null)
            {
                LogN.Carga("Sincronizando grilla física", 1.5f);
                yield return new WaitForSecondsRealtime(0.2f); 
                GrillaVisual.Instancia.Aparecer();
                while (ConsolaUI.EstaOcupada) yield return null;
            }

            LogN.Carga("Cargando Datos...", 1.5f);
            while (ConsolaUI.EstaOcupada) yield return null;

            LogN.Info("SYS: Visualizador Dalton listo. Pulsa TAB para ver los elementos");
            yield return new WaitForSecondsRealtime(0.2f);

            GameManager.Instancia?.SetEstadoBloqueoMenu(false);
            _menuHandle?.RemoveFromClassList("menu-handle--hidden");
        }

        private void LimpiarEscena()
        {
            var todos = Object.FindObjectsByType<ProyectoDalton.Atomos.BillboardAtomo>(FindObjectsSortMode.None);
            foreach (var b in todos) Destroy(b.gameObject);

            if (todos.Length > 0) LogN.Info($"LIMPIEZA: {todos.Length} elementos eliminados");
            else LogN.Info("Limpieza: No hay elementos.");
        }

        private void SalirDelSimulador()
        {
            LogN.Info("SYS: Cerrando simulador...");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        #endregion

        #region Helpers de UI

        private void ActualizarVisibilidadBotones(bool mostrar, bool instantaneo = false)
        {
            string claseHidden = "boton-iniciar--hidden";

            // Definimos qué botones mostrar según el estado del juego
            bool modoPausa = _yaIniciado;

            ControlarBoton(_botonIniciar, mostrar && !modoPausa, claseHidden);
            ControlarBoton(_botonVolver,  mostrar && modoPausa,  claseHidden);
            ControlarBoton(_botonLimpiar, mostrar && modoPausa,  claseHidden);
            ControlarBoton(_botonSalir,   mostrar,               claseHidden);
        }

        private void ControlarBoton(Button btn, bool visible, string claseHidden)
        {
            if (btn == null) return;
            
            btn.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (visible) btn.RemoveFromClassList(claseHidden);
            else btn.AddToClassList(claseHidden);
        }

        private IEnumerator TypewriterEffect(Label label, string texto, float velocidad)
        {
            for (int c = 0; c <= texto.Length; c++)
            {
                label.text = texto.Substring(0, c);
                yield return new WaitForSeconds(velocidad);
            }
        }

        private IEnumerator BackspaceEffect(Label label, float velocidad)
        {
            string textoActual = label.text;
            for (int c = textoActual.Length; c >= 0; c--)
            {
                label.text = textoActual.Substring(0, c);
                yield return new WaitForSeconds(velocidad);
            }
        }

        private IEnumerator LifeLoop(Label label, string texto)
        {
            while (true)
            {
                yield return new WaitForSeconds(10f);
                
                // Glitch
                for (int i = 0; i < 4; i++)
                {
                    label.style.opacity = 0.2f; yield return new WaitForSeconds(0.06f);
                    label.style.opacity = 0.7f; yield return new WaitForSeconds(0.06f);
                }
                
                yield return new WaitForSeconds(0.2f);
                yield return StartCoroutine(BackspaceEffect(label, 0.03f));
                label.style.opacity = 1f;
                yield return new WaitForSeconds(1f);
                yield return StartCoroutine(TypewriterEffect(label, texto, 0.06f));
            }
        }

        private void OnTransitionEnd(TransitionEndEvent evt)
        {
            if (_raiz != null && _raiz.ClassListContains("menu-principal--hidden"))
            {
                _raiz.style.display = DisplayStyle.None;
            }
        }
        #endregion
    }
}
