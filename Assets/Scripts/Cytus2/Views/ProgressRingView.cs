using UnityEngine;

namespace Cytus2.Views
{
    [ExecuteInEditMode]
    public class ProgressRingView : MonoBehaviour
    {
        
        [Range(0, 1)]
        public float MaxCutoff;
        [Range(0, 1)]
        public float FillCutoff;
        public Color FillColor;

        SpriteRenderer spriteRenderer;
        int fillCutoffId, fillColorId, maxCutoffId;

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
            FillCutoff = Mathf.Min(FillCutoff, MaxCutoff);
            spriteRenderer.sharedMaterial.SetFloat(fillCutoffId, FillCutoff);
            spriteRenderer.sharedMaterial.SetFloat(maxCutoffId, MaxCutoff);
            spriteRenderer.sharedMaterial.SetColor(fillColorId, FillColor);
        }
        
        public void Reset()
        {
            spriteRenderer.enabled = false;
            MaxCutoff = 0;
            FillCutoff = 0;
        }
        
    }
}