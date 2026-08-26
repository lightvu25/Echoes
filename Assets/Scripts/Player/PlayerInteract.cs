using UnityEngine;
using System;

public class PlayerInteract : MonoBehaviour
{
    public static PlayerInteract Instance { get; private set; }

    public bool IsRewinding = false;

    public event EventHandler<OnCoinPickupEventArgs> OnCoinPickup;
    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;

    public event EventHandler OnDead;

    public class OnCoinPickupEventArgs : EventArgs
    {
        public CoinPickup coinPickup;
    }

    public class OnStateChangedEventArgs : EventArgs
    {
        public State state;
    }

    public enum State
    {
        WaitingToStart,
        Normal,
        GameOver
    }

    private float coinPickups = 0f;
    private float time;
    
    private IInteractable currentInteractable;
    private IExtractable currentExtractable;

    // Thời gian sống sót
    private float timeMax = 50f;
    private State state;

    private Coroutine extractionCoroutine;
    private HealthSystem healthSystem;

    private void Awake()
    {
        Instance = this;

        time = timeMax;
        state = State.WaitingToStart;
    }

    private void Start()
    {
        healthSystem = GetComponentInParent<HealthSystem>();
        if (healthSystem == null) healthSystem = GetComponentInChildren<HealthSystem>();
        
        if (healthSystem != null)
        {
            healthSystem.OnDamaged += HandleDamagedDuringExtraction;
        }

        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnInteractPressed += HandleInteract;
            GameInput.Instance.OnExtractPressed += HandleExtract;
        }
    }

    private void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.OnDamaged -= HandleDamagedDuringExtraction;
        }

        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnInteractPressed -= HandleInteract;
            GameInput.Instance.OnExtractPressed -= HandleExtract;
        }
    }

    private void HandleDamagedDuringExtraction(object sender, HealthSystem.DamageEventArgs e)
    {
        if (extractionCoroutine != null)
        {
            StopCoroutine(extractionCoroutine);
            extractionCoroutine = null;
            Debug.Log("[PlayerInteract] Extraction interrupted by damage!");
        }
    }

    private System.Collections.IEnumerator ExtractMemoryRoutine(IExtractable source)
    {
        Debug.Log("[PlayerInteract] Starting memory extraction...");
        yield return new WaitForSeconds(0.5f);
        
        if (source != null && source.IsAvailable)
        {
            source.Extract();
            Debug.Log("[PlayerInteract] Extraction complete!");
        }
        extractionCoroutine = null;
    }

    private void FixedUpdate()
    {
        // OLD SURVIVAL DEATH LOGIC
        // if (time <= 0f && state != State.GameOver)
        // {
        //     OnDead?.Invoke(this, EventArgs.Empty);
        //     SetState(State.GameOver);
        //     return;
        // }

        switch (state)
        {
            default:
            case State.WaitingToStart:
                if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f ||
                Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f ||
                Input.GetKeyDown(KeyCode.Space) ||
                Input.GetKeyDown(KeyCode.LeftShift))
                {
                    SetState(State.Normal);
                }
                break;
            case State.Normal:
                if (!IsRewinding)
                {
                    // OLD TIME CONSUMPTION LOGIC
                    // ConsumeTime();
                }
                break;
            case State.GameOver:
                break;
        }
    }

    private void HandleInteract()
    {
        // Dialogue owns the interaction button while it is open: first press
        // completes the typewriter, then advances pages and lines.
        if (DialogueController.Instance != null && DialogueController.Instance.IsDialogueActive)
        {
            DialogueController.Instance.Advance();
            return;
        }

        if (UIManager.Instance != null && UIManager.Instance.IsAnyPanelOpen)
        {
            UIManager.Instance.CloseCurrentPanel();
            return;
        }

        if (state == State.Normal && !IsRewinding)
        {
            if (currentInteractable != null)
            {
                currentInteractable.Interact();
            }
        }
    }

    private void HandleExtract()
    {
        if (state == State.Normal && !IsRewinding)
        {
            if (currentExtractable != null && extractionCoroutine == null)
            {
                extractionCoroutine = StartCoroutine(ExtractMemoryRoutine(currentExtractable));
            }
        }
    }


    private void OnTriggerEnter2D(Collider2D collider2D)
    {
        IInteractable interactable = collider2D.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
            currentInteractable = interactable;
        }

        IExtractable extractable = collider2D.GetComponentInParent<IExtractable>();
        if (extractable != null)
        {
            currentExtractable = extractable;
        }
    }

    private void OnTriggerExit2D(Collider2D collider2D)
    {
        IInteractable interactable = collider2D.GetComponentInParent<IInteractable>();
        if (interactable != null && currentInteractable == interactable)
        {
            currentInteractable = null;
        }

        IExtractable extractable = collider2D.GetComponentInParent<IExtractable>();
        if (extractable != null && currentExtractable == extractable)
        {
            currentExtractable = null;
            if (extractionCoroutine != null)
            {
                StopCoroutine(extractionCoroutine);
                extractionCoroutine = null;
            }
        }
    }

    private void SetState(State state)
    {
        this.state = state;
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs
        {
            state = state
        });
    }

    public void ConsumeTime()
    {
        float timeConsumptionRate = 1f;
        time -= timeConsumptionRate * Time.deltaTime;
    }

    public float GetTimeNormalized()
    {
        return time / timeMax;
    }

    public float GetCoinPickups()
    {
        return coinPickups;
    }

    public float GetExactTime()
    {
        return time;
    }

    public void SetTime(float newTime)
    {
        time = newTime;

        if (time > timeMax)
            time = timeMax;

        if (time < 0)
            time = 0;
    }

    public void Dead()
    {
        if (state != State.GameOver)
        {
            OnDead?.Invoke(this, EventArgs.Empty);
            SetState(State.GameOver);
        }
    }
}
