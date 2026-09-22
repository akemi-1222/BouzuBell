using UnityEngine;

public class HammerHit : MonoBehaviour
{
    //命中時に追加する回転速度：度／秒
    private const float ExtraRotationSpeed = 30f;

    //命中補助で目指す回転速度の上限
    private const float BoostSpeedLimit = 300f;

    //鐘を押す強さ
    private const float BellPushScale = 0.5f;

    //鐘に加える力積の上限
    private const float MaxBellImpulse = 12f;

    // 命中補助の間隔：秒
    private const float BoostInterval = 0.8f;

    private float _nextBoostTime;

    private ArticulationBody _body;
    private ArticulationBody _parentBody;

    private BellHit _bell;
    private Rigidbody _bellBody;
    private SphereCollider _hitCollider;

    private Vector3 _beforePosition;
    private Vector3 _beforeVelocity;
    private Vector3 _beforeAngularVelocity;

    private float _targetSpeed;
    private bool _boostPending;

    public void Initialize(
        ArticulationBody body,
        ArticulationBody parentBody,
        BellHit bell)
    {
        _body = body;
        _parentBody = parentBody;
        _bell = bell;

        _bellBody = bell.GetComponent<Rigidbody>();
        _hitCollider = body.GetComponent<SphereCollider>();

        if (_bellBody == null || _hitCollider == null)
        {
            Debug.LogError(
                "鐘のRigidbodyとハンマーのSphereColliderを確認してください。",
                this
            );

            enabled = false;
            return;
        }

        SaveMotion();
    }

    private void FixedUpdate()
    {
        if (_body == null || _parentBody == null)
            return;

        if (_boostPending)
        {
            _boostPending = false;
            ApplyRotationBoost();
        }

        // 次の物理計算で命中したときに使う
        SaveMotion();
    }

    private void SaveMotion()
    {
        _beforePosition = _body.worldCenterOfMass;
        _beforeVelocity = _body.linearVelocity;
        _beforeAngularVelocity = _body.angularVelocity;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled || _bellBody == null)
            return;

        if (_bellBody.isKinematic)
            return;

        // 指定した鐘だけを対象にする
        BellHit hitBell = other.GetComponentInParent<BellHit>();

        if (hitBell != _bell)
            return;

        // ハンマーを鐘から押し出す方向を調べる
        Vector3 escapeDirection;
        float overlapDistance;

        bool overlaps = Physics.ComputePenetration(
            _hitCollider,
            _hitCollider.transform.position,
            _hitCollider.transform.rotation,
            other,
            other.transform.position,
            other.transform.rotation,
            out escapeDirection,
            out overlapDistance
        );

        if (!overlaps)
            return;

        // 鐘を押す方向は、その反対
        Vector3 hitDirection = -escapeDirection;
        hitDirection.z = 0f;

        if (hitDirection.sqrMagnitude < 0.0001f)
            return;

        hitDirection.Normalize();

        // 前の位置に近い、鐘の表面上の点を打撃位置にする
        Vector3 hitPoint = other.ClosestPoint(_beforePosition);
        hitPoint.z = _bellBody.worldCenterOfMass.z;

        // 回転も含めた、ハンマーの打撃位置での速度
        Vector3 hammerVelocity =
            _beforeVelocity +
            Vector3.Cross(
                _beforeAngularVelocity,
                hitPoint - _beforePosition
            );

        Vector3 bellVelocity =
            _bellBody.GetPointVelocity(hitPoint);

        Vector3 relativeVelocity = hammerVelocity - bellVelocity;
        relativeVelocity.z = 0f;

        // 鐘へ近づいている成分だけを使う
        float hitSpeed = Vector3.Dot(
            relativeVelocity,
            hitDirection
        );

        if (hitSpeed <= 0f)
            return;

        // ダメージ用の威力
        float power =
            0.5f * _body.mass * hitSpeed * hitSpeed;

        // 弱すぎる命中や連続命中は受け付けない
        if (!_bell.ReceiveHit(power))
            return;

        // 鐘を押す。中心から外れれば傾きも生まれる
        float impulse = Mathf.Min(
            _body.mass * hitSpeed * BellPushScale,
            MaxBellImpulse
        );

        _bellBody.AddForceAtPosition(
            hitDirection * impulse,
            hitPoint,
            ForceMode.Impulse
        );

        PrepareRotationBoost();
    }

    private void PrepareRotationBoost()
    {
        // ダメージとは別に、加速の連発を防ぐ
        if (Time.time < _nextBoostTime)
            return;

        float beforeSpeed = _beforeAngularVelocity.z;
        float speed = Mathf.Abs(beforeSpeed);

        float limit = BoostSpeedLimit * Mathf.Deg2Rad;

        // ほぼ停止している場合と、十分速い場合は補助しない
        if (speed < 0.05f || speed >= limit)
            return;

        // 上限に近いほど、追加する勢いを小さくする
        float strength = 1f - speed / limit;
        float extra = ExtraRotationSpeed * Mathf.Deg2Rad * strength;

        _targetSpeed = Mathf.Sign(beforeSpeed) * (speed + extra);

        _boostPending = true;
        _nextBoostTime = Time.time + BoostInterval;
    }

    private void ApplyRotationBoost()
    {
        float currentSpeed = _body.angularVelocity.z;
        float direction = Mathf.Sign(_targetSpeed);

        // すでに同じ方向へ十分速く回っている場合は維持
        if (currentSpeed * direction >= Mathf.Abs(_targetSpeed))
            return;

        // 親の回転を差し引き、関節の相対速度に変換する
        float relativeSpeed =
            _targetSpeed - _parentBody.angularVelocity.z;

        _body.jointVelocity =
            new ArticulationReducedSpace(relativeSpeed);
    }
}