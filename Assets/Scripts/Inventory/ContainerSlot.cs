using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ContainerSlot : InteractContainer, IContainerWithUIItem
{
    public bool IsEmploy {get; private set;}
    public ItemBase Item { get; private set; }

    private Vector2Int _size;


    private void Awake()
    {
        Image _image = GetComponent<Image>();
        _size = Vector2Int.RoundToInt(new Vector2((_image.rectTransform.sizeDelta.x / InventoryData.CellSize), (_image.rectTransform.sizeDelta.y / InventoryData.CellSize)));
    }

    public override bool IsFit(ItemBase _item)
    {
        return !IsEmploy && ((_item.Size.x <= _size.x && _item.Size.y <= _size.y) || (_item.Size.y <= _size.x && _item.Size.x <= _size.y));
    }

    private event Action _putEvent;
    public event Action PutEvent {add { _putEvent += value; } remove { _putEvent -= value; }}
    public override void Put(ItemBase _item)
    {
        if (!(_item.Size.x <= _size.x && _item.Size.y <= _size.y))
            _item.SwitchRotate();

        IsEmploy = true;
        Item = _item;
        _item.transform.SetParent(this.transform);
        _item.transform.position = (Vector2)this.transform.position + (((Vector2)_size * InventoryData.CellSize) / 2.0f) * InventoryData.ScaleRelative;

        _putEvent.Invoke();
    }


    private event Action _takeEvent;
    public event Action TakeEvent { add { _takeEvent += value; } remove { _takeEvent -= value; } }
    public void ClearContanerCell(ItemBase _item)
    {
        IsEmploy = false;
        Item = null;

        _takeEvent.Invoke();
    }
}
