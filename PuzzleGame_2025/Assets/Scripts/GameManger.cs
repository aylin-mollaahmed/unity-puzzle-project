using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    [Header("Rect Pieces Prefab (Level 1/2)")]
    [SerializeField] private Transform piecePrefab;

    [Header("Irregular Pieces Prefab (Level 3/4)")]
    [SerializeField] private Transform irregularPiecePrefab;

    [Header("Irregular Generation Settings")]
    [SerializeField] private int tileSizeLevel3 = 100;
    [SerializeField] private int tileSizeLevel4 = 80;
    [SerializeField] private int paddingPixels = 20;
    [SerializeField] private float irregularPPU = 100f;
    [SerializeField] private float irregularCellW = 1f;
    [SerializeField] private float irregularCellH = 1f;

    [Header("Help UI")]
    [SerializeField] private Button helpButton;

    [Header("Finish UI")]
    [SerializeField] private GameObject finalPanel;
    [SerializeField] private TMP_Text finalText;
    [SerializeField] private Button playAgainButton;

    [Header("Snap Settings (Irregular Only)")]
    [SerializeField] private float snapDistanceIrregular = 0.15f;

    //DOM дърво с конфигурации за нивата и правилата за помощ
    private LevelRepository rep;
    //Клас с извлечената конфигурационна информация за нивото
    private LevelConfig currentLevel;

    //Лист от всички парчета
    private List<Transform> pieces;

    //Колко реда и колко колони ще има пъзела
    private Vector2Int dimensions;

    //Размерите на всяко парче (само за Rect)
    private float width;
    private float height;

    //Текущото хваната парче или група от парчета, които ще влачим
    private Transform draggingPiece;

    //Вектор на отместването
    private Vector3 offset;

    //Колко парчета са на правилните места
    private int piecesCorrect;

    //Текущо време
    private float currentTime;

    //Флаг дали времето да тече
    private bool timerRunning;

    //За всяко парче, към коя група принадлежи
    private Dictionary<Transform, Transform> pieceToGroup;

    // За всяка група кои парчета принадлежат към нея
    private Dictionary<Transform, List<Transform>> groupToPieces;

    //Колко пъти е използван бутона help
    private int helpUsed;

    //Точки
    private int scorePoints;

    //За всяко парче, коректната му позиция спрямо рамката
    private Vector3[] correctLocalPos;

    private bool IsIrregularLevel() => currentLevel != null && currentLevel.pieceShape == "Irregular";

    private void Awake()
    {
        rep = new LevelRepository(levelConfigXml);
        currentLevel = rep.GetConfigByDifficulty(UserAndGameDetailsManager.Instance.CurrentGame.difficulty);

        pieces = new List<Transform>();
        dimensions = Vector2Int.zero;
        width = 0f;
        height = 0f;
        draggingPiece = null;
        offset = Vector3.zero;
        piecesCorrect = 0;
        currentTime = 0f;
        timerRunning = true;
        pieceToGroup = new Dictionary<Transform, Transform>();
        groupToPieces = new Dictionary<Transform, List<Transform>>();
        helpUsed = 0;
        scorePoints = 0;
        correctLocalPos = null;

        title.text = "Level " + UserAndGameDetailsManager.Instance.CurrentGame.difficulty;
        CorrectToShow.text = "Points: " + scorePoints;

        UpdateTimerUI();
        UpdateProgressUI();
    }

    private void Start()
    {
        // Генериране на парчетата според типа
        switch (currentLevel.pieceShape)
        {
            case "Rect":
                {
                    Texture2D tex = puzzleImages[UserAndGameDetailsManager.Instance.CurrentGame.pictureId - 1].texture;

                    dimensions = GetDimensions(tex, currentLevel.piecesCount);
                    CreateJigsawPieces(tex);

                    InitGroups();
                    Scatter();
                    UpdateBorder();

                    piecesCorrect = 0;
                    scorePoints = 0;
                    CorrectToShow.text = "Points: " + scorePoints;
                    UpdateProgressUI();
                    break;
                }

            case "Irregular":
                {
                    Texture2D tex = puzzleImages[UserAndGameDetailsManager.Instance.CurrentGame.pictureId - 1].texture;

                    int diff = UserAndGameDetailsManager.Instance.CurrentGame.difficulty;
                    int tileSize = (diff == 4) ? tileSizeLevel4 : tileSizeLevel3;

                    // Това ще напълни: pieces, correctLocalPos, dimensions
                    IrregularPuzzleBuilder.Build(
                        tex,
                        irregularPiecePrefab,
                        gameHolder,
                        irregularCellW,
                        irregularCellH,
                        tileSize,
                        paddingPixels,
                        irregularPPU,
                        out pieces,
                        out correctLocalPos,
                        out dimensions
                    );

                    // При irregular не ползваме width/height за snap, но Scatter/Border искат стойности
                    // Настрой ги приблизително на база клетки (за да не хвърля NaN)
                    width = irregularCellW;
                    height = irregularCellH;

                    InitGroups();
                    Scatter();
                    UpdateBorder(); // ще е правоъгълна рамка, ок е визуално

                    piecesCorrect = 0;
                    scorePoints = 0;
                    CorrectToShow.text = "Points: " + scorePoints;
                    UpdateProgressUI();
                    break;
                }

            default:
                Debug.LogError($"Unknown pieceShape: {currentLevel.pieceShape}");
                break;
        }
    }

    //Помощна функция за нивата с правоъгълни парчета
    private Vector2Int GetDimensions(Texture2D puzzleTexture, int pieceCount)
    {
        Vector2Int d = Vector2Int.zero;

        if (puzzleTexture.width < puzzleTexture.height)
        {
            d.x = pieceCount;
            d.y = (pieceCount * puzzleTexture.height) / puzzleTexture.width;
        }
        else
        {
            d.x = (pieceCount * puzzleTexture.width) / puzzleTexture.height;
            d.y = pieceCount;
        }
        return d;
    }

    //Функцията, която създава правоъгълните парчета (ниво 1/2) — НЕПИПАНА логика
    private void CreateJigsawPieces(Texture2D jigsawTexture)
    {
        height = 1f / dimensions.y;

        float aspect = (float)jigsawTexture.width / jigsawTexture.height;
        width = aspect / dimensions.x;

        int total = dimensions.x * dimensions.y;
        correctLocalPos = new Vector3[total];

        pieces.Clear();

        for (int row = 0; row < dimensions.y; row++)
        {
            for (int col = 0; col < dimensions.x; col++)
            {
                Transform piece = Instantiate(piecePrefab, gameHolder);

                piece.localPosition = new Vector3(
                    (-width * dimensions.x / 2) + (width / 2) + (width * col),
                    (-height * dimensions.y / 2) + (height / 2) + (height * row),
                    -1f
                );

                piece.localScale = new Vector3(width, height, 1f);

                int index = (row * dimensions.x) + col;
                correctLocalPos[index] = piece.localPosition;
                piece.name = index.ToString();

                pieces.Add(piece);

                float width1 = 1f / dimensions.x;
                float height1 = 1f / dimensions.y;

                Vector2[] uv = new Vector2[4];
                uv[0] = new Vector2(width1 * col, height1 * row);
                uv[1] = new Vector2(width1 * (col + 1), height1 * row);
                uv[2] = new Vector2(width1 * col, height1 * (row + 1));
                uv[3] = new Vector2(width1 * (col + 1), height1 * (row + 1));

                Mesh mesh = piece.GetComponent<MeshFilter>().mesh;
                mesh.uv = uv;

                piece.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", jigsawTexture);
            }
        }
    }

    //Групи
    private void InitGroups()
    {
        // важно: ако презареждаш/сменяш ниво в бъдеще, тук трябва да чистиш старите GroupRoot-ове
        foreach (var piece in pieces)
        {
            Transform groupRoot = new GameObject("GroupRoot").transform;
            groupRoot.SetParent(gameHolder, worldPositionStays: false);
            groupRoot.localPosition = piece.localPosition;

            piece.SetParent(groupRoot, worldPositionStays: true);

            pieceToGroup[piece] = groupRoot;
            groupToPieces[groupRoot] = new List<Transform> { piece };
        }
    }

    private List<Transform> GetAllGroups()
    {
        return new List<Transform>(groupToPieces.Keys);
    }

    //Scatter
    private void Scatter()
    {
        float orthoHeight = Camera.main.orthographicSize;
        float screenAspect = (float)Screen.width / Screen.height;
        float orthoWidth = (screenAspect * orthoHeight);

        // При Rect width/height са реални, при Irregular ги държим на клетка (приблизително)
        float pieceWidth = width * gameHolder.localScale.x;
        float pieceHeight = height * gameHolder.localScale.y;

        orthoHeight -= pieceHeight;
        orthoWidth -= pieceWidth;

        foreach (Transform group in GetAllGroups())
        {
            float x = Random.Range(-orthoWidth, orthoWidth);
            float y = Random.Range(-orthoHeight, orthoHeight);

            if (currentLevel.randomOrientation)
            {
                int[] angles = { 0, 90, 180, 270 };
                float z = angles[Random.Range(0, angles.Length)];
                group.eulerAngles = new Vector3(0, 0, z);
            }
            else
            {
                group.eulerAngles = Vector3.zero;
            }

            group.position = new Vector3(x, y, -1);
        }
    }

    //Border (правоъгълна рамка; за irregular пак е ок)
    private void UpdateBorder()
    {
        LineRenderer lineRenderer = gameHolder.GetComponent<LineRenderer>();

        float finalWidth = width * dimensions.x;
        float finalHeight = height * dimensions.y;

        float halfWidth = finalWidth / 2f;
        float halfHeight = finalHeight / 2f;

        float borderZ = 0f;

        lineRenderer.SetPosition(0, new Vector3(-halfWidth, halfHeight, borderZ));
        lineRenderer.SetPosition(1, new Vector3(halfWidth, halfHeight, borderZ));
        lineRenderer.SetPosition(2, new Vector3(halfWidth, -halfHeight, borderZ));
        lineRenderer.SetPosition(3, new Vector3(-halfWidth, -halfHeight, borderZ));

        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.enabled = true;
    }

    private void Update()
    {
        if (timerRunning)
        {
            currentTime += Time.deltaTime;
            UpdateTimerUI();
        }

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(
                Camera.main.ScreenToWorldPoint(Input.mousePosition),
                Vector2.zero
            );

            if (hit)
            {
                Transform pickedPiece = hit.transform;

                if (!pieceToGroup.ContainsKey(pickedPiece))
                    return;

                Transform group = pieceToGroup[pickedPiece];
                draggingPiece = group;

                offset = draggingPiece.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
                offset += Vector3.back;
            }
        }

        if (draggingPiece && currentLevel.randomOrientation)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                draggingPiece.Rotate(0f, 0f, 90f);
            }
        }

        if (draggingPiece && Input.GetMouseButtonUp(0))
        {
            // различен snap за Rect и Irregular
            if (IsIrregularLevel())
                SnapAndDisableIfCorrect_Irregular();
            else
                SnapAndDisableIfCorrect_Rect(); // старото 1:1

            draggingPiece.position += Vector3.forward;
            draggingPiece = null;
        }

        if (draggingPiece)
        {
            Vector3 newPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            newPosition += offset;
            draggingPiece.position = newPosition;
        }
    }

    // ===== SNAP: Rect (стария код 1:1, без промени) =====
    private void SnapAndDisableIfCorrect_Rect()
    {
        if (draggingPiece == null) return;
        if (!groupToPieces.ContainsKey(draggingPiece)) return;

        Transform anchor = groupToPieces[draggingPiece][0];
        int anchorIndex = int.Parse(anchor.name);

        Vector2 targetAnchorLocal = correctLocalPos[anchorIndex];
        Vector3 targetAnchorWorld = gameHolder.TransformPoint(targetAnchorLocal);

        Vector3 anchorWorld = anchor.position;

        bool closeEnough = Vector2.Distance(anchorWorld, targetAnchorWorld) < (width / 2);
        bool rotationOk = IsRotationCorrect(draggingPiece);

        if (closeEnough && rotationOk)
        {
            Vector3 deltaWorld = targetAnchorWorld - anchorWorld;
            draggingPiece.position += deltaWorld;

            foreach (var p in groupToPieces[draggingPiece])
                p.GetComponent<BoxCollider2D>().enabled = false;

            piecesCorrect += groupToPieces[draggingPiece].Count;
            scorePoints += groupToPieces[draggingPiece].Count;

            CorrectToShow.text = "Points: " + scorePoints;
            UpdateProgressUI();

            if (piecesCorrect == pieces.Count)
            {
                timerRunning = false;
                var repo = new XmlUserRepository();

                int totalPoints = scorePoints + int.Parse(UserAndGameDetailsManager.Instance.CurrentUser.totalPoints);
                repo.AddPoints(UserAndGameDetailsManager.Instance.CurrentUser.username, totalPoints);
                UserAndGameDetailsManager.Instance.CurrentUser.totalPoints = totalPoints.ToString();

                ShowFinalPanel();
            }
        }
    }

    // ===== SNAP: Irregular (Collider2D + константа) =====
    private void SnapAndDisableIfCorrect_Irregular()
    {
        if (draggingPiece == null) return;
        if (!groupToPieces.ContainsKey(draggingPiece)) return;

        Transform anchor = groupToPieces[draggingPiece][0];
        int anchorIndex = int.Parse(anchor.name);

        Vector2 targetAnchorLocal = correctLocalPos[anchorIndex];
        Vector3 targetAnchorWorld = gameHolder.TransformPoint(targetAnchorLocal);

        Vector3 anchorWorld = anchor.position;

        bool closeEnough = Vector2.Distance(anchorWorld, targetAnchorWorld) < snapDistanceIrregular;
        bool rotationOk = IsRotationCorrect(draggingPiece);

        if (closeEnough && rotationOk)
        {
            Vector3 deltaWorld = targetAnchorWorld - anchorWorld;
            draggingPiece.position += deltaWorld;

            foreach (var p in groupToPieces[draggingPiece])
            {
                var col = p.GetComponent<Collider2D>();
                if (col != null) col.enabled = false;
            }

            piecesCorrect += groupToPieces[draggingPiece].Count;
            scorePoints += groupToPieces[draggingPiece].Count;

            CorrectToShow.text = "Points: " + scorePoints;
            UpdateProgressUI();

            if (piecesCorrect == pieces.Count)
            {
                timerRunning = false;
                var repo = new XmlUserRepository();

                int totalPoints = scorePoints + int.Parse(UserAndGameDetailsManager.Instance.CurrentUser.totalPoints);
                repo.AddPoints(UserAndGameDetailsManager.Instance.CurrentUser.username, totalPoints);
                UserAndGameDetailsManager.Instance.CurrentUser.totalPoints = totalPoints.ToString();

                ShowFinalPanel();
            }
        }
    }

    //Timer UI
    private void UpdateTimerUI()
    {
        int totalSeconds = Mathf.FloorToInt(currentTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        Timer.text = "Timer: " + $"{minutes:00}:{seconds:00}";
    }

    //Progress UI
    private void UpdateProgressUI()
    {
        if (pieces == null || pieces.Count == 0)
        {
            Progress.text = "Progress: 0%";
            return;
        }

        float progress = (float)piecesCorrect / pieces.Count;
        int percent = Mathf.RoundToInt(progress * 100f);
        Progress.text = "Progress: " + $"{percent}%";
    }

    //Rotation check
    private bool IsRotationCorrect(Transform group)
    {
        if (!currentLevel.randomOrientation) return true;

        float z = group.localEulerAngles.z;
        z = Mathf.Round(z / 90f) * 90f;
        return Mathf.Approximately(z, 0f);
    }

    private void ShowFinalPanel()
    {
        finalText.text = "Поздравления! Завършихте нивото\n" + $"Спечелени точки: {scorePoints}";
        finalPanel.SetActive(true);
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene("HomePage");
    }

    // ===== HELP wrapper (избира кой вариант) =====
    public void RequestHelp()
    {
        if (IsIrregularLevel())
            RequestHelp_Irregular();
        else
            RequestHelp_Rect(); // старото 1:1
    }

    // ===== HELP: Rect (старото ти RequestHelp 1:1) =====
    private void RequestHelp_Rect()
    {
        if (!currentLevel.help.enabled) return;
        if (helpUsed >= currentLevel.help.maxUses) return;

        List<int> piecesToConnect = FindPiecesToConnect_Rect();
        if (piecesToConnect == null) return;

        MergeGroupsByIndex(piecesToConnect);

        helpUsed++;
        scorePoints = scorePoints - currentLevel.help.costPoints;
        CorrectToShow.text = "Points: " + scorePoints;

        if (helpButton != null && helpUsed >= currentLevel.help.maxUses)
            helpButton.interactable = false;
    }

    // ===== HELP: Irregular (Collider2D) =====
    private void RequestHelp_Irregular()
    {
        if (!currentLevel.help.enabled) return;
        if (helpUsed >= currentLevel.help.maxUses) return;

        List<int> piecesToConnect = FindPiecesToConnect_Irregular();
        if (piecesToConnect == null) return;

        MergeGroupsByIndex(piecesToConnect);

        helpUsed++;
        scorePoints = scorePoints - currentLevel.help.costPoints;
        CorrectToShow.text = "Points: " + scorePoints;

        if (helpButton != null && helpUsed >= currentLevel.help.maxUses)
            helpButton.interactable = false;
    }

    // ===== IsUnsolved Rect (старото ти 1:1) =====
    private bool IsUnsolved_Rect(Transform piece)
    {
        var col = piece.GetComponent<BoxCollider2D>();
        return col != null
               && col.enabled
               && groupToPieces[pieceToGroup[piece]].Count == 1;
    }

    // ===== IsUnsolved Irregular =====
    private bool IsUnsolved_Irregular(Transform piece)
    {
        var col = piece.GetComponent<Collider2D>();
        return col != null
               && col.enabled
               && groupToPieces[pieceToGroup[piece]].Count == 1;
    }

    // ===== FindPiecesToConnect Rect (старото ти 1:1) =====
    private List<int> FindPiecesToConnect_Rect()
    {
        List<int> resultToReturn = new List<int>();
        for (int i = 0; i < pieces.Count; i++)
        {
            Transform startPiece = pieces[i];
            if (!IsUnsolved_Rect(startPiece))
                continue;

            int idx = int.Parse(startPiece.name);
            int col = idx % dimensions.x;
            int row = idx / dimensions.x;

            if (col > 0 && IsUnsolved_Rect(pieces[idx - 1]))
            {
                resultToReturn.Add(idx);
                resultToReturn.Add(idx - 1);
                return resultToReturn;
            }
            if (col < dimensions.x - 1 && IsUnsolved_Rect(pieces[idx + 1]))
            {
                resultToReturn.Add(idx);
                resultToReturn.Add(idx + 1);
                return resultToReturn;
            }
            if (row < dimensions.y - 1 && IsUnsolved_Rect(pieces[idx + dimensions.x]))
            {
                resultToReturn.Add(idx);
                resultToReturn.Add(idx + dimensions.x);
                return resultToReturn;
            }
            if (row > 0 && IsUnsolved_Rect(pieces[idx - dimensions.x]))
            {
                resultToReturn.Add(idx);
                resultToReturn.Add(idx - dimensions.x);
                return resultToReturn;
            }
        }
        return null;
    }

    // ===== FindPiecesToConnect Irregular =====
    private List<int> FindPiecesToConnect_Irregular()
    {
        List<int> resultToReturn = new List<int>();
        for (int i = 0; i < pieces.Count; i++)
        {
            Transform startPiece = pieces[i];
            if (!IsUnsolved_Irregular(startPiece))
                continue;

            int idx = int.Parse(startPiece.name);
            int col = idx % dimensions.x;
            int row = idx / dimensions.x;

            if (col > 0 && IsUnsolved_Irregular(pieces[idx - 1]))
            {
                resultToReturn.Add(idx);
                resultToReturn.Add(idx - 1);
                return resultToReturn;
            }
            if (col < dimensions.x - 1 && IsUnsolved_Irregular(pieces[idx + 1]))
            {
                resultToReturn.Add(idx);
                resultToReturn.Add(idx + 1);
                return resultToReturn;
            }
            if (row < dimensions.y - 1 && IsUnsolved_Irregular(pieces[idx + dimensions.x]))
            {
                resultToReturn.Add(idx);
                resultToReturn.Add(idx + dimensions.x);
                return resultToReturn;
            }
            if (row > 0 && IsUnsolved_Irregular(pieces[idx - dimensions.x]))
            {
                resultToReturn.Add(idx);
                resultToReturn.Add(idx - dimensions.x);
                return resultToReturn;
            }
        }
        return null;
    }

    // ===== Merge groups (оставено както беше при теб) =====
    private Transform FindPieceByIndex(int index)
    {
        foreach (var p in pieces)
        {
            if (p.name == index.ToString()) return p;
        }
        return null;
    }

    private void MergeGroupsByIndex(List<int> indexes)
    {
        Transform firstPiece = FindPieceByIndex(indexes[0]);
        Transform secondPiece = FindPieceByIndex(indexes[1]);

        Transform groupFirst = pieceToGroup[firstPiece];
        Transform groupSecond = pieceToGroup[secondPiece];

        if (currentLevel.randomOrientation)
        {
            groupFirst.localEulerAngles = Vector3.zero;
            groupSecond.localEulerAngles = Vector3.zero;
        }

        Vector3 offsetLocal = correctLocalPos[indexes[1]] - correctLocalPos[indexes[0]];
        Vector3 offsetGlobal = gameHolder.TransformVector(offsetLocal);

        Vector3 targetSecondGlobal = firstPiece.position + offsetGlobal;
        Vector3 delta = targetSecondGlobal - secondPiece.position;
        groupSecond.position += delta;

        secondPiece.SetParent(groupFirst);
        pieceToGroup[secondPiece] = groupFirst;
        groupToPieces[groupFirst].Add(secondPiece);

        groupToPieces.Remove(groupSecond);
        Destroy(groupSecond.gameObject);
    }
}




