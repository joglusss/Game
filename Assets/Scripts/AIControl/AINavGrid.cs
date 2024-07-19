using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Xml.Serialization;
using UnityEngine.UIElements;

public class AINavGrid : MonoBehaviour
{
    #region Base fields

    [System.Serializable]
    public class NavGridSettings
    {
        public MeshCollider _mapMesh;

        public float _widthCell = 0.2f;
        public float _maxHightCell = 3.0f;
        public float _minHightCell = 0.25f;

        public float _maxSlopeAngle = 45.0f;
        public float _maxDistBetweenCell = 0.128f;

        public int _maxPointsInPath = 50;

        public LayerMask _NavMapLayer;
        public LayerMask _NavObstacleLayer;
        public LayerMask _NavObstacleDinamicLayer;
        public LayerMask _NavObstacleTransparentLayer;

        public NavGridSettings()
        {
            _maxDistBetweenCell = (_widthCell) / Mathf.Cos(_maxSlopeAngle);
        }
    }
    public class GridData
    {
        public Dictionary<Vector2Int, List<NavCell>> _navCellsDictionary;
        public HashSet<NavCell> _navCellsHashSet;
        public Vector3Int[] _fullKeysArray;

        public GridData()
        {
            _navCellsDictionary = new Dictionary<Vector2Int, List<NavCell>>();
            _navCellsHashSet = new HashSet<NavCell>();
        }
    }
    public class NavCell
    {
        public Vector2Int key;
        public Vector3Int fullKey; // x,z are key, y is list index
        public Vector3 position;

        public int isSeen;
        public int isSeen360;

        public float hight;

        public float distFromPlayerNormalized;
        public float distFromPlayer;


        public Dictionary<Vector2Int, NavCell> neighborsByDir;
        public NavCell[] neighbors;

        public HashSet<NavCell>[] gruppedCellsBySize;
        private float[] hightOfGruppedCells;
        public bool[] doesSizeFit;


        public Vector3[] centerGruppBySize;

        public bool DoesHightFit(float _hightIn)
        {
            return _hightIn < hight;
        }
        public bool DoesHightFit(float _hightIn, int _width)
        {
            return _hightIn < hightOfGruppedCells[_width];
        }
    }


    [Header("TestGridGenerator")]
    [SerializeField]
    [InspectorName("Navigations Grid Settings")]
    private NavGridSettings _inspectorGridSettings;
    private static NavGridSettings _gridSettings;
    private static GridData _gridData;

