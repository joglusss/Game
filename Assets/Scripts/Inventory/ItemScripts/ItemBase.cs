using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MyEffects;


public class ItemBase : InteractTake
{
    public string ID;

    public int CountMax;
    [SerializeField] 
    private int _count;
    public int Count 
    {   get { return _count; } 
        set 
        { 
            _count = value;

            _text.text = $"{_count}";

            if (_count > 1)
                _text.enabled = true;
            else
                _text.enabled = false;

            if (_count == CountMax)
                _text.color = Color.red;
            else
                _text.color = Color.black;
        }
    }

    private Vector2Int _size;
    public Vector2Int Size 
    { 
        get 
        {
            Vector2Int _temp = Vector2Int.RoundToInt(_image.rectTransform.rotation * (Vector2)_size);
            return new Vector2Int(Mathf.Abs(_temp.x), Mathf.Abs(_temp.y));
        }
    }

    private bool _isDropped;
    public bool IsDropped
    {
        get { return _isDropped; }
        set
        {
            _isDropped = value;

            _meshRender.enabled = _isDropped;
            _collider.enabled = _isDropped;
            _rigidbody.isKinematic = !_isDropped;

            transform.rotation = Quaternion.identity;
            _text.gameObject.SetActive(!_isDropped);
            _image.enabled = !_isDropped;
        }
    }

    private bool _isRotated;
    public bool IsRotated
    {
        get { return _isRotated; }
        set
        {
            _isRotated = value;
            if (value == true)
            {
                _image.rectTransform.eulerAngles = new Vector3(0, 0, -90);

                _text.rectTransform.localRotation = Quaternion.Euler(0, 0, 90);
                _text.rectTransform.localPosition = _image.rectTransform.sizeDelta / -2;
            }
            else
            {
                _image.rectTransform.eulerAngles = Vector3.zero;
                _text.rectTransform.eulerAngles = Vector3.zero;

                _text.rectTransform.localPosition = _image.rectTransform.sizeDelta / new Vector2(-2, 2);
            }
                
        }
    }
    public void SwitchRotate()
    {
        IsRotated = !IsRotated;
    }

    [SerializeField] private string _soundImpactID;

    public void Awake()
    {
        Type = TypeInteract.TakePut;

        if (!this.TryGetComponent(out _rigidbody))
            Debug.LogError("There are no Rigidbody");
        if (!this.TryGetComponent(out _meshRender))
            Debug.LogError("There are no MeshRenderer");
        if (!this.TryGetComponent(out _collider))
            Debug.LogError("There are no Colider");
        if (!this.TryGetComponent(out _image))
            Debug.LogError("There are no Image");
        if (!this.transform.GetChild(0).TryGetComponent(out _text))
            Debug.LogError("There are no Text");

        if (!this.GetComponent<MeshFilter>())
            Debug.LogError("There are no MeshFilter");
        if (!this.GetComponent<CanvasRenderer>())
            Debug.LogError("There are no CanvasRenderer");

        Count = _count;
        _image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _size = Vector2Int.RoundToInt(_image.rectTransform.sizeDelta / InventoryData.CellSize);

        _text.rectTransform.pivot = new Vector2(0, 1);
        _text.rectTransform.anchorMin = new Vector2(0, 1);
        _text.rectTransform.anchorMax = new Vector2(0, 1);
        IsRotated = IsRotated;

        IsDropped = !this.transform.GetComponentInParent<Container>();
    }

    public void OnCollisionEnter(Collision collision)
    {
        EffectsManager.PlayImpactSound(_soundImpactID, this.transform.position);
    }

    private MeshRenderer _meshRender;
    private Collider _collider;
    private Rigidbody _rigidbody;
    private Text _text;

    private Image _image;
    public Vector2 CornerPosition { 
        get {
            Vector2 _temp = (Vector2)_image.rectTransform.position + -OffsetToCorner + (Vector2.one * InventoryData.CellSize / 2.0f) * InventoryData.ScaleRelative;
            return _temp; 
        }
        
    }
    public Vector2 OffsetToCorner { 
        get {
            return _image.rectTransform.pivot * ((Vector2)Size * InventoryData.CellSize * InventoryData.ScaleRelative);
        }
    }
    
    public void Drop()
    {
        IsDropped = true;
        this.transform.SetParent(InventoryData.MainDrop);
        this.transform.localScale = Vector3.one;

        Ray _ray = InteractManager.Main._cMain.ScreenPointToRay((Vector2)_image.rectTransform.position + -((_image.rectTransform.sizeDelta / 2.0f) * _image.rectTransform.pivot) * InventoryData.ScaleRelative);
        RaycastHit _hit;
        if (Physics.Raycast(_ray, out _hit, InteractManager.Main._handDist))
            this.transform.position = _hit.point;
        else
            this.transform.position = _ray.origin + _ray.direction.normalized * InteractManager.Main._handDist;
    }

    public override ItemBase Take()
    {
        if (Count == 1 || InputManager.Main.AltButton)
        {
            if (IsDropped)
                IsDropped = false;
            else
                this.transform.parent.GetComponent<IContainerWithUIItem>().ClearContanerCell(this);
            return this;
        }   
        else
        {
            Count--;

            ItemBase _item = InventoryData.Main.GetNewItem(ID);
            _item.IsDropped = false;
            return _item;
        }
    }

    public override bool IsFit(ItemBase _item)
    {
        if ( _item.ID.Equals(ID) && Count < CountMax )
            return true;
        else
            return false;
    }

    public override void Put(ItemBase _item)
    {
        if (InputManager.Main.AltButton)
        {
            int _addCount = Mathf.Clamp(_item.Count, 1, CountMax - Count);
            _item.Count -= _addCount;
            Count += _addCount;
        }
        else
        {
            _item.Count--;
            Count++;
        }

        if (_item.Count < 1)
        {
            _item.transform.SetParent(InventoryData.MainDrop);
            Destroy(_item.gameObject);
        }
    }
}
