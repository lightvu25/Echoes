using UnityEngine;

public interface IFeedbackProvider
{
    Vector3 PromptOffset { get; }
    Transform transform { get; }
}