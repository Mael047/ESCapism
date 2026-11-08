using System;

// Cualquier componente que avise cuando su personaje muere
public interface IDeathNotifier
{
    event Action Died;
}