    [ContextMenu("BakeGridTest")]
    public void BakeGrid()
    {
        if (_gridSettings == null)
            _gridSettings = _inspectorGridSettings;

        Vector3 _boundPoint1 = new Vector3(_gridSettings._mapMesh.bounds.center.x + _gridSettings._mapMesh.bounds.extents.x, _gridSettings._mapMesh.bounds.center.y + _gridSettings._mapMesh.bounds.extents.y, _gridSettings._mapMesh.bounds.center.z + _gridSettings._mapMesh.bounds.extents.z);
        Vector3 _boundPoint2 = new Vector3(_gridSettings._mapMesh.bounds.center.x + -_gridSettings._mapMesh.bounds.extents.x, _gridSettings._mapMesh.bounds.center.y - _gridSettings._mapMesh.bounds.extents.y, _gridSettings._mapMesh.bounds.center.z + -_gridSettings._mapMesh.bounds.extents.z);
        Debug.DrawRay(_boundPoint1, Vector3.down * 20, Color.red, 20.0f);
        Debug.DrawRay(_boundPoint2, Vector3.down * 20, Color.blue, 20.0f);

        Vector2Int _xzToBound1 = new Vector2Int((int)(_boundPoint1.x / _gridSettings._widthCell), (int)(_boundPoint1.z / _gridSettings._widthCell));
        Debug.Log(_xzToBound1);
        Vector2Int _xzToBound2 = new Vector2Int((int)(_boundPoint2.x / _gridSettings._widthCell), (int)(_boundPoint2.z / _gridSettings._widthCell));
        Debug.Log(_xzToBound2);


        // Generate cells
        GridData _localGridData = new GridData();

        for (int x = _xzToBound2.x; x <= _xzToBound1.x; x++)
            for (int z = _xzToBound2.y; z <= _xzToBound1.y; z++)
            {
                Vector3 _currentCellCenter = new Vector3(x * _gridSettings._widthCell, 0.0f, z * _gridSettings._widthCell);

                RaycastHit _hitToWall;
                float _yStartRayCast = _boundPoint1.y;

                while (Physics.Linecast(new Vector3(_currentCellCenter.x, _yStartRayCast, _currentCellCenter.z), new Vector3(_currentCellCenter.x, _boundPoint2.y, _currentCellCenter.z), out _hitToWall, _gridSettings._NavMapLayer))
                {
                    float _lowerPoint = _hitToWall.point.y;
                    Vector3 _saveCenterPoint = _hitToWall.point;
                    Vector3 _averagePoint = Vector3.zero;

                    if (Vector3.Angle(_hitToWall.normal, Vector3.up) < _gridSettings._maxSlopeAngle)
                    {

                        for (int i = -1; i <= 1; i += 2)
                            for (int j = -1; j <= 1; j += 2)
                            {
                                if (!Physics.Linecast(new Vector3(_currentCellCenter.x, _saveCenterPoint.y + _gridSettings._widthCell, _currentCellCenter.z) + new Vector3(i, 0.0f, j) * _gridSettings._widthCell / 4.0f, new Vector3(_currentCellCenter.x, _saveCenterPoint.y - _gridSettings._widthCell, _currentCellCenter.z) + new Vector3(i, 0.0f, j) * _gridSettings._widthCell / 4.0f, out _hitToWall, _gridSettings._NavMapLayer)
                                    || Vector3.Angle(_hitToWall.normal, Vector3.up) > _gridSettings._maxSlopeAngle)
                                    goto CycleExit;

                                _averagePoint += _hitToWall.point;
                            }

                        _averagePoint /= 4;


                        float _maxHightCurrent = -1;
                        foreach (RaycastHit hit in Physics.RaycastAll(_averagePoint + Vector3.up * 0.001f, Vector3.up, _gridSettings._maxHightCell, _gridSettings._NavObstacleLayer + _gridSettings._NavMapLayer))
                        {
                            if (Vector3.Angle(hit.normal, Vector3.down) < 40.0f)
                            {
                                _maxHightCurrent = hit.distance;
                                break;
                            }
                        }

                        if (_maxHightCurrent < 0) _maxHightCurrent = _gridSettings._maxHightCell;
                        if (_maxHightCurrent < _gridSettings._minHightCell) goto CycleExit;


                        if (!Physics.CheckBox(_averagePoint + Vector3.up * (_gridSettings._minHightCell) / 2.0f, new Vector3(_gridSettings._widthCell, _gridSettings._minHightCell, _gridSettings._widthCell) / 2.0f, Quaternion.identity, _gridSettings._NavObstacleLayer))
                        {


                            if (!_localGridData._navCellsDictionary.ContainsKey(new Vector2Int(x, z)))
                                _localGridData._navCellsDictionary.Add(new Vector2Int(x, z), new List<NavCell>());

                            NavCell _generatedNavCell = new NavCell { position = _averagePoint, hight = _maxHightCurrent, key = new Vector2Int(x, z), isSeen = -1, isSeen360 = -1};

                            _localGridData._navCellsDictionary[new Vector2Int(x, z)].Add(_generatedNavCell);
                            _localGridData._navCellsHashSet.Add(_generatedNavCell);
                        }
                    }

                CycleExit:

                    _yStartRayCast = _lowerPoint - 0.001f;
                }


            }

        // Sets neighbors

        foreach (NavCell i in _localGridData._navCellsHashSet)
        {
            List<NavCell> _listNavCells = new List<NavCell>();
            Dictionary<Vector2Int, NavCell> _dictionaryOfNavCellsNeibByDic = new Dictionary<Vector2Int, NavCell>();

            for (int x = -1; x <= 1; x++)
                for (int z = -1; z <= 1; z++)
                {
                    if (x == 0 && z == 0)
                        continue;

                    _dictionaryOfNavCellsNeibByDic.Add(new Vector2Int(x, z), null);

                    NavCell _newNeighbor;
                    if ((_newNeighbor = GetNavCell((i.position + new Vector3(x, 0, z) * _gridSettings._widthCell), _localGridData)) != null)
                    {
                        if (x != 0 && z != 0 && (GetNavCell((i.position + new Vector3(0, 0, z) * _gridSettings._widthCell), _localGridData) == null || GetNavCell((i.position + new Vector3(x, 0, 0) * _gridSettings._widthCell), _localGridData) == null))
                            continue;

                        _listNavCells.Add(_newNeighbor);
                        _dictionaryOfNavCellsNeibByDic[new Vector2Int(x, z)] = _newNeighbor;
                    }
                }
            i.neighbors = _listNavCells.ToArray();
            i.neighborsByDir = _dictionaryOfNavCellsNeibByDic;
        }

        // Delete Collisions

        foreach (List<NavCell> i in _localGridData._navCellsDictionary.Values)
        {
            for (int j = 0; j < i.Count - 1; j++)
            {
                if (Vector3.Distance(i[j].position, i[j + 1].position) < _gridSettings._maxDistBetweenCell)
                {
                    _localGridData._navCellsHashSet.Remove(i[j]);
                    i.RemoveAt(j);
                    j--;
                }
            }
        }


        // Deleting Island 

        List<NavCell> _navCellsIslandSearch = _localGridData._navCellsHashSet.ToList();
        List<HashSet<NavCell>> _listsOfIsland = new List<HashSet<NavCell>>();

        while (_navCellsIslandSearch.Count > 0)
        {
            List<NavCell> _forSearch = new List<NavCell> { _navCellsIslandSearch[0] };
            HashSet<NavCell> _searched = new HashSet<NavCell>();

            while (_forSearch.Count > 0)
            {
                NavCell _currentNavCell = _forSearch[0];

                _searched.Add(_currentNavCell);
                _forSearch.Remove(_currentNavCell);
                _navCellsIslandSearch.Remove(_currentNavCell);

                foreach (NavCell i in _currentNavCell.neighbors)
                    if (!_forSearch.Contains(i) && !_searched.Contains(i))
                        _forSearch.Add(i);
            }

            _listsOfIsland.Add(_searched);
        }

        HashSet<NavCell> _mainIsland = _listsOfIsland[0];
        foreach (HashSet<NavCell> i in _listsOfIsland)
        {
            if (i.Count > _mainIsland.Count)
                _mainIsland = i;
        }

        // Update grid data

        _localGridData._navCellsHashSet.Clear();
        _localGridData._navCellsHashSet = _mainIsland;
        _localGridData._navCellsDictionary.Clear();

        foreach (NavCell i in _localGridData._navCellsHashSet)
        {
            if (!_localGridData._navCellsDictionary.ContainsKey(i.key))
                _localGridData._navCellsDictionary.Add(i.key, new List<NavCell>());
            _localGridData._navCellsDictionary[i.key].Add(i);
        }

        // Calculate Size

        foreach (NavCell i in _localGridData._navCellsHashSet)
        {
            int _from = 0,
                _to = 0;

            i.gruppedCellsBySize = new HashSet<NavCell>[5];
            i.doesSizeFit = new bool[5];

            for (int _size = 1; _size < 5; _size++)
            {
                HashSet<NavCell> _localCellsGruppedBySize = new HashSet<NavCell>();

                for (int x = _from; x <= _to; x++)
                    for (int z = _from; z <= _to; z++)
                    {
                        Vector3 _offset = new Vector3(x * _gridSettings._widthCell, 0, z * _gridSettings._widthCell) + i.position;

                        NavCell _localNavCell = GetNavCell(_offset, _localGridData);
                        if (_localNavCell == null)
                        {
                            i.doesSizeFit[_size] = false;
                            goto CycleExit;
                        }

                        _localCellsGruppedBySize.Add(_localNavCell);
                    }

                i.gruppedCellsBySize[_size] = _localCellsGruppedBySize;
                i.doesSizeFit[_size] = true;

            CycleExit:

                _from -= (_size % 2) == 0 ? 1 : 0;
                _to += (_size % 2) > 0 ? 1 : 0;
            }
        }

        // Set full key 

        List<Vector3Int> _localFullKeysList = new List<Vector3Int>();

        foreach (List<NavCell> iList in _localGridData._navCellsDictionary.Values)
            for (int i = 0; i < iList.Count; i++)
            {
                iList[i].fullKey = new Vector3Int(iList[i].key.x, i, iList[i].key.y);

                _localFullKeysList.Add(iList[i].fullKey);
            }

        _localGridData._fullKeysArray = _localFullKeysList.ToArray();

        // Draw 
        
        foreach (NavCell i in _localGridData._navCellsHashSet)
        {
            Vector3 _p1 = i.position + new Vector3(_gridSettings._widthCell / 2, 0, _gridSettings._widthCell / 2),
                    _p2 = i.position + new Vector3(_gridSettings._widthCell / 2, 0, -_gridSettings._widthCell / 2),
                    _p3 = i.position + new Vector3(-_gridSettings._widthCell / 2, 0, -_gridSettings._widthCell / 2),
                    _p4 = i.position + new Vector3(-_gridSettings._widthCell / 2, 0, _gridSettings._widthCell / 2);

            Color _color = Color.red;

            if (i.neighbors.Length == 7)
                _color = Color.yellow;
            if (i.neighbors.Length == 6)
                _color = Color.green;
            if (i.neighbors.Length == 5)
                _color = Color.cyan;
            if (i.neighbors.Length == 4)
                _color = Color.blue;
            if (i.neighbors.Length == 3)
                _color = Color.magenta;
            if (i.neighbors.Length == 2)
                _color = Color.black;

            Debug.DrawLine(_p1, _p2, _color, 240.0f);
            Debug.DrawLine(_p2, _p3, _color, 240.0f);
            Debug.DrawLine(_p3, _p4, _color, 240.0f);
            Debug.DrawLine(_p4, _p1, _color, 240.0f);
        }
       
        // Set Data;

        _gridData = _localGridData;
    }

