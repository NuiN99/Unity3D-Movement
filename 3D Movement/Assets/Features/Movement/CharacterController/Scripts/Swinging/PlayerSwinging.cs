using UnityEngine;

public class PlayerSwinging : MonoBehaviour
{
    Rigidbody _anchor;
    bool _addedRigidbody;
    bool _attached;

    [SerializeField] float maxAttachDistance = 25f;
    
    [SerializeField] KeyCode activateKey = KeyCode.Mouse1;
    [SerializeField] LayerMask attachableLayers;

    [SerializeField] ConfigurableJoint joint;

    [SerializeField] LineRenderer ropeLineRenderer;

    Vector3 ConnectedAnchorWorld => _anchor.transform.TransformPoint(joint.connectedAnchor);
    
    void Update()
    {
        if (Input.GetKeyDown(activateKey)) Activate();
        else if(Input.GetKeyUp(activateKey)) Detach();

        if (!_attached)
        {
            joint.connectedAnchor = transform.position;
        }
    }

    void LateUpdate()
    {
        if (!_attached)
        {
            ropeLineRenderer.enabled = false;
            return;
        }
        
        ropeLineRenderer.enabled = true;
        ropeLineRenderer.positionCount = 2;
        ropeLineRenderer.SetPosition(0, transform.position);
        ropeLineRenderer.SetPosition(1, ConnectedAnchorWorld);
    }

    void Activate()
    {
        if (!Physics.Raycast(MainCamera.Cam.transform.position, MainCamera.Cam.transform.forward, out RaycastHit hit, maxAttachDistance, attachableLayers)) return;
        if (!hit.collider.TryGetComponent(out _anchor))
        {
            _anchor = hit.collider.gameObject.AddComponent<Rigidbody>();
            _anchor.isKinematic = true;
            _addedRigidbody = true;
        }
        else
        {
            _addedRigidbody = false;
        }

        SoftJointLimit limit = joint.linearLimit;
        limit.limit = Vector3.Distance(transform.position, hit.point);
        joint.linearLimit = limit;
            
        joint.connectedBody = _anchor;
        joint.connectedAnchor = _anchor.gameObject.transform.InverseTransformPoint(hit.point);

        _attached = true;
    }

    void Detach()
    {
        joint.connectedBody = null;
        if(_anchor != null && _addedRigidbody) Destroy(_anchor);

        _attached = false;
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        
    }
}
