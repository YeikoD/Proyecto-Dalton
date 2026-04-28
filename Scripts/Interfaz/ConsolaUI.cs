using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

namespace ProyectoDalton.Interfaz
{
    public enum TipoLog { Normal, Alerta, Carga }

    public struct DatosMensaje
    {
        public string texto;
        public TipoLog tipo;
        public float duracionEspecial;

        public DatosMensaje(string texto, TipoLog tipo, float duracion = 0)
        {
            this.texto = texto;
            this.tipo = tipo;
            this.duracionEspecial = duracion;
        }
    }

    /// <summary>
    /// Controlador de la consola en pantalla con soporte para acciones especiales como carga.
    /// </summary>
    public class ConsolaUI : MonoBehaviour
    {
        private static ConsolaUI _instancia;
        
        [Header("Configuración")]
        [SerializeField] private int maxMensajes = 10;
        [SerializeField] private float caracteresPorSegundo = 1000f;
        
        private VisualElement _contenedorMensajes;
        private ScrollView _scrollView;
        private Queue<Label> _labelsEnPantalla = new Queue<Label>();
        private Queue<DatosMensaje> _colaMensajes = new Queue<DatosMensaje>();
        private bool _estaProcesandoCola = false;
        private Coroutine _rutinaFusion;

        private void Awake()
        {
            _instancia = this;
        }

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _contenedorMensajes = root.Q<VisualElement>("ContenedorMensajes");
            _scrollView = root.Q<ScrollView>("ContenedorMensajes");
            
            if (_contenedorMensajes == null) return;

            // La consola ahora permanece oculta hasta que el Menú Principal o un evento la active.

            // Mensaje de sistema
            Escribir(new DatosMensaje("Consola de diagnóstico activa.", TipoLog.Normal));

            // Suscribirse a los eventos de entorno
            ProyectoDalton.Entorno.ControlEntorno.OnVisualFusionDisparado += ActivarEfectoFusion;
            ProyectoDalton.Entorno.ControlEntorno.OnEstructuraRota += ActivarEfectoRuptura;
        }

        private void OnDisable()
        {
            ProyectoDalton.Entorno.ControlEntorno.OnVisualFusionDisparado -= ActivarEfectoFusion;
            ProyectoDalton.Entorno.ControlEntorno.OnEstructuraRota -= ActivarEfectoRuptura;
        }

        private void ActivarEfectoFusion(float duracion)
        {
            if (_rutinaFusion != null) StopCoroutine(_rutinaFusion);
            LimpiarEfectosBorde();
            _rutinaFusion = StartCoroutine(SecuenciaColorBorde(duracion, "fusion"));
        }

        private void ActivarEfectoRuptura(ProyectoDalton.Atomos.ArrastrarAtomo.InformacionCompuesto info)
        {
            if (_rutinaFusion != null) StopCoroutine(_rutinaFusion);
            LimpiarEfectosBorde();
            _rutinaFusion = StartCoroutine(SecuenciaColorBorde(2.0f, "ruptura"));
        }

        private void LimpiarEfectosBorde()
        {
            var consolaRaiz = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("ConsolaRaiz");
            if (consolaRaiz == null) return;
            consolaRaiz.RemoveFromClassList("panel-base--fusion");
            consolaRaiz.RemoveFromClassList("panel-accent-left--fusion");
            consolaRaiz.RemoveFromClassList("panel-base--ruptura");
            consolaRaiz.RemoveFromClassList("panel-accent-left--ruptura");
        }

        private IEnumerator SecuenciaColorBorde(float duracion, string tipo)
        {
            var consolaRaiz = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("ConsolaRaiz");
            if (consolaRaiz == null) yield break;

            consolaRaiz.AddToClassList($"panel-base--{tipo}");
            consolaRaiz.AddToClassList($"panel-accent-left--{tipo}");
            
            yield return new WaitForSeconds(duracion);
            
            consolaRaiz.RemoveFromClassList($"panel-base--{tipo}");
            consolaRaiz.RemoveFromClassList($"panel-accent-left--{tipo}");
        }

        private void Update()
        {
            // Atajo de teclado: la tecla encima de Tab (| en teclados latinos / ` en US)
            if (UnityEngine.InputSystem.Keyboard.current != null && 
                UnityEngine.InputSystem.Keyboard.current.backquoteKey.wasPressedThisFrame)
            {
                AlternarVisibilidad();
            }
        }

