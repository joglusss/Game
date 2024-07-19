using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using MyEffects;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class InteractPutTake : InteractTake
{
    [SerializeField] int _maxItemCount = 1;
    [SerializeField] public List<string> _itemInside;
    [SerializeField] private bool _isAddFirstModel;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    [Space]
    [SerializeField] private List<string> _permissibleItem;
    [Space]
    [SerializeField] private AudioClip _clipPut;
    [SerializeField] private AudioClip _clipTake;

    private void Awake()
    {
        this.transform.TryGetComponent(out _meshFilter);
        this.transform.TryGetComponent(out _meshRenderer);

        UpdateStatus();
    }

    public override bool IsFit(ItemBase _item)
    {
        return _permissibleItem.Contains(_item.ID) && _itemInside.Count < _maxItemCount && _item.Count == 1;
    }

    private event Action<ItemBase> _putEvent;
    public event Action<ItemBase> PutEvent { add {_putEvent += value;} remove {_putEvent -= value;}}
    public override void Put(ItemBase _item)
    {
        if (_clipPut != null)
            EffectsManager.PlaySound(_clipPut, this.transform, this.transform.position);

        _itemInside.Add(_item.ID);
        UpdateStatus();

        _item.transform.SetParent(InventoryData.MainDrop);
        Destroy(_item.gameObject);

        _putEvent.Invoke(_item);
    }


    private event Action _takeEvent;
    public event Action TakeEvent { add {_takeEvent += value;}  remove {_takeEvent -= value;}}
    public override ItemBase Take()
    {
        if (_clipTake != null)
            EffectsManager.PlaySound(_clipTake, this.transform, this.transform.position);

        string _takeID = _itemInside[_itemInside.Count - 1];
        _itemInside.RemoveAt(_itemInside.Count - 1);

        UpdateStatus();

        ItemBase _item = InventoryData.Main.GetNewItem(_takeID);
        _item.IsDropped = false;

        _takeEvent.Invoke();
        return _item;
    }

    public void UpdateStatus()
    {
        if (_itemInside.Count == 0)
            Type = TypeInteract.Put;
        else
            Type = TypeInteract.Take;

        if(_isAddFirstModel)
            if (_itemInside.Count == 0)
                _meshFilter.mesh = null;
            else
            {
                _meshFilter.mesh = InventoryData.Main.GetItemMeshModel(_itemInside[_itemInside.Count - 1]);
                _meshRenderer.material = InventoryData.Main.GetItemMaterialModel(_itemInside[_itemInside.Count - 1]);
            }
    }
}
