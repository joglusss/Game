using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class AIUnitBase : MonoBehaviour
{
    private void Awake()
    {
        _currentPathIndex = 0;
        _currentPath = new Vector3[] { this.transform.position };
        _FSM = this.GetComponent<Animator>();

        UpdatePath(this);
        StartCoroutine(Movement());
        StartCoroutine(ShowPath());
    }

    private void Update()
    {
        FSMStatsUpdateSet();
    }

    #region Unit stats
    [SerializeField] private float _HP = 100.0f;
    public float HP { get { return _HP; } set { _HP += value; FSMHitSet(); if (_HP < 0) FSMDeadSet(); } }

    [SerializeField] private AINavGrid.NavUnitData _navUnitData = new AINavGrid.NavUnitData() { _speed = 1.0f, _hight = 0.24f, _width = 1 };
    #endregion

    #region FSMData Update
    private Animator _FSM;

    // Add to Animator -- Dead(bool), Hit(trigger), Seen(bool), Seen360(bool), Distance(float), Moving(bool), Time(float)

    private void FSMDeadSet()
    {
        _FSM.SetBool("Dead", true);
    }
    private void FSMHitSet()
    {
        _FSM.SetTrigger("Hit");
    }
    private void FSMStatsUpdateSet()
    {
        AINavGrid.NavCell _localNavCell = AINavGrid.GetNavCell(this.transform.position + Vector3.down * _navUnitData._offset, _navUnitData._width);

        if (_localNavCell != null)
        {
            _FSM.SetBool("Seen", Convert.ToBoolean(_localNavCell.isSeen));
            _FSM.SetBool("Seen360", Convert.ToBoolean(_localNavCell.isSeen360));
            _FSM.SetFloat("Distance", _localNavCell.distFromPlayerNormalized);
            _FSM.SetBool("Moving", !(_currentPathIndex >= _currentPath.Length));
            _FSM.SetFloat("Time", _FSM.GetFloat("Time") + Time.deltaTime);
        }

    }

    #endregion

    #region UpdatePath
    [Space]
    [SerializeField] private int _updatePathDelay = 1000;
    [SerializeField] private float _timeCalculateStartPoint = 1.0f;

    private bool PausePathUpdate { get; set; }

    public struct PathUpdateData
    {
        public AINavGrid.enumFPA FPAenum;
        public Func<Vector3[]> FPAFunc;

        public Transform targetFPA;

        public int lastIndex;
        public Vector3 startPoint;


        public float speed;
        public bool pauseUpdate;
    }
    public PathUpdateData _currentFPAUpdateData;

    public void SetStandFPA()
    {
        PausePathUpdate = false;

        _currentFPAUpdateData = new PathUpdateData()
        {
            FPAenum = AINavGrid.enumFPA.Stand,
        };
    }
    public void SetFollowFPA(Transform _followedTransformIn)
    {
        PausePathUpdate = false;

        _currentFPAUpdateData = new PathUpdateData()
        {
            FPAenum = AINavGrid.enumFPA.Follow,
            targetFPA = _followedTransformIn,
        };
    }
    public void SetAvoidFPA()
    {
        PausePathUpdate = false;

        _currentFPAUpdateData = new PathUpdateData()
        {
            FPAenum = AINavGrid.enumFPA.Avoid,
        };
    }
    public void SetSneakFPA(Transform _sneakTargetIn)
    {
        PausePathUpdate = false;

        _currentFPAUpdateData = new PathUpdateData()
        {
            FPAenum = AINavGrid.enumFPA.Sneak,
            targetFPA = _sneakTargetIn,
        };
    }


    private void PreparePathUpdateData(ref PathUpdateData _ref)
    {
        Vector3 _pStart = this.transform.position;

        float _distance = _timeCalculateStartPoint * _navUnitData._speed;

        for (_ref.lastIndex = _currentPathIndex; _ref.lastIndex < _currentPath.Length && _distance > 0.001f; _ref.lastIndex++)
        {
            float temp = Mathf.Clamp(Vector3.Distance(_pStart, _currentPath[_ref.lastIndex]), 0.0f, _distance);
            _pStart = Vector3.MoveTowards(_pStart, _currentPath[_ref.lastIndex], temp);
            _distance -= temp;
        }

        _ref.startPoint = _pStart;

        switch (_ref.FPAenum)
        {
            default:
                _ref.FPAFunc = () => new Vector3[0];
                break;
            case AINavGrid.enumFPA.Stand:
                _ref.FPAFunc = () => new Vector3[0];
                break;
            case AINavGrid.enumFPA.Follow:
                Vector3 _pEndFollow = _ref.targetFPA.position;
                _ref.FPAFunc = () => AINavGrid.FPAFollow(_pStart, _pEndFollow, _navUnitData);
                break;
            case AINavGrid.enumFPA.Avoid:
                _ref.FPAFunc = () => AINavGrid.FPAAvoid(_pStart, 5.0f, _navUnitData);
                break;
            case AINavGrid.enumFPA.Sneak:
                Vector3 _pEndSneak = _ref.targetFPA.position;
                _ref.FPAFunc = () => AINavGrid.FPASneak(_pStart, _pEndSneak, _navUnitData);
                break;

        }
    }

    private async void UpdatePath(MonoBehaviour _this)
    {
        while (_this != null)
        {
            float _time = Time.realtimeSinceStartup;
            PathUpdateData _savePUD = _currentFPAUpdateData;
            PreparePathUpdateData(ref _savePUD);

            Vector3[] _endPartPath = null;
            PausePathUpdate = _savePUD.pauseUpdate;

            await Task.Run(() => {
                _endPartPath = _savePUD.FPAFunc.Invoke();
                if (_endPartPath.Length == 0)
                    _endPartPath = new Vector3[] { _savePUD.startPoint };
            });

            List<Vector3> _startPartPath = new List<Vector3>();
            for (int i = _currentPathIndex; i < _savePUD.lastIndex - 1; i++)
                _startPartPath.Add(_currentPath[i]);

            _startPartPath.AddRange(_endPartPath);

            _currentPath = _startPartPath.ToArray();
            _currentPathIndex = 0;

            _navUnitData._speed = _savePUD.speed;

            //for (int i = 1; i < _currentPath.Length; i++)
            //    Debug.DrawLine(_currentPath[i - 1], _currentPath[i], Color.green,1.0f);

            //Debug.Log(Time.realtimeSinceStartup - _time);
            //Debug.Log(_updatePathDelay - (int)((Time.realtimeSinceStartup - _time) * 1000));

            await Task.Delay(Mathf.Clamp(_updatePathDelay - (int)((Time.realtimeSinceStartup - _time) * 1000), 0, int.MaxValue));
            while (PausePathUpdate)
                await Task.Yield();
        }
    }
    #endregion

    #region Movement
    private int _currentPathIndex;
    private Vector3[] _currentPath;
    
    private IEnumerator Movement()
    {
        while (true)
        {
            yield return new WaitWhile(() => _currentPathIndex >= _currentPath.Length);

            this.transform.position = Vector3.MoveTowards(this.transform.position, _currentPath[_currentPathIndex], _navUnitData._speed * Time.deltaTime);

            if (this.transform.position == _currentPath[_currentPathIndex])
                _currentPathIndex++;
            
            yield return null;
        }
    }

    private IEnumerator ShowPath() {
        while (true) {

            Vector3 _point = this.transform.position;

            for (int i = _currentPathIndex ; i < _currentPath.Length; i++)
            {
                Debug.DrawLine(_point, _currentPath[i], Color.blue, 0.1f);
                _point = _currentPath[i];
            }

            yield return null;
        }
    }
    #endregion


}
