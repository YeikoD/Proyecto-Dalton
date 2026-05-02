using UnityEngine;
using UnityEngine.UIElements;
using ProyectoDalton.Atomos;
using ProyectoDalton.Entorno;

namespace ProyectoDalton.Interfaz
{
    /// <summary>
    /// Gestiona el panel de detalles (Tooltip) que aparece al seleccionar un átomo.
    /// </summary>
    public class DetalleAtomoUI : MonoBehaviour
    {
        private VisualElement _tooltipRaiz;
        private Label _labelNombre;
        private Label _labelSimbolo;
        private Label _labelMasaBase;
        private Label _labelMasa;
        private Label _labelDescripcion;
        private Label _labelFormula;
        private Label _labelComposicion;
        private Label _labelTituloTeoria;
        private Label _labelTituloNotas;
        private VisualElement _simboloBox;
        private VisualElement _lineaTeoria;
        private VisualElement _lineaNotas;
        private VisualElement _iconoMasa;
        private VisualElement _iconoFormula;
        private VisualElement _iconoComposicion;
        private VisualElement _footerClasificacion;
        private VisualElement _footerIcono;

        [Header("Especial Dalton")]
        private VisualElement _filaDalton;
        private Label _labelDalton;
        private VisualElement _iconoDalton;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null) return;

            _tooltipRaiz = root.Q<VisualElement>("TooltipRaiz");
            _labelNombre = root.Q<Label>("NombreElemento");
            _labelSimbolo = root.Q<Label>("SimboloElemento");
            _labelMasaBase = root.Q<Label>("MasaBaseElemento");
            _labelMasa = root.Q<Label>("MasaElemento");
            _labelDescripcion = root.Q<Label>("DescripcionTeorica");
            _labelFormula = root.Q<Label>("FormulaQuimica");
            _labelComposicion = root.Q<Label>("ComposicionMasa");
            _labelTituloTeoria = root.Q<Label>("TituloTeoria");
            _labelTituloNotas = root.Q<Label>("TituloObservaciones");
            _simboloBox = root.Q<VisualElement>("SimboloBox");
            _lineaTeoria = root.Q<VisualElement>("LineTeoria");
            _lineaNotas = root.Q<VisualElement>("LineObservaciones");
            _iconoMasa = root.Q<VisualElement>(className: "icono-balance");
            _iconoFormula = root.Q<VisualElement>(className: "icono-ciencia");
            _iconoComposicion = root.Q<VisualElement>(className: "icono-composicion");
            _footerClasificacion = root.Q<VisualElement>("FooterClasificacion");
            _footerIcono = root.Q<VisualElement>(className: "tooltip__footer-icono");

            // Nuevos campos Dalton
            _filaDalton = root.Q<VisualElement>("FilaCompuestoDalton");
            _labelDalton = root.Q<Label>("LabelCompuestoDalton");
            _iconoDalton = root.Q<VisualElement>("IconoCompuestoDalton");

            if (_tooltipRaiz == null)
            {
                Debug.LogError("[DetalleAtomoUI] No se encontró 'TooltipRaiz' en el UXML. Verifica el nombre.");
            }

            // Suscribirse a los eventos del Átomo
            BillboardAtomo.OnAtomoSeleccionado += Mostrar;
            BillboardAtomo.OnAtomoDeseleccionado += Ocultar;

