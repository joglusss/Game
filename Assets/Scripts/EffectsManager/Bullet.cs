using UnityEngine;
using MyEffects;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Bullet : MonoBehaviour
{
    public float _damage;
    public float _armorPenetration;
    public Rigidbody _rigidbody;

    private void Awake()
    {
        _rigidbody = this.GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {

       //HitTrigger _hitTrigger = null;
       // collision.collider.TryGetComponent(out _hitTrigger);
       // if (_hitTrigger != null)
       //     _hitTrigger.Hit(_damage, _armorPenetration);

       // if (collision.collider.sharedMaterial != null)
       //     EffectsManager.RenderDecal(collision.collider.sharedMaterial.name, collision.contacts[0].point, collision.contacts[0].normal);
       // _rigidbody.velocity = Vector3.zero;
       // this.gameObject.SetActive(false);
    }
    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("tingger");
        //HitTrigger _hitTrigger = null;
        //other.TryGetComponent(out _hitTrigger);
        //if (_hitTrigger != null)
        //{
        //    _hitTrigger.Hit(_damage, _armorPenetration);
        //    _rigidbody.velocity = Vector3.zero;
        //    this.gameObject.SetActive(false);
        //}
    }
}
