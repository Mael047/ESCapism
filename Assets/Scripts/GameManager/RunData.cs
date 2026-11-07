using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RunData
{
    //Economía
    public int money;
    //Recurso de la noche(Cambiar)
    public int drop;

    public List<string> permanentUpgrades = new List<string>();

    public EmotionalStats emotionalStats = new EmotionalStats();


    [Serializable]

 public class EmotionalStats
    {
    public int StatA;
    public int StatB;
    }

}
