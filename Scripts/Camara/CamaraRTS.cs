using UnityEngine;
using UnityEngine.InputSystem;
using ProyectoDalton.Entorno;

namespace ProyectoDalton.Camara
{
    public class CamaraRTS : MonoBehaviour
    {
        [Header("Desplazamiento Horizontal (WASD / Flechas)")]
        public float velocidadMovimiento = 20f;
        public float suavizadoMovimiento = 10f;
        public bool moverConBordesPantalla = false;
        public float grosorBorde = 15f;

        [Header("Límites del Mapa (Centralizado)")]
        [Tooltip("Los límites se leen desde ControlEntorno.")]
        public bool usarLimitesMatematicos = true;
        public float limiteMinX = -50f;
        public float limiteMaxX = 50f;
        public float limiteMinZ = -50f;
        public float limiteMaxZ = 50f;

        [Header("Elevación Vertical (Q/E y Rueda del Ratón)")]
        public float velocidadElevacionTeclado = 20f;
        public float velocidadElevacionRueda = 5f;
        public float alturaMinima = 2f;
        public float alturaMaxima = 20f;

        [Header("Evasión de Átomos")]
        [Tooltip("Evita matemáticamente que la cámara atraviese los átomos.")]
        public bool evitarAtomos = true;
        public float radioCamara = 1.0f;
        public LayerMask capaAtomos;

        [Header("Giro e Inclinación (Clic Derecho)")]
        [Tooltip("Velocidad al girar a los lados (Eje X del ratón)")]
        public float velocidadRotacionHorizontal = 30f;
        [Tooltip("Velocidad al mirar arriba/abajo (Eje Y del ratón)")]
        public float velocidadRotacionVertical = 20f;
        public float suavizadoRotacion = 10f;
        [Tooltip("Ángulo mínimo al mirar hacia adelante")]
        public float inclinacionMinima = 5f;
        [Tooltip("Ángulo máximo al mirar directo hacia el suelo")]
        public float inclinacionMaxima = 85f;

        private Vector3 posicionObjetivo;
        private float rotacionObjetivoY; // Giro (Yaw)
        private float inclinacionObjetivoX; // Mirar arriba/abajo (Pitch)

        void Start()
        {
            posicionObjetivo = transform.position;
            rotacionObjetivoY = transform.eulerAngles.y;
            inclinacionObjetivoX = transform.eulerAngles.x;
        }

        void Update()
        {
            if (Keyboard.current == null || Mouse.current == null) return;

            // Si la cámara está bloqueada por el GameManager, no procesamos movimiento ni rotación
            if (ProyectoDalton.Core.GameManager.Instancia != null && ProyectoDalton.Core.GameManager.Instancia.BloquearCamara) return;

            ManejarMovimiento();
            ManejarRotacionEInclinacion();
            EvitarAtomosMatematicamente();

            transform.position = Vector3.Lerp(transform.position, posicionObjetivo, Time.deltaTime * suavizadoMovimiento);

            Quaternion rotacionDeseada = Quaternion.Euler(inclinacionObjetivoX, rotacionObjetivoY, 0);
            transform.rotation = Quaternion.Lerp(transform.rotation, rotacionDeseada, Time.deltaTime * suavizadoRotacion);
        }

