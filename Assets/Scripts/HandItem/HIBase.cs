using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MyEffects;

public class HIBase : MonoBehaviour
{
    #region ItemLink
    public ItemBase ItemBaseLink { get; set; }
    #endregion

    #region Animation
    [System.Serializable]protected struct Point
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    [SerializeField] protected Point _basePoint;

    protected Point CurrentPoint { get { return new Point { position = this.transform.localPosition, rotation = this.transform.localRotation }; }}
    private void ItemTakeAnimation()
    {
        Point _from = new Point()
        {
            position = _basePoint.position + Vector3.down * 0.1f + Vector3.left * 0.1f,
            rotation = _basePoint.rotation * Quaternion.Euler(45, -90, 0)
        };

       AnimateDisplacement(this.transform, _from, _basePoint, 0.5f);
    }

    private IEnumerator AnimationDisplacementCoroutine(Transform _transform, Point _from, Point _to, float _time)
    {
        IsAnimated = true;
        float _t = 0;
        while (_t < _time)
        {
            _t += Time.deltaTime;

            _transform.localRotation = Quaternion.Lerp(_from.rotation, _to.rotation, _t / _time);
            _transform.localPosition = Vector3.Lerp(_from.position, _to.position, _t / _time);

            yield return null;
        }

        IsAnimated = false;
    }
    private IEnumerator AnimationDisplacementCoroutine(Transform _transform, Point _from, Point _to, float _time, Action _endFunc)
    {
        IsAnimated = true;

        float _t = 0;
        while (_t < _time)
        {
            _t += Time.deltaTime;

            _transform.localRotation = Quaternion.Lerp(_from.rotation, _to.rotation, _t / _time);
            _transform.localPosition = Vector3.Lerp(_from.position, _to.position, _t / _time);

            yield return null;
        }

        IsAnimated = false;
        _endFunc.Invoke();
    }
    private IEnumerator AnimationDisplacementCoroutine(Transform _transform, Point _to, float _time)
    {
        IsAnimated = true;

        float _speedRotation = Quaternion.Angle(_transform.localRotation, _to.rotation) / _time;
        float _speedPosition = Vector3.Distance(_transform.localPosition, _to.position) / _time;
        
        while (_transform.localPosition != _to.position || _transform.localRotation != _to.rotation)
        {
            _transform.localRotation = Quaternion.RotateTowards(_transform.localRotation, _to.rotation, _speedRotation * Time.deltaTime);
            _transform.localPosition = Vector3.MoveTowards(_transform.localPosition, _to.position, _speedPosition * Time.deltaTime);

            yield return null;
        }

        IsAnimated = false;
    }

    protected bool IsAnimated { get; private set; }
    private IEnumerator _currentAnimation;
    protected void AnimateDisplacement(Transform _transform, Point _from, Point _to, float _time)
    {
        if (_currentAnimation != null)
            StopCoroutine(_currentAnimation);
        if (this.gameObject.activeSelf)
        {
            _currentAnimation = AnimationDisplacementCoroutine(_transform, _from, _to, _time);
            StartCoroutine(_currentAnimation);
        }
    }
    protected void AnimateDisplacement(Transform _transform, Point _from, Point _to, float _time, Action _endFunc)
    {
        if (_currentAnimation != null)
            StopCoroutine(_currentAnimation);
        if (this.gameObject.activeSelf)
        {
            _currentAnimation = AnimationDisplacementCoroutine(_transform, _from, _to, _time, _endFunc);
            StartCoroutine(_currentAnimation);
        }
    }
    protected void AnimateDisplacement(Transform _transform, Point _to, float _time)
    {
        if (_currentAnimation != null)
            StopCoroutine(_currentAnimation);
        if (this.gameObject.activeSelf)
        {
            _currentAnimation = AnimationDisplacementCoroutine(_transform, _to, _time);
            StartCoroutine(_currentAnimation);
        }
    }

    private List<Vector3> _positionOffsetList = new List<Vector3>();
    private List<Quaternion> _rotationOffsetList = new List<Quaternion>();
    protected void AddItemOffset(Vector3 _position)
    {
        _positionOffsetList.Add(_position);
    }
    protected void AddItemOffset(Quaternion _position)
    {
        _rotationOffsetList.Add(_position);
    }

