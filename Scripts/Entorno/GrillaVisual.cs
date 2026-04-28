using UnityEngine;
using System.Collections;
using ProyectoDalton.Interfaz;

namespace ProyectoDalton.Entorno
{
    /// <summary>
    /// Maneja la animación visual de la grilla del simulador.
    /// </summary>
    public class GrillaVisual : MonoBehaviour
    {
        public static GrillaVisual Instancia { get; private set; }

        [Header("Animación de Entrada")]
        [SerializeField] private Vector3 posicionFuera = new Vector3(0, 0, -50f);
        [SerializeField] private Vector3 posicionFinal = Vector3.zero;
        [SerializeField] private float duracionAnimacion = 1.5f;
        [SerializeField] private AnimationCurve curvaMovimiento = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private void Awake()
        {
            if (Instancia == null) Instancia = this;
            else Destroy(gameObject);

            // Empezamos fuera de vista
            transform.position = posicionFuera;
        }

        /// <summary>
        /// Inicia la animación de entrada de la grilla.
        /// </summary>
        public void Aparecer()
        {
            //LogN.Info("<color=orange>Debug:</color> Grilla iniciando movimiento...");
            StopAllCoroutines();
            StartCoroutine(AnimarEntrada());
        }

        private IEnumerator AnimarEntrada()
        {
            float tiempo = 0;
            Vector3 posInicial = transform.position;

            while (tiempo < duracionAnimacion)
            {
                tiempo += Time.deltaTime;
                float progreso = curvaMovimiento.Evaluate(tiempo / duracionAnimacion);
                transform.position = Vector3.Lerp(posInicial, posicionFinal, progreso);
                yield return null;
            }

            transform.position = posicionFinal;
        }
    }
}
