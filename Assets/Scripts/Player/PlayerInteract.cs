using UnityEngine;
using System;

public class PlayerInteract : MonoBehaviour
{
    public static PlayerInteract Instance { get; private set; }

    public bool IsRewinding = false;

    public event EventHandler<OnCoinPickupEventArgs> OnCoinPickup;
    public event EventHandler<OnTimePickupEventArgs> OnTimePickup;
    public event EventHandler<OnGoalEventArgs> OnGoal;
    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;

    public event EventHandler OnDead;

    public class OnCoinPickupEventArgs : EventArgs
    {
        public CoinPickup coinPickup;
    }

    public class OnTimePickupEventArgs : EventArgs
    {
        public TimePickup timePickup;
    }

    public class OnStateChangedEventArgs : EventArgs
    {
        public State state;
    }

    public class OnGoalEventArgs : EventArgs
    {
        public bool passing;
        public int score;
        public Goal goal;
}

    public enum State
    {
        WaitingToStart,
        Normal,
        GameOver
    }

    private float coinPickups = 0f;
    private float time;
    private float timeMax = 5f;
    private State state;

    private void Awake()
    {
        Instance = this;

        time = timeMax;
        state = State.WaitingToStart;
    }

    private void FixedUpdate()
    {
        if (time <= 0f && state != State.GameOver)
        {
            OnDead?.Invoke(this, EventArgs.Empty);

            SetState(State.GameOver);
            return;
        }

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
                    ConsumeTime();
                }
                break;
            case State.GameOver:
                break;
        }
    }

    private void OnTriggerEnter2D(Collider2D collider2D)
    {
        if (collider2D.gameObject.TryGetComponent(out CoinPickup coinPickup))
        {
            float coinPickupAmount = 10f;
            coinPickups += coinPickupAmount;

        OnCoinPickup?.Invoke(this, new OnCoinPickupEventArgs
        {
            coinPickup = coinPickup
        });
            coinPickup.DestroySelf();
        }

        if (collider2D.gameObject.TryGetComponent(out Goal goal))
        {
            OnGoal?.Invoke(this, new OnGoalEventArgs
            {
                passing = true,
                score = (int)coinPickups,
                goal = goal
            });
            SetState(State.GameOver);
            return;
        }

        if (collider2D.gameObject.TryGetComponent(out TimePickup timePickup))
        {
            float timeAmount = 2f;
            time += timeAmount;
            if (time > timeMax)
            {
                time = timeMax;
            }
            OnTimePickup?.Invoke(this, new OnTimePickupEventArgs
            {
                timePickup = timePickup
            });
            timePickup.DestroySelf();
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