using UnityEngine;

[ExecuteInEditMode]
public class ProgressRing : MonoBehaviour
{
    [Range(0, 1)] public float maxCutoff;
    [Range(0, 1)] public float fillCutoff;
    public Color fillColor;

    private SpriteRenderer spriteRenderer;
    private int fillCutoffId, fillColorId, maxCutoffId;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        maxCutoffId = Shader.PropertyToID("_MaxCutoff");
        fillColorId = Shader.PropertyToID("_FillColor");
        fillCutoffId = Shader.PropertyToID("_FillCutoff");
    }

    private void Update()
    {
        spriteRenderer.enabled = true;
        fillCutoff = Mathf.Min(fillCutoff, maxCutoff);
        spriteRenderer.sharedMaterial.SetFloat(fillCutoffId, fillCutoff);
        spriteRenderer.sharedMaterial.SetFloat(maxCutoffId, maxCutoff);
        spriteRenderer.sharedMaterial.SetColor(fillColorId, fillColor);
    }
    
    public void Reset()
    {
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        maxCutoff = 0;
        fillCutoff = 0;
    }

}