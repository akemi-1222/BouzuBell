using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BellMotion : MonoBehaviour
{
    [Header("元の位置に戻る強さ")]
    [SerializeField, Min(0f)] private float _returnPower = 25f;

    [Header("移動の揺れを抑える強さ")]
    [SerializeField, Min(0f)] private float _moveDamping = 5f;

    [Header("元の角度に戻る強さ")]
    [SerializeField, Min(0f)] private float _rotationPower = 15f;

    [Header("回転の揺れを抑える強さ")]
    [SerializeField, Min(0f)] private float _rotationDamping = 4f;

    private Rigidbody _body;

    private Vector3 _initialPosition;
    private Quaternion _initialRotation;

    private bool _isPlaying;

    private void Awake()
    {
        _body = GetComponent<Rigidbody>();

        _initialPosition = _body.position;
        _initialRotation = _body.rotation;

        //重力で落下させず、元の位置へ戻る力で支える
        _body.useGravity = false;

        //抵抗は、このスクリプトで計算する
        _body.linearDamping = 0f;
        _body.angularDamping = 0f;

        //上下左右の移動と、画面内での回転を許可する
        _body.constraints =
            RigidbodyConstraints.FreezePositionZ |
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationY;

        ResetMotion();
    }

    private void FixedUpdate()
    {
        if (!_isPlaying || _body.isKinematic)
            return;

        ReturnPosition();
        ReturnRotation();
    }

    //元の位置に戻す
    private void ReturnPosition()
    {
        Vector3 distance = _initialPosition - _body.position;
        distance.z = 0f;

        Vector3 velocity = _body.linearVelocity;
        velocity.z = 0f;

        //離れるほど強く戻し、移動速度に応じて揺れを抑える
        Vector3 acceleration = distance * _returnPower - velocity * _moveDamping;

        _body.AddForce(acceleration, ForceMode.Acceleration);
    }

    //元の傾きに戻す
    private void ReturnRotation()
    {
        float currentAngle = _body.rotation.eulerAngles.z;
        float initialAngle = _initialRotation.eulerAngles.z;

        //元の角度まで、どちらへ何度回すか
        float angleDifference = Mathf.DeltaAngle(currentAngle, initialAngle);

        float angleInRadians = angleDifference * Mathf.Deg2Rad;

        float acceleration = angleInRadians * _rotationPower - _body.angularVelocity.z * _rotationDamping;

        _body.AddTorque( Vector3.forward * acceleration, ForceMode.Acceleration);
    }

    //ゲーム内のプレイボタンから呼ぶ
    public void BeginMotion()
    {
        ResetMotion();

        _body.isKinematic = false;
        _isPlaying = true;

        _body.WakeUp();
    }

    //停止時に元の状態へ戻す
    public void ResetMotion()
    {
        _isPlaying = false;

        if (!_body.isKinematic)
        {
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
        }

        _body.isKinematic = true;
        _body.position = _initialPosition;
        _body.rotation = _initialRotation;
    }
}