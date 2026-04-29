using UnityEngine;

public class MapUserAvatarMotion : MonoBehaviour
{
    [SerializeField] private float bobHeight = 0.08f;
    [SerializeField] private float bobSpeed = 2.2f;
    [SerializeField] private float sideStepDistance = 0.03f;
    [SerializeField] private float swayAngle = 9f;
    [SerializeField] private float swaySpeed = 1.45f;
    [SerializeField] private float leanAngle = 3.5f;
    [SerializeField] private float leanSpeed = 2.8f;

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private float phaseOffset;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;
        phaseOffset = Random.Range(0f, 10f);
    }

    public void Configure(Vector3 localPosition, Quaternion localRotation)
    {
        baseLocalPosition = localPosition;
        baseLocalRotation = localRotation;
        transform.localPosition = localPosition;
        transform.localRotation = localRotation;
    }

    private void Update()
    {
        float animationTime = Time.unscaledTime + phaseOffset;

        Vector3 nextPosition = baseLocalPosition;
        nextPosition.y += Mathf.Sin(animationTime * bobSpeed) * bobHeight;
        nextPosition.x += Mathf.Sin(animationTime * (bobSpeed * 0.55f)) * sideStepDistance;
        transform.localPosition = nextPosition;

        Quaternion animatedRotation = Quaternion.Euler(
            Mathf.Sin(animationTime * leanSpeed) * leanAngle,
            Mathf.Sin(animationTime * swaySpeed) * swayAngle,
            Mathf.Sin(animationTime * 1.15f) * 1.8f);

        transform.localRotation = baseLocalRotation * animatedRotation;
    }
}
