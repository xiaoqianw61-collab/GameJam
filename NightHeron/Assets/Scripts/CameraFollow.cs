using UnityEngine;

/// <summary>
/// 摄像机跟随玩家
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 1.5f, -10f);
    public Vector2 clampMin = new Vector2(-20, -20);
    public Vector2 clampMax = new Vector2(40, 20);

    void LateUpdate()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
            else return;
        }

        Vector3 desiredPos = target.position + offset;
        desiredPos.x = Mathf.Clamp(desiredPos.x, clampMin.x, clampMax.x);
        desiredPos.y = Mathf.Clamp(desiredPos.y, clampMin.y, clampMax.y);

        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed);
    }
}
