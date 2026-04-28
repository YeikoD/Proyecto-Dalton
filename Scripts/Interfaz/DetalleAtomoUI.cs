using UnityEngine;
using UnityEngine.UIElements;
using ProyectoDalton.Atomos;

namespace ProyectoDalton.Interfaz
{
    /// <summary>
    /// Gestiona el panel de detalles (Tooltip) que aparece al seleccionar un átomo.
    /// </summary>
    public class DetalleAtomoUI : MonoBehaviour
    {
        private static DetalleAtomoUI _instancia;
        
        private VisualElement _tooltipRaiz;
        private Label _labelSimbolo;
        private Label _labelNombre;
        private Label _labelMasa;
        private Label _labelDescripcion;
        private Label _labelNota;
        private Label _labelTituloTeoria;
        private Label _labelTituloNotas;
        private VisualElement _simboloBox;

        private void Awake()
        {
            _instancia = this;
        }

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null) return;

            _tooltipRaiz = root.Q<VisualElement>("TooltipRaiz");
            _labelNombre = root.Q<Label>("NombreElemento");
            _labelMasa = root.Q<Label>("MasaElemento");
            _labelDescripcion = root.Q<Label>("DescripcionTeorica");
            _labelNota = root.Q<Label>("NotaElemento");
            _labelTituloTeoria = root.Q<Label>("TituloTeoria");
            _labelTituloNotas = root.Q<Label>("TituloObservaciones");
            _simboloBox = root.Q<VisualElement>("SimboloBox");

            if (_tooltipRaiz == null)
            {
                Debug.LogError("[DetalleAtomoUI] No se encontró 'TooltipRaiz' en el UXML. Verifica el nombre.");
            }
        }

        /// <summary>
        /// Muestra el panel con la información del átomo.
        /// </summary>
        public static void Mostrar(DatosAtomoSO datos, float masaPersonalizada = -1f)
        {
            if (_instancia == null || _instancia._tooltipRaiz == null || datos == null) return;

            // Rellenar datos
            if (_instancia._labelNombre != null) 
            {
                string prefijo = datos.esElemento ? "ELEMENTO: " : "ATOMO: ";
                _instancia._labelNombre.text = prefijo + datos.nombreElemento.ToUpper();
                _instancia._labelNombre.style.color = datos.colorTextoMenu;
            }

            // Aplicar color a los títulos
            if (_instancia._labelTituloTeoria != null) _instancia._labelTituloTeoria.style.color = datos.colorTextoMenu;
            if (_instancia._labelTituloNotas != null) _instancia._labelTituloNotas.style.color = datos.colorTextoMenu;

            if (_instancia._labelMasa != null)
            {
                float masaAMostrar = masaPersonalizada > 0 ? masaPersonalizada : datos.masaAtomica;
                _instancia._labelMasa.text = $"Masa Total: {masaAMostrar:F1}u";
            }
            if (_instancia._labelDescripcion != null) _instancia._labelDescripcion.text = datos.descripcionTeorica;
            if (_instancia._labelNota != null) _instancia._labelNota.text = datos.nota;

            // Cambiar icono del box
            if (_instancia._simboloBox != null)
            {
                if (datos.icono != null)
                {
                    _instancia._simboloBox.style.backgroundImage = new StyleBackground(datos.icono);
                }
                else
                {
                    _instancia._simboloBox.style.backgroundImage = null;
                }
            }

            // Mostrar con animación
            _instancia._tooltipRaiz.RemoveFromClassList("panel--hidden");
        }

        /// <summary>
        /// Oculta el panel de detalles.
        /// </summary>
        public static void Ocultar()
        {
            if (_instancia == null || _instancia._tooltipRaiz == null) return;
            _instancia._tooltipRaiz.AddToClassList("panel--hidden");
        }
    }
}