            // Suscribirse al evento de Fusión
            ControlEntorno.OnVisualFusionDisparado += ActivarEfectoFusion;
            ControlEntorno.OnEstructuraRota += ActivarEfectoRuptura;
        }

        private void OnDisable()
        {
            // Desuscribirse para evitar fugas de memoria
            BillboardAtomo.OnAtomoSeleccionado -= Mostrar;
            BillboardAtomo.OnAtomoDeseleccionado -= Ocultar;
            ControlEntorno.OnVisualFusionDisparado -= ActivarEfectoFusion;
            ControlEntorno.OnEstructuraRota -= ActivarEfectoRuptura;
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
            if (_tooltipRaiz == null) return;
            _tooltipRaiz.RemoveFromClassList("panel-base--fusion");
            _tooltipRaiz.RemoveFromClassList("panel-base--ruptura");
        }

        private System.Collections.IEnumerator SecuenciaColorBorde(float duracion, string tipo)
        {
            if (_tooltipRaiz == null) yield break;

            _tooltipRaiz.AddToClassList($"panel-base--{tipo}");
            yield return new WaitForSeconds(duracion);
            _tooltipRaiz.RemoveFromClassList($"panel-base--{tipo}");
        }

        /// <summary>
        /// Muestra el panel con la información del átomo.
        /// </summary>
        private void Mostrar(DatosAtomoSO datos, ArrastrarAtomo.InformacionCompuesto info)
        {
            if (_tooltipRaiz == null || datos == null) return;

            // 1. Datos Básicos (Cabecera)
            if (_labelNombre != null) 
            {
                _labelNombre.text = datos.nombreElemento.ToUpper();
            }

            if (_labelSimbolo != null)
            {
                _labelSimbolo.text = "Elemento: " + datos.simbolo;
            }

            if (_labelMasaBase != null)
            {
                _labelMasaBase.text = "Masa: " + datos.masaAtomica.ToString("F1") + "u";
            }

            // 2. Teoría (Desde SO)
            if (_labelDescripcion != null) _labelDescripcion.text = datos.descripcionTeorica;

            // 3. Observaciones (Datos Dinámicos del Simulador)
            var inst = ProyectoDalton.Entorno.GestorCompuestosDalton.Instancia;
            var especial = inst?.BuscarCompuestoPorFormula(info.formula);

            if (_labelMasa != null)
            {
                _labelMasa.text = $"Masa Total: {info.masaTotal:F1}u";
            }

            if (_labelFormula != null)
            {
                if (inst == null) ProyectoDalton.Interfaz.LogN.Info("DetalleAtomoUI: El GestorCompuestosDalton no se encuentra en la escena.");
                
                string textoFormula = "Fórmula: " + info.formula;
                
                if (especial != null)
                {
                    textoFormula += $" ({especial.nombreCompuesto})";
                }

                _labelFormula.text = textoFormula;
                _labelFormula.style.display = DisplayStyle.Flex;
            }

            if (_labelComposicion != null)
            {
                _labelComposicion.text = info.desgloseComposicion;
                _labelComposicion.style.display = DisplayStyle.Flex;
            }

            // 4. Compuesto Especial de Dalton
            if (especial != null && _filaDalton != null && _labelDalton != null)
            {
                _labelDalton.text = $"Compuesto: {especial.nombreCompuesto}";
                _filaDalton.style.display = DisplayStyle.Flex;
                
                // Aplicar el color de identidad al icono decorativo definido en CSS
                if (_iconoDalton != null)
                {
                    _iconoDalton.style.unityBackgroundImageTintColor = especial.colorRepresentativo; 
                }
            }
            else if (_filaDalton != null)
            {
                _filaDalton.style.display = DisplayStyle.None;
            }

            // 5. Colores Dinámicos (Identidad del Átomo)
            Color colorIdentidad = datos.colorTextoMenu;
            Color colorSombra = colorIdentidad;
            colorSombra.a = 0.8f; // Sombra vibrante del color del átomo

            Color colorBorde = colorIdentidad;
            colorBorde.a = 0.15f; 

            // Textos destacados (Sólidos)
            if (_labelNombre != null) _labelNombre.style.color = colorIdentidad;
            if (_labelTituloTeoria != null) _labelTituloTeoria.style.color = colorIdentidad;
            if (_labelTituloNotas != null) _labelTituloNotas.style.color = colorIdentidad;

            // Elementos estructurales
            if (_lineaTeoria != null) _lineaTeoria.style.backgroundColor = colorBorde;
            if (_lineaNotas != null) _lineaNotas.style.backgroundColor = colorBorde;
            
            // Tintado dinámico de iconos (para que sigan el color del átomo)
            if (_iconoMasa != null) _iconoMasa.style.unityBackgroundImageTintColor = colorIdentidad;
            if (_iconoFormula != null) _iconoFormula.style.unityBackgroundImageTintColor = colorIdentidad;
            if (_iconoComposicion != null) _iconoComposicion.style.unityBackgroundImageTintColor = colorIdentidad;
            
            // Tintado del footer (borde e icono)
            if (_footerClasificacion != null)
            {
                _footerClasificacion.style.borderLeftColor = colorBorde;
                _footerClasificacion.style.borderRightColor = colorBorde;
                _footerClasificacion.style.borderTopColor = colorBorde;
                _footerClasificacion.style.borderBottomColor = colorBorde;
            }
            
            // if (_footerIcono != null) _footerIcono.style.unityBackgroundImageTintColor = colorIdentidad; // ELIMINADO: El usuario no quiere que cambie de color

            _tooltipRaiz.style.borderLeftColor = colorBorde;
            _tooltipRaiz.style.borderRightColor = colorBorde;
            _tooltipRaiz.style.borderTopColor = colorBorde;
            _tooltipRaiz.style.borderBottomColor = colorBorde;

            // Cambiar icono del box
            if (_simboloBox != null)
            {
                if (datos.icono != null)
                {
                    _simboloBox.style.backgroundImage = new StyleBackground(datos.icono);
                    _simboloBox.style.unityBackgroundImageTintColor = colorIdentidad;
                }
                else
                {
                    _simboloBox.style.backgroundImage = null;
                    _simboloBox.style.unityBackgroundImageTintColor = StyleKeyword.Initial;
                }
            }

            // Mostrar con animación
            _tooltipRaiz.RemoveFromClassList("panel--hidden");
        }

        /// <summary>
        /// Oculta el panel de detalles.
        /// </summary>
        private void Ocultar()
        {
            if (_tooltipRaiz == null) return;
            _tooltipRaiz.AddToClassList("panel--hidden");
        }
    }
}
