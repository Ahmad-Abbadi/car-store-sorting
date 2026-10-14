using UnityEngine;

/// <summary> Receives a horizontal shove from an attack. </summary>
public interface IKnockbackReceiver
{
    /// <summary> Pushes the receiver along a direction by a distance. </summary>
    void ReceiveKnockback(Vector3 direction, float distance);
}
