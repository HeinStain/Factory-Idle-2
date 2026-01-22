using System.Collections;
using UnityEngine;
using TMPro;

public class MoneyVisual : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneValueDebugText;

    [Header("Digit Animators")]
    [SerializeField] private Animator ones;
    [SerializeField] private Animator tens;
    [SerializeField] private Animator hundreds;
    [SerializeField] private Animator thousands;
    [SerializeField] private Animator tenThousands;
    [SerializeField] private Animator hundredThousands;
    [SerializeField] private Animator millions;
    [SerializeField] private Animator tenMillions;
    [SerializeField] private Animator hundredMillions;

    [Header("Slot Spin")]
    [Tooltip("Baseline: how many lowest digits spin (ones=0, tens=1, etc).")]
    [SerializeField] private int spinningDigitCount = 9;

    [Tooltip("Base interval for ones digit at Animator speed = 1. KEEP around your clip length (ex: ~0.31 for ~0.29s clip).")]
    [SerializeField] private float spinInterval = 0.31f;

    [Tooltip("Baseline slowdown per digit.")]
    [SerializeField] private float spinIntervalStep = 0.15f;

    [Tooltip("If no money changes, spin ends after this delay (baseline mode).")]
    [SerializeField] private float settleDelay = 0.12f;

    [Header("Burst (big jumps)")]
    [Tooltip("If change forces us to spin beyond baseline, guarantee at least this long of spin.")]
    [SerializeField] private float burstMinSpinTime = 0.25f;

    [Tooltip("When many reels spin, use gentler slowdown so upper reels still move.")]
    [SerializeField] private float burstIntervalStep = 0.08f;

    [Header("Speed Multipliers")]
    [Tooltip("Animator speed multiplier during SPIN phase.")]
    [SerializeField] private float spinAnimatorSpeed = 6f;

    [Tooltip("Animator speed multiplier during SETTLE phase (cycling-to-target).")]
    [SerializeField] private float settleAnimatorSpeed = 14f;

    [Tooltip("When spinning this many digits or more, use burstIntervalStep so top digits don't become glacial.")]
    [SerializeField] private int fastStepThreshold = 5;

    [Tooltip("Extra pause between locking digits (high -> low). Set to 0 for ultra fast.")]
    [SerializeField] private float lockBeatDelay = 0f;

    private const int DigitCount = 9;

    private readonly Animator[] animators = new Animator[DigitCount];
    private readonly int[] displayedDigits = new int[DigitCount];
    private readonly int[] targetDigits = new int[DigitCount];

    private Coroutine spinRoutine;

    // How many digits are currently spinning (0..9)
    private int activeSpinningDigitCount;

    // Spin is active while Time.time < spinUntilTime
    private float spinUntilTime;

    // Track applied speed so we can reset cleanly
    private int appliedSpeedCount = -1;
    private float appliedSpeedValue = 1f;

    private void Start()
    {
        animators[0] = ones;
        animators[1] = tens;
        animators[2] = hundreds;
        animators[3] = thousands;
        animators[4] = tenThousands;
        animators[5] = hundredThousands;
        animators[6] = millions;
        animators[7] = tenMillions;
        animators[8] = hundredMillions;

        spinningDigitCount = Mathf.Clamp(spinningDigitCount, 0, DigitCount);
        activeSpinningDigitCount = spinningDigitCount;
        spinUntilTime = 0f;

        int startMoney = (MoneyManager.Instance != null) ? MoneyManager.Instance.GetMoneyCount() : 0;

        FillDigits(startMoney, displayedDigits);
        FillDigits(startMoney, targetDigits);

        // Initialize animator params so visuals match on scene load.
        for (int i = 0; i < DigitCount; i++)
        {
            if (animators[i] == null) continue;
            animators[i].speed = 1f;
            animators[i].SetInteger("Bottom Number", displayedDigits[i]);
            animators[i].SetInteger("Top Number", displayedDigits[i]);
        }

        if (moneValueDebugText != null)
            moneValueDebugText.text = startMoney.ToString();

        if (MoneyManager.Instance != null)
            MoneyManager.Instance.MoneyChanged += OnMoneyChanged;
    }

    private void OnDestroy()
    {
        if (MoneyManager.Instance != null)
            MoneyManager.Instance.MoneyChanged -= OnMoneyChanged;

        ApplyAnimSpeed(DigitCount, 1f);
        appliedSpeedCount = -1;
        appliedSpeedValue = 1f;
    }

    private void OnMoneyChanged(int oldValue, int newValue)
    {
        if (moneValueDebugText != null)
            moneValueDebugText.text = newValue.ToString();

        int delta = newValue - oldValue;
        if (delta == 0) return;

        FillDigits(newValue, targetDigits);

        if (delta > 0)
        {
            // Highest digit that ACTUALLY changed (DISPLAYED -> TARGET)
            int highestChanged = -1;
            for (int i = 0; i < DigitCount; i++)
            {
                if (targetDigits[i] != displayedDigits[i])
                    highestChanged = i;
            }
            if (highestChanged < 0) return;

            // Spin from ones up to the highest changed digit, but at least baseline
            int desiredSpinCount = Mathf.Max(spinningDigitCount, highestChanged + 1);
            desiredSpinCount = Mathf.Clamp(desiredSpinCount, 0, DigitCount);

            // Upgrade range mid-spin if needed
            activeSpinningDigitCount = Mathf.Max(activeSpinningDigitCount, desiredSpinCount);

            bool isBurst = activeSpinningDigitCount > spinningDigitCount;

            // Baseline mode: digits above baseline stay readable (snap directly)
            if (!isBurst)
            {
                for (int i = spinningDigitCount; i < DigitCount; i++)
                {
                    if (targetDigits[i] != displayedDigits[i])
                        FlipToTarget(i);
                }
            }

            // Extend spin window; big jumps get a minimum spin time
            float until = Time.time + settleDelay;
            if (isBurst)
                until = Mathf.Max(until, Time.time + burstMinSpinTime);

            spinUntilTime = Mathf.Max(spinUntilTime, until);

            if (spinRoutine == null && activeSpinningDigitCount > 0)
                spinRoutine = StartCoroutine(SpinDigits());
        }
        else // spending
        {
            StopSpin();

            // Spending: no slot spin; flip everything straight to target
            for (int i = 0; i < DigitCount; i++)
            {
                if (targetDigits[i] != displayedDigits[i])
                    FlipToTarget(i);
            }
        }
    }

    private void StopSpin()
    {
        if (spinRoutine != null)
        {
            StopCoroutine(spinRoutine);
            spinRoutine = null;
        }

        spinUntilTime = 0f;
        activeSpinningDigitCount = spinningDigitCount;

        ApplyAnimSpeed(DigitCount, 1f);
        appliedSpeedCount = -1;
        appliedSpeedValue = 1f;
    }

    private IEnumerator SpinDigits()
    {
        float[] acc = new float[DigitCount];

        while (true)
        {
            // SPIN PHASE
            while (Time.time < spinUntilTime)
            {
                float dt = Time.deltaTime;
                int count = Mathf.Clamp(activeSpinningDigitCount, 0, DigitCount);

                ApplyAnimSpeed(count, Mathf.Max(0.01f, spinAnimatorSpeed));

                for (int i = 0; i < count; i++)
                {
                    float interval = GetInterval(i, count, settling: false);
                    acc[i] += dt;

                    // One step max per frame per digit (prevents trigger spam)
                    if (acc[i] >= interval)
                    {
                        acc[i] -= interval;
                        StepDigit(i);
                    }
                }

                yield return null;
            }

            int settleCount = Mathf.Clamp(activeSpinningDigitCount, 0, DigitCount);
            if (settleCount == 0)
                break;

            // SETTLE PHASE (still cycles-until-match, but turbo)
            ApplyAnimSpeed(settleCount, Mathf.Max(0.01f, settleAnimatorSpeed));

            for (int lockIndex = settleCount - 1; lockIndex >= 0; lockIndex--)
            {
                // If money starts changing again, go back to spin
                if (Time.time < spinUntilTime)
                    break;

                float lockAcc = 0f;

                // Cycle forward until this digit matches target digit
                while (displayedDigits[lockIndex] != targetDigits[lockIndex])
                {
                    if (Time.time < spinUntilTime)
                        break;

                    float dt = Time.deltaTime;

                    // Keep lower digits spinning while this digit locks
                    for (int j = 0; j < lockIndex; j++)
                    {
                        float interval = GetInterval(j, settleCount, settling: true);
                        acc[j] += dt;

                        if (acc[j] >= interval)
                        {
                            acc[j] -= interval;
                            StepDigit(j);
                        }
                    }

                    float lockInterval = GetInterval(lockIndex, settleCount, settling: true);
                    lockAcc += dt;

                    if (lockAcc >= lockInterval)
                    {
                        lockAcc -= lockInterval;
                        StepDigit(lockIndex);
                    }

                    yield return null;
                }

                if (Time.time < spinUntilTime)
                    break;

                if (lockBeatDelay > 0f)
                    yield return new WaitForSeconds(lockBeatDelay);
                else
                    yield return null;
            }

            // If changes resumed during settle, loop back to spin
            if (Time.time < spinUntilTime)
                continue;

            // Final correctness pass (safety)
            for (int i = 0; i < settleCount; i++)
            {
                if (displayedDigits[i] != targetDigits[i])
                    FlipToTarget(i);
            }

            activeSpinningDigitCount = spinningDigitCount;
            break;
        }

        // Reset speeds
        ApplyAnimSpeed(DigitCount, 1f);
        appliedSpeedCount = -1;
        appliedSpeedValue = 1f;

        spinRoutine = null;
    }

    private float GetInterval(int digitIndex, int spinCount, bool settling)
    {
        float speed = settling ? Mathf.Max(0.01f, settleAnimatorSpeed) : Mathf.Max(0.01f, spinAnimatorSpeed);

        // Interval is based on your base clip length (spinInterval at speed=1), scaled by animator speed
        float baseInterval = spinInterval / speed;

        float step = (spinCount >= fastStepThreshold) ? burstIntervalStep : spinIntervalStep;
        return baseInterval * (1f + digitIndex * step);
    }

    private void ApplyAnimSpeed(int count, float speed)
    {
        count = Mathf.Clamp(count, 0, DigitCount);

        if (appliedSpeedCount == count && Mathf.Approximately(appliedSpeedValue, speed))
            return;

        // set speed for active range
        for (int i = 0; i < count; i++)
        {
            if (animators[i] != null)
                animators[i].speed = speed;
        }

        // reset any previously sped digits above new count
        int max = (appliedSpeedCount < 0) ? 0 : Mathf.Max(appliedSpeedCount, count);
        for (int i = count; i < max; i++)
        {
            if (animators[i] != null)
                animators[i].speed = 1f;
        }

        appliedSpeedCount = count;
        appliedSpeedValue = speed;
    }

    private void StepDigit(int index)
    {
        int from = displayedDigits[index];
        int to = (from + 1) % 10;

        displayedDigits[index] = to;
        FlipDigit(index, from, to);
    }

    private void FlipToTarget(int index)
    {
        int from = displayedDigits[index];
        int to = targetDigits[index];
        if (from == to) return;

        displayedDigits[index] = to;
        FlipDigit(index, from, to);
    }

    private void FlipDigit(int index, int from, int to)
    {
        Animator a = animators[index];
        if (a == null) return;

        a.SetInteger("Bottom Number", from);
        a.SetInteger("Top Number", to);
        a.SetTrigger("Flip");
    }

    private static void FillDigits(int moneyValue, int[] digitsOut)
    {
        int temp = moneyValue;
        for (int i = 0; i < DigitCount; i++)
        {
            digitsOut[i] = temp % 10;
            temp /= 10;
        }
    }

    // Debug buttons
    public void Add1()         { if (MoneyManager.Instance != null) MoneyManager.Instance.IncreaseMoney(1); }
    public void Add10()        { if (MoneyManager.Instance != null) MoneyManager.Instance.IncreaseMoney(10); }
    public void Add100()       { if (MoneyManager.Instance != null) MoneyManager.Instance.IncreaseMoney(100); }
    public void Add1000()      { if (MoneyManager.Instance != null) MoneyManager.Instance.IncreaseMoney(1000); }
    public void Add10000()     { if (MoneyManager.Instance != null) MoneyManager.Instance.IncreaseMoney(10000); }
    public void Add100000()    { if (MoneyManager.Instance != null) MoneyManager.Instance.IncreaseMoney(100000); }
    public void Add1000000()   { if (MoneyManager.Instance != null) MoneyManager.Instance.IncreaseMoney(1000000); }
    public void Add10000000()  { if (MoneyManager.Instance != null) MoneyManager.Instance.IncreaseMoney(10000000); }
    public void Add100000000() { if (MoneyManager.Instance != null) MoneyManager.Instance.IncreaseMoney(100000000); }
}
