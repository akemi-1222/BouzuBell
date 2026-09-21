using UnityEngine;

public class DoublePendulumController : MonoBehaviour
{
    [Header("接続位置：根元 → Hook → Hammer")]
    [SerializeField] private Transform _point1;
    [SerializeField] private Transform _point2;
    [SerializeField] private Transform _point3;

    [Header("アームを動かすスクリプト")]
    [SerializeField] private ConnectorArmController _arm1;
    [SerializeField] private ConnectorArmController _arm2;

    [Header("Hammerの切り替え")]
    [SerializeField] private HammerSelectButtom _hammerSelect;

    [Header("重さ")]
    [SerializeField, Min(0.01f)] private float _hookMass = 5f;
    [SerializeField, Min(0.01f)] private float _woodMass = 1f;
    [SerializeField, Min(0.01f)] private float _metalMass = 3f;

    [Header("重力の倍率")]
    [SerializeField, Range(1f, 3f)] private float _gravityMultiplier = 2f;

    [Header("開始時の回転速度：度／秒")]
    [SerializeField] private float _startSpeed1 = 0f;
    [SerializeField] private float _startSpeed2 = 0f;

    [Header("回転補助の強さ")]
    [SerializeField, Min(0f)] private float _assistTorque = 5f;

    [Header("この速度より遅いときに補助：度／秒")]
    [SerializeField, Min(1f)] private float _assistBelowSpeed = 360f;

    [Header("命中する鐘")]
    [SerializeField] private BellHit _bell;

    [Header("ハンマーの当たり判定の半径：ワールド単位")]
    [SerializeField, Min(0.01f)]
    private float _hammerHitRadius = 0.2f;

    //物理計算用のオブジェクト
    private GameObject _physicsRoot;
    private ArticulationBody _firstBody;
    private ArticulationBody _secondBody;

    //開始時の位置と長さ
    private Vector3 _fixedPoint1;
    private Vector3 _initialPoint2;
    private Vector3 _initialPoint3;
    private float _length1;
    private float _length2;

    //直前に回っていた方向
    private float _lastDirection1;
    private float _lastDirection2;

    private bool _isSimulating;
    private bool _hasPhysicsError;

    //プレイボタンから呼ぶ
    public bool BeginSimulation()
    {
        if (_isSimulating)
            return !_hasPhysicsError;

        if (!CheckSettings())
            return false;

        //停止時に戻せるように、現在の配置を保存
        _fixedPoint1 = _point1.position;
        _initialPoint2 = _point2.position;
        _initialPoint3 = _point3.position;

        _length1 = Vector3.Distance(_fixedPoint1, _initialPoint2);
        _length2 = Vector3.Distance(_initialPoint2, _initialPoint3);

        _hasPhysicsError = false;

        //固定する根元を作る
        _physicsRoot = new GameObject("PendulumPhysicsRoot");
        _physicsRoot.transform.position = _fixedPoint1;

        ArticulationBody rootBody = _physicsRoot.AddComponent<ArticulationBody>();

        rootBody.immovable = true;
        rootBody.useGravity = false;

        //1本目：根元からHook
        _firstBody = CreateBody("HookPhysics", rootBody, _initialPoint2, _fixedPoint1, _hookMass);

        float hammerMass = _metalMass;

        if (_hammerSelect.IsWood)
            hammerMass = _woodMass;

        //2本目：HookからHammer
        _secondBody = CreateBody( "HammerPhysics", _firstBody, _initialPoint3, _initialPoint2, hammerMass);

        SetupHammerHit();
        _bell.ResetBell();

        //Unityの物理計算用に、度をラジアンへ変換
        _firstBody.jointVelocity = new ArticulationReducedSpace(_startSpeed1 * Mathf.Deg2Rad);

        //2本目は、1本目に対する相対的な速度
        _secondBody.jointVelocity = new ArticulationReducedSpace(_startSpeed2 * Mathf.Deg2Rad);

        //初速が0のときにも、補助する方向を決めておく
        _lastDirection1 = 1f;

        if (_startSpeed1 < 0f)
            _lastDirection1 = -1f;

        _lastDirection2 = -1f;

        float worldSpeed2 = _startSpeed1 + _startSpeed2;

        if (worldSpeed2 != 0f)
            _lastDirection2 = Mathf.Sign(worldSpeed2);

        _isSimulating = true;
        return true;
    }

    //開始できる設定か確認する
    private bool CheckSettings()
    {
        if (_point1 == null || _point2 == null || _point3 == null ||
            _arm1 == null || _arm2 == null || _hammerSelect == null || _bell == null)
        {
            Debug.LogError("Inspectorの参照をすべて設定してください", this);
            return false;
        }

        Vector3 point1 = _point1.position;
        Vector3 point2 = _point2.position;
        Vector3 point3 = _point3.position;

        if (!IsFinite(point1) || !IsFinite(point2) || !IsFinite(point3))
        {
            Debug.LogError("接続位置に異常な数値があります。", this);
            return false;
        }

        if (Mathf.Abs(point1.z - point2.z) > 0.001f ||
            Mathf.Abs(point1.z - point3.z) > 0.001f)
        {
            Debug.LogError("3つの接続位置のワールドZ座標をそろえてください。", this);
            return false;
        }

        if (Vector3.Distance(point1, point2) < 0.05f ||
            Vector3.Distance(point2, point3) < 0.05f)
        {
            Debug.LogError("2本とも、長さを付けて配置してください。", this);
            return false;
        }

        return true;
    }