    #endregion

    #region Total Methods

    public static NavCell GetNavCell(Vector3 _position)
    {
        Vector2Int _key = new Vector2Int(Mathf.RoundToInt(_position.x / _gridSettings._widthCell), Mathf.RoundToInt(_position.z / _gridSettings._widthCell));


        if (_gridData._navCellsDictionary.ContainsKey(_key))
            foreach (NavCell i in _gridData._navCellsDictionary[_key])
            {
                if (Vector3.Distance(_position, i.position) < _gridSettings._maxDistBetweenCell)
                    return i;
            }
        return null;
    }
    public static NavCell GetNavCell(Vector3 _position, GridData _gridDataIn)
    {
        Vector2Int _key = new Vector2Int(Mathf.RoundToInt(_position.x / _gridSettings._widthCell), Mathf.RoundToInt(_position.z / _gridSettings._widthCell));

        if (_gridDataIn._navCellsDictionary.ContainsKey(_key))
            foreach (NavCell i in _gridDataIn._navCellsDictionary[_key])
            {
                if (Vector3.Distance(_position, i.position) < _gridSettings._maxDistBetweenCell)
                    return i;

            }
        return null;
    }
    public static NavCell GetNavCell(Vector3 _position, int _width)
    {
        _position += (new Vector3(_gridSettings._widthCell, 0, _gridSettings._widthCell) / 2.0f) * (1 - (_width % 2));

        Vector2Int _key = new Vector2Int(Mathf.RoundToInt(_position.x / _gridSettings._widthCell), Mathf.RoundToInt(_position.z / _gridSettings._widthCell));

        if (_gridData._navCellsDictionary.ContainsKey(_key))
            foreach (NavCell i in _gridData._navCellsDictionary[_key])
            {
                if (Vector3.Distance(_position, i.position) < _gridSettings._maxDistBetweenCell)
                    return i;
            }
        return null;
    }

