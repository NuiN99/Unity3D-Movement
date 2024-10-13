using System.Collections;
using System.Collections.Generic;
using NuiN.NExtensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MovementDebugHUD : MonoBehaviour
{
    [SerializeField] Rigidbody rb;
    [SerializeField] CollisionEventDispatcher playerColliison;
    
    [SerializeField] TMP_Text text;

    float _jumpStartHeight;
    float _jumpEndHeight;
    bool _jumping;

    void OnEnable() => playerColliison.CollisionEnter += CollidedWithGround;
    void OnDisable() => playerColliison.CollisionEnter -= CollidedWithGround;

    void FixedUpdate()
    {
        string fullText = "";

        fullText += "Speed XZ: " + rb.velocity.With(y: 0).magnitude.ToString("0.00") + "\n";
        fullText += "Speed Y: " + rb.velocity.y.ToString("0.00") + "\n";

        string jumpHeightString = (_jumpEndHeight - _jumpStartHeight).ToString("0:00");

        float curHeight = rb.position.y;
        
        if (!_jumping && rb.velocity.y > 0)
        {
            if (rb.velocity.y > 0.1f)
            {
                jumpHeightString = "0.00";
            }
            
            _jumpStartHeight = curHeight;
            _jumpEndHeight = curHeight;
            _jumping = true;
        }
        
        if (_jumping && curHeight > _jumpEndHeight)
        {
            _jumpEndHeight = curHeight;
            
            float height = _jumpEndHeight - _jumpStartHeight;

            if (height > 0.1f)
            {
                jumpHeightString = height.ToString("0.00");
            }
        }

        fullText += "Jump Y: " + jumpHeightString + "\n";
        
        text.SetText(fullText);
    }

    void CollidedWithGround(Collision other)
    {
        _jumpStartHeight = float.MinValue;
        _jumpEndHeight = float.MinValue;
        _jumping = false;
    }
}