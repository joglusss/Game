using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace MyEffects
{
    public class EffectsManager : MonoBehaviour
    {
        #region Effect
        [System.Serializable] private class EffectStruct
        {
            public string _ID;
            public int _listEffectSize;
            public GameObject _effectPrefab;


            public void Init()
            {
                _currentIndex = 0;

                Time = (int)(_effectPrefab.GetComponent<ParticleSystem>().main.duration * 1000);

                _effectArray = new GameObject[_listEffectSize];
                for (int i = 0; i < _effectArray.Length; i++)
                {
                    _effectArray[i] = Instantiate(_effectPrefab);
                    _effectArray[i].transform.SetParent(EffectsManager.Main.transform);
                    _effectArray[i].SetActive(false);
                }
            }

            public int Time { get; private set; }

            private int _currentIndex;
            private int CurrentIndex { get => _currentIndex; set => _currentIndex = Mathf.RoundToInt(Mathf.Repeat(value, _effectArray.Length)); }
            private GameObject[] _effectArray;

            public GameObject GetEffect()
            {
                CurrentIndex++;
                return _effectArray[CurrentIndex];
            }
        }
        [Header("Effect")]
        [SerializeField] private EffectStruct[] _effectClassDictionary;

        private static Dictionary<string, EffectStruct> _effectStructDictionary;
        public static async void PlayEffect(string _ID, Vector3 _position, Quaternion _rotation)
        {
            GameObject _temp = _effectStructDictionary[_ID].GetEffect();
            _temp.transform.SetPositionAndRotation(_position, _rotation);
            _temp.SetActive(true);

            await Task.Delay(_effectStructDictionary[_ID].Time);

            _temp.SetActive(false);

        }
        #endregion

        #region ImpactSound
        [System.Serializable]
        private class SoundClass
        {
            public string _ID;
            public AudioClip _sound;
        }

        [Header("Sound")]
        [SerializeField] private int _countAudioSource;
        [SerializeField] private GameObject _audioSourcePrefab;
        [SerializeField] private SoundClass[] _soundClassArray;


        private static AudioSource[] _audioSourceArray;
        private static int _currentAudioSourceIndex;
        private static int CurrentAudioSourceIndex { get => _currentAudioSourceIndex; set => _currentAudioSourceIndex = Mathf.RoundToInt(Mathf.Repeat(value, _audioSourceArray.Length)); }

        private static Dictionary<string, SoundClass> _soundClassDictionary;
        public async static void PlayImpactSound(string _ID, Vector3 _position)
        {
            if (!_soundClassDictionary.ContainsKey(_ID))
                return;

            AudioSource _audioSourceTemp = null;
            for (int i = 0; i < _audioSourceArray.Length; i++)
            {
                _audioSourceTemp = _audioSourceArray[_currentAudioSourceIndex];
                if (_audioSourceTemp.gameObject.activeSelf)
                    CurrentAudioSourceIndex++;
                else
                    break;
            }

            _audioSourceTemp.clip = _soundClassDictionary[_ID]._sound;
            _audioSourceTemp.transform.position = _position;

            _audioSourceTemp.gameObject.SetActive(true);

            await Task.Delay((int)(_audioSourceTemp.clip.length * 1000));

            _audioSourceTemp.gameObject.SetActive(false);
        }
        public async static void PlayImpactSound(string _ID, Transform _parent, Vector3 _position)
        {
            if (!_soundClassDictionary.ContainsKey(_ID))
                return;

            AudioSource _audioSourceTemp = null;
            for (int i = 0; i < _audioSourceArray.Length; i++)
            {
                _audioSourceTemp = _audioSourceArray[_currentAudioSourceIndex];
                if (_audioSourceTemp.gameObject.activeSelf)
                    CurrentAudioSourceIndex++;
                else
                    break;
            }

            _audioSourceTemp.clip = _soundClassDictionary[_ID]._sound;
            _audioSourceTemp.transform.position = _position;

            _audioSourceTemp.transform.SetParent(_parent);
            _audioSourceTemp.gameObject.SetActive(true);

            await Task.Delay((int)(_audioSourceTemp.clip.length * 1000));

            _audioSourceTemp.transform.SetParent(Main.transform);
            _audioSourceTemp.gameObject.SetActive(false);
        }

        public async static void PlaySound(AudioClip _clip, Vector3 _position)
        {
            AudioSource _audioSourceTemp = null;
            for (int i = 0; i < _audioSourceArray.Length; i++)
            {
                if(_audioSourceArray.Length >= _currentAudioSourceIndex)
                    CurrentAudioSourceIndex++;
                _audioSourceTemp = _audioSourceArray[_currentAudioSourceIndex];
                if (_audioSourceTemp.gameObject.activeSelf)
                   CurrentAudioSourceIndex++;
                else
                    break;
            }

            _audioSourceTemp.clip = _clip;
            _audioSourceTemp.transform.position = _position;

            _audioSourceTemp.gameObject.SetActive(true);

            await Task.Delay((int)(_clip.length * 1000));

            _audioSourceTemp.gameObject.SetActive(false);
        }
        public async static void PlaySound(AudioClip _clip, Transform _parent, Vector3 _position)
        {
            AudioSource _audioSourceTemp = null;
            for (int i = 0; i < _audioSourceArray.Length; i++)
            {
                if (_audioSourceArray.Length >= _currentAudioSourceIndex)
                    CurrentAudioSourceIndex++;
                _audioSourceTemp = _audioSourceArray[_currentAudioSourceIndex];
                if (_audioSourceTemp.gameObject.activeSelf)
                    CurrentAudioSourceIndex++;
                else
                    break;
            }

            _audioSourceTemp.clip = _clip;
            _audioSourceTemp.transform.position = _position;

            _audioSourceTemp.transform.SetParent(_parent);
            _audioSourceTemp.gameObject.SetActive(true);

            await Task.Delay((int)(_clip.length * 1000));

            _audioSourceTemp.transform.SetParent(Main.transform);
            _audioSourceTemp.gameObject.SetActive(false);
        }

        #endregion

        #region Bullet
        [Header("Bullet")]
        [SerializeField] private int _countBullet;
        [SerializeField] private GameObject _bulletPrefab;

        private static int _currentBulletIndex;
        private static int CurrentBullletIndex { get => _currentAudioSourceIndex; set => _currentAudioSourceIndex = Mathf.RoundToInt(Mathf.Repeat(value, _bulletArray.Length)); }
        private static Bullet[] _bulletArray;
        public async static void ShootBullet(Vector3 _position, Vector3 _forward,float _damage, float _armorPenetration)
        {
            Bullet _bullet = null;

            for (int i = 0; i < _bulletArray.Length; i++)
            {
                if ((_bullet = _bulletArray[CurrentBullletIndex]).gameObject.activeSelf)
                    CurrentBullletIndex++;
                else
                    break;
            }

            _bullet._damage = _damage;
            _bullet._armorPenetration = _armorPenetration;
            _bullet.transform.position = _position;
            _bullet.gameObject.SetActive(true);
            _bullet._rigidbody.AddForce(_forward * 2000.0f);

            await Task.Delay(2000);

            _bullet._rigidbody.velocity = Vector3.zero;
            _bullet.gameObject.SetActive(false);

        }
        #endregion

        #region Decals
        [System.Serializable]private class DecalClass
        {
            public PhysicMaterial _material;
            [Space]
            public int _count;
            public GameObject _decal;

            public void Init()
            {
                _array = new Transform[_count];
                for (int i = 0; i < _array.Length; i++)
                {
                    _array[i] = Instantiate(_decal).transform;
                    _array[i].SetParent(Main.transform);
                    _array[i].gameObject.SetActive(false);
                }
            }

            private int _currentIndex;
            private Transform[] _array;
            public Transform GetDecal()
            {
                _array[_currentIndex].gameObject.SetActive(false);
                _currentIndex = Mathf.RoundToInt(Mathf.Repeat(_currentIndex + 1, _array.Length));
                return _array[_currentIndex];
            }
        }
        [SerializeField] private DecalClass[] _decalArray;

        private static Dictionary<string, DecalClass> _decalDictionary;
        public static void RenderDecal(string _materialName, Vector3 _position, Vector3 _forward)
        {
            Debug.Log(_materialName);
            if (!_decalDictionary.ContainsKey(_materialName))
                return;

            Transform _temp = _decalDictionary[_materialName].GetDecal();
            _temp.position = _position;
            _temp.forward = _forward;
            _temp.gameObject.SetActive(true);
        }
        #endregion

        public static EffectsManager Main { get; private set; }
        private void Awake()
        {
            if (Main == null)
                Main = this;
            else
                Destroy(this);

            _effectStructDictionary = new Dictionary<string, EffectStruct>();
            for (int i = 0; i < _effectClassDictionary.Length; i++)
            {
                _effectStructDictionary.Add(_effectClassDictionary[i]._ID, _effectClassDictionary[i]);
                _effectClassDictionary[i].Init();
            }

            _soundClassDictionary = new Dictionary<string, SoundClass>();
            for (int i = 0; i < _soundClassArray.Length; i++)
            {
                _soundClassDictionary.Add(_soundClassArray[i]._ID, _soundClassArray[i]);
            }

            _audioSourceArray = new AudioSource[_countAudioSource];
            for(int i = 0; i < _countAudioSource; i++)
            {
                _audioSourceArray[i] = Instantiate(_audioSourcePrefab).GetComponent<AudioSource>();
                _audioSourceArray[i].transform.SetParent(this.transform);
                _audioSourceArray[i].gameObject.SetActive(false);
            }

            _bulletArray = new Bullet[_countBullet];
            for (int i = 0; i < _countBullet; i++)
            {
                _bulletArray[i] = Instantiate(_bulletPrefab).GetComponent<Bullet>();
                _bulletArray[i].gameObject.SetActive(false);
                _bulletArray[i].transform.SetParent(this.transform);

            }

            _decalDictionary = new Dictionary<string, DecalClass>();
            for (int i = 0; i < _decalArray.Length; i++)
            {
                _decalArray[i].Init();
                _decalDictionary.Add(_decalArray[i]._material.name, _decalArray[i]);
            }
        }
    }
}

