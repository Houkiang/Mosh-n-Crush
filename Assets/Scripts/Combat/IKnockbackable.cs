using UnityEngine;

public interface IKnockbackable
{
    void TakeKnockback(Vector3 sourcePosition, float force, float stunDuration);
}