    protected void SetOffset()
    {
        Vector3 _a = _basePoint.position;
        foreach (Vector3 i in _positionOffsetList)
            _a += i;
        this.transform.localPosition = _a;

        Quaternion _b = _basePoint.rotation;
        foreach (Quaternion i in _rotationOffsetList)
            _b *= i;
        this.transform.localRotation = _b;

        _positionOffsetList.Clear();
        _rotationOffsetList.Clear();
    }
    #endregion

    #region Audio
    [Header("Audio")]
    [SerializeField] private int _takeAudioIndex = -1;
    [SerializeField] private List<AudioClip> _audioList;

    protected void AudioPlay(int _audioIndex)
    {
        if (_audioList.Count < _audioIndex && _audioIndex < 0)
            return;

        EffectsManager.PlaySound(_audioList[_audioIndex], this.transform, this.transform.position);
    }
    #endregion

    #region Sway
    [Header("Sway")]
    [SerializeField] private float _swayMaxAngle = 60.0f;
    [SerializeField] private float _swaySpeedMultiply = 50.0f;
    private Vector3 _saveOffsetRotation;
    private Vector3 _lastCameraPos;
    [Space]
    [SerializeField] private float _shakeRadius = 0.002f;
    [SerializeField] private float _shakeSpeedMultiply = 10;
    [SerializeField] private Vector2 _shakeSpeedLimit = new Vector2(0.001f, 0.01f);
    private float _shakeSpeed;
    private Vector3 _shakePoint;
    private Vector3 _shakeOffset;
    [Space]
    [SerializeField] private Vector2 _rotateMoveSpeedMultiply = new Vector2(20, -120);
    [SerializeField] private float _maxPositionMoveOffset = 0.005f;
    [SerializeField] private float _positionMoveSpeedMultiply = 0.01f;
    private Vector3 _saveMoveOffset;

    protected void ClearSway()
    {
        _saveOffsetRotation = Vector3.zero;
        _lastCameraPos = transform.parent.position;

        _shakePoint = Vector3.zero;
        _shakeOffset = Vector3.zero;

        _saveMoveOffset = Vector3.zero;

        _positionOffsetList.Clear();
        _rotationOffsetList.Clear();
    }
    private void SwayUpdate()
    {
        Vector3 _deltaStepOffset = Quaternion.FromToRotation(InputManager.Main.transform.forward, Vector3.forward) * -InputManager.DeltaMove;
        _deltaStepOffset.y = (_lastCameraPos + -transform.parent.position).y;
        _lastCameraPos = transform.parent.position;

        // Random position sway
        if (_shakeOffset == _shakePoint)
        {
            _shakeSpeed = UnityEngine.Random.Range(_shakeSpeedLimit.x, _shakeSpeedLimit.y);

            _shakeOffset = _shakePoint;

            float x = UnityEngine.Random.Range(-_shakeRadius, _shakeRadius);
            float y = UnityEngine.Random.Range(-_shakeRadius, _shakeRadius);
            _shakePoint = new Vector3(x, y, 0.0f);
        }

        _shakeOffset = Vector3.MoveTowards(_shakeOffset, _shakePoint, _shakeSpeed * Time.deltaTime);
        AddItemOffset(_shakeOffset);

        // Rotation sway by camera move
        _saveOffsetRotation = Vector3.Lerp(_saveOffsetRotation, _basePoint.rotation.eulerAngles, _swaySpeedMultiply * Time.deltaTime);

        _saveOffsetRotation += -(Vector3)InputManager.DeltaCameraRotation + _shakeOffset * _shakeSpeedMultiply + Vector3.right * _deltaStepOffset.y * _rotateMoveSpeedMultiply.y;
        _saveOffsetRotation.z = _saveOffsetRotation.y;
        _saveOffsetRotation += Vector3.up * _deltaStepOffset.x * _rotateMoveSpeedMultiply.x;
        _saveOffsetRotation.x = Mathf.Clamp(_saveOffsetRotation.x, -_swayMaxAngle, _swayMaxAngle);
        _saveOffsetRotation.y = Mathf.Clamp(_saveOffsetRotation.y, -_swayMaxAngle, _swayMaxAngle);


        AddItemOffset(Quaternion.Euler(_saveOffsetRotation));

        //Sway position by camera and player move
        _saveMoveOffset = Vector3.MoveTowards(_saveMoveOffset, Vector3.zero, _positionMoveSpeedMultiply * Time.deltaTime);
        _saveMoveOffset += _deltaStepOffset * _positionMoveSpeedMultiply * 2;
        _saveMoveOffset.x = Mathf.Clamp(_saveMoveOffset.x, -_maxPositionMoveOffset, _maxPositionMoveOffset);
        _saveMoveOffset.z = Mathf.Clamp(_saveMoveOffset.z, -_maxPositionMoveOffset, _maxPositionMoveOffset);
        _saveMoveOffset.y = 0;
        AddItemOffset(_saveMoveOffset);
    }
    #endregion

