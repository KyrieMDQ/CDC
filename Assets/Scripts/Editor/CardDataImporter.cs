// CardDataImporter.cs
using UnityEngine;
using UnityEditor;
using System.Collections;
using Unity.EditorCoroutines.Editor; 
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class APIResponse
{
    public CartaData[] data;
    public int count;
}

[System.Serializable]
public class CartaData 
{
    public string idUnico;
    public string NombreCarta;
    public int CostoPoderPolitico;
    public string Faccion;
    public string TipoCarta;
    public int Influencia;
    public int Resistencia;
    public string Descripcion;
    public string Rareza;
    public bool TieneFueros;
    public bool AccionInmediata;
    public string Ilustracion; 

    public string EffectTypeString; 
    public int EffectAmount;
    public int ParamCartasARobar;
    public int ParamReduccionDeCosto;
    public int ParamDuracionTurnos;
    public bool TieneArrollar;
    public int ParamDaño;         // <--- NUEVO
    public int ParamCuracion;     // <--- NUEVO
}

public class CardDataImporter : EditorWindow
{
    private const string API_URL = "http://localhost/GAME/api/v1/get_cartas.php";
    private const string IMAGES_BASE_URL = "http://localhost/GAME/assets/";
    private bool isLoading = false;
    private float progress = 0f;
    private string statusText = "Listo para importar.";
    private Vector2 scrollPosition; 

