using System;
using UnityEngine;

[Serializable]
public abstract class UiAction<Data>
{
    public Data data;
    public abstract void Execute();
}