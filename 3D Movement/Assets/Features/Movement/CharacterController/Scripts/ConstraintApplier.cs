using NuiN.Movement;
using UnityEngine;

public class ConstraintApplier : MonoBehaviour
{
    [SerializeField] MovementController mover;

    [SerializeField] float constraintDuration = 5f;
    [SerializeField] bool allowMove = false;
    [SerializeField] bool allowRotate = false;
    
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            mover.ApplyConstraint(constraintDuration, new MovementConstraint(allowMove, allowRotate));
        }
    }
}
