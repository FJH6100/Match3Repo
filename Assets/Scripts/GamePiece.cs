using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePiece : MonoBehaviour
{
    private int x;
    public int X
    {
        get { return x; }
        set
        {
            if (IsMovable())
                x = value;
        }
    }

    private int y;
    public int Y
    {
        get { return y; }
        set
        {
            if (IsMovable())
                y = value;
        }
    }

    private Grid.PieceType type;
    public Grid.PieceType Type
    {
        get { return type; }
    }

    private Grid grid;
    public Grid GridRef
    {
        get { return grid; }
    }

    private MovablePiece movable;
    public MovablePiece Movable
    {
        get { return movable; }
    }

    private ClearablePiece clearable;
    public ClearablePiece Clearable
    {
        get { return clearable; }
    }

    private ColorPiece color;
    public ColorPiece Color
    {
        get { return color; }
    }

    void Awake()
    {
        movable = GetComponent<MovablePiece>();
        color = GetComponent<ColorPiece>();
        clearable = GetComponent<ClearablePiece>();
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            grid.mousePressed = true;
        }

    }

    public void Init(int _x, int _y, Grid _grid, Grid.PieceType _type)
    {
        x = _x;
        y = _y;
        grid = _grid;
        type = _type;
    }

    private void OnMouseDown()
    {
        grid.PressPiece(this);
    }

    private void OnMouseEnter()
    {
        if (grid.mousePressed)
        {
            grid.EnterPiece(this);
            grid.mousePressed = false;
        }
        //else
        //    Debug.Log("Mouse not held.");
    }

    private void OnMouseUp()
    {
        grid.ReleasePiece();
        grid.mousePressed = false;
    }


    public bool IsMovable()
    {
        return movable != null;
    }

    public bool IsColored()
    {
        return color != null;
    }

    public bool IsClearable()
    {
        return clearable != null;
    }
}
