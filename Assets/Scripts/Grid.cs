using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Grid : MonoBehaviour
{
    public enum PieceType
    {
        EMPTY,
        NORMAL,
        OBSTACLE,
        FOX,
        PAW,
        ROW_CLEAR,
        COLUMN_CLEAR,
        ALL_CLEAR,
        COUNT
    };
    public int xDim;
    public int yDim;
    [System.Serializable]
    public struct PiecePrefab
    {
        public PieceType type;
        public GameObject prefab;
    }
    private Dictionary<PieceType, GameObject> piecePrefabDict;
    public PiecePrefab[] piecePrefabs;
    public GameObject bgPrefab;
    public GameObject pathPrefab;
    private GamePiece[,] pieces;
    private GameObject[,] bgArray;
    public float fillTime;
    private bool inverse = false;
    [System.Serializable]
    public struct PathValues
    {
        public int xValue;
        public int yValue;
    }
    [System.Serializable]
    public struct ObstacleValues
    {
        public int xValue;
        public int yValue;
    }
    public PathValues[] pathValues;
    public ObstacleValues[] obstacleValues;
    public List<KeyValuePair<int,int>> myPath;

    private GamePiece pressedPiece;
    private GamePiece enteredPiece;
    private Color32 bgColor = new Color32(128, 128, 128, 255);
    private Color32 pathColor = new Color32(70, 255, 255, 255);

    private int currentFoxLocation = 0;

    [HideInInspector]
    public bool mousePressed = false;

    // Start is called before the first frame update
    void Start()
    {
        myPath = new List<KeyValuePair<int, int>>();
        foreach (PathValues pv in pathValues)
            myPath.Add(new KeyValuePair<int, int>(pv.xValue, pv.yValue));
        piecePrefabDict = new Dictionary<PieceType, GameObject>();
        foreach (PiecePrefab p in piecePrefabs)
        {
            if (!piecePrefabDict.ContainsKey(p.type))
                piecePrefabDict.Add(p.type,p.prefab);
        }

        bgArray = new GameObject[xDim, yDim];
        for (int x = 0; x < xDim; x++)
        {
            for (int y = 0; y < yDim; y++)
            {
                GameObject background;
                if (myPath.Contains(new KeyValuePair<int, int>(x, y)))
                {
                    background = (GameObject)Instantiate(pathPrefab, GetWorldPosition(x, y, 0f), Quaternion.identity);
                    bgArray[x, y] = background;
                    continue;
                }

                background = (GameObject)Instantiate(bgPrefab, GetWorldPosition(x, y, 0f), Quaternion.identity);
                background.transform.parent = transform;
                bgArray[x, y] = background;
            }
        }

        pieces = new GamePiece[xDim,yDim];
        for (int x = 0; x < xDim; x++)
        {
            for (int y = 0; y < yDim; y++)
            {
                SpawnNewPiece(x, y, PieceType.EMPTY);
            }
        }
        Destroy(pieces[myPath[0].Key,myPath[0].Value].gameObject);
        SpawnNewPiece(myPath[0].Key, myPath[0].Value, PieceType.FOX);

        foreach (ObstacleValues ov in obstacleValues)
        {
            Destroy(pieces[ov.xValue, ov.yValue].gameObject);
            SpawnNewPiece(ov.xValue, ov.yValue, PieceType.OBSTACLE);
        }

        StartCoroutine(Fill());
    }

    public IEnumerator Fill()
    {
        bool needsRefill = true;
        while (needsRefill)
        {
            yield return new WaitForSeconds(fillTime);
            while (FillStep())
            {
                inverse = !inverse;
                yield return new WaitForSeconds(fillTime);
            }
            needsRefill = ClearAllValidMatches();
        }
    }

    public bool FillStep()
    {
        bool movedPiece = false;
        for (int y = yDim - 2; y >= 0; y--)
        {
            for (int loopX = 0; loopX < xDim; loopX++)
            {
                int x = loopX;
                if (inverse)
                    x = xDim - 1 - loopX;
                GamePiece piece = pieces[x, y];
                if (piece.IsMovable())
                {
                    GamePiece pieceBelow = pieces[x, y + 1];
                    if (pieceBelow.Type == PieceType.EMPTY && piece.Type != PieceType.FOX)
                    {
                        Destroy(pieceBelow.gameObject);
                        piece.Movable.Move(x, y + 1, fillTime);
                        pieces[x, y + 1] = piece;
                        SpawnNewPiece(x, y, PieceType.EMPTY);
                        movedPiece = true;
                    }
                    else
                    {
                        for (int diag = -1; diag <= 1; diag++)
                        {
                            if (diag != 0)
                            {
                                int diagX = x + diag;

                                if (inverse)
                                {
                                    diagX = x - diag;
                                }

                                if (diagX >= 0 && diagX < xDim)
                                {
                                    GamePiece diagonalPiece = pieces[diagX, y + 1];

                                    if (diagonalPiece.Type == PieceType.EMPTY)
                                    {
                                        bool hasPieceAbove = true;

                                        for (int aboveY = y; aboveY >= 0; aboveY--)
                                        {
                                            GamePiece pieceAbove = pieces[diagX, aboveY];

                                            if (pieceAbove.IsMovable() && pieceAbove.Type != PieceType.FOX)
                                            {
                                                break;
                                            }
                                            else if ((!pieceAbove.IsMovable() && pieceAbove.Type != PieceType.EMPTY) || pieceAbove.Type == PieceType.FOX)
                                            {
                                                hasPieceAbove = false;
                                                break;
                                            }
                                        }

                                        if (!hasPieceAbove)
                                        {
                                            Destroy(diagonalPiece.gameObject);
                                            piece.Movable.Move(diagX, y + 1, fillTime);
                                            pieces[diagX, y + 1] = piece;
                                            SpawnNewPiece(x, y, PieceType.EMPTY);
                                            movedPiece = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        for (int x = 0; x < xDim; x++)
        {
            GamePiece pieceBelow = pieces[x, 0];
            if (pieceBelow.Type == PieceType.EMPTY)
            {
                PieceType type;
                int Rand = Random.Range(0, 15);
                if (Rand == 0)
                    type = PieceType.PAW;
                else
                    type = PieceType.NORMAL;
                Destroy(pieceBelow.gameObject);
                GameObject newPiece = (GameObject)Instantiate(piecePrefabDict[type], GetWorldPosition(x, -1, -1), Quaternion.identity);
                newPiece.transform.parent = transform;

                pieces[x, 0] = newPiece.GetComponent<GamePiece>();
                pieces[x, 0].Init(x, -1, this, type);
                pieces[x, 0].Movable.Move(x, 0, fillTime);
                pieces[x, 0].Color.SetColor(Random.Range(0, pieces[x, 0].Color.numColors));
                movedPiece = true;
            }
        }
        return movedPiece;
    }
    public Vector3 GetWorldPosition(int x, int y, float z)
    {
        return new Vector3(transform.position.x - xDim / 2f + x + .5f, transform.position.y + yDim / 2f - y - .5f, z);
    }

    public GamePiece SpawnNewPiece(int x, int y, PieceType type)
    {
        Quaternion rot;
        if (type == PieceType.ROW_CLEAR)
            rot = Quaternion.Euler(0, 0, 90);
        else
            rot = Quaternion.identity;
        GameObject newPiece = (GameObject)Instantiate(piecePrefabDict[type], GetWorldPosition(x, y, -1), rot);
        newPiece.name = "Piece(" + x + "," + y + ")";
        newPiece.transform.parent = transform;

        pieces[x, y] = newPiece.GetComponent<GamePiece>();
        pieces[x, y].Init(x, y, this, type);

        return pieces[x, y];
    }

    public bool IsAdjacent(GamePiece piece1, GamePiece piece2)
    {
        int absY = (int)Mathf.Abs(piece1.Y - piece2.Y);
        int absX = (int)Mathf.Abs(piece1.X - piece2.X);
        return (piece1.X == piece2.X && absY == 1)
            || (piece1.Y == piece2.Y && absX == 1);
    }

    public void SwapPieces(GamePiece piece1, GamePiece piece2)
    {
        if (piece1.IsMovable() && piece2.IsMovable())
        {
            pieces[piece1.X, piece1.Y] = piece2;
            pieces[piece2.X, piece2.Y] = piece1;

            if ((GetMatch(piece1, piece2.X, piece2.Y) != null || GetMatch(piece2, piece1.X, piece1.Y) != null) || piece1.Type == PieceType.FOX
                || piece1.Type == PieceType.ALL_CLEAR || piece2.Type == PieceType.ALL_CLEAR)
            {
                int piece1X = piece1.X;
                int piece1Y = piece1.Y;

                piece1.Movable.Move(piece2.X, piece2.Y, fillTime);
                piece2.Movable.Move(piece1X, piece1Y, fillTime);

                if (piece1.Type == PieceType.ALL_CLEAR && piece1.IsClearable() && piece2.IsColored())
                {
                    ClearPiece(piece1.X, piece1.Y);
                    ClearColor(piece2.GetComponent<SpriteRenderer>().color);
                }
                else if (piece2.Type == PieceType.ALL_CLEAR && piece2.IsClearable() && piece1.IsColored())
                {
                    ClearPiece(piece2.X, piece2.Y);
                    ClearColor(piece1.GetComponent<SpriteRenderer>().color);
                }

                ClearAllValidMatches();

                StartCoroutine(Fill());
            }
            else
            {
                pieces[piece1.X, piece1.Y] = piece1;
                pieces[piece2.X, piece2.Y] = piece2;
            }
        }
    }

    public void PressPiece(GamePiece piece)
    {
        if (pressedPiece == null)
        {
            pressedPiece = piece;
            bgArray[piece.X, piece.Y].GetComponent<SpriteRenderer>().color = Color.yellow;
        }
        else
        {
            enteredPiece = piece;
            ReleasePiece();
        }
    }

    public void EnterPiece(GamePiece piece)
    {
        enteredPiece = piece;
    }

    public void ReleasePiece()
    {
        if (enteredPiece != null && pressedPiece != null)
        {
            if (pressedPiece != enteredPiece)
            {
                if (myPath.Contains(new KeyValuePair<int, int>(pressedPiece.X, pressedPiece.Y)))
                {
                    bgArray[pressedPiece.X, pressedPiece.Y].GetComponent<SpriteRenderer>().color = pathColor;
                }
                else
                {
                    bgArray[pressedPiece.X, pressedPiece.Y].GetComponent<SpriteRenderer>().color = bgColor;
                }
                if (IsAdjacent(pressedPiece, enteredPiece) && pressedPiece.Type != PieceType.FOX && enteredPiece.Type != PieceType.FOX)
                {
                    SwapPieces(pressedPiece, enteredPiece);
                }
                pressedPiece = null;
                enteredPiece = null;
            }
        }
    }

    public List<GamePiece> GetMatch(GamePiece piece, int newX, int newY)
    {
        if (piece.IsColored())
        {
            Color color = piece.GetComponent<SpriteRenderer>().color;
            List<GamePiece> horizontalPieces = new List<GamePiece>();
            List<GamePiece> verticalPieces = new List<GamePiece>();
            List<GamePiece> matchingPieces = new List<GamePiece>();

            horizontalPieces.Add(piece);

            for (int dir = 0; dir <= 1; dir++)
            {
                for (int xOffset = 1; xOffset < xDim; xOffset++)
                {
                    int x;

                    if (dir == 0) //left
                        x = newX - xOffset;
                    else //right
                        x = newX + xOffset;

                    if (x < 0 || x >= xDim)
                        break;

                    if (pieces[x, newY].IsColored() && pieces[x, newY].GetComponent<SpriteRenderer>().color == color)
                    {
                        horizontalPieces.Add(pieces[x, newY]);
                    }
                    else
                        break;
                }
            }

            if (horizontalPieces.Count >= 3)
            {
                foreach (GamePiece p in horizontalPieces)
                {
                    matchingPieces.Add(p);
                    for (int dir = 0; dir <= 1; dir++)
                    {
                        for (int yOffset = 1; yOffset < yDim; yOffset++)
                        {
                            int y;

                            if (dir == 0) //up
                                y = newY - yOffset;
                            else //down
                                y = newY + yOffset;

                            if (y < 0 || y >= yDim)
                                break;

                            if (pieces[p.X, y].IsColored() && pieces[p.X, y].GetComponent<SpriteRenderer>().color == color)
                            {
                                verticalPieces.Add(pieces[p.X, y]);
                            }
                            else
                                break;
                        }
                    }
                    if (verticalPieces.Count < 2)
                    {
                        verticalPieces.Clear();
                    }
                    else
                    {
                        foreach (GamePiece q in verticalPieces)
                            matchingPieces.Add(q);
                        break;
                    }
                }
            }

            if (matchingPieces.Count >= 3)
                return matchingPieces;

            horizontalPieces.Clear();
            verticalPieces.Clear();
            verticalPieces.Add(piece);

            for (int dir = 0; dir <= 1; dir++)
            {
                for (int yOffset = 1; yOffset < yDim; yOffset++)
                {
                    int y;

                    if (dir == 0) //up
                        y = newY - yOffset;
                    else //down
                        y = newY + yOffset;

                    if (y < 0 || y >= yDim)
                        break;

                    if (pieces[newX, y].IsColored() && pieces[newX, y].GetComponent<SpriteRenderer>().color == color)
                    {
                        verticalPieces.Add(pieces[newX, y]);
                    }
                    else
                        break;
                }
            }

            if (verticalPieces.Count >= 3)
            {
                foreach (GamePiece p in verticalPieces)
                {
                    matchingPieces.Add(p);
                    for (int dir = 0; dir <= 1; dir++)
                    {
                        for (int xOffset = 1; xOffset < xDim; xOffset++)
                        {
                            int x;

                            if (dir == 0) //left
                                x = newX - xOffset;
                            else //right
                                x = newX + xOffset;

                            if (x < 0 || x >= xDim)
                                break;

                            if (pieces[x, p.Y].IsColored() && pieces[x, p.Y].GetComponent<SpriteRenderer>().color == color)
                            {
                                horizontalPieces.Add(pieces[x, p.Y]);
                            }
                            else
                                break;
                        }
                    }
                    if (horizontalPieces.Count < 2)
                    {
                        horizontalPieces.Clear();
                    }
                    else
                    {
                        foreach (GamePiece q in horizontalPieces)
                            matchingPieces.Add(q);
                        break;
                    }
                }
            }
            if (matchingPieces.Count >= 3)
                return matchingPieces;
        }

        return null;
    }

    public bool ClearAllValidMatches()
    {
        bool needsRefill = false;
        for (int y = 0; y < yDim; y++)
        {
            for (int x = 0; x < xDim; x++)
            {
                if (pieces[x, y].IsClearable())
                {
                    List<GamePiece> match = GetMatch(pieces[x, y], x, y);

                    if (match != null)
                    {
                        PieceType specialPieceType = PieceType.COUNT;
                        GamePiece randomPiece = match[Random.Range(0, match.Count)];
                        int specialPieceX = randomPiece.X;
                        int specialPieceY = randomPiece.Y;

                        if (match.Count == 4)
                        {
                            if (pressedPiece == null || enteredPiece == null)
                                specialPieceType = (PieceType)Random.Range((int)PieceType.ROW_CLEAR, (int)PieceType.COLUMN_CLEAR);
                            else if (pressedPiece.Y == enteredPiece.Y)
                                specialPieceType = PieceType.ROW_CLEAR;
                            else
                                specialPieceType = PieceType.COLUMN_CLEAR;
                        }
                        else if (match.Count >= 5)
                            specialPieceType = PieceType.ALL_CLEAR;
                        int counter = 0;
                        foreach (GamePiece p in match)
                        {
                            if (ClearPiece(p.X, p.Y))
                            {
                                needsRefill = true;
                                if (p == pressedPiece || p == enteredPiece)
                                {
                                    specialPieceX = p.X;
                                    specialPieceY = p.Y;
                                }
                                if (p.Type == PieceType.ROW_CLEAR)
                                    ClearRow(p.Y);
                                else if (p.Type == PieceType.COLUMN_CLEAR)
                                    ClearColumn(p.X);
                            }
                            if (p.Type == PieceType.PAW)
                                counter++;
                        }
                        if (specialPieceType != PieceType.COUNT)
                        {
                            Destroy(pieces[specialPieceX, specialPieceY]);
                            GamePiece newPiece = SpawnNewPiece(specialPieceX, specialPieceY, specialPieceType);

                            if ((specialPieceType == PieceType.ROW_CLEAR || specialPieceType == PieceType.COLUMN_CLEAR)
                                && newPiece.IsColored() && match[0].IsColored())
                                newPiece.GetComponent<SpriteRenderer>().color = match[0].GetComponent<SpriteRenderer>().color;
                        }
                        StartCoroutine(AddPaws(counter));
                    }
                }
            }
        }
        return needsRefill;
    }

    public IEnumerator AddPaws(int counter)
    {
        yield return new WaitForSeconds(1);
        for (int i = 1; i <= counter; i++)
        {
            if (currentFoxLocation != myPath.Count - 1)
            {
                KeyValuePair<int, int> kvp = myPath[currentFoxLocation];
                KeyValuePair<int, int> kvp2 = myPath[currentFoxLocation + 1];
                if (pieces[kvp2.Key, kvp2.Value].Type != PieceType.OBSTACLE)
                {
                    SwapPieces(pieces[kvp.Key, kvp.Value], pieces[kvp2.Key, kvp2.Value]);
                    currentFoxLocation++;
                }
            }
        }
        bool needsRefill = true;
        while (needsRefill)
        {
            yield return new WaitForSeconds(fillTime);
            while (FillStep())
            {
                inverse = !inverse;
                yield return new WaitForSeconds(fillTime);
            }
            needsRefill = ClearAllValidMatches();
        }
    }

    public bool ClearPiece(int x, int y)
    {
        if (pieces[x, y].IsClearable())
        {
            pieces[x, y].Clearable.Clear();
            SpawnNewPiece(x, y, PieceType.EMPTY);
            ClearObstacles(x, y);
            return true;
        }
        return false;
    }

    public void ClearObstacles(int x, int y)
    {
        for (int adjacentX = x - 1; adjacentX <= x + 1; adjacentX++)
        {
            if (adjacentX != x && adjacentX >= 0 && adjacentX < xDim)
            {
                if(pieces[adjacentX, y].Type == PieceType.OBSTACLE && pieces[adjacentX, y].IsClearable())
                {
                    pieces[adjacentX, y].Clearable.Clear();
                    SpawnNewPiece(adjacentX, y, PieceType.EMPTY);
                }
            }
        }
        for (int adjacentY = y - 1; adjacentY <= y + 1; adjacentY++)
        {
            if (adjacentY != y && adjacentY >= 0 && adjacentY < yDim)
            {
                if (pieces[x, adjacentY].Type == PieceType.OBSTACLE && pieces[x, adjacentY].IsClearable())
                {
                    pieces[x, adjacentY].Clearable.Clear();
                    SpawnNewPiece(x, adjacentY, PieceType.EMPTY);
                }
            }
        }
    }

    public void ClearRow(int row)
    {        
        for (int i = 0; i < xDim; i++)
        {           
            if (pieces[i, row].IsClearable())
            {
                ClearPiece(i, row);
            }
        }
    }

    public void ClearColumn(int column)
    {        
        for (int i = 0; i < yDim; i++)
        {           
            if (pieces[column, i].IsClearable())
            {
                ClearPiece(column, i);
            }
        }
    }

    public void ClearColor(Color thisColor)
    {
        for (int y = 0; y < yDim; y++)
        {
            for (int x = 0; x < xDim; x++)
            {
                if (pieces[x, y].IsColored() && pieces[x, y].GetComponent<SpriteRenderer>().color.Equals(thisColor))
                {
                    ClearPiece(x, y);
                }
            }
        }
    }
}
