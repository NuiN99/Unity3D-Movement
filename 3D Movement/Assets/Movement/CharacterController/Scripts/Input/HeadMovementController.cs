using NuiN.Movement;
using UnityEngine;

public class HeadMovementController : MonoBehaviour
{
    [SerializeField] Transform head;

    IMovementInput _input;

    void Awake()
    {
        _input = GetComponent<IMovementInput>();
    }

    void Update()
    {
        head.localRotation = _input.GetHeadRotation();
    }
}