        public static void Escribir(DatosMensaje datos)
        {
            if (_instancia == null || _instancia._contenedorMensajes == null) return;
            _instancia._colaMensajes.Enqueue(datos);
            if (!_instancia._estaProcesandoCola) _instancia.StartCoroutine(_instancia.ProcesarCola());
        }

        private IEnumerator ProcesarCola()
        {
            _estaProcesandoCola = true;
            while (_colaMensajes.Count > 0)
            {
                var datos = _colaMensajes.Dequeue();
                yield return StartCoroutine(EjecutarMensaje(datos));
                yield return null; // Sin espera entre mensajes
            }
            _estaProcesandoCola = false;
        }

        private IEnumerator EjecutarMensaje(DatosMensaje datos)
        {
            Label nuevoLabel = new Label("");
            nuevoLabel.AddToClassList("console-label");
            nuevoLabel.AddToClassList("text-base");
            
            if (datos.tipo == TipoLog.Alerta)
            {
                nuevoLabel.AddToClassList("console-label--important");
                nuevoLabel.AddToClassList("text-bold");
            }

            _contenedorMensajes.Add(nuevoLabel);
            _labelsEnPantalla.Enqueue(nuevoLabel);

            if (_labelsEnPantalla.Count > maxMensajes)
                _contenedorMensajes.Remove(_labelsEnPantalla.Dequeue());

            // 1. Escritura del texto con soporte para múltiples caracteres por frame
            float caracteresAcumulados = 0;
            int indiceUltimoCaracter = 0;

            while (indiceUltimoCaracter < datos.texto.Length)
            {
                caracteresAcumulados += caracteresPorSegundo * Time.deltaTime;
                int caracteresAEscribir = Mathf.FloorToInt(caracteresAcumulados);
                
                if (caracteresAEscribir > indiceUltimoCaracter)
                {
                    indiceUltimoCaracter = Mathf.Min(caracteresAEscribir, datos.texto.Length);
                    nuevoLabel.text = datos.texto.Substring(0, indiceUltimoCaracter);
                    HacerScrollAlFinal();
                }
                
                yield return null; // Esperamos al siguiente frame
            }

            // 2. Si es tipo Carga, simular puntos suspensivos
            if (datos.tipo == TipoLog.Carga)
            {
                float tiempoPasado = 0;
                int puntos = 0;
                string textoBase = datos.texto;

                while (tiempoPasado < datos.duracionEspecial)
                {
                    puntos = (puntos + 1) % 4;
                    nuevoLabel.text = textoBase + new string('.', puntos);
                    
                    float intervaloPuntos = 0.5f;
                    yield return new WaitForSeconds(intervaloPuntos);
                    tiempoPasado += intervaloPuntos;
                }
                // Al terminar, dejamos los 3 puntos fijos
                nuevoLabel.text = textoBase + "... [OK]";
            }
        }

        /// <summary>
        /// Cambia la visibilidad de la consola con animación.
        /// </summary>
        public void SetVisibilidad(bool visible)
        {
            var consolaRaiz = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("ConsolaRaiz");
            if (consolaRaiz == null) return;

            if (visible)
            {
                consolaRaiz.RemoveFromClassList("panel--hidden");
            }
            else
            {
                consolaRaiz.AddToClassList("panel--hidden");
            }
        }

        public static void Mostrar() => _instancia?.SetVisibilidad(true);
        public static void Ocultar() => _instancia?.SetVisibilidad(false);
        
        public static void AlternarVisibilidad()
        {
            if (_instancia == null) return;
            var consolaRaiz = _instancia.GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("ConsolaRaiz");
            if (consolaRaiz == null) return;

            bool estaVisible = !consolaRaiz.ClassListContains("panel--hidden");
            _instancia.SetVisibilidad(!estaVisible);
        }

        public static bool EstaOcupada => _instancia != null && (_instancia._estaProcesandoCola || _instancia._colaMensajes.Count > 0);

        private void HacerScrollAlFinal()
        {
            if (_scrollView != null)
                _scrollView.scrollOffset = new Vector2(0, _scrollView.contentContainer.layout.height);
        }
    }

    public static class LogN
    {
        public static void Info(string mensaje) => ConsolaUI.Escribir(new DatosMensaje(mensaje, TipoLog.Normal));
        public static void Alerta(string mensaje) => ConsolaUI.Escribir(new DatosMensaje(mensaje, TipoLog.Alerta));
        public static void Carga(string mensaje, float duracion = 3f) => ConsolaUI.Escribir(new DatosMensaje(mensaje, TipoLog.Carga, duracion));
    }
}
