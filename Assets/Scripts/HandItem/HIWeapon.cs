using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class HIWeapon : HIBase
{
    #region Ammo
    [Header("Ammo")]
    [SerializeField] protected Transform _shootPosition;
    [SerializeField] protected float _damageModifire = 1.0f;
    [SerializeField] protected ItemDataAmmo _shell;
    [SerializeField] protected List<ItemDataAmmo> _ammoList;
    [SerializeField] protected string _muzzleEffect;
    #endregion

    #region Shoot
    [Header("Recoil")]
    [SerializeField] private Vector3 _positionRecoil;
    [SerializeField] private Quaternion _rotateRecoil;
    [SerializeField] private Vector3 _cameraRotationRecoil;
    
    [SerializeField] private float _timeRecoil = 20.0f;

    protected virtual void PullTrigger()
    {
        Debug.Log("PullTrigger base");
    }

    protected void Shoot(ItemDataAmmo _ammo)
    {
        MyEffects.EffectsManager.ShootBullet(_shootPosition.position, _shootPosition.forward, _ammo.Damage * _damageModifire, _ammo.ArmorPenetration);

        InputManager.Main.AddCameraRotation(_cameraRotationRecoil);
        StartCoroutine(Recoil());
    }

    protected IEnumerator Recoil()
    {
        Quaternion _rotatetemp = _rotateRecoil;
        float _tempTime = _timeRecoil;
        while (_tempTime > 0)
        {
            _tempTime -= Time.deltaTime;
            AddItemOffset(Quaternion.Lerp(Quaternion.identity, _rotateRecoil, _tempTime/_timeRecoil));
            AddItemOffset(Vector3.Lerp(Vector3.zero, _positionRecoil, _tempTime / _timeRecoil));
            yield return null;
        }
    }
    #endregion

    #region Reload
    [Header("Reload")]
    [SerializeField] Vector3 _reloadPosition;
    [SerializeField] Vector3 _reloadRotation;

    private Action<bool> _isReloadEventDelegate;

    public bool IsReload {
        get { return _isReload; }
        private set
        {
            if (_isReload == value)
                return;

            _isReload = value;

            if (value)
            {
                AnimateDisplacement(
                      this.transform,
                      CurrentPoint,
                      new Point() { position = _reloadPosition, rotation = Quaternion.Euler(_reloadRotation) },
                      0.1f);
                StartReload();
            }
            else
            {
                AnimateDisplacement(
                        this.transform,
                        CurrentPoint,
                        _basePoint,
                        0.1f,
                        () => ClearSway()
                        );
                EndReload();
            }
        }
    }
    private bool _isReload;

    protected virtual void StartReload() { return; }
    protected virtual void EndReload() { return; }
    #endregion

    #region Animation
    protected Animation _animation;
    #endregion


    protected override void Awake()
    {
        base.Awake();

        _animation = this.transform.GetComponent<Animation>();

        _isReloadEventDelegate = (bool a) => IsReload = a;
        InteractManager.Main.EnableHandEvent += _isReloadEventDelegate;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        InteractManager.Main.EnableHandEvent -= _isReloadEventDelegate;
    }

    protected override void Update()
    {
        if (!IsReload)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
                PullTrigger();

            base.Update();
        }   
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        IsReload = InteractManager.Main.EnableHand;
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        IsReload = false;
    }

}
