using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;

public class NightmareMonster : EnemyBase
{
    [SerializeField, Min(0f)] private float flightHeight = 1.6f;
    [SerializeField, Min(0f)] private float bobAmplitude = 0.25f;
    [SerializeField, Min(0f)] private float bobFrequency = 2.2f;

    [SerializeField, Min(0f)] private float attackWindUp = 0.15f;

    protected override async UniTaskVoid BehaviorLoopAsync(CancellationToken token)
    {
        while (currentState != EnemyState.Dead && !token.IsCancellationRequested)
        {
            if (targetTransform == null || Stats == null) break;

            if (currentState == EnemyState.Chasing)
            {
                Vector3 targetPos = targetTransform.position + Vector3.up * flightHeight;
                targetPos += Vector3.up * (Mathf.Sin(Time.time * bobFrequency) * bobAmplitude);

                myTransform.position = Vector3.MoveTowards(
                    myTransform.position,
                    targetPos,
                    Stats.MoveSpeed * Time.deltaTime);

                // 비행 개체는 수평(x) 기준으로 공격 판정 (고도 때문에 거리 판정이 흔들리지 않게)
                float absDx = Mathf.Abs(targetTransform.position.x - myTransform.position.x);
                if (absDx <= Stats.AttackRange)
                {
                    currentState = EnemyState.Attacking;
                    await AttackSequenceAsync(token);
                }
                else
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            else
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
    }

    protected override async UniTask AttackSequenceAsync(CancellationToken token)
    {
        if (Stats == null) return;

        // 살짝 접근/상승 후 타격(원거리/근접 연출은 추후 교체 가능)
        var tween = myTransform.DOPunchPosition(Vector3.up * 0.15f, attackWindUp, 8, 0.8f)
            .SetEase(Ease.OutQuad);
        await DOTweenUniTaskUtil.AwaitTweenAsync(tween, token);

        if (currentState != EnemyState.Dead)
        {
            GameEvents.OnPlayerHit.OnNext(Stats.AttackDamage);
        }

        await UniTask.Delay(TimeSpan.FromSeconds(Stats.AttackCooldown), cancellationToken: token);

        if (currentState != EnemyState.Dead)
        {
            currentState = EnemyState.Chasing;
        }
    }
}
