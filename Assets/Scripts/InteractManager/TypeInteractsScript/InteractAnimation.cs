using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;

public class InteractAnimation : InteractCalled
{
    [SerializeField] private Animation _animation;
    [SerializeField] private AnimationClip _clip;
    [SerializeField] private float _speed = 1.0f;
    [Space]
    [SerializeField] private bool _isReturn;
    [SerializeField] private float _returnSpeed = 1.0f;
    [Space]
    [SerializeField] private AnimationCurve _curveRotationX;
    [SerializeField] private AnimationCurve _curveRotationY;
    [SerializeField] private AnimationCurve _curveRotationZ;
    [SerializeField] private AnimationCurve _curveRotationW;
    [Space]
    [SerializeField] private AnimationCurve _curvePositionX;
    [SerializeField] private AnimationCurve _curvePositionY;
    [SerializeField] private AnimationCurve _curvePositionZ;

    private Quaternion GetRotation(float _time)
    {
        return new Quaternion(_curveRotationX.Evaluate(_time), _curveRotationY.Evaluate(_time), _curveRotationZ.Evaluate(_time), _curveRotationW.Evaluate(_time));
    }
    private Vector3 GetPosition(float _time)
    {
        return new Vector3(_curvePositionX.Evaluate(_time), _curvePositionY.Evaluate(_time), _curvePositionZ.Evaluate(_time));
    }


#if UNITY_EDITOR
    [ContextMenu("Save Animation Curves", false, 10)]
    public void SaveAnimationCurves()
    {
        _curveRotationX = null;
        _curveRotationY = null;
        _curveRotationZ = null;
        _curveRotationW = null;

        _curvePositionX = null;
        _curvePositionY = null;
        _curvePositionZ = null;

        foreach (EditorCurveBinding i in AnimationUtility.GetCurveBindings(_clip))
        {
            if (i.path == this.gameObject.name)
            {
                if (i.propertyName == "m_LocalRotation.x")
                {
                    _curveRotationX = AnimationUtility.GetEditorCurve(_clip, i);
                    Debug.Log("Added " + i.path + "  " + i.propertyName);
                }

                if (i.propertyName == "m_LocalRotation.y")
                {
                    _curveRotationY = AnimationUtility.GetEditorCurve(_clip, i);
                    Debug.Log("Added " + i.path + "  " + i.propertyName);
                }

                if (i.propertyName == "m_LocalRotation.z")
                {
                    _curveRotationZ = AnimationUtility.GetEditorCurve(_clip, i);
                    Debug.Log("Added " + i.path + "  " + i.propertyName);
                }

                if (i.propertyName == "m_LocalRotation.w")
                {
                    _curveRotationW = AnimationUtility.GetEditorCurve(_clip, i);
                    Debug.Log("Added " + i.path + "  " + i.propertyName);
                }

                if (i.propertyName == "m_LocalPosition.x")
                {
                    _curvePositionX = AnimationUtility.GetEditorCurve(_clip, i);
                    Debug.Log("Added " + i.path + "  " + i.propertyName);
                }

                if (i.propertyName == "m_LocalPosition.y")
                {
                    _curvePositionY = AnimationUtility.GetEditorCurve(_clip, i);
                    Debug.Log("Added " + i.path + "  " + i.propertyName);
                }

                if (i.propertyName == "m_LocalPosition.z")
                { 
                    _curvePositionZ = AnimationUtility.GetEditorCurve(_clip, i);
                    Debug.Log("Added " + i.path + "  " + i.propertyName);
                }   
            }

        }
    }
#endif

    private bool _breakStatus;
    private void BreakInteract()
    {
        _breakStatus = true;
    }

    public override void Awake()
    {
        Type = TypeInteract.Animation;
        
    }

    public override void Interact()
    {
        StartCoroutine(InteractCoroutine());
    }

    private IEnumerator InteractCoroutine()
    {
        _breakStatus = false;

        Camera _camera = InteractManager.Main._cMain;

        float _time = _animation[_clip.name].time;
        float _deltaTime = 0.1f;

        Vector3 _startPosition = GetPosition(_time);
        Quaternion _startRotation = Quaternion.Inverse(GetRotation(_time));

        Vector3 _startPoint = this.transform.worldToLocalMatrix.MultiplyPoint3x4(InteractManager.Main._currentHit.point);
        Matrix4x4 _toworld = this.transform.localToWorldMatrix;

        _animation.Play(_clip.name);

        InteractManager.Main.EnableFakeCursor();
        while (true)
        {
            Vector3 _position1 = _toworld.MultiplyPoint3x4(Matrix4x4.TRS(GetPosition(_time) - _startPosition, GetRotation(_time) * _startRotation, Vector3.one).MultiplyPoint3x4(_startPoint));
            Vector3 _position2 = _toworld.MultiplyPoint3x4(Matrix4x4.TRS(GetPosition(_time + _deltaTime) - _startPosition, GetRotation(_time + _deltaTime) * _startRotation, Vector3.one).MultiplyPoint3x4(_startPoint));
            Vector3 _direction = _camera.WorldToScreenPoint(_position2) - _camera.WorldToScreenPoint(_position1);

            InteractManager.Main.SetFakeCursor(_camera.WorldToScreenPoint(_position1));
            
            float _angle = Vector3.Angle(_direction, Mouse.current.delta.ReadValue());
            _angle = (90.0f - _angle) / 45.0f;

            _time += Time.deltaTime * _angle * Mathf.Clamp01(Mouse.current.delta.ReadValue().magnitude) * _speed;
            _time = Mathf.Clamp(_time, 0.001f, _clip.length - 0.001f);

            _animation[_clip.name].speed = Mathf.Sign(_angle) * 0.00001f;
            _animation[_clip.name].time = _time;

            if (Mouse.current.leftButton.wasReleasedThisFrame || !InteractManager.Main.EnableHand || _breakStatus)
            {
                Mouse.current.WarpCursorPosition(_camera.WorldToScreenPoint(_position1));
                break;
            }    

            yield return null;
        }

        if (_isReturn)
        {
            _animation[_clip.name].speed = -_returnSpeed;
            _animation.Blend(_clip.name);
        }

        InteractManager.Main.InteractEnd();
    }


    
}
