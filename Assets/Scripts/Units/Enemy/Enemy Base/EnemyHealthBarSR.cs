using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class EnemyHealthBarSR : MonoBehaviour
{
    private static readonly int PropProgress = Shader.PropertyToID("_Progress");

    private SpriteRenderer sr;
    private MaterialPropertyBlock mpb;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (mpb == null) mpb = new MaterialPropertyBlock();
    }

    public void SetVisible(bool visible)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        sr.enabled = visible;
    }

    public void SetProgress01(float progress01)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (mpb == null) mpb = new MaterialPropertyBlock();

        sr.GetPropertyBlock(mpb);
        mpb.SetFloat(PropProgress, Mathf.Clamp01(progress01));
        sr.SetPropertyBlock(mpb);
    }
}
