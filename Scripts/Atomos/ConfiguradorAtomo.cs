using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProyectoDalton.Atomos
{
    /// <summary>
    /// Utilidad de Editor para configurar el átomo visualmente desde el inspector 
    /// de la escena/prefab y exportar los valores al ScriptableObject.
    /// </summary>
    [ExecuteInEditMode]
    public class ConfiguradorAtomo : MonoBehaviour
    {
        [Header("Datos Generales")]
        public string nombreElemento = "Nuevo Átomo";
        public string simbolo = "X";
        public float masaAtomica = 1f;
        public Sprite icono;
        [TextArea(2, 4)]
        public string descripcionTeorica = "";

        [Header("Billboard y UI")]
        public Color colorTextoMenu = Color.white;
        public Color colorLinea = Color.white;

        [Header("Exportación")]
        [Tooltip("Si está vacío, se creará uno nuevo en Assets/Data.")]
        public DatosAtomoSO soDestino;

        private void OnValidate()
        {
            // Aplicar la escala automáticamente según la teoría de Dalton (Raíz cúbica de la masa)
            // Constante base del Hidrógeno: ~0.303
            float masaSegura = Mathf.Max(masaAtomica, 0.1f);
            float escalaCalculada = 0.3032686f * Mathf.Pow(masaSegura, 1f / 3f);
            transform.localScale = Vector3.one * escalaCalculada;
            
            // Opcional: Actualizar el nombre del objeto para mantener el orden
            // if (gameObject.name != "Atomo_" + nombreElemento)
            //     gameObject.name = "Atomo_" + nombreElemento;
        }

#if UNITY_EDITOR
        [ContextMenu("Exportar Configuración al SO (Assets/Data)")]
        public void ExportarAlSO()
        {
            if (soDestino == null)
            {
                // Crear un nuevo SO
                soDestino = ScriptableObject.CreateInstance<DatosAtomoSO>();
                string path = $"Assets/Data/Datos_{nombreElemento}.asset";
                
                // Asegurarse que el directorio existe
                if (!AssetDatabase.IsValidFolder("Assets/Data"))
                {
                    AssetDatabase.CreateFolder("Assets", "Data");
                }

                // Asegurar un nombre único para no sobreescribir sin querer
                path = AssetDatabase.GenerateUniqueAssetPath(path);
                AssetDatabase.CreateAsset(soDestino, path);
                Debug.Log($"[ConfiguradorAtomo] Nuevo SO creado en: {path}");
            }

            // Volcar datos de este configurador al SO
            soDestino.nombreElemento = nombreElemento;
            soDestino.simbolo = simbolo;
            soDestino.masaAtomica = masaAtomica;
            soDestino.icono = icono;
            soDestino.descripcionTeorica = descripcionTeorica;
            soDestino.colorTextoMenu = colorTextoMenu;
            soDestino.colorLinea = colorLinea;

            // Marcar como sucio para que Unity lo guarde
            EditorUtility.SetDirty(soDestino);
            
            // Asignar automáticamente el SO al BillboardAtomo si lo tiene
            BillboardAtomo billboard = GetComponent<BillboardAtomo>();
            if (billboard != null)
            {
                billboard.datos = soDestino;
                EditorUtility.SetDirty(billboard);
            }

            // Guardar assets
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[ConfiguradorAtomo] SO '{soDestino.name}' actualizado exitosamente.");
        }
#endif
    }
}