    public static NavCell GetClosetByYAxisNavCell(Vector3 _position)
    {

        Vector2Int _key = new Vector2Int(Mathf.RoundToInt(_position.x / _gridSettings._widthCell), Mathf.RoundToInt(_position.z / _gridSettings._widthCell));

        float _distance = int.MaxValue;
        NavCell _return = null;

        if (_gridData._navCellsDictionary.ContainsKey(_key))
            foreach (NavCell i in _gridData._navCellsDictionary[_key])
            {
                float _distanceTemp = Vector3.Distance(_position, i.position);
                if (_distanceTemp < _distance)
                {
                    _distance = _distanceTemp;
                    _return = i;
                }
            }
        return _return;
    }
    public static NavCell GetClosetByYAxisNavCell(Vector3 _position, int _width)
    {
        _position += (new Vector3(_gridSettings._widthCell, 0, _gridSettings._widthCell) / 2.0f) * (1 - (_width % 2));

        Vector2Int _key = new Vector2Int(Mathf.RoundToInt(_position.x / _gridSettings._widthCell), Mathf.RoundToInt(_position.z / _gridSettings._widthCell));

        float _distance = int.MaxValue;
        NavCell _return = null;

        if (_gridData._navCellsDictionary.ContainsKey(_key))
            foreach (NavCell i in _gridData._navCellsDictionary[_key])
            {
                float _distanceTemp = Vector3.Distance(_position, i.position);
                if (_distanceTemp < _distance)
                {
                    _distance = _distanceTemp;
                    _return = i;
                }
            }
        return _return;
    }


    public static Vector2Int GetNavCellKey(Vector3 _position)
    {
        return new Vector2Int(Mathf.RoundToInt(_position.x / _gridSettings._widthCell), Mathf.RoundToInt(_position.z / _gridSettings._widthCell));
    }
    public static Vector2Int GetNavCellKey(Vector2 _position)
    {
        return new Vector2Int(Mathf.RoundToInt(_position.x / _gridSettings._widthCell), Mathf.RoundToInt(_position.y / _gridSettings._widthCell));
    }

    #endregion

    #region Find path algorithm
    [System.Serializable] public struct NavUnitData
    {
        public float _speed;
        public float _hight;
        public int _width;
        public float _offset;
    }

    private class Path
    {
        public NavCell navCell;
        public Path fromPath;

        public float G;
        public float H;
        public float D;
        public int S;
        public int C;

        public float F { get { return G + H; } }

        public Vector3[] SmoothPath(NavUnitData _navUnitDataIn)
        {
            List<NavCell> _nodeList = new List<NavCell>();
            Path _from = this;
            do
            {
                _nodeList.Add(_from.navCell);
            } while ((_from = _from.fromPath) != null);

            for (int i = 0; i + 2 < _nodeList.Count; i++)
            {
                while (i + 2 < _nodeList.Count && DDACheckLine(_nodeList[i], _nodeList[i + 2]))
                {
                    _nodeList.RemoveRange(i + 1, 1);
                }
            }

            _nodeList.Reverse();

            Vector3 _offset = Vector3.up * _navUnitDataIn._offset + new Vector3(1.0f, 0.0f, 1.0f) * _gridSettings._widthCell * (1 - (int)(_navUnitDataIn._width % 2));

            Vector3[] _pathList = new Vector3[_nodeList.Count];
            for (int i = 0; i < _pathList.Length; i++)
            {
                _pathList[i] = (_nodeList[i].position + _offset);
            }

            return _pathList;
        }
        public Vector3[] AccuratePath(NavUnitData _navUnitDataIn)
        {
            List<NavCell> _nodeList = new List<NavCell>();
            Path _from = this;
            do
            {
                _nodeList.Add(_from.navCell);
            } while ((_from = _from.fromPath) != null);

            _nodeList.Reverse();

            Vector3 _offset = Vector3.up * _navUnitDataIn._offset + new Vector3(1.0f, 0.0f, 1.0f) * _gridSettings._widthCell * (1 - (int)(_navUnitDataIn._width % 2));

            Vector3[] _pathList = new Vector3[_nodeList.Count];
            for (int i = 0; i < _pathList.Length; i++)
            {
                _pathList[i] = (_nodeList[i].position + _offset);
            }

            return _pathList;
        }
    }

