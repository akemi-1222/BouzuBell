using UnityEngine;

public class DoublePendulumController : MonoBehaviour
{
    [Header("接続位置：根元 → Hook → Hammer")]
    [SerializeField] private Transform _point1;
    [SerializeField] private Transform _point2;
    [SerializeField] private Transform _point3;

    [Header("アームの画像を動かすスクリプト")]
    [SerializeField] private ConnectorArmController _arm1;
    [SerializeField] private ConnectorArmController _arm2;

    [Header("Hammerの切り替え")]
    [SerializeField] private HammerSelectButtom _hammerSelect;

    [Header("重さ")]
    [SerializeField, Min(0.01f)] private float _hookMass = 5f;
    [SerializeField, Min(0.01f)] private float _woodMass = 1f;
    [SerializeField, Min(0.01f)] private float _metalMass = 3f;

    [Header("重力の倍率")]
    [SerializeField, Range(1f, 3f)]
    private float _gravityMultiplier = 2f;

    [Header("開始時の回転速度：度／秒")]
    [SerializeField] private float _startSpeed1 = 0f;

    // 1本目に対する2本目の相対的な回転速度
    [SerializeField] private float _startSpeed2 = 0f;

    [Header("回転補助の強さ：0なら補助しない")]
    [SerializeField, Min(0f)]
    private float _assistTorque = 5f;

    [Header("この速度より遅いときに補助：度／秒")]
    [SerializeField, Min(1f)]
    private float _assistBelowSpeed = 360f;

    private GameObject _physicsRoot;
    private ArticulationBody _firstBody;
    private ArticulationBody _secondBody;

    // プレイ開始時の位置
    private Vector3 _fixedPoint1;
    private Vector3 _initialPoint2;
    private Vector3 _initialPoint3;

    // プレイ開始時の棒の長さ
    private float _length1;
    private float _length2;

    // ほぼ停止したときに使う回転方向
    private float _lastDirection1;
    private float _lastDirection2;

    private bool _isSimulating;
    private bool _hasPhysicsError;

    // プレイボタンから呼ぶ
    public bool BeginSimulation()
    {
        if (_isSimulating)
            return !_hasPhysicsError;

        if (_point1 == null ||
            _point2 == null ||
            _point3 == null ||
            _arm1 == null ||
            _arm2 == null ||
            _hammerSelect == null)
        {
            Debug.LogError(
                "DoublePendulumControllerの参照をすべて設定してください。",
                this
            );

            return false;
        }

        Vector3 point1 = _point1.position;
        Vector3 point2 = _point2.position;
        Vector3 point3 = _point3.position;

        if (!IsFinite(point1) ||
            !IsFinite(point2) ||
            !IsFinite(point3))
        {
            Debug.LogError(
                "接続位置に異常な数値があります。",
                this
            );

            return false;
        }

        // XY平面上で動かすため、奥行きをそろえる
        if (Mathf.Abs(point1.z - point2.z) > 0.001f ||
            Mathf.Abs(point1.z - point3.z) > 0.001f)
        {
            Debug.LogError(
                "3つの接続位置のワールドZ座標をそろえてください。",
                this
            );

            return false;
        }

        float length1 = Vector3.Distance(point1, point2);
        float length2 = Vector3.Distance(point2, point3);

        if (length1 < 0.05f || length2 < 0.05f)
        {
            Debug.LogError(
                "2本とも、長さを付けて配置してください。",
                this
            );

            return false;
        }

        // 開始時の配置と長さを保存
        _fixedPoint1 = point1;
        _initialPoint2 = point2;
        _initialPoint3 = point3;

        _length1 = length1;
        _length2 = length2;

        _hasPhysicsError = false;

        // 画像の拡大率に影響されない物理用の階層
        _physicsRoot = new GameObject("PendulumPhysicsRoot");
        _physicsRoot.transform.position = point1;

        ArticulationBody rootBody =
            _physicsRoot.AddComponent<ArticulationBody>();

        // 根元は固定
        rootBody.immovable = true;
        rootBody.useGravity = false;

        // 根元からHookまで
        _firstBody = CreateBody(
            "HookPhysics",
            rootBody,
            point2,
            point1,
            Mathf.Max(0.01f, _hookMass)
        );

        float hammerMass = _hammerSelect.IsWood
            ? _woodMass
            : _metalMass;

        // HookからHammerまで
        _secondBody = CreateBody(
            "HammerPhysics",
            _firstBody,
            point3,
            point2,
            Mathf.Max(0.01f, hammerMass)
        );

        // 初速は開始時に一度だけ設定
        _firstBody.jointVelocity =
            new ArticulationReducedSpace(
                _startSpeed1 * Mathf.Deg2Rad
            );

        _secondBody.jointVelocity =
            new ArticulationReducedSpace(
                _startSpeed2 * Mathf.Deg2Rad
            );

        // 初速が0の場合の補助方向も決めておく
        _lastDirection1 = _startSpeed1 < 0f ? -1f : 1f;

        float worldSpeed2 = _startSpeed1 + _startSpeed2;

        _lastDirection2 = worldSpeed2 == 0f
            ? -1f
            : Mathf.Sign(worldSpeed2);

        _isSimulating = true;
        return true;
    }

