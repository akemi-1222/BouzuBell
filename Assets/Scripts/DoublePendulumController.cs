using UnityEngine;

[DefaultExecutionOrder(-100)]
public class DoublePendulumController : MonoBehaviour
{
    [Header("接続位置：根元 → Hook → Hammer")]
    [SerializeField] private Transform _point1;
    [SerializeField] private Transform _point2;
    [SerializeField] private Transform _point3;

    [Header("アームの画像を動かすスクリプト")]
    [SerializeField] private ConnectorArmController _arm1;
    [SerializeField] private ConnectorArmController _arm2;

    [Header("鐘")]
    [SerializeField] private BellHit _bell;
    [SerializeField] private BellMotion _bellMotion;

    [Header("Hammerの切り替え")]
    [SerializeField] private HammerSelectButtom _hammerSelect;

    [Header("重さ")]
    [SerializeField, Min(0.01f)] private float _hookMass = 5f;
    [SerializeField, Min(0.01f)] private float _woodMass = 1f;
    [SerializeField, Min(0.01f)] private float _metalMass = 3f;

    [Header("重力の倍率")]
    [SerializeField, Range(1f, 3f)]
    private float _gravityMultiplier = 2f;

    [Header("動き始めた後の回転補助")]
    [SerializeField, Min(0f)]
    private float _assistTorque = 5f;

    [Header("この速度より遅いときに補助：度／秒")]
    [SerializeField, Min(1f)]
    private float _assistBelowSpeed = 360f;

    [Header("ハンマーの当たり判定の半径")]
    [SerializeField, Min(0.01f)]
    private float _hammerHitRadius = 0.2f;

    [Header("連続回転の対策")]
    [SerializeField, Min(360f)]
    private float _spinLimitAngle = 720f;

    [Header("連続回転中のブレーキ：0なら補助停止だけ")]
    [SerializeField, Min(0f)]
    private float _spinBrake = 0f;

    //第１アームの回転を記録する
    private float _previousArmAngle;
    private float _turnDirection;
    private float _turnAngle;

    //小さな揺れを反転と間違えないための記録
    private float _reverseAngle;
    private float _reverseTime;

    private bool _limitingSpin;
    private float _assistBlend = 1f;
    private float _brakeBlend;

    //HammerHitから、加速してよいか確認する
    public bool CanBoost =>
        _isSimulating &&
        !_hasPhysicsError &&
        !_limitingSpin &&
        _assistBlend >= 0.99f;

    //画像とは別に作る、物理計算用のオブジェクト
    private GameObject _physicsRoot;
    private ArticulationBody _firstBody;
    private ArticulationBody _secondBody;

    //プレイ開始時の位置と長さ
    private Vector3 _fixedPoint1;
    private Vector3 _initialPoint2;
    private Vector3 _initialPoint3;
    private float _length1;
    private float _length2;

    //0なら、まだ回転方向が決まっていない
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

        //停止時に戻すため、現在の配置を保存
        _fixedPoint1 = _point1.position;
        _initialPoint2 = _point2.position;
        _initialPoint3 = _point3.position;

        _length1 = Vector3.Distance(_fixedPoint1, _initialPoint2);
        _length2 = Vector3.Distance(_initialPoint2, _initialPoint3);

        ResetSpinGuard();

        //開始方向は重力に任せる
        _lastDirection1 = 0f;
        _lastDirection2 = 0f;

        _hasPhysicsError = false;

        //画像の拡大率に影響されない物理用の根元
        _physicsRoot = new GameObject("PendulumPhysicsRoot");
        _physicsRoot.transform.position = _fixedPoint1;

        ArticulationBody rootBody =
            _physicsRoot.AddComponent<ArticulationBody>();

        rootBody.immovable = true;
        rootBody.useGravity = false;

        //1本目：根元からHook
        _firstBody = CreateBody( "HookPhysics", rootBody, _initialPoint2, _fixedPoint1, _hookMass);

        // 第１アームだけ、回転の勢いを少し落とす
        _firstBody.angularDamping = 0.1f;

        float hammerMass = _metalMass;

        if (_hammerSelect.IsWood)
            hammerMass = _woodMass;