    private static bool DDACheckLine(NavCell _key1, NavCell _key2)
    {
        Vector3 _sP = _key1.position;
        Vector3 _eP = _key2.position;

        int _deltaX = Math.Abs(_key1.key.x - _key2.key.x);
        int _deltaY = Math.Abs(_key1.key.y - _key2.key.y);

        int _l = Math.Max(_deltaX, _deltaY);

        if (_l == 0)
        {
            return true;
        }

        Vector2 _delta = new Vector2((_eP.x - _sP.x) / _l, (_eP.z - _sP.z) / _l);
        Vector2 _next = new Vector2(_sP.x, _sP.z);

        _next += _delta;

        _l++;
        for (; _l > 0; _l--)
        {
            //Vector3 _p1 = _key1.position + new Vector3(_gridSettings._widthCell / 2, 0, _gridSettings._widthCell / 2),
            //       _p2 = _key1.position + new Vector3(_gridSettings._widthCell / 2, 0, -_gridSettings._widthCell / 2),
            //       _p3 = _key1.position + new Vector3(-_gridSettings._widthCell / 2, 0, -_gridSettings._widthCell / 2),
            //       _p4 = _key1.position + new Vector3(-_gridSettings._widthCell / 2, 0, _gridSettings._widthCell / 2);

            //Color _color = Color.red;

            //Debug.DrawLine(_p1, _p2, _color, 20.0f);
            //Debug.DrawLine(_p2, _p3, _color, 20.0f);
            //Debug.DrawLine(_p3, _p4, _color, 20.0f);
            //Debug.DrawLine(_p4, _p1, _color, 20.0f);

            //Debug.Log(_next);
            //Debug.Log(_key1.key);

            _key2 = _key1.neighborsByDir[GetNavCellKey(_next) - _key1.key];

            if (_key2 == null)
                return false;

            _key1 = _key2;
            _next += _delta;
        }

        return true;
    }

