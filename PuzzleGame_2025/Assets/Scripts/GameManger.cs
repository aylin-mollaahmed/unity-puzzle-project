using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.U2D;
using UnityEngine.UI;


public class GameManger : MonoBehaviour
{
    [Header("XML Config")]
    [SerializeField] private TextAsset levelConfigXml;

    [Header("Images")]
    [SerializeField] private Sprite[] puzzleImages;

    [Header("Game Elements")]
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text CorrectToShow;
    [SerializeField] private TMP_Text Timer;
    [SerializeField] private TMP_Text Progress;
    [SerializeField] private Transform gameHolder;
    [SerializeField] private Transform piecePrefab;


    private LevelRepository rep;
    private LevelConfig currentLevel;

    private List<Transform> pieces;
    Vector2Int dimensions;
    float width;
    float height;

    private Transform draggingPiece = null;
    private Vector3 offset;
    private int piecesCorrect = 0;

    private float currentTime = 0f;
    private bool timerRunning = true;

    private void Awake()
    {
        rep = new LevelRepository(levelConfigXml);
        // currentLevel = rep.GetConfigByDifficulty(UserAndGameDetailsManager.Instance.CurrentGame.difficulty);
        int demoDi = 2;
        currentLevel = rep.GetConfigByDifficulty(demoDi);
        pieces = new List<Transform>();
        // title.text = "Level " + UserAndGameDetailsManager.Instance.CurrentGame.difficulty;
        
        title.text = "Level " + demoDi;
        CorrectToShow.text = "Points: " + piecesCorrect;
        UpdateTimerUI();
        UpdateProgressUI();
    }
    private void Start()
    {

        switch (currentLevel.pieceShape)
        {

            case "Rect":
                //dimensions = GetDimensions(puzzleImages[UserAndGameDetailsManager.Instance.CurrentGame.pictureId].texture, currentLevel.piecesCount);
                dimensions = GetDimensions(puzzleImages[1].texture, currentLevel.piecesCount);
                //CreateJigsawPieces(puzzleImages[UserAndGameDetailsManager.Instance.CurrentGame.pictureId].texture);
                CreateJigsawPieces(puzzleImages[1].texture);
                Scatter();
                UpdateBorder();
                piecesCorrect = 0;
                break;

            case "Irregular":
                
                break;

            default:
                Debug.LogError($"Unknown pieceShape: {currentLevel.pieceShape}");
                break;
        }
    }

    //Помощна функция за нивата с правоъгълни парчета
    Vector2Int GetDimensions(Texture2D puzzleTexture, int pieceCount)
    {
        Vector2Int dimensions = Vector2Int.zero;
       
        if (puzzleTexture.width < puzzleTexture.height)
        {
            dimensions.x = pieceCount;
            dimensions.y = (pieceCount * puzzleTexture.height) / puzzleTexture.width;
        }
        else
        {
            dimensions.x = (pieceCount * puzzleTexture.width) / puzzleTexture.height;
            dimensions.y = pieceCount;
        }
        return dimensions;
    }
    void CreateJigsawPieces(Texture2D jigsawTexture)
    {

        height = 1f / dimensions.y;
        float aspect = (float)jigsawTexture.width / jigsawTexture.height;
        width = aspect / dimensions.x;

        for (int row = 0; row < dimensions.y; row++)
        {
            for (int col = 0; col < dimensions.x; col++)
            {
                // Create the piece in the right location of the right size.
                Transform piece = Instantiate(piecePrefab, gameHolder);
                piece.localPosition = new Vector3(
                  (-width * dimensions.x / 2) + (width * col) + (width / 2),
                  (-height * dimensions.y / 2) + (height * row) + (height / 2),
                  -1);
                piece.localScale = new Vector3(width, height, 1f);

                // We don't have to name them, but always useful for debugging.
                piece.name = $"Piece {(row * dimensions.x) + col}";
                pieces.Add(piece);

                // Assign the correct part of the texture for this jigsaw piece
                // We need our width and height both to be normalised between 0 and 1 for the UV.
                float width1 = 1f / dimensions.x;
                float height1 = 1f / dimensions.y;
                // UV coord order is anti-clockwise: (0, 0), (1, 0), (0, 1), (1, 1)
                Vector2[] uv = new Vector2[4];
                uv[0] = new Vector2(width1 * col, height1 * row);
                uv[1] = new Vector2(width1 * (col + 1), height1 * row);
                uv[2] = new Vector2(width1 * col, height1 * (row + 1));
                uv[3] = new Vector2(width1 * (col + 1), height1 * (row + 1));
                // Assign our new UVs to the mesh.
                Mesh mesh = piece.GetComponent<MeshFilter>().mesh;
                mesh.uv = uv;
                // Update the texture on the piece
                piece.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", jigsawTexture);
            }
        }
    }
    private void Scatter()
    {
        // Calculate the visible orthographic size of the screen.
        float orthoHeight = Camera.main.orthographicSize;
        float screenAspect = (float)Screen.width / Screen.height;
        float orthoWidth = (screenAspect * orthoHeight);

        // Ensure pieces are away from the edges.
        float pieceWidth = width * gameHolder.localScale.x;
        float pieceHeight = height * gameHolder.localScale.y;

        orthoHeight -= pieceHeight;
        orthoWidth -= pieceWidth;

        // Place each piece randomly in the visible area.
        foreach (Transform piece in pieces)
        {
            float x = Random.Range(-orthoWidth, orthoWidth);
            float y = Random.Range(-orthoHeight, orthoHeight);
            if (currentLevel.randomOrientation)
            {
                int[] angles = { 0, 90, 180, 270 };
                float z = angles[Random.Range(0, angles.Length)];
                piece.eulerAngles = new Vector3(0, 0, z);
            }
            else
            {
                piece.eulerAngles = new Vector3(0, 0, 0);
            }

            piece.position = new Vector3(x, y, -1);
        }
    }

