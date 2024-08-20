using System;
using System.Collections;
using NuiN.Movement;
using NuiN.NExtensions;
using UnityEngine;
using Random = UnityEngine.Random;

public class RandomMovementInput : MonoBehaviour, IMovementInput
{
    Vector3 _direction;
    Quaternion _rotation;
    bool _isRunning;
    
    void Start()
    {
        StartCoroutine(ChangeStatesRepeating());
    }

    public Action OnJump { get; set; }

    public Vector3 GetDirection()
    {
        return _direction;
    }

    Quaternion IMovementInput.GetRotation()
    {
        return _rotation;
    }

    public Quaternion GetCameraRotation()
    {
        return _rotation;
    }

    bool IMovementInput.ShouldJump()
    {
        return RandomUtils.BelowPercent(1f);
    }

    bool IMovementInput.IsRunning()
    {
        return _isRunning;
    }

    IEnumerator ChangeStatesRepeating()
    {
        while (true)
        {
            _direction = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            _rotation = Quaternion.LookRotation(_direction);
            _isRunning = Random.Range(0, 2) == 1;
            
            if(_isRunning) OnJump?.Invoke();
            
            yield return new WaitForSeconds(Random.Range(2f, 5f));
        }
    }
}