    public enum enumFPA {Stand, Follow, Avoid, Sneak}
    public static Vector3[] FPAFollow(Vector3 _pStartIn, Vector3 _pEndIn, NavUnitData _navUnitDataIn)
    {
        NavCell _endCell = GetClosetByYAxisNavCell(_pEndIn, _navUnitDataIn._width);
        if (_endCell == null)
            return new Vector3[0];

        List<Path> _cellsForExplore = new List<Path>();
        void AddForSorting(Path _a)
        {
            int _c = _cellsForExplore.FindIndex(i => (_a.F <= i.F));
            if (_c == -1)
                _c = _cellsForExplore.Count;

            _cellsForExplore.Insert(_c, _a);
        }

        HashSet<NavCell> _exploredNods = new HashSet<NavCell>();

        NavCell _startCell = GetClosetByYAxisNavCell(_pStartIn, _navUnitDataIn._width);
        if (_startCell == null)
            return new Vector3[0];

        _cellsForExplore.Add(new Path { navCell = _startCell, G = 0, H = Vector3.Distance(_pStartIn, _pEndIn) });
        

        while (_cellsForExplore.Count > 0)
        {
            Path _currentPath = _cellsForExplore[0];

            if (_currentPath.navCell == _endCell)
            {
                return _currentPath.SmoothPath(_navUnitDataIn);
            }

            _cellsForExplore.Remove(_currentPath);
            _exploredNods.Add(_currentPath.navCell);

            foreach (NavCell iNavCell in _currentPath.navCell.neighbors)
            {
                if (_exploredNods.Contains(iNavCell))
                    continue;

                if (!iNavCell.DoesHightFit(_navUnitDataIn._hight))
                {
                    _exploredNods.Add(iNavCell);
                    continue;
                }

                Path _newPath = null;
                void SetPathData()
                {
                    _newPath.navCell = iNavCell;
                    _newPath.fromPath = _currentPath;
                    _newPath.G = _currentPath.G + Vector3.Distance(iNavCell.position, _currentPath.navCell.position);
                    _newPath.H = Vector3.Distance(iNavCell.position, _pEndIn);
                }

                if (!_cellsForExplore.Any(a => (_newPath = a).navCell == iNavCell))
                {
                    _newPath = new Path(); /*{ navCell = iNavCell, fromPath = _currentPath, G = _currentPath.G + Vector3.Distance(iNavCell.position, _currentPath.navCell.position), H = Vector3.Distance(iNavCell.position, _pEndIn)};*/
                    SetPathData();

                    AddForSorting(_newPath);
                }
                else if (_newPath.fromPath.G > _currentPath.G)
                {
                    SetPathData();
                    //_newPath.fromPath = _currentPath;
                    //_newPath.G = _currentPath.G + Vector3.Distance(iNavCell.position, _currentPath.navCell.position);
                }
            }
        }

        return new Vector3[0];
    }
    public static Vector3[] FPAAvoid(Vector3 _pStartIn, float _maxDist, NavUnitData _navUnitDataIn)
    {
        List<Path> _pathsToReturn = new List<Path>();
        HashSet<NavCell> _exploredNods = new HashSet<NavCell>();

        List<Path> _cellsForExplore = new List<Path>();
        void AddForSorting(Path _a)
        {
            //int _c = _cellsForExplore.FindIndex(i => (_a.G <= i.G ));
            //if (_c == -1)
            //    _c = _cellsForExplore.Count;

            //_cellsForExplore.Insert(_c, _a);
            _cellsForExplore.Add(_a);
            _pathsToReturn.Add(_a);
        }

        NavCell _startCell = GetClosetByYAxisNavCell(_pStartIn, _navUnitDataIn._width);
        if (_startCell == null)
            return new Vector3[0];

        _cellsForExplore.Add(new Path { navCell = _startCell, G = _startCell.isSeen360 * _startCell.distFromPlayerNormalized, H = _startCell.distFromPlayerNormalized, S = _startCell.isSeen360});

        float GetCost(Path _a)
        {
            return  (1.0f + _a.G) / (_a.D);
        }

        while (_cellsForExplore.Count > 0)
        {
            Path _currentPath = _cellsForExplore[0];

            _cellsForExplore.Remove(_currentPath);
            _exploredNods.Add(_currentPath.navCell);

            foreach (NavCell iNavCell in _currentPath.navCell.neighbors)
            {
                if (_exploredNods.Contains(iNavCell))
                    continue;

                if (!iNavCell.DoesHightFit(_navUnitDataIn._hight))
                {
                    _exploredNods.Add(iNavCell);
                    continue;
                }

                Path _newPath = null;
                void SetPathData()
                {
                    _newPath.navCell = iNavCell;
                    _newPath.fromPath = _currentPath;

                    
                    float _delta = iNavCell.distFromPlayerNormalized - _currentPath.navCell.distFromPlayerNormalized;
                    _newPath.H = iNavCell.distFromPlayerNormalized;
                    _newPath.G = iNavCell.isSeen360 * iNavCell.distFromPlayer;
                    _newPath.D = _currentPath.D + Vector3.Distance(iNavCell.position, _currentPath.navCell.position);
                    _newPath.S = _currentPath.S + iNavCell.isSeen360;
                    _newPath.C = _currentPath.C + 1;
                }

                if (!_cellsForExplore.Any(a => (_newPath = a).navCell == iNavCell))
                {
                    _newPath = new Path();
                    SetPathData();

                    if (_newPath.D < _maxDist)
                        AddForSorting(_newPath);
                }
                else if (GetCost(_newPath.fromPath) < GetCost(_currentPath))
                {
                    SetPathData();
                }
            }
        }

        Path _pathReturn = _pathsToReturn[0];
        foreach (Path i in _pathsToReturn)
        {
            if (_pathReturn.H < i.H)
                _pathReturn = i;
        }

        return _pathReturn.AccuratePath(_navUnitDataIn);
    }
    public static Vector3[] FPASneak(Vector3 _pStartIn, Vector3 _observer, NavUnitData _navUnitDataIn)
    {
        NavCell _endCell = GetClosetByYAxisNavCell(_observer , _navUnitDataIn._width);
        if (_endCell == null)
            return new Vector3[0];

        HashSet<NavCell> _exploredNods = new HashSet<NavCell>();

        NavCell _startCell = GetClosetByYAxisNavCell(_pStartIn, _navUnitDataIn._width);
        if (_startCell == null)
            return new Vector3[0];

        float GetCost(Path a)
        {
            return a.G * (1 + a.S);
        }

        List<Path> _cellsForExplore = new List<Path>();
        void AddForSorting(Path a)
        {
            int _c = _cellsForExplore.FindIndex(i => (GetCost(a) + a.H * a.C <= GetCost(i) + i.H * i.C));
            if (_c == -1)
                _c = _cellsForExplore.Count;

            _cellsForExplore.Insert(_c, a);
        }


        _cellsForExplore.Add(new Path { navCell = _startCell, S = 0, H = Vector3.Distance(_pStartIn, _endCell.position) });


        while (_cellsForExplore.Count > 0)
        {
            Path _currentPath = _cellsForExplore[0];

            if (_currentPath.navCell == _endCell)
            {
                return _currentPath.SmoothPath(_navUnitDataIn);
            }

            _cellsForExplore.Remove(_currentPath);
            _exploredNods.Add(_currentPath.navCell);

            foreach (NavCell iNavCell in _currentPath.navCell.neighbors)
            {
                if (_exploredNods.Contains(iNavCell))
                    continue;

                if (!iNavCell.DoesHightFit(_navUnitDataIn._hight))
                {
                    _exploredNods.Add(iNavCell);
                    continue;
                }

                Path _newPath = null;
                void SetPathData()
                {
                    _newPath.navCell = iNavCell;
                    _newPath.fromPath = _currentPath;
                    _newPath.S = _currentPath.S + (iNavCell.isSeen * 5 + iNavCell.isSeen360);
                    _newPath.C = _currentPath.C + 1;
                    _newPath.G = _currentPath.G + Vector3.Distance(iNavCell.position, _currentPath.navCell.position);
                    _newPath.H = Vector3.Distance(iNavCell.position, _endCell.position);
                }

                if (!_cellsForExplore.Any(a => (_newPath = a).navCell == iNavCell))
                {
                    _newPath = new Path();
                    SetPathData();

                    AddForSorting(_newPath);
                }
                else if (GetCost(_newPath.fromPath) > GetCost(_currentPath))
                {
                    SetPathData();
                }
            }
        }

        return new Vector3[0];
    }

    #endregion

    #region Nods status Update 
    [SerializeField] private CycleNodeUpdate _mainCycleNodeUpdate;
    public Transform Observer { get => _mainCycleNodeUpdate._observer; private set => _mainCycleNodeUpdate._observer = value; }

