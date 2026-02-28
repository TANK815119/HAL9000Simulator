using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

//a varaible which can be toggled to be either local or network
public interface IVariable<T>
{
    T Value { get; set; }
}

public class LocalVariable<T> : IVariable<T>
{
    public T Value { get; set; }
}

public class NetworkVariableWrapper<T> : IVariable<T>
{
    private NetworkVariable<T> networkVariable;

    public NetworkVariableWrapper(NetworkVariable<T> networkVariable)
    {
        this.networkVariable = networkVariable;
    }

    public T Value
    {
        get => networkVariable.Value;
        set => networkVariable.Value = value;
    }
}
