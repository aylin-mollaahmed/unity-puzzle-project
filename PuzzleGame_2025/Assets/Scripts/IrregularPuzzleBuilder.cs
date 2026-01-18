using System.Collections.Generic;
using UnityEngine;

public static class IrregularPuzzleBuilder
{
    private struct Edges4
    {
        public Tile.PosNegType up, right, down, left;
    }

    /// <summary>
    /// Generates irregular (jigsaw tabs/slots) pieces using Tile/Bezier logic.
    /// Outputs pieces list + correct local positions + grid dimensions.
    /// </summary>
    public static void Build(
        Texture2D original,
        Transform piecePrefab,      // Prefab: SpriteRenderer + PolygonCollider2D (recommended)
        Transform parent,           // Your gameHolder
        float cellWidth,            // world units per grid cell (e.g. 1f)
        float cellHeight,           // world units per grid cell (e.g. 1f)
        int tileSizePixels,         // Tile.tileSize (must divide original width/height)
        int paddingPixels,          // Tile.padding
        float pixelsPerUnit,        // Sprite PPU (e.g. 100)
        out List<Transform> pieces,
        out Vector3[] correctLocalPos,
        out Vector2Int dimensions
    )
    {
        pieces = new List<Transform>();
        correctLocalPos = null;
        dimensions = Vector2Int.zero;

        if (original == null)
        {
            Debug.LogError("IrregularPuzzleBuilder.Build: original texture is null.");
            return;
        }
        if (!original.isReadable)
        {
            Debug.LogError("IrregularPuzzleBuilder.Build: texture must be Read/Write Enabled.");
            return;
        }

        // Configure tutorial static params
        Tile.tileSize = tileSizePixels;
        Tile.padding = paddingPixels;

        if (original.width % tileSizePixels != 0 || original.height % tileSizePixels != 0)
        {
            Debug.LogError(
                $"IrregularPuzzleBuilder.Build: texture size must be multiple of tileSizePixels={tileSizePixels}. " +
                $"Got {original.width}x{original.height}."
            );
            return;
        }

        int cols = original.width / tileSizePixels;
        int rows = original.height / tileSizePixels;
        dimensions = new Vector2Int(cols, rows);

        int total = cols * rows;
        correctLocalPos = new Vector3[total];
        pieces = new List<Transform>(total);

        // Generate edge types so neighbors match (tab/slot opposite)
        Edges4[] edges = GenerateEdges(cols, rows);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int index = row * cols + col;

                // 1) Cut tile texture with bezier borders
                Tile t = new Tile(original);
                t.xIndex = col;
                t.yIndex = row;

                t.SetCurveType(Tile.Direction.UP, edges[index].up);
                t.SetCurveType(Tile.Direction.RIGHT, edges[index].right);
                t.SetCurveType(Tile.Direction.DOWN, edges[index].down);
                t.SetCurveType(Tile.Direction.LEFT, edges[index].left);

                t.Apply(); // produces t.finalCut

                // 2) Create sprite from cut texture (pivot CENTER!)
                Sprite sprite = Sprite.Create(
                    t.finalCut,
                    new Rect(0, 0, t.finalCut.width, t.finalCut.height),
                    new Vector2(0.5f, 0.5f),
                    pixelsPerUnit,
                    0,
                    SpriteMeshType.Tight
                );

                // 3) Instantiate piece prefab under parent (gameHolder)
                Transform piece = Object.Instantiate(piecePrefab, parent);
                piece.name = index.ToString();

                var sr = piece.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = sprite;

                // 4) Ensure collider fits the sprite shape
                // (Unity sometimes doesn't refresh PolygonCollider2D automatically)
                var existingPoly = piece.GetComponent<PolygonCollider2D>();
                if (existingPoly != null)
                {
                    Object.Destroy(existingPoly);
                    piece.gameObject.AddComponent<PolygonCollider2D>();
                }

                // 5) Set correct local position on a centered grid (same idea as your Rect)
                Vector3 localPos = new Vector3(
                    (-cellWidth * cols / 2f) + (cellWidth / 2f) + (cellWidth * col),
                    (-cellHeight * rows / 2f) + (cellHeight / 2f) + (cellHeight * row),
                    -1f
                );

                piece.localPosition = localPos;
                piece.localRotation = Quaternion.identity;

                correctLocalPos[index] = localPos;
                pieces.Add(piece);
            }
        }
    }

    private static Edges4[] GenerateEdges(int cols, int rows)
    {
        int total = cols * rows;
        Edges4[] e = new Edges4[total];

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int i = row * cols + col;

                // Outer borders flat
                if (col == 0) e[i].left = Tile.PosNegType.NONE;
                if (row == 0) e[i].down = Tile.PosNegType.NONE;

                // RIGHT edge: random if not last col, mirror into neighbor LEFT
                if (col < cols - 1)
                {
                    var type = (Random.value < 0.5f) ? Tile.PosNegType.POS : Tile.PosNegType.NEG;
                    e[i].right = type;

                    int r = i + 1;
                    e[r].left = (type == Tile.PosNegType.POS) ? Tile.PosNegType.NEG : Tile.PosNegType.POS;
                }
                else e[i].right = Tile.PosNegType.NONE;

                // UP edge: random if not last row, mirror into neighbor DOWN
                if (row < rows - 1)
                {
                    var type = (Random.value < 0.5f) ? Tile.PosNegType.POS : Tile.PosNegType.NEG;
                    e[i].up = type;

                    int u = i + cols;
                    e[u].down = (type == Tile.PosNegType.POS) ? Tile.PosNegType.NEG : Tile.PosNegType.POS;
                }
                else e[i].up = Tile.PosNegType.NONE;
            }
        }

        return e;
    }
}

