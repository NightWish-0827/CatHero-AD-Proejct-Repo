using UnityEngine;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;
using DG.Tweening;

public class Roulette : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform           piecePrefab;
    [SerializeField] private Transform           linePrefab;
    [SerializeField] private Transform           pieceParent;
    [SerializeField] private Transform           lineParent;
    [SerializeField] private RoulettePieceData[] roulettePieceData;
    [SerializeField] private Transform           spinningRoulette;
    [SerializeField] private Transform           pointerTransform;

    [Header("Spin Settings")]
    [SerializeField] private float               spinDuration = 4f;
    [Range(5, 15)]
    [SerializeField] private int                 extraRevolutions = 10;

    [Header("Sequence - Anticipation")]
    [SerializeField] private float               anticipationDuration = 0.4f;
    [SerializeField] private float               anticipationScaleMultiplier = 0.85f;
    [SerializeField] private float               anticipationRotateOffsetZ = -35f;
    [SerializeField] private Ease                anticipationEase = Ease.OutQuad;

    [Header("Sequence - Spin")]
    [SerializeField] private Ease                spinEase = Ease.OutBack;
    [SerializeField] private float               spinEaseOvershoot = 1.2f;
    [SerializeField] private bool                spinAllowOvershoot = false;
    [SerializeField] private Ease                spinSafeEase = Ease.OutCubic;

    [Header("Sequence - Impact")]
    [SerializeField] private Vector3             impactPunchScale = new Vector3(0.25f, 0.25f, 0.25f);
    [SerializeField] private float               impactDuration = 0.4f;
    [SerializeField] private int                 impactVibrato = 4;
    [SerializeField] private float               impactElasticity = 1f;

    [Header("Pointer Kick Tuning")]
    [SerializeField] private float               pointerSmoothTime = 0.03f;
    [SerializeField] private float               pointerMaxSpeed = 999f;
    [SerializeField] private float               pointerKickPerTick = 35f;
    [SerializeField] private float               pointerKickMax = 70f;
    [SerializeField] private float               pointerKickReturnSpeed = 120f;

    private float               pieceAngle;
    private float               halfPieceAngle;
    private float               halfPieceAngleWithPaddings;
    
    private int                 accumulatedWeight;
    private bool                isSpinning = false;
    private int                 selectedIndex = 0;

    private float               pointerKick;
    private float               pointerKickVel;
    private float               pointerRestLocalZ;
    private Vector3             initialRouletteScale;

    private void Awake()
    {
        DOTween.Init();

        pieceAngle                 = 360f / roulettePieceData.Length;
        halfPieceAngle             = pieceAngle * 0.5f;
        halfPieceAngleWithPaddings = halfPieceAngle - (halfPieceAngle * 0.25f);

        if (spinningRoulette != null)
            initialRouletteScale = spinningRoulette.localScale;
        if (pointerTransform != null)
            pointerRestLocalZ = pointerTransform.localEulerAngles.z;

        SpawnPiecesAndLines();
        CalculateWeightsAndIndices();
    }

    private void Update()
    {
        if (pointerTransform != null && pointerKick > 0f)
        {
            float targetZ = pointerRestLocalZ - pointerKick;
            float current = Mathf.SmoothDampAngle(pointerTransform.localEulerAngles.z, targetZ, ref pointerKickVel, pointerSmoothTime, pointerMaxSpeed, Time.deltaTime);
            pointerTransform.localRotation = Quaternion.Euler(0f, 0f, current);
            
            pointerKick = Mathf.MoveTowards(pointerKick, 0f, Time.deltaTime * pointerKickReturnSpeed);
        }
    }

    private void SpawnPiecesAndLines()
    {
        for ( int i = 0; i < roulettePieceData.Length; ++ i )
        {
            Transform piece = Instantiate(piecePrefab, pieceParent.position, Quaternion.identity, pieceParent);
            piece.GetComponent<RoulettePiece>().Setup(roulettePieceData[i]);
            piece.RotateAround(pieceParent.position, Vector3.back, (pieceAngle * i));

            Transform line = Instantiate(linePrefab, lineParent.position, Quaternion.identity, lineParent);
            line.RotateAround(lineParent.position, Vector3.back, (pieceAngle * i) + halfPieceAngle);
        }
    }

    private void CalculateWeightsAndIndices()
    {
        accumulatedWeight = 0;
        for ( int i = 0; i < roulettePieceData.Length; ++ i )
        {
            roulettePieceData[i].index = i;
            if ( roulettePieceData[i].chance <= 0 ) roulettePieceData[i].chance = 1;

            accumulatedWeight += roulettePieceData[i].chance;
            roulettePieceData[i].weight = accumulatedWeight;
        }
    }

    private int GetRandomIndex()
    {
        int weight = Random.Range(0, accumulatedWeight);
        for ( int i = 0; i < roulettePieceData.Length; ++ i )
        {
            if ( roulettePieceData[i].weight > weight ) return i;
        }
        return 0;
    }

    private float GetAngleForIndex(int index, float t01)
    {
        float center = pieceAngle * index;
        float left = Mathf.Repeat(center - halfPieceAngleWithPaddings, 360f);
        float right = Mathf.Repeat(center + halfPieceAngleWithPaddings, 360f);

        t01 = Mathf.Clamp01(t01);

        if ( left <= right ) return Mathf.Lerp(left, right, t01);

        float aLen = 360f - left;
        float pick = t01 * (aLen + right);

        if ( pick < aLen ) return left + pick;
        return pick - aLen;
    }

    private float GetRandomAngleForIndex(int index) => GetAngleForIndex(index, Random.value);

    private static float RollTo01(int roll)
    {
        unchecked
        {
            uint x = (uint)roll;
            x ^= x >> 16;
            x *= 0x7feb352d;
            x ^= x >> 15;
            x *= 0x846ca68b;
            x ^= x >> 16;
            return (x & 0x00FFFFFF) / 16777216f; // 0..1
        }
    }

    private bool hasReservedResult;
    private int reservedIndex = -1;
    private float reservedAngleT01 = 0.5f;

    // ????/???? ??????: "??? ???"?? ???? ????, ?????? ?? ??????? ????.
    public void ReserveResultIndex(int index, int angleRoll = 0)
    {
        reservedIndex = Mathf.Clamp(index, 0, roulettePieceData.Length - 1);
        reservedAngleT01 = RollTo01(angleRoll);
        hasReservedResult = true;
    }

    public void ClearReservedResult()
    {
        hasReservedResult = false;
        reservedIndex = -1;
        reservedAngleT01 = 0.5f;
    }

    // roll(????)?? ????/???? SDK???? ??????, ???? roll???? ??? ???? ???????? ???????? ????.
    public int GetIndexFromRoll(int roll)
    {
        if (roulettePieceData == null || roulettePieceData.Length == 0)
            return 0;

        if (accumulatedWeight <= 0)
            CalculateWeightsAndIndices();

        int mod = accumulatedWeight <= 0 ? 0 : roll % accumulatedWeight;
        if (mod < 0) mod += accumulatedWeight;

        for (int i = 0; i < roulettePieceData.Length; ++i)
        {
            if (roulettePieceData[i].weight > mod) return i;
        }
        return 0;
    }

    public void ReserveResultFromRoll(int roll, int angleRoll = 0)
    {
        ReserveResultIndex(GetIndexFromRoll(roll), angleRoll);
    }

    private void TriggerPointerKick(int count = 1)
    {
        if ( pointerTransform != null )
        {
            pointerKick += pointerKickPerTick * count; 
            pointerKick = Mathf.Clamp(pointerKick, 0f, pointerKickMax);
        }
    }

    private static async UniTask AwaitForComplete(Tween tween)
    {
        if (tween == null)
            return;

        while (tween.IsActive() && !tween.IsComplete())
            await UniTask.Yield();
    }

    private async UniTask<RoulettePieceData> SpinCore(int index, float angleT01)
    {
        if (isSpinning)
        {
            Debug.LogWarning("Roulette is already spinning.");
            return null;
        }
        isSpinning = true;

        selectedIndex = Mathf.Clamp(index, 0, roulettePieceData.Length - 1);
        float targetAngle = GetAngleForIndex(selectedIndex, angleT01);

        float startZ = spinningRoulette.eulerAngles.z;
        float deltaToFinal = Mathf.Repeat(targetAngle - startZ, 360f);
        float endZ = startZ + (360f * extraRevolutions) + deltaToFinal;

        try
        {
            Sequence anticSeq = DOTween.Sequence();
            float anticAngle = startZ + anticipationRotateOffsetZ;

            anticSeq.Append(spinningRoulette.DOScale(initialRouletteScale * anticipationScaleMultiplier, anticipationDuration).SetEase(anticipationEase))
                    .Join(spinningRoulette.DORotate(new Vector3(0, 0, anticAngle), anticipationDuration).SetEase(anticipationEase));

            await AwaitForComplete(anticSeq);

            float currentZ = anticAngle;
            int lastTickStep = Mathf.FloorToInt(Mathf.Abs(currentZ) / pieceAngle);

            Tween spinTween = DOTween.To(() => currentZ, x => 
            {
                currentZ = x;
                spinningRoulette.localRotation = Quaternion.Euler(0, 0, currentZ);

                int tickStep = Mathf.FloorToInt(Mathf.Abs(currentZ) / pieceAngle);
                if (tickStep > lastTickStep)
                {
                    TriggerPointerKick(tickStep - lastTickStep);
                    lastTickStep = tickStep;
                }
            }, endZ, spinDuration);

            if (!spinAllowOvershoot)
            {
                spinTween.SetEase(spinSafeEase);
            }
            else
            {
                if (spinEase == Ease.InBack || spinEase == Ease.OutBack || spinEase == Ease.InOutBack)
                    spinTween.SetEase(spinEase, spinEaseOvershoot);
                else
                    spinTween.SetEase(spinEase);
            }
            await AwaitForComplete(spinTween);

            spinningRoulette.localScale = initialRouletteScale;
            
            await AwaitForComplete(spinningRoulette.DOPunchScale(impactPunchScale, impactDuration, impactVibrato, impactElasticity));
        }
        finally
        {
            isSpinning = false;
        }

        return roulettePieceData[selectedIndex];
    }

    public async UniTask Spin(UnityAction<RoulettePieceData> action=null)
    {
        int index = GetRandomIndex();
        float t = Random.value;
        RoulettePieceData result = await SpinCore(index, t);
        if (result != null)
            action?.Invoke(result);
    }

    public async UniTask<RoulettePieceData> SpinFixedAsync(int fixedIndex, int angleRoll = 0)
    {
        float t = RollTo01(angleRoll);
        return await SpinCore(fixedIndex, t);
    }

    public async UniTask<RoulettePieceData> SpinReservedAsync()
    {
        if (!hasReservedResult)
        {
            return null;
        }

        hasReservedResult = false;
        return await SpinCore(reservedIndex, reservedAngleT01);
    }
}