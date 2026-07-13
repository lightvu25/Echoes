using UnityEngine;

public class ComboVFXController : MonoBehaviour
{
    private Animator animator;

    [Tooltip("The exact names of the animation states for each combo step. Element 0 = Combo Step 1.")]
    [SerializeField] private string[] comboStateNames = new string[] { "Slash1", "Slash2", "Slash3" };

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public void PlayComboStep(int step)
    {
        if (animator != null && comboStateNames != null)
        {
            // If the step is out of bounds, default to the last available animation state
            int index = Mathf.Min(step, comboStateNames.Length - 1);
            
            if (index >= 0)
            {
                string stateName = comboStateNames[index];
                if (!string.IsNullOrEmpty(stateName))
                {
                    animator.Play(stateName, 0, 0f); // Play from the beginning (0f)
                }
            }
        }
    }
}
