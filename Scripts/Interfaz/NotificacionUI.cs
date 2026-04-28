using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using ProyectoDalton.Entorno;

namespace ProyectoDalton.Interfaz
{
    /// <summary>
    /// Gestiona la ventana de notificación superior (efecto "Cine") al formarse compuestos.
    /// </summary>
    public class NotificacionUI : MonoBehaviour
    {
        private VisualElement _notificacionRaiz;
        private Label _tituloLabel;
        private Label _mensajeLabel;
        private Coroutine _rutinaOcultar;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null) return;

            _notificacionRaiz = root.Q<VisualElement>("NotificacionRaiz");
            _tituloLabel = root.Q<Label>("NotificacionTitulo");
            _mensajeLabel = root.Q<Label>("NotificacionMensaje");

            // Nos suscribimos a ambos eventos del entorno
            ControlEntorno.OnCompuestoFormado += MostrarNotificacionCompuesto;
            ControlEntorno.OnCohesionFormada += MostrarNotificacionCohesion;
            ControlEntorno.OnEstructuraRota += MostrarNotificacionRuptura;
        }

        private void OnDisable()
        {
            ControlEntorno.OnCompuestoFormado -= MostrarNotificacionCompuesto;
            ControlEntorno.OnCohesionFormada -= MostrarNotificacionCohesion;
            ControlEntorno.OnEstructuraRota -= MostrarNotificacionRuptura;
        }

        private void MostrarNotificacionCompuesto(ProyectoDalton.Atomos.ArrastrarAtomo.InformacionCompuesto info)
        {
            if (_notificacionRaiz == null) return;

            if (_tituloLabel != null) _tituloLabel.text = "¡NUEVO COMPUESTO!";
            if (_mensajeLabel != null) _mensajeLabel.text = $"Se ha formado {info.formula} con una masa de {info.masaTotal:F1}u.";

            MostrarPanel("fusion");
        }

        private void MostrarNotificacionCohesion(ProyectoDalton.Atomos.ArrastrarAtomo.InformacionCompuesto info)
        {
            if (_notificacionRaiz == null) return;

            if (_tituloLabel != null) _tituloLabel.text = "¡COHESIÓN!";
            if (_mensajeLabel != null) _mensajeLabel.text = $"Las partículas de {info.formula} se mantienen unidas por cohesión ({info.masaTotal:F1}u).";

            MostrarPanel("fusion");
        }

        private void MostrarNotificacionRuptura(ProyectoDalton.Atomos.ArrastrarAtomo.InformacionCompuesto info)
        {
            if (_notificacionRaiz == null) return;

            if (_tituloLabel != null) _tituloLabel.text = "¡ENLACE ROTO!";
            string tipo = info.esCompuesto ? "El compuesto" : "La estructura";
            if (_mensajeLabel != null) _mensajeLabel.text = $"{tipo} {info.formula} se ha separado.";

            MostrarPanel("ruptura");
        }

        private void MostrarPanel(string tipoEfecto)
        {
            // Limpiamos clases previas por si acaso
            _notificacionRaiz.RemoveFromClassList("panel-base--fusion");
            _notificacionRaiz.RemoveFromClassList("panel-accent-bottom--fusion");
            _notificacionRaiz.RemoveFromClassList("panel-base--ruptura");
            _notificacionRaiz.RemoveFromClassList("panel-accent-bottom--ruptura");
            if (_tituloLabel != null)
            {
                _tituloLabel.RemoveFromClassList("notificacion__titulo--fusion");
                _tituloLabel.RemoveFromClassList("notificacion__titulo--ruptura");
            }

            // Mostramos el panel quitando la clase oculta y añadiendo los bordes de fusión
            _notificacionRaiz.RemoveFromClassList("notificacion--hidden");
            _notificacionRaiz.AddToClassList($"panel-base--{tipoEfecto}");
            _notificacionRaiz.AddToClassList($"panel-accent-bottom--{tipoEfecto}");
            if (_tituloLabel != null) _tituloLabel.AddToClassList($"notificacion__titulo--{tipoEfecto}");

            // Si ya hay una rutina esperando para ocultar, la detenemos
            if (_rutinaOcultar != null) StopCoroutine(_rutinaOcultar);

            // Iniciamos la rutina para ocultarlo después de un tiempo
            _rutinaOcultar = StartCoroutine(OcultarDespuesDe(3.5f, tipoEfecto));
        }

        private IEnumerator OcultarDespuesDe(float tiempo, string tipoEfecto)
        {
            yield return new WaitForSeconds(tiempo);
            _notificacionRaiz.AddToClassList("notificacion--hidden");
            
            // Retrasamos un poco quitar el color para que no se vea feo mientras desaparece
            yield return new WaitForSeconds(0.6f); 
            _notificacionRaiz.RemoveFromClassList($"panel-base--{tipoEfecto}");
            _notificacionRaiz.RemoveFromClassList($"panel-accent-bottom--{tipoEfecto}");
            if (_tituloLabel != null) _tituloLabel.RemoveFromClassList($"notificacion__titulo--{tipoEfecto}");
            
            _rutinaOcultar = null;
        }
    }
}
