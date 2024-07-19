using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine;

public class InteractDoor : InteractCalled
{
    [SerializeField] private bool _isClosed;

    public enum Aixs {X,Y,Z,NX,NY,NZ}
    public Aixs _axisEnum;
    private Vector3 _axis;

    public Aixs _directionEnum;
    private Vector3 _direction;

    public Vector2 _limit;
    public float _doorAngle;

    private Quaternion _startWorldRotation;
    public override void Awake()
    {
        Type = TypeInteract.Door;

        switch (_axisEnum)
        {
            case Aixs.X: _axis = transform.right; break;
            case Aixs.Y: _axis = transform.up; break;
            case Aixs.Z: _axis = transform.forward; break;
            case Aixs.NX: _axis = transform.right; break;
            case Aixs.NY: _axis = transform.up; break;
            case Aixs.NZ: _axis = transform.forward; break;
        }

        switch (_directionEnum)
        {
            case Aixs.X: _direction = transform.right; break;
            case Aixs.Y: _direction = transform.up; break;
            case Aixs.Z: _direction = transform.forward; break;
            case Aixs.NX: _direction = -transform.right; break;
            case Aixs.NY: _direction = -transform.up; break;
            case Aixs.NZ: _direction = -transform.forward; break;
        }

        _startWorldRotation = transform.localRotation;
        transform.localRotation = _startWorldRotation * Quaternion.Euler(_axis * _doorAngle);
    }

    public override void Interact()
    {
        StartCoroutine(InteractCoroutine());
    }

    private IEnumerator InteractCoroutine()
    {
        Camera _Camera = InteractManager.Main._cMain;

        float _dir = Mathf.Sign(Vector3.SignedAngle(_startWorldRotation * _direction, InputManager.Main.transform.position - this.transform.position, _axis));

        Vector3 _startPoint = transform.worldToLocalMatrix.MultiplyPoint3x4(InteractManager.Main._currentHit.point);

        InteractManager.Main.EnableFakeCursor();
        while (true)
        {
            _doorAngle += Mouse.current.delta.ReadValue().x * _dir / 10.0f;

            if(!_isClosed)
                _doorAngle = Mathf.Clamp(_doorAngle, _limit.x, _limit.y);
            else
                _doorAngle = Mathf.Clamp(_doorAngle, (_startWorldRotation * _axis).magnitude - 0.25f, (_startWorldRotation * _axis).magnitude + 0.25f);

            transform.localRotation = _startWorldRotation * Quaternion.Euler(_axis * _doorAngle);

            InteractManager.Main.SetFakeCursor(_Camera.WorldToScreenPoint(transform.localToWorldMatrix.MultiplyPoint3x4(_startPoint)));

            if (Mouse.current.leftButton.wasReleasedThisFrame || !InteractManager.Main.EnableHand)
            {
                Mouse.current.WarpCursorPosition(_Camera.WorldToScreenPoint(transform.localToWorldMatrix.MultiplyPoint3x4(_startPoint)));
                break;
            }   

            yield return null;
        }
        InteractManager.Main.InteractEnd();
    }
}