    [System.Serializable] private struct CycleNodeUpdate
    {
        [SerializeField] public Transform _observer;

        [SerializeField] private int _innerloopBatchCount;
        [SerializeField] private float _cycleDuration;
        private MonoBehaviour _monoBehavior;

        public void BakeWallMatrix(NavGridSettings _gridSettingIn, MonoBehaviour _monoBehaviorIn)
        {
            _monoBehavior = _monoBehaviorIn;

            Vector3 _boundPoint1 = new Vector3(_gridSettingIn._mapMesh.bounds.center.x + _gridSettingIn._mapMesh.bounds.extents.x, _gridSettingIn._mapMesh.bounds.center.y + _gridSettingIn._mapMesh.bounds.extents.y, _gridSettingIn._mapMesh.bounds.center.z + _gridSettingIn._mapMesh.bounds.extents.z);
            Vector3 _boundPoint2 = new Vector3(_gridSettingIn._mapMesh.bounds.center.x + -_gridSettingIn._mapMesh.bounds.extents.x, _gridSettingIn._mapMesh.bounds.center.y + -_gridSettingIn._mapMesh.bounds.extents.y, _gridSettingIn._mapMesh.bounds.center.z + -_gridSettingIn._mapMesh.bounds.extents.z);

            Vector3Int _xyzToBound1 = new Vector3Int((int)(_boundPoint1.x / _gridSettingIn._widthCell), (int)(_boundPoint1.y / _gridSettingIn._widthCell), (int)(_boundPoint1.z / _gridSettingIn._widthCell));
            Vector3Int _xyzToBound2 = new Vector3Int((int)(_boundPoint2.x / _gridSettingIn._widthCell), (int)(_boundPoint2.y / _gridSettingIn._widthCell), (int)(_boundPoint2.z / _gridSettingIn._widthCell));


            // Generate cells wall

            _wallMatrixKeys = new HashSet<Vector3Int>();

            for (int x = _xyzToBound2.x; x <= _xyzToBound1.x; x++)
                for (int y = _xyzToBound2.y; y <= _xyzToBound1.y; y++)
                    for (int z = _xyzToBound2.z; z <= _xyzToBound1.z; z++)
                    {
                        Vector3 _centerPosition = new Vector3(x, y, z) * _gridSettingIn._widthCell;

                        Vector3[] _directions = new Vector3[] { Vector3.forward, Vector3.back, Vector3.up, Vector3.down, Vector3.right, Vector3.left };

                        for (int i = 0; i < _directions.Length; i += 2)
                        {
                            RaycastHit _hit;
                            if (Physics.Linecast(_centerPosition + _directions[i] * _gridSettingIn._widthCell / 1.99f, _centerPosition + _directions[i + 1] * _gridSettingIn._widthCell / 2.0f, out _hit, _gridSettingIn._NavObstacleLayer + _gridSettingIn._NavMapLayer - _gridSettingIn._NavObstacleTransparentLayer))
                            {
                                _wallMatrixKeys.Add(new Vector3Int(x, y, z));
                                break;
                            }
                        }
                    }

            // Set Data

        }

        private static HashSet<Vector3Int> _wallMatrixKeys;
        private static bool CheckVisionRayToPoint(Vector3 _pObserver, NavCell _navCell)
        {
            Vector3Int _key1 = new Vector3Int(Mathf.RoundToInt(_pObserver.x / _gridSettings._widthCell), Mathf.RoundToInt(_pObserver.y / _gridSettings._widthCell), Mathf.RoundToInt(_pObserver.z / _gridSettings._widthCell));
            Vector3Int _key2 = new Vector3Int(Mathf.RoundToInt(_navCell.position.x / _gridSettings._widthCell), Mathf.RoundToInt(_navCell.position.y / _gridSettings._widthCell), Mathf.RoundToInt(_navCell.position.z / _gridSettings._widthCell));

            Vector3 _sP = _pObserver;
            Vector3 _eP = _navCell.position;

            int _deltaX = Math.Abs(_key1.x - _key2.x);
            int _deltaY = Math.Abs(_key1.y - _key2.y);
            int _deltaZ = Math.Abs(_key1.z - _key2.z);

            int _l = Math.Max(_deltaX, _deltaY);
            _l = Math.Max(_l, _deltaZ);

            if (_l == 0)
            {
                return true;
            }

            Vector3 _delta = new Vector3((_eP.x - _sP.x) / _l, (_eP.y - _sP.y) / _l, (_eP.z - _sP.z) / _l);
            Vector3 _next = new Vector3(_sP.x, _sP.y, _sP.z);

            _l++;
            for (; _l > 0; _l--)
            {
                Vector3Int _currentKey = new Vector3Int(Mathf.RoundToInt(_next.x / _gridSettings._widthCell), Mathf.RoundToInt(_next.y / _gridSettings._widthCell), Mathf.RoundToInt(_next.z / _gridSettings._widthCell));

                if (_wallMatrixKeys.Contains(_currentKey))
                    if (_currentKey.y != _key2.y)
                        return false;

                _next += _delta;
            }

            return true;
        }
        private struct IJobParallelVisionStatusUpdate : IJobParallelFor
        {
            public Vector3 _observerPositionIn;
            public Vector3 _observerForwardIn;

            public void Execute(int index)
            {
                Vector3Int _key = _gridData._fullKeysArray[index];

                NavCell _navCell = _gridData._navCellsDictionary[new Vector2Int(_key.x, _key.z)][_key.y];

                _navCell.isSeen360 = Convert.ToInt32(CheckVisionRayToPoint(_observerPositionIn, _navCell));
                _navCell.isSeen = _navCell.isSeen360 * (1 - Math.Clamp((int)(Vector3.Angle(_observerForwardIn, _navCell.position - _observerPositionIn) / 55.0f), 0 , 1));

            }
        }

        private static void DistanceFromObserverUpdate(Vector3 _pObserver, float _observerHight)
        {
            NavCell _startCell = GetClosetByYAxisNavCell(_pObserver);

            if (_startCell == null)
                return;

            List<Path> _cellsForExplore = new List<Path>();
            HashSet<NavCell> _exploredNods = new HashSet<NavCell>();

            _cellsForExplore.Add(new Path { navCell = _startCell, G = 0});

            float _maxDistance = 0;

            while (_cellsForExplore.Count > 0)
            {
                Path _currentPath = _cellsForExplore[0];

                _cellsForExplore.Remove(_currentPath);
                _exploredNods.Add(_currentPath.navCell);

                foreach (NavCell iNavCell in _currentPath.navCell.neighbors)
                {
                    if (_exploredNods.Contains(iNavCell))
                        continue;

                    if (!iNavCell.DoesHightFit(_observerHight))
                    {
                        _exploredNods.Add(iNavCell);
                        continue;
                    }

                    Path _newPath = null;
                    if (!_cellsForExplore.Any(a => (_newPath = a).navCell == iNavCell))
                    {
                        _newPath = new Path() { navCell = iNavCell, fromPath = _currentPath, G = _currentPath.G + Vector3.Distance(iNavCell.position, _currentPath.navCell.position)};
                    }
                    else if (_newPath.fromPath.G > _currentPath.G)
                    {
                        _newPath.fromPath = _currentPath;
                        _newPath.G = _currentPath.G + Vector3.Distance(iNavCell.position, _currentPath.navCell.position);
                    }

                    _newPath.navCell.distFromPlayer = _newPath.G;

                    _cellsForExplore.Remove(_newPath);
                    _cellsForExplore.Add(_newPath);

                    _maxDistance = Mathf.Max(_maxDistance, _newPath.G);
                }
            }

            foreach (NavCell i in _gridData._navCellsHashSet)
            {
                i.distFromPlayerNormalized = i.distFromPlayer / _maxDistance;
            }
        }

        public bool IsCycleUpdating { set { if (value) StartUpdate(); else StopUpdate(); } get => _cycleIsWork; }
        private bool _cycleIsWork;

        public void StopUpdate()
        {
            _cycleIsWork = false;
        }
        public async void StartUpdate()
        {
            if (_cycleIsWork)
            {
                Debug.LogError("Cycle is already started"); return;
            }

            if (_wallMatrixKeys == null)
            {
                Debug.LogError("Wall matrix wasn't bake"); return;
            }

            _cycleIsWork = true;

            IJobParallelVisionStatusUpdate _iJob = new IJobParallelVisionStatusUpdate();

            while (_cycleIsWork && _monoBehavior != null)
            {
                float _startTime = Time.realtimeSinceStartup;

                _iJob._observerPositionIn = _observer.position;
                _iJob._observerForwardIn = _observer.forward;

                JobHandle _jh = _iJob.Schedule(_gridData._fullKeysArray.Length, _innerloopBatchCount);

                Vector3 _pObserver = _observer.position;
                await Task.Run(() => DistanceFromObserverUpdate(_pObserver, 1.6f));

                await Task.Delay(Math.Clamp((int)((_cycleDuration - Time.realtimeSinceStartup + _startTime) / 0.001f), 0, int.MaxValue));
                while (!_jh.IsCompleted)
                    await Task.Yield();

                _jh.Complete();
            }
        }

        public void ShowWall() {
            foreach (Vector3Int i in _wallMatrixKeys) {
                Gizmos.color = Color.blue;
                Gizmos.DrawCube(new Vector3(i.x, i.y, i.z) * _gridSettings._widthCell, Vector3.one * _gridSettings._widthCell);
                //Debug.DrawRay(new Vector3(i.x, i.y, i.z) * _gridSettings._widthCell, Vector3.up, Color.red, 20.0f);
            }
        }
    }

