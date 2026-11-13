using System;
using UnityEngine;
using UnityEngine.Events;

public enum TutorialCardPosition { Auto, Below, Above, Left, Right, Center }
public enum TutorialAdvance { NextButton, Action }

// Un paso del tutorial. Se arma desde el Inspector del TutorialManager.
[Serializable]
public class TutorialStep
{
    [Tooltip("Título de la tarjeta")]
    public string title = "TÍTULO";

    [Tooltip("Texto. Escribe las teclas entre llaves y salen en rosa: {E}, {Clic izquierdo}")]
    [TextArea(2, 4)] public string text = "Texto del paso";

    [Header("Qué se resalta")]
    [Tooltip("Elemento a resaltar: un objeto de UI o un objeto del mundo (con Renderer o Collider). Vacío = sin resaltar, tarjeta al centro")]
    public Transform target;
    [Tooltip("Más objetos para resaltar junto al Target: el hueco envuelve a todos")]
    public Transform[] extraTargets;
    [Tooltip("Espacio extra alrededor del elemento resaltado (píxeles)")]
    public Vector2 padding = new Vector2(12f, 12f);

    [Header("Tarjeta")]
    public TutorialCardPosition cardPosition = TutorialCardPosition.Auto;
    [Tooltip("Mueve la tarjeta desde su posición calculada (píxeles)")]
    public Vector2 cardOffset;
    public bool showArrow = true;

    [Header("Cómo se avanza")]
    [Tooltip("NextButton: botón Siguiente / Enter.\nAction: el jugador tiene que hacer algo; el juego se reanuda y el paso termina cuando el código llama TutorialManager.Notify(\"actionId\")")]
    public TutorialAdvance advance = TutorialAdvance.NextButton;
    public string actionId;

    [Header("Eventos")]
    [Tooltip("Se llama al empezar este paso (ej. abrir un panel para poder resaltarlo)")]
    public UnityEvent onStepStart;
}
