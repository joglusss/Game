using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshCombiner : MonoBehaviour
{
    [SerializeField] private bool _addMesh = false;

    public void Start()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];

        List<Material> materials = new List<Material>();

        int i = 0;
        while (i < meshFilters.Length)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            meshFilters[i].gameObject.SetActive(false);

            // Destroy(meshFilters[i].gameObject);

            MeshRenderer tempMeshRenderer = meshFilters[i].GetComponent<MeshRenderer>();
            foreach (Material m in tempMeshRenderer.sharedMaterials)
                if (!materials.Contains(m) && m != null)
                    materials.Add(m);

            i++;
        }
        transform.GetComponent<MeshFilter>().mesh = new Mesh();
        transform.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);
        transform.gameObject.SetActive(true);

        GetComponent<MeshRenderer>().sharedMaterials = materials.ToArray();
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        if(_addMesh)
            gameObject.AddComponent<MeshCollider>();
    }
}


