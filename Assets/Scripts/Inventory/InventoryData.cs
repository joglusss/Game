using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryData : MonoBehaviour
{
    public static InventoryData Main { get; private set; }
    public static Transform MainDrop { get; private set; }
    public static Canvas MainCanvas { get; private set; }
    public const float CellSize = 25.0f;
    public static Vector3 ScaleRelative { get => MainCanvas.transform.localScale; }

    public Sprite _gridCellSprite;

    #region TestDictionary
    [SerializeField] private List<ItemDataBase> _listItemData;

    private Dictionary<string, ItemDataBase> _dictionaryItemData;
    #endregion

    public ItemBase GetNewItem(string ID)
    {
        GameObject _temp = Instantiate(_dictionaryItemData[ID].DropPrefab);
        return _temp.GetComponent<ItemBase>();
    }

    public GameObject GetHandPrefab(string ID, ItemBase _itemBase)
    {
        _dictionaryItemData[ID].HandPrefab.SetActive(false);
        GameObject _HandPrefab = _dictionaryItemData[ID].HandPrefab != null ? Instantiate(_dictionaryItemData[ID].HandPrefab) : new GameObject();

        _HandPrefab.GetComponent<HIBase>().ItemBaseLink = _itemBase;

        return _HandPrefab;
    }

    public Mesh GetItemMeshModel(string ID)
    {
        if (_dictionaryItemData.ContainsKey(ID))
            return Instantiate(_dictionaryItemData[ID].MeshModel);
        else
            return null;
    }
    public Material GetItemMaterialModel(string ID)
    {
        if (_dictionaryItemData.ContainsKey(ID))
            return Instantiate(_dictionaryItemData[ID].MaterialModel);
        else
            return null; 
    }


    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Debug.LogWarning("There are more one InventoryData scripts");

        if (!(MainCanvas = this.GetComponent<Canvas>()))
            Debug.Log("There are no Canvas");

        MainDrop = new GameObject(){ name = "MainDrop" }.transform;

        _dictionaryItemData = new Dictionary<string, ItemDataBase>();
        foreach (ItemDataBase i in _listItemData)
            _dictionaryItemData.Add(i.ID,i);
    }

    
}
