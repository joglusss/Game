using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class Container : InteractContainer, IContainerWithUIItem
{
    private static Image _markImage;

    private Image _gridImage;

    private Vector2Int _gridSize;
    private bool[,] _occupiedCells;

    private Vector2 GridCornerPosition { get{ return (Vector2)_gridImage.rectTransform.position + -_gridImage.rectTransform.sizeDelta * _gridImage.rectTransform.pivot * InventoryData.ScaleRelative; }}

    private Vector2Int KeyCell(Vector2 _position)
    {
        return Vector2Int.FloorToInt((_position - GridCornerPosition) / (InventoryData.CellSize * InventoryData.ScaleRelative));
    }

    private Vector2 CornerCell(Vector2Int _key)
    {
        return GridCornerPosition + (Vector2)_key * InventoryData.CellSize * InventoryData.ScaleRelative;
    }

    
    private void Awake()
    {
        if (_markImage == null)
        {
            _markImage = new GameObject().AddComponent<Image>();
            _markImage.name = "MarkImage";
            ClearMarkCells();
        }

        Type = TypeInteract.Put;

        _gridImage = GetComponent<Image>();
        _gridImage.sprite = InventoryData.Main._gridCellSprite;
        _gridImage.type = Image.Type.Tiled;
        _gridImage.pixelsPerUnitMultiplier = InventoryData.Main._gridCellSprite.texture.width / InventoryData.CellSize;

        _occupiedCells = new bool[(int)(_gridImage.rectTransform.sizeDelta.x / InventoryData.CellSize), (int)(_gridImage.rectTransform.sizeDelta.y / InventoryData.CellSize)];
        _gridSize = new Vector2Int((int)(_gridImage.rectTransform.sizeDelta.x / InventoryData.CellSize), (int)(_gridImage.rectTransform.sizeDelta.y / InventoryData.CellSize));
    }

    public void DrawMarkCells(Vector2Int _size, Vector2 _position)
    {
        _markImage.gameObject.SetActive(true);
        _markImage.rectTransform.SetParent(_gridImage.transform);

        _markImage.rectTransform.sizeDelta = (Vector2)_size * InventoryData.CellSize;
        _markImage.rectTransform.localScale = _gridImage.rectTransform.localScale;

        Vector2Int _startKey = KeyCell(_position);

        if (_startKey.x < 0 || _startKey.y < 0 || _startKey.x + _size.x > _gridSize.x || _startKey.y + _size.y > _gridSize.y)
            _markImage.color = new Color(0.3f, 0.14f, 0.13f, 0.5f);
        else
        {
            for (int x = _startKey.x; x < _startKey.x + _size.x; x++)
                for (int y = _startKey.x; y < _startKey.y + _size.y; y++)
                    if (_occupiedCells[x, y])
                    {
                        _markImage.color = new Color(0.3f, 0.14f, 0.13f, 0.5f);
                        goto outCycle;
                    }

            _markImage.color = new Color(0.12f, 0.25f, 0.1f, 0.5f);
        } 

        outCycle:
        _markImage.rectTransform.position = CornerCell(_startKey) + _markImage.rectTransform.pivot * _markImage.rectTransform.sizeDelta * InventoryData.ScaleRelative;
    }
    public void ClearMarkCells()
    {
        _markImage.gameObject.SetActive(false);
    }

    public override bool IsFit(ItemBase _item)
    {

        Vector2 _startPosition = _item.CornerPosition;
        Vector2Int _size = _item.Size;

        Debug.DrawRay(_startPosition, Vector3.up, Color.red);

        Vector2Int _startCell = KeyCell(_startPosition);

        if (_startCell.x < 0 || _startCell.x + _size.x > _gridSize.x || _startCell.y < 0 || _startCell.y + _size.y > _gridSize.y)
            return false;
        else
            for (int x = _startCell.x; x < _startCell.x + _size.x; x++)
                for (int y = _startCell.y; y < _startCell.y + _size.y; y++)
                    if (_occupiedCells[x, y] == true)
                        return false;
        return true;
    }

    public override void Put(ItemBase _item)
    {
        Vector2 _startPosition = _item.CornerPosition;
        Vector2Int _size = _item.Size;

        Vector2Int _startCell = KeyCell(_startPosition);
       
        for (int x = _startCell.x; x < _startCell.x + _size.x; x++)
            for (int y = _startCell.y; y < _startCell.y + _size.y; y++)
                _occupiedCells[x, y] = true;

        _item.transform.SetParent(this.transform);
        _item.transform.position = CornerCell(_startCell) + _item.OffsetToCorner;

    }
    public void ClearContanerCell(ItemBase _item)
    {
        Vector2 _startPosition = _item.CornerPosition;
        Vector2Int _size = _item.Size;

        Vector2Int _startCell = KeyCell(_startPosition);

        for (int x = _startCell.x; x < _startCell.x + _size.x; x++)
            for (int y = _startCell.y; y < _startCell.y + _size.y; y++)
                _occupiedCells[x, y] = false;
    }
}