    [MenuItem("Tools/Importar Cartas Desde API Local")]
    public static void ShowWindow()
    {
        GetWindow<CardDataImporter>("Importador de Cartas Local");
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        GUILayout.Label("Importador de Cartas (API Local)", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        EditorGUILayout.TextField("API URL", API_URL);
        EditorGUILayout.TextField("Images Base URL", IMAGES_BASE_URL);
        EditorGUILayout.Space(5);

        if (!isLoading) {
            if (GUILayout.Button("Obtener y Procesar Cartas de API Local", GUILayout.Height(40))) {
                if (EditorApplication.isPlayingOrWillChangePlaymode) {
                    EditorUtility.DisplayDialog("Error", "No se puede importar mientras el juego está en modo Play.", "OK");
                    return;
                }
                isLoading = true; progress = 0f; statusText = "Iniciando importación...";
                EditorCoroutineUtility.StartCoroutineOwnerless(ImportarCartas());
            }
        } else {
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 20), progress, statusText);
            Repaint(); 
        }
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("Este proceso creará/actualizará ScriptableObjects y descargará imágenes.", MessageType.Info);
        EditorGUILayout.EndScrollView();
    }

    private IEnumerator ImportarCartas()
    {
        Debug.Log("🔄 Iniciando importación de cartas desde API Local...");
        yield return EditorCoroutineUtility.StartCoroutineOwnerless(FetchCardsFromAPI());
    }

    private IEnumerator FetchCardsFromAPI()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(API_URL))
        {
            statusText = "Contactando API..."; progress = 0.1f; Repaint();
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success) {
                Debug.LogError($"❌ Error en la petición HTTP: {www.error} (URL: {API_URL})");
                if (!string.IsNullOrEmpty(www.downloadHandler?.text)) Debug.LogError("Respuesta del servidor: " + www.downloadHandler.text);
                statusText = "Error en petición API: " + www.error;
            } else {
                string jsonResponse = www.downloadHandler.text;
                statusText = "Respuesta recibida, procesando JSON..."; progress = 0.3f; Repaint();
                APIResponse response = null;
                try { response = JsonUtility.FromJson<APIResponse>(jsonResponse); }
                catch (Exception e) {
                    Debug.LogError($"❌ Error al deserializar JSON: {e.Message}. JSON recibido:\n{jsonResponse}");
                    statusText = "Error al leer JSON.";
                }

                if (response?.data == null) {
                    Debug.LogError("❌ No se encontraron cartas en la respuesta JSON o estructura incorrecta ('data' array).");
                    statusText = "No se encontraron datos de cartas en JSON.";
                } else if (response.data.Length == 0) {
                     Debug.LogWarning("⚠️ La API devolvió 0 cartas.");
                     statusText = "API devolvió 0 cartas.";
                } else {
                    Debug.Log($"📝 Encontradas {response.data.Length} cartas para procesar.");
                    statusText = $"Procesando {response.data.Length} cartas..."; progress = 0.4f; Repaint();
                    yield return EditorCoroutineUtility.StartCoroutineOwnerless(ProcessCartasWithImages(response.data));
                    statusText = $"¡Importación de {response.data.Length} cartas completada!";
                }
            }
        }
        isLoading = false; progress = 1f; Repaint();
    }

    private IEnumerator ProcessCartasWithImages(CartaData[] cartas)
    {
        // ... (Tu método ProcessCartasWithImages se mantiene igual, solo asegúrate que CrearScriptableCard se llame correctamente) ...
        string carpetaCartas = "Assets/ScriptableObjects/Cards";
        string carpetaImagenes = "Assets/Art/CardImages";
        CrearCarpetaSiNoExiste(carpetaCartas);
        CrearCarpetaSiNoExiste(carpetaImagenes);

        float totalCards = cartas.Length;
        for (int i = 0; i < cartas.Length; i++) {
            var apiCartaData = cartas[i];
            progress = 0.4f + (((i + 1) / totalCards) * 0.6f);
            statusText = $"Procesando {i + 1}/{cartas.Length}: {apiCartaData.NombreCarta}";
            Repaint();

            if (!string.IsNullOrEmpty(apiCartaData.Ilustracion)) {
                yield return EditorCoroutineUtility.StartCoroutineOwnerless(DownloadAndSaveImage(apiCartaData, carpetaImagenes));
            }
            CrearScriptableCard(apiCartaData, carpetaCartas, carpetaImagenes);
        }
        
        AssetDatabase.SaveAssets(); 
        AssetDatabase.Refresh();    
        Debug.Log($"✅ Se procesaron {cartas.Length} cartas exitosamente!");
    }

    private IEnumerator DownloadAndSaveImage(CartaData cartaData, string carpetaImagenes)
    {
        // ... (Tu método DownloadAndSaveImage se mantiene igual) ...
        if (string.IsNullOrEmpty(cartaData.Ilustracion)) yield break;
        string imageUrl = IMAGES_BASE_URL + cartaData.Ilustracion;
        string sanitizedFileName = Path.GetFileName(cartaData.Ilustracion); // Usar solo el nombre de archivo
        string imagePath = Path.Combine(carpetaImagenes, sanitizedFileName);

        if (File.Exists(imagePath)) { yield break; }

        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl)) {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success) {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                byte[] bytes = texture.EncodeToPNG(); 
                Directory.CreateDirectory(Path.GetDirectoryName(imagePath)); 
                File.WriteAllBytes(imagePath, bytes);
                AssetDatabase.ImportAsset(imagePath);
                ConfigureTextureImporter(imagePath);
            } else { Debug.LogError($"❌ Error al descargar {sanitizedFileName} desde {imageUrl}: {www.error}"); }
        }
        Repaint();
    }

    private void ConfigureTextureImporter(string imagePath)
    {
        // ... (Tu método ConfigureTextureImporter se mantiene igual) ...
        TextureImporter importer = AssetImporter.GetAtPath(imagePath) as TextureImporter;
        if (importer != null) {
            bool needsReimport = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; needsReimport = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; needsReimport = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; needsReimport = true; }
            if (needsReimport) { EditorUtility.SetDirty(importer); importer.SaveAndReimport(); }
        } else { Debug.LogError($"❌ No se pudo obtener TextureImporter para: {imagePath}."); }
    }

    private void CrearCarpetaSiNoExiste(string rutaCompleta)
    {
        // ... (Tu método CrearCarpetaSiNoExiste se mantiene igual) ...
        if (AssetDatabase.IsValidFolder(rutaCompleta)) return;
        string parentFolder = Path.GetDirectoryName(rutaCompleta);
        string newFolderName = Path.GetFileName(rutaCompleta);
        if (string.IsNullOrEmpty(parentFolder) && newFolderName == "Assets") return;
        if (string.IsNullOrEmpty(newFolderName)) return;
        if (AssetDatabase.IsValidFolder(parentFolder) || string.IsNullOrEmpty(parentFolder)) { AssetDatabase.CreateFolder(parentFolder, newFolderName); }
        else { Debug.LogError($"No se pudo crear {rutaCompleta} porque {parentFolder} no es válida."); }
    }

    private void CrearScriptableCard(CartaData apiCartaData, string carpetaCartas, string carpetaImagenes)
    {
        if (apiCartaData == null || string.IsNullOrEmpty(apiCartaData.idUnico)) {
            Debug.LogError("CrearScriptableCard: apiCartaData es null o no tiene idUnico.");
            return;
        }
        string nombreArchivoSeguro = SanitizeFileName(apiCartaData.idUnico);
        if (string.IsNullOrEmpty(nombreArchivoSeguro)) nombreArchivoSeguro = SanitizeFileName(apiCartaData.NombreCarta);
        if (string.IsNullOrEmpty(nombreArchivoSeguro)) nombreArchivoSeguro = "CartaSinID_" + Guid.NewGuid().ToString().Substring(0,8);
        string nombreArchivoAsset = $"{nombreArchivoSeguro}.asset";
        string rutaCompletaAsset = Path.Combine(carpetaCartas, nombreArchivoAsset);

        ScriptableCard cartaAsset = AssetDatabase.LoadAssetAtPath<ScriptableCard>(rutaCompletaAsset);
        bool isNewAsset = false;
        if (cartaAsset == null) {
            cartaAsset = ScriptableObject.CreateInstance<ScriptableCard>();
            AssetDatabase.CreateAsset(cartaAsset, rutaCompletaAsset);
            isNewAsset = true;
        }

        cartaAsset.IdUnico = apiCartaData.idUnico;
        cartaAsset.NombreCarta = apiCartaData.NombreCarta;
        cartaAsset.Descripcion = apiCartaData.Descripcion;
        cartaAsset.Influencia = apiCartaData.Influencia;
        cartaAsset.Resistencia = apiCartaData.Resistencia;
        cartaAsset.CostoPoderPolitico = apiCartaData.CostoPoderPolitico;
        cartaAsset.Faccion = apiCartaData.Faccion;
        cartaAsset.TipoCarta = apiCartaData.TipoCarta;
        cartaAsset.Rareza = apiCartaData.Rareza;
        cartaAsset.TieneFueros = apiCartaData.TieneFueros;
        cartaAsset.AccionInmediata = apiCartaData.AccionInmediata;
        cartaAsset.TieneArrollar = apiCartaData.TieneArrollar;

        if (!string.IsNullOrEmpty(apiCartaData.EffectTypeString)) {
            try {
                cartaAsset.effectType = (ActionEffectType)System.Enum.Parse(typeof(ActionEffectType), apiCartaData.EffectTypeString, true);
            } catch (System.ArgumentException e) {
                Debug.LogWarning($"No se pudo parsear EffectTypeString '{apiCartaData.EffectTypeString}' para '{apiCartaData.NombreCarta}'. Usando 'None'. Error: {e.Message}");
                cartaAsset.effectType = ActionEffectType.None;
            }
        } else {
            cartaAsset.effectType = ActionEffectType.None;
        }

        cartaAsset.effectAmount = apiCartaData.EffectAmount;
        cartaAsset.ParamCartasARobar = apiCartaData.ParamCartasARobar;
        cartaAsset.ParamReduccionDeCosto = apiCartaData.ParamReduccionDeCosto;
        cartaAsset.ParamDuracionTurnos = apiCartaData.ParamDuracionTurnos;
        cartaAsset.ParamDaño = apiCartaData.ParamDaño;         // <--- ASIGNACIÓN NUEVA
        cartaAsset.ParamCuracion = apiCartaData.ParamCuracion; // <--- ASIGNACIÓN NUEVA

        if (!string.IsNullOrEmpty(apiCartaData.Ilustracion)) {
            string sanitizedImageFileName = Path.GetFileName(apiCartaData.Ilustracion);
            string imageAssetPath = Path.Combine(carpetaImagenes, sanitizedImageFileName);
            Sprite loadedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(imageAssetPath);
            if (loadedSprite == null && File.Exists(imageAssetPath)) { 
                ConfigureTextureImporter(imageAssetPath); 
                loadedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(imageAssetPath);
            }
            cartaAsset.Ilustracion = loadedSprite;
        } else {
            cartaAsset.Ilustracion = null;
        }

        EditorUtility.SetDirty(cartaAsset);
        if(isNewAsset) AssetDatabase.SaveAssetIfDirty(cartaAsset);
    }
    
    private string SanitizeFileName(string name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty;
        foreach (char c in System.IO.Path.GetInvalidFileNameChars()) { name = name.Replace(c, '_'); }
        return name.Replace(" ", "_");
    }
}