        //2本目：HookからHammer
        _secondBody = CreateBody(
            "HammerPhysics",
            _firstBody,
            _initialPoint3,
            _initialPoint2,
            hammerMass
        );

        //新しく作った物体なので、初速は与えない
        SetupHammerHit();

        _bell.ResetBell();
        _bellMotion.BeginMotion();

        _isSimulating = true;
        return true;
    }

    //開始に必要な設定を確認する
    private bool CheckSettings()
    {
        if (_point1 == null || _point2 == null || _point3 == null ||
            _arm1 == null || _arm2 == null ||
            _bell == null || _bellMotion == null ||
            _hammerSelect == null)
            return false;

        if (_bell.GetComponent<Rigidbody>() == null)
            return false;

        if (_bellMotion.gameObject != _bell.gameObject)
            return false;

        Vector3 point1 = _point1.position;
        Vector3 point2 = _point2.position;
        Vector3 point3 = _point3.position;

        if (!IsFinite(point1) || !IsFinite(point2) || !IsFinite(point3))
            return false;

        if (Mathf.Abs(point1.z - point2.z) > 0.001f ||
            Mathf.Abs(point1.z - point3.z) > 0.001f)
            return false;
        

        if (Vector3.Distance(point1, point2) < 0.05f ||
            Vector3.Distance(point2, point3) < 0.05f)
            return false;

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

        //関節の回転軸をワールドZ方向に向ける
        Quaternion rotation = Quaternion.Euler(0f, -90f, 0f);

        body.anchorRotation = rotation;
        body.parentAnchorRotation = rotation;
        body.twistLock = ArticulationDofLock.FreeMotion;

        mass = Mathf.Max(0.01f, mass);
        body.mass = mass;

        //重力はFixedUpdateで加える
        body.useGravity = false;

        body.linearDamping = 0f;
        body.angularDamping = 0.05f;
        body.jointFriction = 0f;

        //関節のバネとモーターは使わない
        ArticulationDrive drive = body.xDrive;
        drive.stiffness = 0f;
        drive.damping = 0f;
        drive.forceLimit = 0f;
        body.xDrive = drive;

        //Colliderを追加しても、重心と慣性を変えない
        body.automaticCenterOfMass = false;
        body.automaticInertiaTensor = false;

        body.centerOfMass = Vector3.zero;
        body.inertiaTensorRotation = Quaternion.identity;

        //これまでと同じ、小さな球の重りとして計算
        const float radius = 0.1f;
        float inertia = 0.4f * mass * radius * radius;

        body.inertiaTensor = Vector3.one * inertia;

        body.solverIterations = 12;
        body.solverVelocityIterations = 12;
        body.sleepThreshold = 0f;

        return body;
    }

    private void SetupHammerHit()
    {
        SphereCollider hitCollider = _secondBody.gameObject.AddComponent<SphereCollider>();

        hitCollider.radius = Mathf.Max(0.01f, _hammerHitRadius);
        hitCollider.isTrigger = true;

        HammerHit hammerHit = _secondBody.gameObject.AddComponent<HammerHit>();

        hammerHit.Initialize(_secondBody, _firstBody, _bell, this);
    }

    private void FixedUpdate()
    {
        if (!_isSimulating || _hasPhysicsError)
            return;

        UpdateSpinGuard();

        Vector3 gravity =
            Physics.gravity * Mathf.Clamp(_gravityMultiplier, 1f, 3f);

        _lastDirection1 = ApplyForces(
            _firstBody,
            gravity,
            _lastDirection1,
            0f
            );

        _lastDirection2 = ApplyForces(
            _secondBody,
            gravity,
            _lastDirection2,
            1f * _assistBlend
        );

        ApplySpinBrake(_firstBody);
        ApplySpinBrake(_secondBody);
    }

    //力を加え、覚えておく回転方向を返す
    private float ApplyForces(
    ArticulationBody body,
    Vector3 gravity,
    float direction,
    float assistRate)
    {
        //重力を加える
        body.AddForce(gravity * body.mass, ForceMode.Force);

        if (_assistTorque <= 0f)
            return direction;

        float speed = body.angularVelocity.z;
        float absoluteSpeed = Mathf.Abs(speed);

        float speedLimit = Mathf.Max(1f, _assistBelowSpeed) * Mathf.Deg2Rad;

        //十分速いときは、補助しない
        if (absoluteSpeed >= speedLimit)
            return direction;

        //停止付近では補助を弱める。
        //回転方向が変わっても、力が急に反転しないようにする。
        const float smoothSpeed = 0.5f;

        float smoothDirection = speed / Mathf.Sqrt( speed * speed + smoothSpeed * smoothSpeed);

        //速くなるほど補助を弱める
        float strength = 1f - absoluteSpeed / speedLimit;

        float torque = smoothDirection * _assistTorque * strength * assistRate;

        body.AddTorque(
            Vector3.forward * torque,
            ForceMode.Force
        );

        return smoothDirection;
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

        //根元に近い方から順番に動かす
        _arm1.ApplyPhysicsPosition(point2);
        _arm2.ApplyPhysicsPosition(point3);
    }

    // 開始・停止時に記録をリセットする
    private void ResetSpinGuard()
    {
        Vector3 arm = _initialPoint2 - _fixedPoint1;

        _previousArmAngle =
            Mathf.Atan2(arm.y, arm.x) * Mathf.Rad2Deg;

        _turnDirection = 0f;
        _turnAngle = 0f;
        _reverseAngle = 0f;
        _reverseTime = 0f;

        _limitingSpin = false;
        _assistBlend = 1f;
        _brakeBlend = 0f;
    }

    private void UpdateSpinGuard()
    {
        Vector3 arm =
            _firstBody.transform.position - _fixedPoint1;

        float angle =
            Mathf.Atan2(arm.y, arm.x) * Mathf.Rad2Deg;

        // 359度から0度へ進んだ場合も、正しく差を求める
        float change = Mathf.DeltaAngle(_previousArmAngle, angle);
        _previousArmAngle = angle;

        float speed = change / Time.fixedDeltaTime;

        // 最初に動き始めた方向を記録する
        if (_turnDirection == 0f && Mathf.Abs(speed) >= 5f)
            _turnDirection = Mathf.Sign(speed);

        if (_turnDirection != 0f)
        {
            float forwardChange = change * _turnDirection;

            // 少し逆に戻った分は差し引く
            _turnAngle = Mathf.Max(0f, _turnAngle + forwardChange);

            // 逆方向へ、ある程度はっきり動いているか
            if (speed * _turnDirection < -5f)
            {
                _reverseAngle += Mathf.Abs(change);
                _reverseTime += Time.fixedDeltaTime;

                // 逆方向へ5度以上、0.2秒以上動いたら反転と判断
                if (_reverseAngle >= 5f && _reverseTime >= 0.2f)
                {
                    _turnDirection = Mathf.Sign(speed);
                    _turnAngle = _reverseAngle;

                    _reverseAngle = 0f;
                    _reverseTime = 0f;
                    _limitingSpin = false;
                }
            }
            else
            {
                _reverseAngle = 0f;
                _reverseTime = 0f;
            }

            if (_turnAngle >= _spinLimitAngle)
                _limitingSpin = true;
        }

        // 連続回転中は加速を止める。
        // 解除後は約0.5秒かけて普段の補助へ戻す。
        if (_limitingSpin)
        {
            _assistBlend = 0f;
        }
        else
        {
            _assistBlend = Mathf.MoveTowards(
                _assistBlend, 1f, Time.fixedDeltaTime * 2f);
        }

        // ブレーキは急に切り替えず、徐々に効かせる
        _brakeBlend = Mathf.MoveTowards(
            _brakeBlend,
            _limitingSpin ? 1f : 0f,
            Time.fixedDeltaTime * 2f
        );
    }

    private void ApplySpinBrake(ArticulationBody body)
    {
        if (_spinBrake <= 0f || _brakeBlend <= 0f)
            return;

        float torque =
            -body.angularVelocity.z * _spinBrake * _brakeBlend;

        body.AddTorque(
            Vector3.forward * torque,
            ForceMode.Force
        );
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

    //停止ボタンから呼ぶ
    public void EndSimulation()
    {
        if (!_isSimulating)
            return;

        _isSimulating = false;
        _hasPhysicsError = false;

        ResetSpinGuard();

        RemovePhysicsObjects();
        _bellMotion.ResetMotion();

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