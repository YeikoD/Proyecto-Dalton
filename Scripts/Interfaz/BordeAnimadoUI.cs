using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

namespace ProyectoDalton.Interfaz
{
    /// <summary>
    /// Crea una luz que recorre el perímetro de un VisualElement.
    /// </summary>
    public class BordeAnimadoUI : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private string[] nombresPaneles = { "ConsolaRaiz", "MenuRaiz", "TooltipRaiz" };
        [SerializeField] private float velocidad = 200f; 
        [SerializeField] private float largoLuz = 60f;
        [SerializeField] private Color colorLuz = new Color(0.9f, 0.9f, 0.9f, 0.8f);

        private UIDocument _uiDocument;

        private Texture2D _texturaH;
        private Texture2D _texturaV;

        private void Start()
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null) return;

            // Crear texturas de degradado
            _texturaH = CrearTexturaDegradado(32, 1, true);
            _texturaV = CrearTexturaDegradado(1, 32, false);

            StartCoroutine(InicializarTardio());
        }

        private Texture2D CrearTexturaDegradado(int width, int height, bool horizontal)
        {
            Texture2D tex = new Texture2D(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float t = horizontal ? (x / (float)(width - 1)) : (y / (float)(height - 1));
                    float alpha = 1f - Mathf.Abs((t * 2f) - 1f); 
                    tex.SetPixel(x, y, new Color(colorLuz.r, colorLuz.g, colorLuz.b, alpha * colorLuz.a));
                }
            }
            tex.Apply();
            return tex;
        }

        private IEnumerator InicializarTardio()
        {
            yield return new WaitForSeconds(0.2f);
            var root = _uiDocument.rootVisualElement;

            foreach (var nombre in nombresPaneles)
            {
                var panel = root.Q<VisualElement>(nombre);
                if (panel != null) StartCoroutine(AnimarPerimetro(panel));
            }
        }

        private IEnumerator AnimarPerimetro(VisualElement panel)
        {
            while (float.IsNaN(panel.layout.width) || panel.layout.width <= 0) yield return new WaitForSeconds(0.1f);

            VisualElement luz = new VisualElement();
            luz.AddToClassList("border-light");
            luz.pickingMode = PickingMode.Ignore;
            panel.Add(luz);

            while (true)
            {
                float w = panel.layout.width;
                float h = panel.layout.height;
                if (w <= 0 || h <= 0) { yield return new WaitForSeconds(0.5f); continue; }

                // TOP
                luz.style.backgroundImage = new StyleBackground(_texturaH);
                luz.style.width = largoLuz;
                luz.style.height = 3;
                luz.style.top = 1; 
                yield return Mover(luz, true, -largoLuz, w, w / velocidad);

                // RIGHT
                luz.style.backgroundImage = new StyleBackground(_texturaV);
                luz.style.width = 3;
                luz.style.height = largoLuz;
                luz.style.left = w - 4;
                yield return Mover(luz, false, -largoLuz, h, h / velocidad);

                // BOTTOM
                luz.style.backgroundImage = new StyleBackground(_texturaH);
                luz.style.width = largoLuz;
                luz.style.height = 3;
                luz.style.top = h - 4;
                yield return Mover(luz, true, w, -largoLuz, w / velocidad);

                // LEFT
                luz.style.backgroundImage = new StyleBackground(_texturaV);
                luz.style.width = 3;
                luz.style.height = largoLuz;
                luz.style.left = 1;
                yield return Mover(luz, false, h, -largoLuz, h / velocidad);
            }
        }

        private IEnumerator Mover(VisualElement luz, bool esHorizontal, float inicio, float fin, float duracion)
        {
            float tiempo = 0;
            if (duracion <= 0) yield break;

            while (tiempo < duracion)
            {
                tiempo += Time.deltaTime;
                float pos = Mathf.Lerp(inicio, fin, tiempo / duracion);
                
                if (esHorizontal) luz.style.left = pos;
                else luz.style.top = pos;
                
                yield return null;
            }
        }
    }
}
