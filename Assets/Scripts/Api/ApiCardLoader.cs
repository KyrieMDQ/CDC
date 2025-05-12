using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ApiCardLoader : MonoBehaviour
{
    public string apiUrl = "http://localhost/GAME/api/v1/get_cartas.php";

    public void DescargarCartas(System.Action<List<CardJsonData>> callback)
    {
        StartCoroutine(DescargarCartasCoroutine(callback));
    }

    private IEnumerator DescargarCartasCoroutine(System.Action<List<CardJsonData>> callback)
    {
        UnityWebRequest request = UnityWebRequest.Get(apiUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Error al contactar la API: " + request.error);
            callback?.Invoke(null);
            yield break;
        }

        string json = FixJsonArray(request.downloadHandler.text);
        ApiResponse response = JsonUtility.FromJson<ApiResponse>(json);
        callback?.Invoke(response?.data);
    }

    private string FixJsonArray(string json)
    {
        return "{\"data\":" + json + "}";
    }
}


