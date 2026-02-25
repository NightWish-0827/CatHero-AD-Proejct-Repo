using DG.Tweening;
using Cysharp.Threading.Tasks;
using System.Threading;

// DOTweenUniTaskUtil Static Utility Class
public static class DOTweenUniTaskUtil
{
    public static async UniTask AwaitTweenAsync(Tween tween, CancellationToken token)
    {
        if (tween == null)
            return;

        using (token.Register(() =>
        {
            if (tween.IsActive())
                tween.Kill();
        }))
        {
            while (tween != null && tween.IsActive() && !tween.IsComplete())
            {
                await UniTask.Yield(token);
            }
        }
        token.ThrowIfCancellationRequested();
    }

    // `await myTween.AwaitForComplete();` 형태로 쓰기 위한 확장 메서드
    public static UniTask AwaitForComplete(this Tween tween, CancellationToken token = default)
        => AwaitTweenAsync(tween, token);
}
// DOTweenUniTaskUtil Static Utility Class