    // Update the border to fit the chosen puzzle.
    private void UpdateBorder()
    {
        LineRenderer lineRenderer = gameHolder.GetComponent<LineRenderer>();

        // Calculate half sizes to simplify the code.
        float halfWidth = (width * dimensions.x) / 2f;
        float halfHeight = (height * dimensions.y) / 2f;

        // We want the border to be behind the pieces.
        float borderZ = 0f;

        // Set border vertices, starting top left, going clockwise.
        lineRenderer.SetPosition(0, new Vector3(-halfWidth, halfHeight, borderZ));
        lineRenderer.SetPosition(1, new Vector3(halfWidth, halfHeight, borderZ));
        lineRenderer.SetPosition(2, new Vector3(halfWidth, -halfHeight, borderZ));
        lineRenderer.SetPosition(3, new Vector3(-halfWidth, -halfHeight, borderZ));

        // Set the thickness of the border line.
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;

        // Show the border line.
        lineRenderer.enabled = true;
    }

    private void Update()
    {
        if (timerRunning)
        {
            currentTime += Time.deltaTime;
            UpdateTimerUI();
        }

        // Хващане на парче
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(
                Camera.main.ScreenToWorldPoint(Input.mousePosition),
                Vector2.zero
            );

            if (hit)
            {
                draggingPiece = hit.transform;
                offset = draggingPiece.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
                offset += Vector3.back; // над другите докато го мъкнеш
            }
        }

        // Въртене (докато има избрано парче)
        if (draggingPiece && currentLevel.randomOrientation)
        {
            if (Input.GetKeyDown(KeyCode.R) || Input.GetMouseButtonDown(1))
            {
                draggingPiece.Rotate(0f, 0f, 90f);
            }
        }

        // Пускане на парче
        if (draggingPiece && Input.GetMouseButtonUp(0))
        {
            SnapAndDisableIfCorrect();
            draggingPiece.position += Vector3.forward;
            draggingPiece = null;
        }

        // Влачене
        if (draggingPiece)
        {
            Vector3 newPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            newPosition += offset;
            draggingPiece.position = newPosition;
        }
    }

    private void SnapAndDisableIfCorrect()
    {
        int pieceIndex = pieces.IndexOf(draggingPiece);
        if (pieceIndex < 0) return;

        int col = pieceIndex % dimensions.x;
        int row = pieceIndex / dimensions.x;

        Vector2 targetPosition = new Vector2(
            (-width * dimensions.x / 2) + (width * col) + (width / 2),
            (-height * dimensions.y / 2) + (height * row) + (height / 2)
        );
        
        // ВАЖНО: targetPosition е local (защото и ти местиш local при създаване)
        if (Vector2.Distance(draggingPiece.localPosition, targetPosition) < (width / 2)
                                                    && IsRotationCorrect(draggingPiece))
        {
            draggingPiece.localPosition = targetPosition;
            draggingPiece.localEulerAngles = Vector3.zero; // изправяме го
            draggingPiece.GetComponent<BoxCollider2D>().enabled = false;

            piecesCorrect++;
            UpdateProgressUI();
            CorrectToShow.text = "Points: " + piecesCorrect;


            if (piecesCorrect == pieces.Count)
            {
                timerRunning = false;
                
            }
        }


    }
    private void UpdateTimerUI()
    {
        int totalSeconds = Mathf.FloorToInt(currentTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        if (Timer != null)
            Timer.text = "Timer: " + $"{minutes:00}:{seconds:00}";
    }
    private void UpdateProgressUI()
    {
        if (Progress == null) return;

        if (pieces == null || pieces.Count == 0)
        {
            Progress.text = "Progress: " + "0%";
            return;
        }

        float progress = (float)piecesCorrect / pieces.Count;
        int percent = Mathf.RoundToInt(progress * 100f);
        Progress.text = "Progress: " + $"{percent}%";
    }
    private bool IsRotationCorrect(Transform piece)
    {
        if (!currentLevel.randomOrientation) return true; // Level 1

        float z = piece.localEulerAngles.z;
        z = Mathf.Round(z / 90f) * 90f; 
        z = (z + 360f) % 360f;

        return Mathf.Approximately(z, 0f);
    }



}


