using UnityEngine;

public class HealthBar : MonoBehaviour
{
    public Transform fillPivot;      
    public Renderer fillRenderer;    
    public Color fullColor = new Color(0.2f, 0.9f, 0.2f);
    public Color lowColor = new Color(0.9f, 0.15f, 0.15f);
    public bool hideWhenFull = false;

    Health health;
    Camera cam;
    Renderer[] renderers;
    MaterialPropertyBlock block;
    float fullWidth;
    float height;

    void Awake()
    {
        health = GetComponentInParent<Health>();
        cam = Camera.main;
        renderers = GetComponentsInChildren<Renderer>();
        block = new MaterialPropertyBlock();
        fullWidth = fillPivot.localScale.x;
        height = fillPivot.localScale.y;
    }

    void Start()
    {
        if (health == null)
        {
            Debug.LogError("HealthBar: у родителя нет компонента Health", this);
            enabled = false;
            return;
        }
        health.Changed += OnChanged;
        Refresh();
    }

    void OnDestroy()
    {
        if (health != null) health.Changed -= OnChanged;
    }

    void OnChanged(float current, float max)
    {
        Refresh();
    }

    void LateUpdate()
    {
        if (cam != null) transform.rotation = cam.transform.rotation;
    }

    void Refresh()
    {
        float ratio = Mathf.Clamp01(health.Ratio);
        fillPivot.localScale = new Vector3(fullWidth * ratio, height, 1f);

        fillRenderer.GetPropertyBlock(block);
        block.SetColor("_BaseColor", Color.Lerp(lowColor, fullColor, ratio));
        fillRenderer.SetPropertyBlock(block);

        bool visible = !(hideWhenFull && ratio >= 1f);
        foreach (var r in renderers) r.enabled = visible;
    }
}