    //回転する関節と重りを作る
    private ArticulationBody CreateBody(
        string objectName,
        ArticulationBody parentBody,
        Vector3 position,
        Vector3 jointPosition,
        float mass)
    {
        GameObject bodyObject = new GameObject(objectName);
        bodyObject.transform.SetParent(parentBody.transform, false);
        bodyObject.transform.position = position;
        bodyObject.transform.rotation = Quaternion.identity;

        ArticulationBody body = bodyObject.AddComponent<ArticulationBody>();

        body.jointType = ArticulationJointType.RevoluteJoint;
        body.matchAnchors = false;

        //親と子の接続位置を合わせる
        body.anchorPosition = bodyObject.transform.InverseTransformPoint(jointPosition);

        body.parentAnchorPosition = parentBody.transform.InverseTransformPoint(jointPosition);

        //画面に沿って回るように、回転軸を設定
        Quaternion rotation = Quaternion.Euler(0f, -90f, 0f);
        body.anchorRotation = rotation;
        body.parentAnchorRotation = rotation;

        body.twistLock = ArticulationDofLock.FreeMotion;

        mass = Mathf.Max(0.01f, mass);
        body.mass = mass;

        //重力は自分で加える
        body.useGravity = false;

        //抵抗をなくす
        body.linearDamping = 0f;
        body.angularDamping = 0f;
        body.jointFriction = 0f;

        //関節のバネとモーターを無効にする
        ArticulationDrive drive = body.xDrive;
        drive.stiffness = 0f;
        drive.damping = 0f;
        drive.forceLimit = 0f;
        body.xDrive = drive;

        //小さな球の重りとして、回りにくさを設定
        body.centerOfMass = Vector3.zero;
        body.inertiaTensorRotation = Quaternion.identity;

        const float radius = 0.1f;
        float inertia = 0.4f * mass * radius * radius;
        body.inertiaTensor = Vector3.one * inertia;

        body.solverIterations = 12;
        body.solverVelocityIterations = 12;
        body.sleepThreshold = 0f;

        return body;
    }

    //一定時間ごとに物理的な力を加える
    private void FixedUpdate()
    {
        if (!_isSimulating || _hasPhysicsError)
            return;

        Vector3 gravity = Physics.gravity * Mathf.Clamp(_gravityMultiplier, 1f, 3f);

        _lastDirection1 = ApplyForces(_firstBody, gravity, _lastDirection1);

        _lastDirection2 = ApplyForces(_secondBody, gravity, _lastDirection2);
    }

    //力を加え、次回に使う回転方向を返す
    private float ApplyForces(
        ArticulationBody body,
        Vector3 gravity,
        float direction)
    {
        body.AddForce(gravity * body.mass, ForceMode.Force);

        if (_assistTorque <= 0f)
            return direction;

        float speed = body.angularVelocity.z;
        float absoluteSpeed = Mathf.Abs(speed);

        //ほぼ停止している間は、前回の方向を使う
        if (absoluteSpeed > 0.05f)
            direction = Mathf.Sign(speed);

        float speedLimit = Mathf.Max(1f, _assistBelowSpeed) * Mathf.Deg2Rad;

        if (absoluteSpeed >= speedLimit)
            return direction;

        //遅いほど強く補助する
        float strength = 1f - absoluteSpeed / speedLimit;
        float torque = direction * _assistTorque * strength;

        body.AddTorque(Vector3.forward * torque, ForceMode.Force);

        return direction;
    }

    private void SetupHammerHit()
    {
        // Colliderを追加しても、今の重心と慣性設定を維持する
        _secondBody.automaticCenterOfMass = false;
        _secondBody.automaticInertiaTensor = false;

        SphereCollider hitCollider =
            _secondBody.gameObject.AddComponent<SphereCollider>();

        hitCollider.radius = Mathf.Max(0.01f, _hammerHitRadius);
        hitCollider.isTrigger = false;

        // 高速で動くハンマーの衝突を検出しやすくする
        _secondBody.collisionDetectionMode =
            CollisionDetectionMode.ContinuousSpeculative;

        HammerHit hammerHit =
            _secondBody.gameObject.AddComponent<HammerHit>();

        hammerHit.Initialize(_secondBody, _bell);
    }

    //物理計算の結果に画像を合わせる
    private void LateUpdate()
    {
        if (!_isSimulating || _hasPhysicsError)
            return;

        Vector3 point2 = _firstBody.transform.position;
        Vector3 point3 = _secondBody.transform.position;

        float length1 = Vector3.Distance(_fixedPoint1, point2);
        float length2 = Vector3.Distance(point2, point3);

        //小さな計算誤差だけ許容する
        float tolerance1 = Mathf.Max(0.001f, _length1 * 0.01f);
        float tolerance2 = Mathf.Max(0.001f, _length2 * 0.01f);

        if (!IsFinite(point2) || !IsFinite(point3) ||
            Mathf.Abs(length1 - _length1) > tolerance1 ||
            Mathf.Abs(length2 - _length2) > tolerance2)
        {
            _hasPhysicsError = true;
            _physicsRoot.SetActive(false);

            return;
        }

        //必ず根元に近い方から動かす
        _arm1.ApplyPhysicsPosition(point2);
        _arm2.ApplyPhysicsPosition(point3);
    }

    //座標が異常な数値になっていないか確認する
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

    //停止ボタンから呼ぶ
    public void EndSimulation()
    {
        if (!_isSimulating)
            return;

        _isSimulating = false;
        _hasPhysicsError = false;

        RemovePhysicsObjects();

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