    #region Collision
    [Header("Collider")]
    [SerializeField] LayerMask _layerWall;

    [SerializeField] private Vector3 _centerCollision;
    [SerializeField] private Vector3 _sizeCollision;

    [SerializeField] private float _totalremoveSpeed = 20.0f;
    [Space]
    [SerializeField] [Range(0, 100)] private float _removePercent = 60.0f;
    [SerializeField] private float _removeSpeed = 1.0f;
    [SerializeField] private Vector3 _removePosition = new Vector3(0.098f, -0.145f, 0.078f);
    [SerializeField] private Vector3 _removeRotation = new Vector3(-32.0f, -65.0f, 45.0f);
    [Space]
    [SerializeField] [Range(0, 100)] private float _removePercentAim = 90.0f;
    [SerializeField] private float _removeSpeedAim = 1.0f;
    [SerializeField] private Vector3 _removePositionAim = new Vector3(0.098f, -0.145f, 0.078f);
    [SerializeField] private Vector3 _removeRotationAim = new Vector3(-32.0f, -65.0f, 45.0f);


    private Vector3 _saveBackOffset;
    private Quaternion _saveBackRotation = Quaternion.identity;

    private Vector3 _deltaBackOffset;
    private Quaternion _deltaBackRotation;

    private void Collision()
    {
        Vector3 _centerInWorld = transform.position + transform.rotation * Quaternion.Inverse(_deltaBackRotation) * (_centerCollision + -_deltaBackOffset);
        Vector3 _dir = transform.rotation * Vector3.forward * _sizeCollision.z * 2;

        RaycastHit _hit1, _hit2, _hit3, _hit4;
        bool _bool1 = Physics.Raycast(_centerInWorld + transform.rotation * _sizeCollision + -_dir, _dir, out _hit1, _sizeCollision.z * 2f, _layerWall),
             _bool2 = Physics.Raycast(_centerInWorld + transform.rotation * Quaternion.Euler(180, 0, 0) * _sizeCollision, _dir, out _hit2, _sizeCollision.z * 2f, _layerWall),
             _bool3 = Physics.Raycast(_centerInWorld + transform.rotation * Quaternion.Euler(0, 180, 0) * _sizeCollision, _dir, out _hit3, _sizeCollision.z * 2f, _layerWall),
             _bool4 = Physics.Raycast(_centerInWorld + transform.rotation * Quaternion.Euler(180, 180, 0) * _sizeCollision + -_dir, _dir, out _hit4, _sizeCollision.z * 2f, _layerWall);

        _saveBackOffset = Vector3.back * (_sizeCollision.z * 2 - Mathf.Min(new float[] { _hit1.distance + Convert.ToInt32(!_bool1) * _sizeCollision.z * 2,
                                                                                         _hit2.distance + Convert.ToInt32(!_bool2) * _sizeCollision.z * 2,
                                                                                         _hit3.distance + Convert.ToInt32(!_bool3) * _sizeCollision.z * 2,
                                                                                         _hit4.distance + Convert.ToInt32(!_bool4) * _sizeCollision.z * 2}));


        if (IsAiming == true && 1.0f >= (2 * _sizeCollision.z + _saveBackOffset.z) / ((2 * _sizeCollision.z * _removePercentAim) / 100.0f))
        {
            _saveBackRotation = Quaternion.Lerp(Quaternion.Euler(_removeRotationAim), Quaternion.identity, ((2 * _sizeCollision.z + _saveBackOffset.z) / ((2 * _sizeCollision.z * _removePercentAim) / 100.0f) * _removeSpeedAim));
            _saveBackOffset = _saveBackOffset + Vector3.Lerp(_removePositionAim, Vector3.zero, ((2 * _sizeCollision.z + _saveBackOffset.z) / ((2 * _sizeCollision.z * _removePercentAim) / 100.0f)) * _removeSpeedAim);
        }
        else
        {
            _saveBackRotation = Quaternion.Lerp(Quaternion.Euler(_removeRotation), Quaternion.identity, ((2 * _sizeCollision.z + _saveBackOffset.z) / ((2 * _sizeCollision.z * _removePercent) / 100.0f)) * _removeSpeed);
            _saveBackOffset = _saveBackOffset + Vector3.Lerp(_removePosition, Vector3.zero, ((2 * _sizeCollision.z + _saveBackOffset.z) / ((2 * _sizeCollision.z * _removePercent) / 100.0f)) * _removeSpeed);
        }


        AddItemOffset(_deltaBackOffset = Vector3.Lerp(_deltaBackOffset, _saveBackOffset, Time.deltaTime * _totalremoveSpeed));
        AddItemOffset(_deltaBackRotation = Quaternion.Lerp(_deltaBackRotation, _saveBackRotation, Time.deltaTime * _totalremoveSpeed));

    }
    #endregion

