using UnityEngine;

public interface IExtractable
{
    bool IsAvailable { get; }
    void Extract();
}