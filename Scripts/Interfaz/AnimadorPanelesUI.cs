using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

namespace ProyectoDalton.Interfaz
{
    /// <summary>
    /// Controlador que maneja animaciones cíclicas para elementos de la interfaz
    /// mediante el intercambio de clases USS y el sistema de Transiciones.
    /// </summary>
    public class AnimadorPanelesUI : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private UIDocument _document;
        [SerializeField] private float _duracionCiclo = 1.5f;
        [SerializeField] private string _claseAnimacion = "panel-base--pulse";
        [SerializeField] private string _claseObjetivo = "panel-base";

        private VisualElement _root;
        private bool _enEstadoActivo = false;
        private Coroutine _coroutineAnimacion;

        private void OnEnable()
        {
            if (_document == null) _document = GetComponent<UIDocument>();
            if (_document == null)
            {
                LogN.Alerta($"[AnimadorPanelesUI] No se encontró UIDocument en {gameObject.name}");
                return;
            }

            _root = _document.rootVisualElement;
            _coroutineAnimacion = StartCoroutine(RutinaAnimacion());
        }

        private void OnDisable()
        {
            if (_coroutineAnimacion != null)
            {
                StopCoroutine(_coroutineAnimacion);
            }
        }

        private IEnumerator RutinaAnimacion()
        {
            yield return new WaitForSeconds(0.5f);
            
            while (true)
            {
                _enEstadoActivo = !_enEstadoActivo;
                
                // Refrescamos la lista por si se añadieron paneles nuevos
                var elementos = _root.Query<VisualElement>(className: _claseObjetivo).ToList();

                foreach (var elemento in elementos)
                {
                    if (_enEstadoActivo)
                        elemento.AddToClassList(_claseAnimacion);
                    else
                        elemento.RemoveFromClassList(_claseAnimacion);
                }

                yield return new WaitForSeconds(_duracionCiclo);
            }
        }
    }
}