    #region Aim
    [Header("Aim")]
    [SerializeField] private Vector3 _aimPosition = new Vector3(0.0f, -0.02f, 0.13f);
    [SerializeField] private Vector3 _aimRotation = Vector3.zero;

    private Vector3 _saveAimPosition;
    
    public bool IsAiming { get; private set; }
    private void Aim()
    {
        if (IsAiming = Mouse.current.rightButton.isPressed)
           _saveAimPosition = Vector3.MoveTowards(_saveAimPosition, _aimPosition - _basePoint.position , Time.deltaTime);
        else
            _saveAimPosition = Vector3.MoveTowards(_saveAimPosition, Vector3.zero, Time.deltaTime);

        AddItemOffset(_saveAimPosition);
    }
    #endregion

    protected virtual void Awake()
    {
        this.transform.localPosition = _basePoint.position;
        this.transform.localRotation = _basePoint.rotation;
        
       // BasePoint = new Point() { position = transform.localPosition, rotation = transform.localRotation };
    }

    protected virtual void OnDestroy()
    {
        StopAllCoroutines();
    }

    protected virtual void Update()
    {
        if (!IsAnimated)
        {
            SetOffset();

            SwayUpdate();
            Aim();
            Collision();
        }
    }

    protected virtual void OnEnable()
    {
        ItemTakeAnimation();

        if(_takeAudioIndex > -1 && _takeAudioIndex < _audioList.Count)
            EffectsManager.PlaySound(_audioList[_takeAudioIndex], this.transform, this.transform.position);
    }

    protected virtual void OnDisable()
    {
        StopAllCoroutines();
    }

    protected void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);

        Vector3 _centerInWorld = transform.position + transform.rotation * _centerCollision;
        Vector3 _dir = transform.rotation * Vector3.forward * _sizeCollision.z * 2;

        Gizmos.DrawRay(_centerInWorld + transform.rotation * _sizeCollision + -_dir, _dir);
        Gizmos.DrawRay(_centerInWorld + transform.rotation * Quaternion.Euler(180, 0, 0) * _sizeCollision, _dir);
        Gizmos.DrawRay(_centerInWorld + transform.rotation * Quaternion.Euler(180, 180, 0) * _sizeCollision + -_dir, _dir);
        Gizmos.DrawRay(_centerInWorld + transform.rotation * Quaternion.Euler(0, 180, 0) * _sizeCollision, _dir);

        Gizmos.color = new Color(0, 1, 0, 0.3f); ;

        Vector3 _sizeRemove = _sizeCollision * 2;
        _sizeRemove.z = (_sizeCollision.z * 2 * _removePercent) / 100.0f;
        Gizmos.DrawWireCube(_centerInWorld + transform.rotation * Vector3.back * (_sizeCollision.z - _sizeRemove.z / 2), _sizeRemove);

        Gizmos.color = new Color(1, 1, 0,0.3f);

        Vector3 _sizeRemoveAim = _sizeCollision * 2;
        _sizeRemoveAim.z = (_sizeCollision.z * 2 * _removePercentAim) / 100.0f;
        Gizmos.DrawWireCube(_centerInWorld + transform.rotation * Vector3.back * (_sizeCollision.z - _sizeRemoveAim.z / 2), _sizeRemoveAim);
    }
}
