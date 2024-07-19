using System;
using UnityEngine;
using MyEffects;
using System.Linq;

public class HIWBreakOpen : HIWeapon
{
    private ItemGunChamberless _itemGunChamberless;

    #region Reload
    [Space]
    [SerializeField] private InteractCalled _breakOpen;
    [SerializeField] private AnimationClip _openAnimation;
    [SerializeField] private float _speedOpen;
    [SerializeField] private InteractPutTake[] _placeForAmmo;

    private bool _breakStatus;
    public void BreakOpen()
    {
        if (_breakStatus == true)
            return;
        _breakStatus = true;

        _animation[_openAnimation.name].time = 0.0f;
        _animation[_openAnimation.name].speed = _speedOpen;
        _animation.Blend(_openAnimation.name);

        foreach (InteractPutTake i in _placeForAmmo)
            i.InteractEnabled = true;
    }
    private void BreakClose()
    {
        if (_breakStatus == false)
            return;
        _breakStatus = false;

        _animation[_openAnimation.name].time = _openAnimation.length;
        _animation[_openAnimation.name].speed = -_speedOpen;
        _animation.Blend(_openAnimation.name);

        foreach (InteractPutTake i in _placeForAmmo)
            i.InteractEnabled = false;
    }

    protected override void StartReload()
    {
        _breakOpen.InteractEnabled = true;
    }
    protected override void EndReload()
    {
        BreakClose();

        _breakOpen.InteractEnabled = false;
        foreach (InteractPutTake i in _placeForAmmo)
            i.InteractEnabled = false;

        if(_reloadCocking)
            CurrentAmmo = 0;
    }

    private Action TakeEventMethod(int _countIn)
    {
        int _count = _countIn;
        void Inner()
        {
            Debug.Log(_count);
            _itemGunChamberless._ammo[_count] = null;
        }
        return Inner;
    }
    private Action<ItemBase> PutEventMethod(int _countIn)
    {
        int _count = _countIn;
        void Inner(ItemBase _a)
        {
            _itemGunChamberless._ammo[_count] = _a.ID;
        }
        return Inner;
    }
    #endregion

    #region Shoot
    

    [SerializeField] AnimationClip[] _shootClip;

    [SerializeField]private bool _reloadCocking;
    private int CurrentAmmo { get => _itemGunChamberless._currentAmmo; set => _itemGunChamberless._currentAmmo = _reloadCocking ? Mathf.Clamp(value, 0, _placeForAmmo.Length) : value - _placeForAmmo.Length * (value / _placeForAmmo.Length); }


    protected override void PullTrigger()
    {
        if ((CurrentAmmo < _placeForAmmo.Length || !_reloadCocking) && !_animation.isPlaying)
            _animation.Play(_shootClip[CurrentAmmo].name);
    }

    private void CheckShoot()
    {
        ItemDataAmmo _ammoToShoot = _ammoList.Find(a => a.ID == _itemGunChamberless._ammo[CurrentAmmo]);
        if (_ammoToShoot != null)
        {
            EffectsManager.PlayEffect(_muzzleEffect, _shootPosition.position, _shootPosition.rotation);

            if (_shell != null)
            {
                _itemGunChamberless._ammo[CurrentAmmo] = _shell.ID;
                _placeForAmmo[CurrentAmmo]._itemInside[0] = _shell.ID;
            }
            else
            {
                _itemGunChamberless._ammo[CurrentAmmo] = null;
                _placeForAmmo[CurrentAmmo]._itemInside.Clear();
            }
            _placeForAmmo[CurrentAmmo].UpdateStatus();

            Shoot(_ammoToShoot);
        }
        else
        {
            Debug.Log("Click");
        }

        CurrentAmmo++;
    }
    #endregion


    protected override void Awake()
    {
        base.Awake();

        _itemGunChamberless = ItemBaseLink as ItemGunChamberless;

        _breakOpen.InteractEnabled = false;
        foreach (InteractPutTake i in _placeForAmmo)
            i.InteractEnabled = false;

        if (_itemGunChamberless._ammo.Length != _placeForAmmo.Length)
            _itemGunChamberless._ammo = new string[_placeForAmmo.Length];

        for (int i = 0; i < _placeForAmmo.Length; i++)
        {
            _placeForAmmo[i].TakeEvent += TakeEventMethod(i);
            _placeForAmmo[i].PutEvent += PutEventMethod(i);
        }
    }

}
