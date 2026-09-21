using UnityEngine;

public class HammerHit : MonoBehaviour
{
    private ArticulationBody _body;
    private BellHit _bell;

    public void Initialize(
        ArticulationBody body,
        BellHit bell)
    {
        _body = body;
        _bell = bell;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_body == null || _bell == null)
            return;

        // 指定した鐘に当たった場合だけ処理する
        BellHit hitBell =
            collision.collider.GetComponentInParent<BellHit>();

        if (hitBell != _bell)
            return;

        // 衝突を解決するために働いた力積
        float impact = collision.impulse.magnitude;

        // 弱すぎる衝突や連続命中は補助もしない
        if (!_bell.ReceiveHit(impact))
            return;

        if (collision.contactCount == 0)
            return;

        // 鐘の表面から離れる方向
        Vector3 direction = collision.GetContact(0).normal;

        // 画面の奥や手前には力を加えない
        direction.z = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        direction.Normalize();

        // 衝突の力積の10％を追加
        // 強い衝突でも、追加する力には上限を付ける
        float boost = Mathf.Min(
            impact * 0.1f,
            _body.mass * 0.5f
        );

        _body.AddForce(
            direction * boost,
            ForceMode.Impulse
        );
    }
}