    private ArticulationBody CreateBody(
        string objectName,
        ArticulationBody parentBody,
        Vector3 bodyPosition,
        Vector3 jointPosition,
        float mass)
    {
        GameObject bodyObject = new GameObject(objectName);

        bodyObject.transform.SetParent(parentBody.transform, false);
        bodyObject.transform.position = bodyPosition;
        bodyObject.transform.rotation = Quaternion.identity;

        ArticulationBody body =
            bodyObject.AddComponent<ArticulationBody>();

        // 1つの軸を中心に回る関節
        body.jointType = ArticulationJointType.RevoluteJoint;
        body.matchAnchors = false;

        // 親と子の接続位置を一致させる
        body.anchorPosition =
            bodyObject.transform.InverseTransformPoint(jointPosition);

        body.parentAnchorPosition =
            parentBody.transform.InverseTransformPoint(jointPosition);

        // 関節の回転軸を画面に垂直な方向へ向ける
        Quaternion jointRotation =
            Quaternion.Euler(0f, -90f, 0f);

        body.anchorRotation = jointRotation;
        body.parentAnchorRotation = jointRotation;

        // 回転角度を制限しない
        body.twistLock = ArticulationDofLock.FreeMotion;

        body.mass = mass;

        // 重力はFixedUpdateで加える
        body.useGravity = false;

        body.linearDamping = 0f;
        body.angularDamping = 0f;
        body.jointFriction = 0f;

        // 関節のバネとモーターは使用しない
        ArticulationDrive drive = body.xDrive;
        drive.stiffness = 0f;
        drive.damping = 0f;
        drive.forceLimit = 0f;
        body.xDrive = drive;

        // 接続点に小さな球の重りがあるものとして計算
        body.centerOfMass = Vector3.zero;
        body.inertiaTensorRotation = Quaternion.identity;

        const float radius = 0.1f;
        float inertia = 0.4f * mass * radius * radius;

        body.inertiaTensor = Vector3.one * inertia;

        body.solverIterations = 12;
        body.solverVelocityIterations = 12;

        // 低速時の自動休止を無効にする
        body.sleepThreshold = 0f;

        return body;
    }

    private void FixedUpdate()
    {
        if (!_isSimulating || _hasPhysicsError)
            return;

        Vector3 gravity =
            Physics.gravity * Mathf.Clamp(_gravityMultiplier, 1f, 3f);

        ApplyForces(
            _firstBody,
            gravity,
            ref _lastDirection1
        );

        ApplyForces(
            _secondBody,
            gravity,
            ref _lastDirection2
        );
    }

    private void ApplyForces(
        ArticulationBody body,
        Vector3 gravity,
        ref float lastDirection)
    {
        // 重力を加える
        body.AddForce(
            gravity * body.mass,
            ForceMode.Force
        );

        if (_assistTorque <= 0f)
            return;

        // ワールド座標での回転速度：ラジアン／秒
        float speed = body.angularVelocity.z;
        float absoluteSpeed = Mathf.Abs(speed);

        // 動いているときは、その回転方向を覚える
        // ほぼ停止したときは、直前の方向を使う
        if (absoluteSpeed > 0.05f)
        {
            lastDirection = Mathf.Sign(speed);
        }

        float speedLimit =
            Mathf.Max(1f, _assistBelowSpeed) * Mathf.Deg2Rad;

        // 十分速いときは補助しない
        if (absoluteSpeed >= speedLimit)
            return;

        // 遅いほど強く補助する
        float strength = 1f - absoluteSpeed / speedLimit;

        float torque =
            lastDirection * _assistTorque * strength;

        body.AddTorque(
            Vector3.forward * torque,
            ForceMode.Force
        );
    }

    private void LateUpdate()
    {
        if (!_isSimulating || _hasPhysicsError)
            return;

        Vector3 point2 = _firstBody.transform.position;
        Vector3 point3 = _secondBody.transform.position;

        float currentLength1 =
            Vector3.Distance(_fixedPoint1, point2);

        float currentLength2 =
            Vector3.Distance(point2, point3);

        // 小さな計算誤差は許容する
        float tolerance1 = Mathf.Max(0.001f, _length1 * 0.01f);
        float tolerance2 = Mathf.Max(0.001f, _length2 * 0.01f);

        bool invalid =
            !IsFinite(point2) ||
            !IsFinite(point3) ||
            Mathf.Abs(currentLength1 - _length1) > tolerance1 ||
            Mathf.Abs(currentLength2 - _length2) > tolerance2;

        if (invalid)
        {
            _hasPhysicsError = true;
            _physicsRoot.SetActive(false);

            Debug.LogError(
                "振り子の接続が維持されていないため停止しました。\n" +
                $"1本目：開始 {_length1:F3} ／ 現在 {currentLength1:F3}\n" +
                $"2本目：開始 {_length2:F3} ／ 現在 {currentLength2:F3}\n" +
                "停止ボタンで編集状態へ戻してください。",
                this
            );

            return;
        }

        // 根元に近いアームから順番に画像へ反映
        _arm1.ApplyPhysicsPosition(point2);
        _arm2.ApplyPhysicsPosition(point3);
    }

    private bool IsFinite(Vector3 position)
    {
        return
            !float.IsNaN(position.x) &&
            !float.IsNaN(position.y) &&
            !float.IsNaN(position.z) &&
            !float.IsInfinity(position.x) &&
            !float.IsInfinity(position.y) &&
            !float.IsInfinity(position.z);
    }

    // 停止ボタンから呼ぶ
    public void EndSimulation()
    {
        if (!_isSimulating)
            return;

        _isSimulating = false;
        _hasPhysicsError = false;

        RemovePhysicsObjects();

        // プレイ前の配置へ戻す
        _arm1.ApplyPhysicsPosition(_initialPoint2);
        _arm2.ApplyPhysicsPosition(_initialPoint3);
    }

    private void RemovePhysicsObjects()
    {
        if (_physicsRoot != null)
        {
            _physicsRoot.SetActive(false);
            Destroy(_physicsRoot);
        }

        _physicsRoot = null;
        _firstBody = null;
        _secondBody = null;
    }

    private void OnDestroy()
    {
        RemovePhysicsObjects();
    }
}