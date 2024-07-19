using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Threading.Tasks;
using System;

public class InteractManager : MonoBehaviour
{
    [Header("Settings")]
    public float _handDist = 1.3f;
    public float _handSize = 0.05f;

    [Header("Cursor textures")]
    [SerializeField] private Texture2D _standartTex;
    [SerializeField] private Texture2D _pushTex;
    [SerializeField] private Texture2D _grabTex;
    [SerializeField] private Texture2D _wantGrabTex;
    [SerializeField] private Texture2D _dropTex;
    [SerializeField] private Texture2D _talkTex;

    public Camera _cMain { get; private set; }
    public RaycastHit _currentHit;
    private void Update()
    {
        Ray _ray = _cMain.ScreenPointToRay(Mouse.current.position.ReadValue());
        float _alpha = Vector3.Angle(InputManager.Main.transform.forward, _ray.direction);
        float _dist = (_handDist * _alpha) / (180 - _alpha) + _handDist;


        InteractBase _tempInteractBase;
        List<RaycastResult> _currentResult;

        if ((_currentResult = UIRaycast(Mouse.current.position.ReadValue())).Count > 0 && (_tempInteractBase = _currentResult[0].gameObject.GetComponent<InteractBase>()) && _tempInteractBase.InteractEnabled)
        {
            switch (_tempInteractBase.Type)
            {
                case InteractBase.TypeInteract.TakePut: Cursor.SetCursor(_wantGrabTex, Vector2.zero, CursorMode.Auto); break;
                default: Cursor.SetCursor(_standartTex, Vector2.zero, CursorMode.Auto); break;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame)
                CallInteract(_tempInteractBase);
        }
        else if (_currentResult.Count == 0 && Physics.SphereCast(_ray , _handSize, out _currentHit, _dist) && (_tempInteractBase = _currentHit.collider.GetComponent<InteractBase>()) != null && _tempInteractBase.InteractEnabled)
        {
            switch (_tempInteractBase.Type)
            {
                case InteractBase.TypeInteract.Push: Cursor.SetCursor(_pushTex, Vector2.zero, CursorMode.Auto); break;
                case InteractBase.TypeInteract.Door: Cursor.SetCursor(_wantGrabTex, Vector2.zero, CursorMode.Auto); break;
                case InteractBase.TypeInteract.Animation: Cursor.SetCursor(_wantGrabTex, Vector2.zero, CursorMode.Auto); break;
                case InteractBase.TypeInteract.Talk: Cursor.SetCursor(_talkTex, Vector2.zero, CursorMode.Auto); break;
                case InteractBase.TypeInteract.Take: Cursor.SetCursor(_wantGrabTex, Vector2.zero, CursorMode.Auto); break;
                case InteractBase.TypeInteract.TakePut: Cursor.SetCursor(_wantGrabTex, Vector2.zero, CursorMode.Auto); break;
                default: Cursor.SetCursor(_standartTex, Vector2.zero, CursorMode.Auto); break;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame)
                CallInteract(_tempInteractBase);
        }
        else
            Cursor.SetCursor(_standartTex, Vector2.zero, CursorMode.Auto);
    }

    #region Fake Cursor
    [Space]
    [SerializeField] private RectTransform _fakeCursor;
    public void SetFakeCursor(Vector2 _position)
    {
        _fakeCursor.position = _position;  
    }
    public void EnableFakeCursor()
    {
        _fakeCursor.sizeDelta = (Vector2.one * 32.0f) / _fakeCursor.lossyScale;
        _fakeCursor.gameObject.SetActive(true);
        Cursor.visible = false;
    }
    #endregion

    #region Interact
    private void CallInteract(InteractBase _interact)
    {
        enabled = false;

        switch (_interact.Type)
        {
            case InteractBase.TypeInteract.Push:
                _interact.GetComponent<InteractCalled>().Interact();
                break;

            case InteractBase.TypeInteract.Door:
                Cursor.SetCursor(_grabTex, Vector2.zero, CursorMode.Auto);
                _interact.GetComponent<InteractCalled>().Interact();
                break;

            case InteractBase.TypeInteract.Animation:
                _interact.GetComponent<InteractCalled>().Interact();
                break;

            case InteractBase.TypeInteract.Talk:
                _interact.GetComponent<InteractCalled>().Interact();
                break;

            case InteractBase.TypeInteract.Take:
                Cursor.SetCursor(_grabTex, Vector2.zero, CursorMode.Auto);
                StartCoroutine(TakeItemCoroutine(_interact));
                break;

            case InteractBase.TypeInteract.TakePut:
                Cursor.SetCursor(_grabTex, Vector2.zero, CursorMode.Auto);
                StartCoroutine(TakeItemCoroutine(_interact));
                break;

            default: InteractEnd(); return;
        }
    }
    public void InteractEnd()
    {
        if (EnableHand == true)
        {
            enabled = true;
            Cursor.lockState = CursorLockMode.Confined;
            _fakeCursor.gameObject.SetActive(false);
            Cursor.visible = true;
        }
    }
    #endregion

