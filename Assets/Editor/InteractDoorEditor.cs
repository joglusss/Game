using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(InteractDoor))]
public class InteractDoorEditor : Editor
{
    private InteractDoor _t;
    private Vector3[] _mesh;
    private Vector3 _axis;
    private int _axisDir;

    private void Awake()
    {
        _t = target as InteractDoor;
        _mesh = _t.transform.GetComponent<MeshFilter>().sharedMesh.vertices;
    }

    public void OnSceneGUI()
    {
        switch (_t._axisEnum)
        {
            case InteractDoor.Aixs.X: _axis = Vector3.right; break;
            case InteractDoor.Aixs.Y: _axis = Vector3.up; break;
            case InteractDoor.Aixs.Z: _axis = Vector3.forward; break;
            case InteractDoor.Aixs.NX: _axis = Vector3.right; break;
            case InteractDoor.Aixs.NY: _axis = Vector3.up; break;
            case InteractDoor.Aixs.NZ: _axis = Vector3.forward; break;
        }

        switch (_t._directionEnum)
        {
            case InteractDoor.Aixs.X: _axisDir = 1; break;
            case InteractDoor.Aixs.Y: _axisDir = 1; break;
            case InteractDoor.Aixs.Z: _axisDir = 1; break;
            case InteractDoor.Aixs.NX: _axisDir = -1; break;
            case InteractDoor.Aixs.NY: _axisDir = -1; break;
            case InteractDoor.Aixs.NZ: _axisDir = -1; break;
        }


        if (_t._doorAngle < _t._limit.x || _t._doorAngle > _t._limit.y)
            Handles.color = Color.red;
        else
            Handles.color = Color.green;
        Handles.matrix = Matrix4x4.TRS(_t.transform.position, _t.transform.rotation * Quaternion.Euler(_axis * _t._doorAngle * _axisDir), _t.transform.localScale);
        Handles.DrawAAPolyLine(_mesh);
    }


}
