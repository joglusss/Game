using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lamp : MonoBehaviour
{
    [SerializeField] private string _nameEmission = "_EmissionColor";
    [SerializeField] private Light _light;

    private Material _material;
    [SerializeField] private Color _emissionColor = Color.white;

    private void Awake()
    {
        _material = GetComponent<MeshRenderer>().material;
        
        if (_light == null)
            _light = FindObjectOfType<Light>();

        //_emissionColor = _material.GetColor(_nameEmission);

        if(!_light.enabled)
            _material.SetColor(_nameEmission, Color.black);
        else
            _material.SetColor(_nameEmission, _emissionColor);
    }

    public void Switch()
    {
        if (_light.enabled)
        {
            _material.SetColor(_nameEmission, Color.black);
            _light.enabled = false;
        }
        else
        {
            _material.SetColor(_nameEmission, _emissionColor);
            _light.enabled = true;
        }

    }
}