        private void ManejarMovimiento()
        {
            Vector3 direccionInput = Vector3.zero;

            // 1. Movimiento Horizontal (Plano XZ)
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) direccionInput.z += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) direccionInput.z -= 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) direccionInput.x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) direccionInput.x += 1f;

            if (moverConBordesPantalla)
            {
                Vector2 posRaton = Mouse.current.position.ReadValue();
                if (posRaton.y >= Screen.height - grosorBorde) direccionInput.z += 1f;
                if (posRaton.y <= grosorBorde) direccionInput.z -= 1f;
                if (posRaton.x >= Screen.width - grosorBorde) direccionInput.x += 1f;
                if (posRaton.x <= grosorBorde) direccionInput.x -= 1f;
            }

            direccionInput = direccionInput.normalized;

            if (direccionInput != Vector3.zero)
            {
                Vector3 forwardAplanado = transform.forward;
                forwardAplanado.y = 0;
                forwardAplanado.Normalize();

                Vector3 rightAplanado = transform.right;
                rightAplanado.y = 0;
                rightAplanado.Normalize();

                Vector3 vectorMovimiento = (forwardAplanado * direccionInput.z) + (rightAplanado * direccionInput.x);
                posicionObjetivo += vectorMovimiento * (velocidadMovimiento * Time.deltaTime);
            }

            // 2. Elevación Vertical (Teclado Q/E y Rueda del Ratón)
            float inputElevacion = 0f;
            
            // Teclado
            if (Keyboard.current.eKey.isPressed) inputElevacion += 1f * velocidadElevacionTeclado * Time.deltaTime; 
            if (Keyboard.current.qKey.isPressed) inputElevacion -= 1f * velocidadElevacionTeclado * Time.deltaTime; 

            // Rueda del Ratón (Solo si NO estamos arrastrando un átomo)
            if (!ProyectoDalton.Atomos.ArrastrarAtomo.AlgunAtomoArrastrado)
            {
                float scroll = Mouse.current.scroll.y.ReadValue();
                if (scroll != 0)
                {
                    // El scroll da valores muy grandes (120), lo normalizamos a 1 o -1
                    inputElevacion += Mathf.Sign(scroll) * velocidadElevacionRueda;
                }
            }

            if (inputElevacion != 0)
            {
                Vector3 intentoElevacion = posicionObjetivo + (Vector3.up * inputElevacion);
                if (intentoElevacion.y >= alturaMinima && intentoElevacion.y <= alturaMaxima)
                {
                    posicionObjetivo = intentoElevacion;
                }
            }

            // 3. Limitar Movimiento (Esfera o Caja Fuerte)
            Collider limiteGlobal = ControlEntorno.Instancia != null ? ControlEntorno.Instancia.limiteFisico : null;

            if (limiteGlobal != null)
            {
                if (limiteGlobal is SphereCollider esfera)
                {
                    Vector3 centroEsfera = esfera.transform.TransformPoint(esfera.center);
                    float radioRealEsfera = esfera.radius * Mathf.Max(esfera.transform.lossyScale.x, esfera.transform.lossyScale.y, esfera.transform.lossyScale.z);
                    
                    float radioPermitido = radioRealEsfera - radioCamara;
                    float distanciaActual = Vector3.Distance(posicionObjetivo, centroEsfera);

                    if (distanciaActual > radioPermitido)
                    {
                        Vector3 direccionDesdeCentro = (posicionObjetivo - centroEsfera).normalized;
                        posicionObjetivo = centroEsfera + direccionDesdeCentro * radioPermitido;
                    }
                }
                else
                {
                    posicionObjetivo = limiteGlobal.ClosestPoint(posicionObjetivo);
                }
            }
            else if (usarLimitesMatematicos)
            {
                posicionObjetivo.x = Mathf.Clamp(posicionObjetivo.x, limiteMinX, limiteMaxX);
                posicionObjetivo.z = Mathf.Clamp(posicionObjetivo.z, limiteMinZ, limiteMaxZ);
            }
        }

        private void ManejarRotacionEInclinacion()
        {
            // Control total de la cabeza con el clic derecho
            if (Mouse.current.rightButton.isPressed)
            {
                // Movimiento horizontal del ratón = Girar a los lados (Yaw)
                float deltaX = Mouse.current.delta.x.ReadValue();
                rotacionObjetivoY += deltaX * velocidadRotacionHorizontal * Time.deltaTime;

                // Movimiento vertical del ratón = Mirar arriba/abajo (Pitch)
                float deltaY = Mouse.current.delta.y.ReadValue();
                // Restamos para invertir el eje y que se sienta como mover la cabeza ("Mouse look" normal)
                inclinacionObjetivoX -= deltaY * velocidadRotacionVertical * Time.deltaTime;
                
                // Limitamos para que no se voltee boca abajo
                inclinacionObjetivoX = Mathf.Clamp(inclinacionObjetivoX, inclinacionMinima, inclinacionMaxima);
            }
        }

        private void EvitarAtomosMatematicamente()
        {
            if (!evitarAtomos) return;

            Collider[] atomosCercanos = Physics.OverlapSphere(posicionObjetivo, radioCamara, capaAtomos);

            foreach (Collider atomo in atomosCercanos)
            {
                Vector3 puntoSuperficie = atomo.ClosestPoint(posicionObjetivo);

                if (puntoSuperficie == posicionObjetivo)
                {
                    Vector3 direccionEscape = (posicionObjetivo - atomo.transform.position).normalized;
                    if (direccionEscape == Vector3.zero) direccionEscape = Vector3.up;
                    posicionObjetivo = atomo.transform.position + direccionEscape * (atomo.bounds.extents.magnitude + radioCamara);
                }
                else
                {
                    float distancia = Vector3.Distance(posicionObjetivo, puntoSuperficie);
                    if (distancia < radioCamara)
                    {
                        Vector3 direccionEscape = (posicionObjetivo - puntoSuperficie).normalized;
                        posicionObjetivo += direccionEscape * (radioCamara - distancia);
                    }
                }
            }
        }
    }
}
