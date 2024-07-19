using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class InputManager : MonoBehaviour
{
    #region links
    private Camera _mCamera;
    private CharacterController _controller;
    public float CharacterHight { get => _controller.height; }

    private InputsMap _iMap;
    #endregion

    #region Moving
    [SerializeField] private float _moveSpeed = 2.5f;
    //[SerializeField] private float _runSpeed = 2.0f;
    [SerializeField] private AnimationCurve _curveRunSmooth = new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(0.15f, 1.0f), new Keyframe(0.2f, 0.0f));
    [SerializeField] private AnimationCurve _curveCameraSwing = new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(0.15f, 1.0f), new Keyframe(0.2f, 0.0f));
    [SerializeField] private AnimationCurve _curveCameraRunSwing = new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(0.15f, 1.0f), new Keyframe(0.2f, 0.0f));

    private Vector3 _cameraBasePosition;

    private float _timeRun, _timeWalk,
                  _curveCameraSwingLength,
                  _curveCameraRunSwingLength;

    public static Vector3 DeltaMove { get; private set; }

    private void CharacterTranspose()
    {
        float _speedUp = 0;
        if (_iMap.Base.RunDouble.triggered || _timeRun < _curveCameraRunSwingLength)
        {
            if (_iMap.Base.RunTap.triggered)
            {
                _timeRun = 0.0f;
            }
            _timeRun += Time.deltaTime;
            _speedUp = 2 * _curveRunSmooth.Evaluate(_timeRun);
        }

        Vector3 _direction = (transform.forward * _iMap.Base.Move.ReadValue<Vector2>().y + transform.right * _iMap.Base.Move.ReadValue<Vector2>().x).normalized;
        _controller.Move(DeltaMove = ((_direction * _moveSpeed + transform.forward * _speedUp * _moveSpeed + Vector3.up * -9.8f) * Time.deltaTime));

        if (_iMap.Base.Move.ReadValue<Vector2>() != Vector2.zero)
            _timeWalk = (_timeWalk + Time.deltaTime) % _curveCameraSwingLength;
        else
            _timeWalk = Mathf.Clamp((_timeWalk + Time.deltaTime), 0, _curveCameraSwingLength);

        if (!(_timeRun < _curveCameraRunSwingLength))
            _mCamera.transform.localPosition = _cameraBasePosition + Vector3.down * _curveCameraSwing.Evaluate(_timeWalk);
        else
            _mCamera.transform.localPosition = _cameraBasePosition + Vector3.down * _curveCameraRunSwing.Evaluate(_timeRun);
    }
    #endregion

    #region Rotation
    public static Vector2 DeltaCameraRotation { get; private set; }

    [SerializeField] private float _camSpeed = 15.0f;
    private float _xCameraAxis;
    private void CameraRotate()
    {
        float _deltaTime = Time.deltaTime;

        DeltaCameraRotation = new Vector2(-_iMap.Base.Look.ReadValue<Vector2>().y * _camSpeed, _iMap.Base.Look.ReadValue<Vector2>().x * _camSpeed);

        _xCameraAxis += DeltaCameraRotation.x;
        _xCameraAxis = Mathf.Clamp(_xCameraAxis, -75, 75);
        _mCamera.transform.localEulerAngles = Vector3.right * _xCameraAxis;

        transform.Rotate(Vector3.up * DeltaCameraRotation.y);
    }

    public void AddCameraRotation(Vector3 _rotation)
    {
        DeltaCameraRotation = new Vector2(_rotation.x, _rotation.y);

        _xCameraAxis += DeltaCameraRotation.x;
        _xCameraAxis = Mathf.Clamp(_xCameraAxis, -75, 75);
        _mCamera.transform.localEulerAngles = Vector3.right * _xCameraAxis;

        transform.Rotate(Vector3.up * DeltaCameraRotation.y);
    }
    #endregion

    #region Buttons
    [Space]
    [SerializeField] private RectTransform _inventory;
    [SerializeField] private RectTransform _button;

    private bool _inventorySwitch;
    public bool InventorySwitch
    {
        get { return _inventorySwitch; }
        private set
        {
            _inventorySwitch = value;
            _inventory.gameObject.SetActive(_inventorySwitch);

            if (_inventorySwitch == false)
            {
                _button.pivot = Vector2.zero;
                _button.position = Vector2.zero;
            }
            else
            {
                _button.pivot = Vector2.up;
                _button.position = (Vector2)_inventory.position + _inventory.sizeDelta * _inventory.pivot * -_inventory.lossyScale + Vector2.up * _inventory.sizeDelta.y * _inventory.lossyScale;
            }


            if (!HandSwitch)
                InteractManager.Main.EnableHand = _inventorySwitch;
        }
    }

    private bool _handSwitch;
    public bool HandSwitch
    {
        get { return _handSwitch; }
        private set {
            if (_handSwitch == value)
                return;

            _handSwitch = value;

            if (!InventorySwitch)
                InteractManager.Main.EnableHand = _handSwitch;
        }
    }

    public bool AltButton { get; private set; }
    #endregion

    #region QuickSlots
    [Header("Quick slots")]
    [SerializeField] private ContainerSlot _containerSlot0;
    [SerializeField] private ContainerSlot _containerSlot1;
    [SerializeField] private ContainerSlot _containerSlot2;
    [SerializeField] private ContainerSlot _containerSlot3;

    private GameObject _HandItem0, _HandItem1, _HandItem2, _HandItem3;

    int _currentSlot = -1;
    private void TakeHandItem(int _slot)
    {
        if (_currentSlot == _slot)
            _currentSlot = -1;
        else
            _currentSlot = _slot;

        _HandItem0?.SetActive(_currentSlot == 0);
        _HandItem1?.SetActive(_currentSlot == 1);
        _HandItem2?.SetActive(_currentSlot == 2);
        _HandItem3?.SetActive(_currentSlot == 3);
    }
    #endregion

    public static InputManager Main;
    private void Awake()
    {
        if (!Main)
            Main = this;
        else
            Destroy(this);

        _controller = GetComponent<CharacterController>();

        _mCamera = FindObjectOfType<Camera>();
        if (!_mCamera) Debug.LogWarning("Main camera link is not found");
        _mCamera.nearClipPlane = _controller.radius / 10.0f;
        _mCamera.transform.localPosition = Vector3.up * _controller.height / 2.5f;

        _cameraBasePosition = _mCamera.transform.localPosition;
        _curveCameraSwing.postWrapMode = WrapMode.Loop;
        _curveCameraSwingLength = _curveCameraSwing.keys[_curveCameraSwing.keys.Length - 1].time;
        _curveCameraRunSwingLength = _curveCameraRunSwing.keys[_curveCameraSwing.keys.Length - 1].time;


        _iMap = new InputsMap();

        _iMap.Base.Hand.performed += context => HandSwitch = true;
        _iMap.Base.Hand.canceled += context => HandSwitch = false;

        _iMap.Base.Alt.performed += context => AltButton = true;
        _iMap.Base.Alt.canceled += context => AltButton = false;

        _iMap.Base.Inventory.performed += context => InventorySwitch = !InventorySwitch;
        _button.GetComponent<Button>().onClick.AddListener(() => InventorySwitch = !InventorySwitch);
        InventorySwitch = false;

        _iMap.Base.QuickSlot0.performed += context => TakeHandItem(0);
        _iMap.Base.QuickSlot1.performed += context => TakeHandItem(1);
        _iMap.Base.QuickSlot2.performed += context => TakeHandItem(2);
        _iMap.Base.QuickSlot3.performed += context => TakeHandItem(3);

        _containerSlot0.PutEvent += () => { _HandItem0 = InventoryData.Main.GetHandPrefab(_containerSlot0.Item.ID, _containerSlot0.Item); _HandItem0.transform.parent = _mCamera.transform; _HandItem0.SetActive(_currentSlot == 0); };
        _containerSlot0.TakeEvent += () => Destroy(_HandItem0);

        _containerSlot1.PutEvent += () => { _HandItem1 = InventoryData.Main.GetHandPrefab(_containerSlot1.Item.ID, _containerSlot1.Item); _HandItem1.transform.parent = _mCamera.transform; _HandItem1.SetActive(_currentSlot == 1);};
        _containerSlot1.TakeEvent += () => Destroy(_HandItem1);

        _containerSlot2.PutEvent += () => { _HandItem2 = InventoryData.Main.GetHandPrefab(_containerSlot2.Item.ID, _containerSlot2.Item); _HandItem2.transform.parent = _mCamera.transform; _HandItem2.SetActive(_currentSlot == 2);};
        _containerSlot2.TakeEvent += () => Destroy(_HandItem2);

        _containerSlot3.PutEvent += () => { _HandItem3 = InventoryData.Main.GetHandPrefab(_containerSlot3.Item.ID, _containerSlot3.Item); _HandItem3.transform.parent = _mCamera.transform; _HandItem3.SetActive(_currentSlot == 3);};
        _containerSlot3.TakeEvent += () => Destroy(_HandItem3);
    }

    private void Update()
    {
        if (!InteractManager.Main.EnableHand)
        {
            CharacterTranspose();
            CameraRotate();
        }
    }

    private void LateUpdate()
    {
        DeltaMove = Vector3.zero;
        DeltaCameraRotation = Vector2.zero;
    }

    private void OnEnable()
    {
        _iMap.Enable();
    }
    private void OnDisable()
    {
        _iMap.Disable();
    }
}
