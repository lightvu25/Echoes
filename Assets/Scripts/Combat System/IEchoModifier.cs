using UnityEngine;

public interface IEchoModifier
{
    int Priority { get; }
    void Initialize(EchoModifierContext context);
    void Remove();
}

public interface IEchoDashModifier
{
    void OnDash(Vector3 startPos, Vector3 endPos);
}
