using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

[RequireComponent(typeof(Renderer))]
public class FondoNebulosaPro : MonoBehaviour
{
    [Header("Configuración de Colores")]
    [Tooltip("El gradiente se convertirá en una textura de alto rendimiento para el Shader.")]
    public Gradient coloresNebulosa; 
    
    private Material mat;
    private Texture2D rampTex;

    void Start()
    {
        mat = GetComponent<Renderer>().material;
        GenerarTexturaGradiente();
    }

    // Se ejecuta cuando cambias valores en el Inspector, permitiéndote ver cambios en vivo en Play Mode
    void OnValidate()
    {
        if (Application.isPlaying && mat != null)
        {
            GenerarTexturaGradiente();
        }
    }

    void GenerarTexturaGradiente()
    {
        if (mat == null || coloresNebulosa == null) return;

        // Respetando el Bajo Acoplamiento: 
        // Si el material ya tiene una textura asignada y NO es la que generamos nosotros en memoria,
        // significa que el usuario arrastró el PNG horneado al Inspector. 
        // En ese caso, apagamos el generador y no interferimos.
        Texture texturaActual = mat.GetTexture("_ColorRamp");
        if (texturaActual != null && texturaActual != rampTex)
        {
            return;
        }

        int rampResolution = 256;
        if (rampTex == null)
        {
            // Creamos una textura de 256x1 pixeles
            rampTex = new Texture2D(rampResolution, 1, TextureFormat.RGBA32, false);
            rampTex.wrapMode = TextureWrapMode.Clamp; // Clamp para evitar bordes repetidos
        }

        Color[] colors = new Color[rampResolution];
        for (int i = 0; i < rampResolution; i++)
        {
            // Evaluamos el gradiente de 0 a 1
            float t = (float)i / (rampResolution - 1);
            colors[i] = coloresNebulosa.Evaluate(t);
        }

        // Asignamos todos los colores
        rampTex.SetPixels(colors);
        rampTex.Apply();

        // Le pasamos la textura generada al Shader si el material existe
        if (mat != null)
        {
            mat.SetTexture("_ColorRamp", rampTex);
        }
    }

    void OnDestroy()
    {
        // Limpiamos la textura de la memoria al destruir el objeto
        if (rampTex != null)
        {
            Destroy(rampTex);
        }
    }

#if UNITY_EDITOR
    // Este botón aparecerá al hacer clic derecho en el componente en el Inspector
    [ContextMenu("Hornear Gradiente a Archivo PNG")]
    public void GuardarGradienteEnArchivos()
    {
        GenerarTexturaGradiente();
        if (rampTex == null) return;

        // Ruta relativa a la carpeta Assets
        string path = "Assets/Shader/TexturaGradiente_Nebulosa.png";
        
        // Convertimos a PNG y lo guardamos
        byte[] bytes = rampTex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        
        // Le decimos a Unity que actualice sus archivos para que lo vea de inmediato
        AssetDatabase.Refresh();
        
        // Configuramos la textura importada para que use WrapMode = Clamp
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer != null)
        {
            importer.wrapMode = TextureWrapMode.Clamp;
            // Opcional: Desactivar compresión para que los colores sean puros
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        Debug.Log("¡Gradiente horneado con éxito en: " + path + "! Ahora puedes arrastrarlo directo al material.");
    }
#endif
}