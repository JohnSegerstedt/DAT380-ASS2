using UnityEngine;

public class Shaker : MonoBehaviour
{
    public float shakingSpeed;
    public Vector3 shakingAxis;
    public float shakingHalfPeriod;
    private short shakingDir = 1;
    private float nextDirChange = 0f;
    void Update()
    {
        if (Time.time > nextDirChange) {
            shakingDir *= -1;
            nextDirChange = Time.time + shakingHalfPeriod;
        }

        transform.position += shakingAxis * shakingDir * shakingSpeed * Time.deltaTime;
    }
}
