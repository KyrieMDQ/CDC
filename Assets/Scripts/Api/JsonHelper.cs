using System;
using UnityEngine;

public static class JsonHelper
{
    public static string FixJsonArray(string json)
    {
        // Asegura que el JSON tenga una estructura de objeto con clave "data"
        return $"{{\"data\":{json}}}";
    }
}
