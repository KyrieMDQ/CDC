using System;
using System.Collections.Generic;

[Serializable]
public class CardJsonData
{
    public string id;
    public string NombreCarta;
    public string Descripcion;
    public int CostoPoderPolitico;
    public string TipoCarta;
    public string Ilustracion;
    public int Influencia;
    public int Resistencia;
    public bool AccionInmediata;
}

[Serializable]
public class ApiResponse
{
    public List<CardJsonData> data;
}
