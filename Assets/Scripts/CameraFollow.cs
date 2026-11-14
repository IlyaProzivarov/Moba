using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 30f, -8f);
    public float smoothSpeed = 8f;

    [Header("√раницы карты")]
    public float mapHalfSize = 20f;   
    public float margin = 2f;         

    [Header("«ум колесиком")]
    public float minZoom = 8f;
    public float maxZoom = 20f;
    public float zoomSpeed = 2f;

    Camera cam;
    Vector3 focus;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        if (target != null) focus = ClampFocus(target.position);
        transform.position = focus + offset;
    }

    void LateUpdate()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            cam.orthographicSize = Mathf.Clamp(
                cam.orthographicSize - scroll * zoomSpeed, minZoom, maxZoom);
        }

        if (target != null)
        {
            Vector3 want = ClampFocus(target.position);
            focus = Vector3.Lerp(focus, want, 1f - Mathf.Exp(-smoothSpeed * Time.unscaledDeltaTime));
        }

        transform.position = focus + offset;
    }

    Vector3 ClampFocus(Vector3 p)
    {
        float tilt = Mathf.Sin(transform.eulerAngles.x * Mathf.Deg2Rad);
        float halfZ = cam.orthographicSize / Mathf.Max(0.1f, tilt);
        float halfX = cam.orthographicSize * cam.aspect;

        float limX = Mathf.Max(0f, mapHalfSize + margin - halfX);
        float limZ = Mathf.Max(0f, mapHalfSize + margin - halfZ);

        return new Vector3(Mathf.Clamp(p.x, -limX, limX), 0f, Mathf.Clamp(p.z, -limZ, limZ));
    }
}