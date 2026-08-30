using UnityEngine;

/// <summary>
/// A prop that turns on the spot, so the scene it lives in is obviously there. Used by the
/// additively loaded <c>Extra</c> scene in the multi-scene example.
/// </summary>
public class SpinningProp : MonoBehaviour
{
    [SerializeField]
    Vector3 _degreesPerSecond = new(0, 45, 0);

    void Update() => transform.Rotate(_degreesPerSecond * Time.deltaTime, Space.Self);
}