    #endregion

    public static AINavGrid main;
    public void Awake()
    {
        if (main == null)
            main = this;
        else
            Destroy(this);

        BakeGrid();
        _mainCycleNodeUpdate.BakeWallMatrix(_gridSettings, this);
        _mainCycleNodeUpdate.StartUpdate();
       // StartCoroutine(ShowVision());
    }

    public IEnumerator ShowVision() {

        while (true) {
            foreach (NavCell i in _gridData._navCellsHashSet)
            {
                Vector3 _p1 = i.position + new Vector3(_gridSettings._widthCell / 2, 0, _gridSettings._widthCell / 2),
                        _p2 = i.position + new Vector3(_gridSettings._widthCell / 2, 0, -_gridSettings._widthCell / 2),
                        _p3 = i.position + new Vector3(-_gridSettings._widthCell / 2, 0, -_gridSettings._widthCell / 2),
                        _p4 = i.position + new Vector3(-_gridSettings._widthCell / 2, 0, _gridSettings._widthCell / 2);

                Color _color = Color.green;

                if (i.isSeen360 == 1)
                {
                    _color = Color.yellow;
                }
                if (i.isSeen == 1)
                {
                    _color = Color.red;
                }

                if (_color == Color.green) {
                    continue;
                }

                

                Debug.DrawLine(_p1, _p2, _color, 0.01f);
                Debug.DrawLine(_p2, _p3, _color, 0.01f);
                Debug.DrawLine(_p3, _p4, _color, 0.01f);
                Debug.DrawLine(_p4, _p1, _color, 0.01f);
            }

            yield return new WaitForSeconds(0.005f);
            yield return null;
        }
    }

    public void OnDrawGizmosSelected()
    {
       // _mainCycleNodeUpdate.ShowWall();
    }
}


