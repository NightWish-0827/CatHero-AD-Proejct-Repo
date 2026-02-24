using DG.Tweening;
using Cysharp.Threading.Tasks;
using System.Threading;

// DOTweenUniTaskUtil Static Utility Class
public static class DOTweenUniTaskUtil
{
    public static async UniTask AwaitTweenAsync(Tween tween, CancellationToken token)
    {
        using (token.Register(() => tween.Kill()))
        {
            while (tween != null && tween.IsActive() && !tween.IsComplete())
            {
                await UniTask.Yield(token);
            }
        }
        token.ThrowIfCancellationRequested();
    }
}
// DOTweenUniTaskUtil Static Utility Class