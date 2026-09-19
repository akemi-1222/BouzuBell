using UnityEngine;
using UnityEngine.EventSystems;

public class ConnectorArmController : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("アームの根元")]
    [SerializeField] private Transform _startPoint;

    [Header("伸縮するアーム画像")]
    [SerializeField] private Transform _connectorArm;

    [Header("先端の接続位置")]
    [SerializeField] private Transform _connectPoint;

    [Header("先端の画像（HookまたはHammer）")]
    [SerializeField] private Transform _visual;

    private Camera _camera;
    private SpriteRenderer _armRenderer;
    private Bounds _armBounds;

    //開始時の大きさと太さ
    private Vector3 _initialScale;
    private float _initialThickness;

    //先端画像の中で、接続点に当たる位置
    private Vector3 _visualConnectPosition;

    private bool _isDragging;
    private Vector2 _pointerPosition;

    private void Start()
    {
        _camera = Camera.main;

        if (_camera == null || _startPoint == null || _connectorArm == null || _connectPoint == null || _visual == null)
        {
            Debug.LogError("カメラと4つの参照を設定してください。", this);

            enabled = false;
            return;
        }

        _armRenderer = _connectorArm.GetComponent<SpriteRenderer>();

        if (_armRenderer == null || _armRenderer.sprite == null)
        {
            Debug.LogError("アームに画像を設定してください。", this);
            enabled = false;
            return;
        }

        _armBounds = _armRenderer.sprite.bounds;
        _initialScale = _connectorArm.localScale;

        //ワールド上での太さを保存する
        _initialThickness = _armBounds.size.y * Mathf.Abs(_initialScale.y) * GetThicknessFactor(_connectorArm.localRotation);

        //回転しても同じ場所で接続できるように保存する
        _visualConnectPosition = _visual.InverseTransformPoint(_connectPoint.position);
    }

    private void LateUpdate()
    {
        //自分をドラッグしているときだけ画面内に制限する
        if (_isDragging)
            MoveTip();

        //親が動いた場合にも、アームと先端画像を合わせ直す
        UpdateArm();
        RotateVisual();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isActiveAndEnabled)
            return;

        _isDragging = true;
        _pointerPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging)
            return;

        _pointerPosition = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isDragging)
            return;

        _pointerPosition = eventData.position;
        MoveTip();

        _isDragging = false;
    }

    private void OnDisable()
    {
        _isDragging = false;
    }

    //先端をマウスの位置へ移動する
    private void MoveTip()
    {
        Rect screen = _camera.pixelRect;
        Vector2 position = _pointerPosition;

        position.x = Mathf.Clamp(
            position.x,
            screen.xMin,
            screen.xMax
        );

        position.y = Mathf.Clamp(
            position.y,
            screen.yMin,
            screen.yMax
        );

        //根元と同じZ座標の平面を用意する
        Plane plane = new Plane(Vector3.forward, _startPoint.position);

        //マウス位置から、その平面上の位置を求める
        Ray ray = _camera.ScreenPointToRay(position);

        if (plane.Raycast(ray, out float distance))
        {
            Vector3 target = ray.GetPoint(distance);

            transform.position += target - _connectPoint.position;
        }
    }

    //アームの長さ・太さ・向き・位置を合わせる
    private void UpdateArm()
    {
        Transform parent = _connectorArm.parent;

        //アームの親を基準にした座標へ変換する
        Vector3 start = parent.InverseTransformPoint(_startPoint.position);

        Vector3 end = parent.InverseTransformPoint(_connectPoint.position);

        Vector2 direction = end - start;

        if (_armBounds.size.x <= 0f)
            return;

        Quaternion rotation = GetRotation(direction, _connectorArm.localRotation);

        Vector3 scale = _initialScale;

        //横方向を接続点まで伸ばす
        scale.x = direction.magnitude / _armBounds.size.x;

        //親のScaleの影響を補正して太さを保つ
        float thickness = _armBounds.size.y * GetThicknessFactor(rotation);

        if (thickness > 0.000001f)
            scale.y = Mathf.Sign(_initialScale.y) * _initialThickness / thickness;

        _connectorArm.localRotation = rotation;
        _connectorArm.localScale = scale;

        //画像の中心を求める
        Vector3 center = _armBounds.center;

        if (_armRenderer.flipX)
            center.x = -center.x;

        if (_armRenderer.flipY)
            center.y = -center.y;

        Vector3 offset = rotation * Vector3.Scale(center, scale);

        //画像の中心を根元と先端の中間に置く
        _connectorArm.localPosition = (start + end) / 2f - offset;
    }

    //HookやHammerをアームの方向へ向ける
    private void RotateVisual()
    {
        Transform parent = _visual.parent;

        Vector3 start = parent.InverseTransformPoint(_startPoint.position);

        Vector3 end = parent.InverseTransformPoint(_connectPoint.position);

        Quaternion rotation = GetRotation( end - start, _visual.localRotation);

        _visual.localRotation = rotation;

        //回転しても画像の接続部分がずれないようにする
        Vector3 offset = rotation * Vector3.Scale( _visualConnectPosition, _visual.localScale);

        _visual.localPosition = -offset;
    }

    //方向から回転を求める
    private Quaternion GetRotation(
        Vector2 direction,
        Quaternion currentRotation)
    {
        //根元と先端が重なったら現在の向きを維持する
        if (direction.sqrMagnitude < 0.000001f)
            return currentRotation;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        return Quaternion.Euler(0f, 0f, angle);
    }

    //親のScaleによって太さが何倍になるかを求める
    private float GetThicknessFactor(Quaternion rotation)
    {
        Transform parent = _connectorArm.parent;

        Vector3 right = parent.TransformVector(rotation * Vector3.right);

        Vector3 up = parent.TransformVector(rotation * Vector3.up);

        if (right.sqrMagnitude < 0.000001f)
            return 0f;

        return Vector3.Cross(right, up).magnitude / right.magnitude;
    }
}