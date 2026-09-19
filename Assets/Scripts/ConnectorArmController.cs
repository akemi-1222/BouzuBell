using UnityEngine;
using UnityEngine.EventSystems;

public class ConnectorArmController : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("アームの根元：ConnectorPoint1")]
    [SerializeField] private Transform _startPoint;

    [Header("アーム画像：ConnectorArm1")]
    [SerializeField] private Transform _connectorArm;

    [Header("Hookの接続位置：HookConnectPoint")]
    [SerializeField] private Transform _hookConnectPoint;

    [Header("Hook画像：HookVisual")]
    [SerializeField] private Transform _hookVisual;

    [Header("Hook画像の角度補正")]
    [SerializeField] private float _hookAngleOffset = 0f;

    [Header("画面端の余白（ピクセル）")]
    [SerializeField, Min(0f)]
    private float _screenMarginPixels = 0f;

    private Camera _mainCamera;
    private SpriteRenderer _armRenderer;

    private Vector3 _initialArmScale;
    private Vector3 _hookAnchorLocal;

    private bool _isReady;
    private bool _isDragging;

    private void Start()
    {
        _mainCamera = Camera.main;

        if (_mainCamera == null)
        {
            StopWithError(
                "カメラのTagをMainCameraに設定してください。"
            );
            return;
        }

        if (_startPoint == null ||
            _connectorArm == null ||
            _hookConnectPoint == null ||
            _hookVisual == null)
        {
            StopWithError(
                "InspectorのStart Point、Connector Arm、" +
                "Hook Connect Point、Hook Visualをすべて設定してください。"
            );
            return;
        }

        if (_connectorArm == _hookConnectPoint ||
            _connectorArm.parent != transform ||
            _hookConnectPoint.parent != transform ||
            _hookVisual.parent != _hookConnectPoint)
        {
            StopWithError(
                "ConnectorArm1とHookConnectPointは" +
                "ArmControllerHookの直接の子、" +
                "HookVisualはHookConnectPointの直接の子にしてください。"
            );
            return;
        }

        if (_startPoint == transform ||
            _startPoint.IsChildOf(transform))
        {
            StopWithError(
                "ConnectorPoint1はArmControllerHookの外に置いてください。"
            );
            return;
        }

        _armRenderer =
            _connectorArm.GetComponent<SpriteRenderer>();

        if (_armRenderer == null ||
            _armRenderer.sprite == null ||
            _armRenderer.drawMode != SpriteDrawMode.Simple)
        {
            StopWithError(
                "ConnectorArm1にSpriteRendererと画像を設定し、" +
                "Draw ModeをSimpleにしてください。"
            );
            return;
        }

        Vector3 hookScale = _hookVisual.localScale;

        if (Mathf.Abs(hookScale.x) < 0.000001f ||
            Mathf.Abs(hookScale.y) < 0.000001f ||
            Mathf.Abs(hookScale.z) < 0.000001f)
        {
            StopWithError(
                "HookVisualのScaleを0以外にしてください。"
            );
            return;
        }

        _initialArmScale = _connectorArm.localScale;

        // Hook画像内で接続点に当たる位置を記録する。
        _hookAnchorLocal = _hookVisual.InverseTransformPoint(
            _hookConnectPoint.position
        );

        _isReady = true;
    }

    private void LateUpdate()
    {
        if (!_isReady)
            return;

        KeepHookInsideScreen();
        UpdateConnectorArm();
        UpdateHookRotation();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_isReady || !isActiveAndEnabled)
            return;

        _isDragging = true;
        MoveHook(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isReady || !isActiveAndEnabled || !_isDragging)
            return;

        MoveHook(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isDragging = false;
    }

    private void OnDisable()
    {
        _isDragging = false;
    }

    private void StopWithError(string message)
    {
        Debug.LogError(
            $"[{gameObject.name}] {message}",
            this
        );

        _isReady = false;
        enabled = false;
    }

    private void MoveHook(Vector2 screenPosition)
    {
        screenPosition = ClampScreenPosition(screenPosition);

        // 根元と同じZ座標のXY平面上で移動する。
        Plane movementPlane = new Plane(
            Vector3.forward,
            _startPoint.position
        );

        Ray ray = _mainCamera.ScreenPointToRay(screenPosition);

        if (!movementPlane.Raycast(ray, out float distance))
            return;

        Vector3 targetPosition = ray.GetPoint(distance);

        // Hookの原点ではなく、接続位置を目的地に合わせる。
        transform.position +=
            targetPosition - _hookConnectPoint.position;
    }

    private Vector2 ClampScreenPosition(Vector2 position)
    {
        Rect rect = _mainCamera.pixelRect;

        float marginX = Mathf.Clamp(
            _screenMarginPixels, 0f, rect.width * 0.5f
        );

        float marginY = Mathf.Clamp(
            _screenMarginPixels, 0f, rect.height * 0.5f
        );

        position.x = Mathf.Clamp(
            position.x,
            rect.xMin + marginX,
            rect.xMax - marginX
        );

        position.y = Mathf.Clamp(
            position.y,
            rect.yMin + marginY,
            rect.yMax - marginY
        );

        return position;
    }

    private void KeepHookInsideScreen()
    {
        Vector3 screenPosition =
            _mainCamera.WorldToScreenPoint(
                _hookConnectPoint.position
            );

        MoveHook(new Vector2(
            screenPosition.x,
            screenPosition.y
        ));
    }

    private void UpdateConnectorArm()
    {
        Transform parent = _connectorArm.parent;

        // 親の回転や負のスケールを考慮する。
        Vector3 start = parent.InverseTransformPoint(
            _startPoint.position
        );

        Vector3 end = parent.InverseTransformPoint(
            _hookConnectPoint.position
        );

        Vector2 direction = end - start;
        Bounds bounds = _armRenderer.sprite.bounds;

        if (bounds.size.x <= Mathf.Epsilon)
            return;

        float angle =
            Mathf.Atan2(direction.y, direction.x)
            * Mathf.Rad2Deg;

        Quaternion rotation =
            Quaternion.Euler(0f, 0f, angle);

        // 長さの上下限は設けず、接続点まで伸ばす。
        Vector3 scale = _initialArmScale;
        scale.x = direction.magnitude / bounds.size.x;

        _connectorArm.localRotation = rotation;
        _connectorArm.localScale = scale;

        Vector3 center = bounds.center;

        if (_armRenderer.flipX)
            center.x = -center.x;

        if (_armRenderer.flipY)
            center.y = -center.y;

        Vector3 centerOffset =
            rotation * Vector3.Scale(center, scale);

        _connectorArm.localPosition =
            (start + end) * 0.5f - centerOffset;
    }

    private void UpdateHookRotation()
    {
        Transform parent = _hookVisual.parent;

        Vector3 start = parent.InverseTransformPoint(
            _startPoint.position
        );

        Vector3 end = parent.InverseTransformPoint(
            _hookConnectPoint.position
        );

        Vector2 direction = end - start;

        if (direction.sqrMagnitude < 0.000001f)
            return;

        float angle =
            Mathf.Atan2(direction.y, direction.x)
            * Mathf.Rad2Deg;

        Quaternion rotation = Quaternion.Euler(
            0f,
            0f,
            angle + _hookAngleOffset
        );

        _hookVisual.localRotation = rotation;

        // 回転後も画像の接続部分を接続点に固定する。
        Vector3 anchorOffset =
            rotation * Vector3.Scale(
                _hookAnchorLocal,
                _hookVisual.localScale
            );

        _hookVisual.localPosition = -anchorOffset;
    }
}