    #region UIRaycast
    EventSystem _eventSystem;
    GraphicRaycaster _raycaster;
    PointerEventData _pointerEventData;
    private List<RaycastResult> UIRaycast(Vector2 _position)
    {
        _pointerEventData.position = _position;

        List<RaycastResult> _results = new List<RaycastResult>();
        _raycaster.Raycast(_pointerEventData, _results);
        return _results;
    }
    #endregion

    #region ItemInteract
    private IEnumerator TakeItemCoroutine(InteractBase _item)
    {
        ItemBase _itemBase = _item.GetComponent<InteractTake>().Take();

        RectTransform _rectTransform = _itemBase.GetComponent<RectTransform>();
        _rectTransform.SetParent(InventoryData.MainCanvas.transform);
        _rectTransform.localScale = Vector3.one;

        Vector2 _offset = -((_rectTransform.sizeDelta / 2.0f) * _rectTransform.pivot) * InventoryData.ScaleRelative;

        InteractContainer _interactContainer = null;

        while (true)
        {
            yield return null;

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                _itemBase.SwitchRotate();
                _offset = -((_rectTransform.sizeDelta / 2.0f) * _rectTransform.pivot) * InventoryData.ScaleRelative;
            }    

            _rectTransform.position = /*_offset +*/ Mouse.current.position.ReadValue();

            Debug.DrawRay(_rectTransform.position, Vector3.up, Color.green);


            List<RaycastResult> _result; 
            RaycastHit _hit;

            _interactContainer = null;

            if ((_result = UIRaycast(_rectTransform.position)).Count > 1 && (_interactContainer = _result[1].gameObject.GetComponent<InteractContainer>()) && _interactContainer.IsFit(_itemBase))
            {
                Cursor.SetCursor(_dropTex, Vector2.zero, CursorMode.Auto);

                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    _interactContainer.Put(_itemBase);
                    
                    if( _rectTransform.parent != InventoryData.MainCanvas.transform)
                        break;
                }
            }
            else if (Physics.Raycast(_cMain.ScreenPointToRay(_rectTransform.position), out _hit, _handDist) && (_interactContainer = _hit.collider.gameObject.GetComponent<InteractContainer>()) && _interactContainer.IsFit(_itemBase))
            {
                Cursor.SetCursor(_dropTex, Vector2.zero, CursorMode.Auto);

                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    _interactContainer.Put(_itemBase);

                    if ( _rectTransform.parent != InventoryData.MainCanvas.transform)
                        break;
                }
            }
            else
            {
                Cursor.SetCursor(_grabTex, Vector2.zero, CursorMode.Auto);

                if (Mouse.current.leftButton.wasPressedThisFrame || !_enableHand)
                {
                    _itemBase.Drop();

                    if ( _rectTransform.parent != InventoryData.MainCanvas.transform)
                        break;
                }  
            }
        } 

        InteractEnd();
    }
    #endregion

    #region HandEnable
    private event Action<bool> _enableHandEvent;
    public event Action<bool> EnableHandEvent { add => _enableHandEvent += value; remove => _enableHandEvent -= value; }

    private bool _enableHand = true;
    public bool EnableHand
    {
        get { return _enableHand; }
        set 
        {
            if (value == _enableHand)
                return;

            if (value)
            {
                enabled = true;
                _enableHand = true;
                InventoryData.MainCanvas.gameObject.SetActive(true);
                Mouse.current.WarpCursorPosition(new Vector2(Screen.width, Screen.height)/2);
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
            }
            else
            {
                enabled = false;
                _enableHand = false;
                Cursor.visible = false;
                _fakeCursor.gameObject.SetActive(false);
                InventoryData.MainCanvas.gameObject.SetActive(false);
            }

            _enableHandEvent?.Invoke(value);
        }
    }
    #endregion


    public static InteractManager Main;
    private void Awake()
    {
        if (!Main)
            Main = this;
        else
            Destroy(this);

        _cMain = FindObjectOfType<Camera>();

        EnableHand = false;

        _raycaster = InventoryData.MainCanvas.GetComponent<GraphicRaycaster>();
        _eventSystem = EventSystem.current;
        _pointerEventData = new PointerEventData(_eventSystem);
    }
}

public abstract class InteractBase : MonoBehaviour
{
    public enum TypeInteract { Null, Push, Door, Talk, Take, TakePut, Put, Animation}
    public TypeInteract Type { get; protected set; } = TypeInteract.Null;

    public bool InteractEnabled { get; set; } = true;
}

public abstract class InteractContainer : InteractBase
{
    public abstract bool IsFit(ItemBase _item);
    public abstract void Put(ItemBase _item);
}

public interface IContainerWithUIItem
{
    void ClearContanerCell(ItemBase _item);
}
public abstract class InteractTake : InteractContainer
{
    public abstract ItemBase Take();
}

[RequireComponent(typeof(Collider))]
public abstract class InteractCalled : InteractBase
{
    public abstract void Awake();
    public abstract void Interact();
}

