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
        private Label _labelMasa;
        private Label _labelDescripcion;
        private Label _labelNota;
        private Label _labelFormula;
        private Label _labelComposicion;
        private Label _labelTituloTeoria;
        private Label _labelTituloNotas;
        private VisualElement _simboloBox;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null) return;

            _tooltipRaiz = root.Q<VisualElement>("TooltipRaiz");
            _labelNombre = root.Q<Label>("NombreElemento");
            _labelMasa = root.Q<Label>("MasaElemento");
            _labelDescripcion = root.Q<Label>("DescripcionTeorica");
            _labelNota = root.Q<Label>("NotaElemento");
            _labelFormula = root.Q<Label>("FormulaQuimica");
            _labelComposicion = root.Q<Label>("ComposicionMasa");
            _labelTituloTeoria = root.Q<Label>("TituloTeoria");
            _labelTituloNotas = root.Q<Label>("TituloObservaciones");
            _simboloBox = root.Q<VisualElement>("SimboloBox");

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

            // 1. Datos Básicos
            if (_labelNombre != null) 
            {
                string prefijo = datos.esElemento ? "ELEMENTO: " : "ATOMO: ";
                _labelNombre.text = prefijo + datos.nombreElemento.ToUpper();
                _labelNombre.style.color = datos.colorTextoMenu;
            }

            if (_labelMasa != null)
            {
                _labelMasa.text = $"Masa Total: {info.masaTotal:F1}u";
            }

            // 2. Matemática de Dalton (Fórmula y Composición)
            if (_labelFormula != null)
            {
                if (info.esCompuesto)
                {
                    _labelFormula.text = "Fórmula: " + info.formula;
                    _labelFormula.style.display = DisplayStyle.Flex;
                }
                else
                {
                    _labelFormula.style.display = DisplayStyle.None;
                }
            }

            if (_labelComposicion != null)
            {
                if (info.esCompuesto)
                {
                    _labelComposicion.text = info.desgloseComposicion;
                    _labelComposicion.style.display = DisplayStyle.Flex;
                }
                else
                {
                    _labelComposicion.style.display = DisplayStyle.None;
                }
            }
            if (_labelDescripcion != null) _labelDescripcion.text = datos.descripcionTeorica;
            if (_labelNota != null) _labelNota.text = datos.nota;

            // Cambiar icono del box
            if (_simboloBox != null)
            {
                if (datos.icono != null)
                {
                    _simboloBox.style.backgroundImage = new StyleBackground(datos.icono);
                }
                else
                {
                    _simboloBox.style.backgroundImage = null;
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
