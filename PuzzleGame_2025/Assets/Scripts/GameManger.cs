using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.U2D;
using UnityEngine.UI;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;


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

    [Header("Help UI")]
    [SerializeField] private Button helpButton;

    [Header("Finish UI")]
    [SerializeField] private GameObject finalPanel;
    [SerializeField] private TMP_Text finalText;
    [SerializeField] private Button playAgainButton;

    [Header("Win Effects")]
    [SerializeField] private ParticleSystem confettiPS;


    //DOM дърво с конфигурации за нивата и правилата за помощ
    private LevelRepository rep;
    //Клас с извлечената конфигурационна информация за нивото
    private LevelConfig currentLevel;

    //Лист от всички парчета и по конкретно техните трансформации
    private List<Transform> pieces;

    //Колко реда и колко колони ще има пъзела
    Vector2Int dimensions;

    //Размерите на всяко парче
    float width;
    float height;

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

    //Колко пъти е използван бутона help, защото лимитираме колко пъти е позволено да се използва
    private int helpUsed;

    //Колко са точките, допълнителна променлива, с която да може да реализираме отменяне на точки при използване на помощ 
    private int scorePoints;

    //За всяко парче, коректната му позиция спрямо рамката
    private Vector3[] correctLocalPos;

    private void Awake()
    {
        //Инициализация на член-данните

        rep = new LevelRepository(levelConfigXml);


        currentLevel = rep.GetConfigByDifficulty(UserAndGameDetailsManager.Instance.CurrentGame.difficulty);


        pieces = new List<Transform>();
        dimensions = Vector2Int.zero;
        width = 0f;
        height = 0f;
        draggingPiece = null;
        offset = Vector3Int.zero;
        piecesCorrect = 0;
        currentTime = 0;
        timerRunning = true;
        pieceToGroup = new Dictionary<Transform, Transform>();
        groupToPieces = new Dictionary<Transform, List<Transform>>();
        helpUsed = 0;
        scorePoints = 0;
        correctLocalPos = null;

        //Банер

        //Номер на нивото
        //Да се смени като се върже сцена 2
        title.text = "Level " + UserAndGameDetailsManager.Instance.CurrentGame.difficulty;


        //Брой изкарани точки
        CorrectToShow.text = "Points: " + scorePoints;

        //Време
        UpdateTimerUI();

        //Прогрес
        UpdateProgressUI();


    }
    private void Start()
    {

        switch (currentLevel.pieceShape)
        {
            case "Rect":
                //Брой редове и колони
                //Да се смени като се върже сцена 2
                dimensions = GetDimensions(puzzleImages[UserAndGameDetailsManager.Instance.CurrentGame.pictureId - 1].texture, currentLevel.piecesCount);


                //Създаване на пъзела
                //Да се смени като се върже сцена 2
                CreateJigsawPieces(puzzleImages[UserAndGameDetailsManager.Instance.CurrentGame.pictureId - 1].texture);


                //Инициализираме групите, тоест всяко парче да е група
                InitGroups();

                //Разпръсване на групите, тоест на парчетата по екрана  и даване на произволна ориентация при нужда
                Scatter();


                UpdateBorder();
                piecesCorrect = 0;
                scorePoints = 0;
                break;

            case "Irregular":

                dimensions = GetDimensions(
                puzzleImages[UserAndGameDetailsManager.Instance.CurrentGame.pictureId - 1].texture,
                currentLevel.piecesCount
    );

                CreateTrianglePieces(puzzleImages[UserAndGameDetailsManager.Instance.CurrentGame.pictureId - 1].texture);

                InitGroups();
                Scatter();
                UpdateBorder();

                piecesCorrect = 0;
                scorePoints = 0;
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

    //Функцията, която създава квадратните парчета
    void CreateJigsawPieces(Texture2D jigsawTexture)
    {
        //Нормализираме данните като въвеждаме удобна мерна единица,за да избегнем работа в пиксели


        //Изчисляваме височината на едно парче, като приемаме, че цялата височина е 1 
        height = 1f / dimensions.y;

        //Пресмятаме какво е отношението на височината и широчината, тоест колко широка е цялата картина
        float aspect = (float)jigsawTexture.width / jigsawTexture.height;

        //Получавам ширината на едно парче
        width = aspect / dimensions.x;

        // Инициализираме вектора с правилните позиции на всяко парче, защото тук вече знаем колко ще са парчетата
        int total = dimensions.x * dimensions.y;
        correctLocalPos = new Vector3[total];


        //Създаваме всяко едно парче като минаваме колона по колона
        for (int row = 0; row < dimensions.y; row++)
        {
            for (int col = 0; col < dimensions.x; col++)
            {
                // piecePrefab е шаблон за едно парче от пъзела
                //Тук му казваме да направи копие на шаблона като дете на gameHolder
                //GameHolder е празен обект, рамката на пъзела, но всички парчета са негови деца
                //Идеята е позицията на парчетата да се измерва спрямо рамката
                //Initiate връща tranform-a на парчето
                Transform piece = Instantiate(piecePrefab, gameHolder);


                // Локалната позиция, тоест позицията спрямо родителя

                //За X координатата
                // (-width * dimensions.x / 2) това пресмята координата на първото парче в лявевия ъгъл, така, че да сме разположили рамката симетрично спрямо (0,0)
                // (width / 2) това премества с половим ширина на парче защото ни интересува къде е средата на парчето
                // (width * col) това измества в зависимост от колоната на която се намираме в момента
                // Същата логика и за Y координатата
                // Идеята е да започнем от долния ляв ъгъл

                piece.localPosition = new Vector3(
                  (-width * dimensions.x / 2) + (width / 2) + (width * col),
                  (-height * dimensions.y / 2) + (height / 2) + (height * row),
                  -1);


                // Размера на парчето 
                // То е 3D обект, за това трябва да се подаде и някаква дълбочина, въпреки, че ние ще го третираме като 2D обект
                piece.localScale = new Vector3(width, height, 1f);

                //Индексиране по формула спрямо ред и колона
                int index = (row * dimensions.x) + col;

                //Запазвам на получения индекс правилната позиция на парчето, и казваме и на всяко парче кой е неговия уникален номер
                correctLocalPos[index] = piece.localPosition;
                piece.name = index.ToString();


                //Добавяме парчето в масива от парчета
                pieces.Add(piece);

                // Ще използваме  UV координати, за да определим коя точно част от картинката да се покаже върху парчето
                // При тези координати винаги се намираме между 0 и 1 и гледаме частите като колко процента от ширината и колко проценти от височината да покажа 
                float width1 = 1f / dimensions.x;
                float height1 = 1f / dimensions.y;


                // Подредбата по тези координати изглежда така за всяко парче:
                // V ↑
                // 1 | (0, 1)--------(1, 1)
                //   |    |              |

                //   |    |              |
                // 0 | (0, 0)--------(1, 0)
                //        0           1 → U


                //Инициализираме масив от такива координати, който съдържа координати на всеки ъгъл
                Vector2[] uv = new Vector2[4];
                uv[0] = new Vector2(width1 * col, height1 * row);
                uv[1] = new Vector2(width1 * (col + 1), height1 * row);
                uv[2] = new Vector2(width1 * col, height1 * (row + 1));
                uv[3] = new Vector2(width1 * (col + 1), height1 * (row + 1));

                //Mesh е формата на обекта, на нея присвояваме координатите на върховете, тоест указваме коя част от текстурата къде да отиде
                Mesh mesh = piece.GetComponent<MeshFilter>().mesh;
                mesh.uv = uv;

                //Слага текстурата от съответната снимка като основна текстура
                piece.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", jigsawTexture);
            }
        }
    }

    //Функция, която дига нивото на абстракция като добавя групи, за да е възможно групирането на парчета
    private void InitGroups()
    {

        foreach (var piece in pieces)
        {
            // Създаваме нова група, която да е дете на gameHolder-a
            Transform groupRoot = new GameObject("GroupRoot").transform;
            groupRoot.SetParent(gameHolder, worldPositionStays: false);

            // Групата да е на същата позиция като парчето, без завъртания
            groupRoot.localPosition = piece.localPosition;


            // Парчето да стане дете на групата
            piece.SetParent(groupRoot, worldPositionStays: true);

            //Сетваме като се подаде това парче да се връща ази група, тоест казваме това паече към коя група принаделжи
            pieceToGroup[piece] = groupRoot;

            //Сетваме че на тази група принадлежи точно това парче и към момента само то
            groupToPieces[groupRoot] = new List<Transform> { piece };
        }
    }
   


    //Функция, която разпръсва парчетата по видимата част на екрана и ги върти ако е нужно
    private void Scatter()
    {
        // Взимаме половината от видимата височина, която вижда камерата
        // Тоест ако върне 5, значи камерата вижда височината от -5 до 5 в глобални координати
        float orthoHeight = Camera.main.orthographicSize;

        //Колко е широк екрана спрямо това колко е висок
        // Например нещо подобно 1920 / 1080 ≈ 1.78
        float screenAspect = (float)Screen.width / Screen.height;

        //Това е половината от видимата ширина
        //Тоест ако се получи 8,5, екрана вижда от -8,5 до 8,5
        float orthoWidth = (screenAspect * orthoHeight);

        //Намираме реалните размери на парчето в зависимост от това дали е уголемен родителя, тоест рамката
        float pieceWidth = width * gameHolder.localScale.x;
        float pieceHeight = height * gameHolder.localScale.y;

        //Смаляваме видимия диапазон по x и y,за да сме сигурни, че няма да е възможно да сложим центъра на парчето в някой от краищата и част от парчето да не се вижда
        //Ппц е достатъчно да извадим и половината размер, но за по-сигурно изваждаме целия размер
        orthoHeight -= pieceHeight/2 + 0.1f;
        orthoWidth -= pieceWidth/2 + 0.1f;

        //Минаваме през всяка група и й задаваме случайни координати в позволения диапазон
        //Ако нивото позволява й даваме и произволна ориентация
        //Към този момент всяка група съдържа само по едно парче, тоест е същото като да го приложим върху групата
        float borderWidth =  width * dimensions.x * gameHolder.localScale.x;
       
        foreach (Transform group in GetAllGroups())
        {
            int leftOrRight = Random.Range(1, 3);
            float x = 0;
            float y = 0;
           
            switch (leftOrRight)
            {
                case 1:
                    x = Random.Range(-orthoWidth, -borderWidth/2 - width * gameHolder.localScale.x /2 - 0.1f);
                    y = Random.Range(-orthoHeight, orthoHeight - 1);
                    break;
                case 2:
                    x = Random.Range( borderWidth / 2 + width * gameHolder.localScale.x / 2 + 0.1f, orthoWidth);
                    y = Random.Range(-orthoHeight, orthoHeight - 1);
                    break;
                
            }


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
    
    // Функция, която чертае рамката на пъзела
    private void UpdateBorder()
    {
        // Достъпваме линията, която сме създали в UI
        LineRenderer lineRenderer = gameHolder.GetComponent<LineRenderer>();

        //Размера на рамката е:
        float finalWidth = width * dimensions.x;
        float finalHeight = height * dimensions.y;

        //Изчисляваме половината от размера, защото искаме да центрираме рамката спрямо (0,0)
        float halfWidth = finalWidth / 2f;
        float halfHeight = finalHeight / 2f;

        //Искаме рамката да е зад парчета за удобство при реденето
        float borderZ = 0f;

        //Присвояваме на съответния индекс на точката координатите
        lineRenderer.SetPosition(0, new Vector3(-halfWidth, halfHeight, borderZ));
        lineRenderer.SetPosition(1, new Vector3(halfWidth, halfHeight, borderZ));
        lineRenderer.SetPosition(2, new Vector3(halfWidth, -halfHeight, borderZ));
        lineRenderer.SetPosition(3, new Vector3(-halfWidth, -halfHeight, borderZ));

        // Присвояваме дебелина на правата
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;

        // Показваме рамката 
        lineRenderer.enabled = true;
    }

    //Функцията, която се вика на всеки кадър
    private void Update()
    {
        //Ако таймерът вече работи, добави му времето, минало от предишния кадър и го обнови
        if (timerRunning)
        {
            currentTime += Time.deltaTime;
            UpdateTimerUI();
        }

        // Ако в кадъра е хванато парче с натиснат ляв бутон
        if (Input.GetMouseButtonDown(0))
        {
            //Input.mousePosition връща координатите в пиксели, къде точно е кликнала мишата на екрана
            //  Camera.main.ScreenToWorldPoint(Input.mousePosition) превръща координати от екрана в глобални такива  
            // Physics2D.Raycast( startPoint, direction ) 
            // Проверяваме дали точно в тази точка има обект с collider
            RaycastHit2D hit = Physics2D.Raycast(
                Camera.main.ScreenToWorldPoint(Input.mousePosition),
                Vector2.zero
            );

            //Ако мишката е хванала такъв обект
            if (hit)
            {
                //На хванатия обект взимаме трансформацията и я присвояваме на член-данната, която пази кое парче ще се влачи

                // Хванатото е парче
                Transform pickedPiece = hit.transform;

                //Взимаме групата, към която принадлежи парчето
                Transform group = pieceToGroup[pickedPiece];

                //Влачим ГРУПАТА, не парчето
                draggingPiece = group;


                //offset = позиция на парчето − позиция на мишката
                //Тоест offset e разстоянието между точката, където сме кликнали, и центъра на парчето
                //Това е вектор на отместването, идеята е да изглежда сякаш влачим парчето там, където сме го хванали, а не да да "скача" до центъра
                offset = draggingPiece.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);

                //Vector3.back = new Vector3(0, 0, -1), искаме парчето визуално да ходи на другите докато го влачим
                offset += Vector3.back;
            }
        }

        // Въртене ako има хваната група и ако нивото позволява въртене
        if (draggingPiece && currentLevel.randomOrientation)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                //Въртим само на 90 градуса
                draggingPiece.Rotate(0f, 0f, 90f);
            }
        }


        // Ако е имало хванато парче и го пуснем
        if (draggingPiece && Input.GetMouseButtonUp(0))
        {
            // Застопоряваме групата и забраняваме вече да се мърда ако е достатъчно близо до правилното място
            SnapAndDisableIfCorrect();

            //Преместваме групата да е зад всички други парчета
            //Vector3.forward = new Vector3(0, 0, 1)
            draggingPiece.position += Vector3.forward;

            //Парчето е пуснато, тоест вече нямаме текущо парче и не искам е да влачим нищо
            draggingPiece = null;
        }

        // Влачене, ако има хванато парче
        if (draggingPiece)
        {
            //Взимаме текущата позиция на мишката 
            Vector3 newPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            //Добавяме вектора на отместването за да изглежда "правилно" влаченето 
            newPosition += offset;

            //Преместваме центъра с което реално преместваме и групата
            draggingPiece.position = newPosition;
        }
    }


    private void SnapAndDisableIfCorrect()
    {
        //Има ли хванато парче
        if (draggingPiece == null)
        {
            return;
        }

        //Има ли такава група
        if (!groupToPieces.ContainsKey(draggingPiece))
        {
            return;
        }

        // За котва взимаме първия елемент на групата, защото винаги сме сигурни, че има такъв, защото така ги инициализираме
        Transform anchor = groupToPieces[draggingPiece][0];

        //Взимаме индекс от името на парчето
        int anchorIndex = int.Parse(anchor.name);

        //Координати спрямо рамката
        Vector2 targetAnchorLocal = correctLocalPos[anchorIndex];

        // Глобални координати
        Vector3 targetAnchorWorld = gameHolder.TransformPoint(targetAnchorLocal);

        // Текущи координати на котвата
        Vector3 anchorWorld = anchor.position;

        bool closeEnough = Vector2.Distance(anchorWorld, targetAnchorWorld) < (width / 2);
        bool rotationOk = IsRotationCorrect(draggingPiece);

        if (closeEnough && rotationOk)
        {
            // местим цялата групата така, че anchor да отиде на target
            Vector3 deltaWorld = targetAnchorWorld - anchorWorld;
            draggingPiece.position += deltaWorld;

            AudioManager.Instance?.PlayPlace();


            //Искаме всички парчета да са вече неактивни
            foreach (var p in groupToPieces[draggingPiece])
                p.GetComponent<BoxCollider2D>().enabled = false;

            //Увеличаваме брой на частите, които са на правилните места с толкова, колкото елемента съдържа групата
            //Тъй като още няма различна логика, броя точки е същия
            piecesCorrect += groupToPieces[draggingPiece].Count;
            scorePoints += groupToPieces[draggingPiece].Count * currentLevel.piecePoint;

            //Актуализираме броя точки
            CorrectToShow.text = "Points: " + scorePoints;

            //Актуализираме прогреса
            UpdateProgressUI();

            //Ако сме завършили пъзела
            if (piecesCorrect == pieces.Count)
            {
                //Актуализираме броя точки
                timerRunning = false;
                var repo = new XmlUserRepository();

                int totalPoints = scorePoints + int.Parse(UserAndGameDetailsManager.Instance.CurrentUser.totalPoints);
                string pictureId = getPictureId(UserAndGameDetailsManager.Instance.CurrentGame.pictureId);
                int unlockedUpToNew = Mathf.Max(int.Parse(UserAndGameDetailsManager.Instance.CurrentUser.unlockedUpTo[pictureId]),UserAndGameDetailsManager.Instance.CurrentGame.difficulty + 1);
                repo.AddPoints(UserAndGameDetailsManager.Instance.CurrentUser.username, totalPoints);
                
                repo.updateLevelLocking(UserAndGameDetailsManager.Instance.CurrentUser.username, unlockedUpToNew, pictureId);
                UserAndGameDetailsManager.Instance.CurrentUser.totalPoints = totalPoints.ToString();

                UserAndGameDetailsManager.Instance.CurrentUser.unlockedUpTo[pictureId] = unlockedUpToNew.ToString();

                AudioManager.Instance?.PlayWin();
                if (confettiPS != null)
                    confettiPS.Play();

                ShowFinalPanel();
            }
        }
    }

    //Ъпдейт на времето в формат 00:00
    private void UpdateTimerUI()
    {
        int totalSeconds = Mathf.FloorToInt(currentTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        Timer.text = "Timer: " + $"{minutes:00}:{seconds:00}";

    }

    //Ъпдейт на прогреса
    private void UpdateProgressUI()
    {
        //Ако още не са създани частите още, за да избегнем деле на 0
        if (pieces == null || pieces.Count == 0)
        {
            Progress.text = "Progress: " + "0%";
            return;
        }

        float progress = (float)piecesCorrect / pieces.Count;
        int percent = Mathf.RoundToInt(progress * 100f);
        Progress.text = "Progress: " + $"{percent}%";
    }


    //Проверка дали групата е с правилна ориентация
    private bool IsRotationCorrect(Transform group)
    {
        //Ako нивот не позволява въртене, ориентацията винаги е правилна
        if (!currentLevel.randomOrientation)
        {
            return true;
        }

        float z = group.localEulerAngles.z;

        //Закргляне към най-близкото кратно на 90 
        z = Mathf.Round(z / 90f) * 90f;

        return Mathf.Approximately(z, 0f);
    }

    //Показване на панела за завършена игра
    private void ShowFinalPanel()
    {
        finalText.text = "Well done! Level completed!\n" + $"Points earned: {scorePoints}";

        finalPanel.SetActive(true);
    }

    //Ако се натисне бутона за рестарт просто се зарежда Home сцената
    public void RestartLevel()
    {
        SceneManager.LoadScene("HomePage");
    }

    //Ако се избере бутона за помощ
    public void RequestHelp()
    {
        //Проверява дали в нивото е позволено изобщо да се използва помощ
        if (!currentLevel.help.enabled)
        {
            return;
        }

        //Проверява дали има още позволени ползвания на help бутона спрямо конфигурацията
        if (helpUsed >= currentLevel.help.maxUses)
        {
            return;
        }


        List<int> piecesToConnect = FindPiecesToConnect();



        if (piecesToConnect == null)
        {
            return;
        }


        MergeGroupsByIndex(piecesToConnect);


        helpUsed++;
        scorePoints = scorePoints - currentLevel.help.costPoints;
        CorrectToShow.text = "Points: " + scorePoints;

        if (helpButton != null && helpUsed >= currentLevel.help.maxUses)
            helpButton.interactable = false;
    }


    private List<Transform> GetAllGroups()
    {
        return new List<Transform>(groupToPieces.Keys);
    }


    private bool IsUnsolved(Transform piece)
    {
        var col = piece.GetComponent<BoxCollider2D>();
        return col != null
               && col.enabled
               && groupToPieces[pieceToGroup[piece]].Count == 1;
    }
    private List<int> FindPiecesToConnect()
    {
        // ако е Irregular (триъгълници) -> друга логика за съседство
        if (currentLevel != null && currentLevel.pieceShape == "Irregular")
            return FindTrianglePiecesToConnect();

        // иначе си остава старата логика за Rect
        return FindPRectPiecesToConnect();
    }
    
   


    // ===================== 2) ДОБАВИ ТОВА: ТРИЪГЪЛНИ СЪСЕДИ (BL->TR диагонал както при теб) =====================
    //
    // Индексиране при теб:
    // squareIndex = row*W + col
    // triIndex0 = squareIndex*2 + 0  (BL, BR, TR)  -> "долно-десния" триъгълник в клетката
    // triIndex1 = squareIndex*2 + 1  (BL, TR, TL)  -> "горно-ляв" триъгълник в клетката
    //
    // Реални съседи по РЪБ (не по "idx±1"):
    // - Вътрешният диагонал (в клетката): tri0 <-> tri1 (винаги)
    // - Вертикален ръб: tri1 (ляво-горе) има "ляв" ръб и "горен" ръб; tri0 има "десен" и "долен"
    //   * Ляв съсед на tri1 е tri0 на клетката вляво
    //   * Горен съсед на tri1 е tri0 на клетката отгоре
    //   * Десен съсед на tri0 е tri1 на клетката вдясно
    //   * Долен съсед на tri0 е tri1 на клетката отдолу
    //
    private List<int> FindTrianglePiecesToConnect()
    {
        // Важно: тук НЕ използваме idx%dimensions.x директно, защото idx е триъгълник, не клетка.
        List<int> result = new List<int>();

        for (int i = 0; i < pieces.Count; i++)
        {
            Transform startPiece = pieces[i];
            if (!IsUnsolved(startPiece))
                continue;

            int triIdx = int.Parse(startPiece.name);

            // 1) намираме клетката и кой триъгълник е (0 или 1)
            int squareIndex = triIdx / 2;
            int triInSquare = triIdx % 2; // 0 или 1

            int col = squareIndex % dimensions.x;
            int row = squareIndex / dimensions.x;

            // Вземи списък със съседни триъгълници по ръб
            List<int> neighbors = GetTriangleNeighbors(triIdx, row, col, triInSquare);

            // Върни първия валиден съсед (несъбран и със самостоятелна група)
            for (int n = 0; n < neighbors.Count; n++)
            {
                int nb = neighbors[n];
                if (nb < 0 || nb >= pieces.Count) continue;

                // намираме самото парче по индекс (името му е индекса)
                Transform neighborPiece = FindPieceByIndex(nb);
                if (neighborPiece == null) continue;

                if (IsUnsolved(neighborPiece))
                {
                    result.Add(triIdx);
                    result.Add(nb);
                    return result;
                }
            }
        }

        return null;
    }

    private List<int> GetTriangleNeighbors(int triIdx, int row, int col, int triInSquare)
    {
        // triInSquare:
        // 0 = (BL, BR, TR)  => има ръбове: долен, десен, диагонал
        // 1 = (BL, TR, TL)  => има ръбове: ляв, горен, диагонал
        //
        // Вътрешният диагонал съсед винаги е "другия" в същата клетка
        int squareIndex = triIdx / 2;

        int tri0 = squareIndex * 2 + 0;
        int tri1 = squareIndex * 2 + 1;

        List<int> res = new List<int>(3);

        // 1) диагонален съсед (в клетката)
        res.Add(triInSquare == 0 ? tri1 : tri0);

        // 2) външни съседи по ръб
        if (triInSquare == 0)
        {
            // tri0: десен ръб -> триъгълник 1 на клетката вдясно
            if (col < dimensions.x - 1)
            {
                int rightSquare = squareIndex + 1;
                res.Add(rightSquare * 2 + 1);
            }

            // tri0: долен ръб -> триъгълник 1 на клетката отдолу
            if (row > 0)
            {
                int downSquare = squareIndex - dimensions.x;
                res.Add(downSquare * 2 + 1);
            }
        }
        else
        {
            // tri1: ляв ръб -> триъгълник 0 на клетката вляво
            if (col > 0)
            {
                int leftSquare = squareIndex - 1;
                res.Add(leftSquare * 2 + 0);
            }

            // tri1: горен ръб -> триъгълник 0 на клетката отгоре
            if (row < dimensions.y - 1)
            {
                int upSquare = squareIndex + dimensions.x;
                res.Add(upSquare * 2 + 0);
            }
        }

        return res;
    }


    

    private List<int> FindPRectPiecesToConnect()
    {
        // взимаме стартово парче, в случая взима подред първото парче, което не си е на мястото започвайки от долния ляв ъгъл
        List<int> resultToReturn = new List<int>();
        for (int i = 0; i < pieces.Count; i++)
        {
            Transform startPiece = pieces[i];
            //Ако парчето си е вече на мястото, минаваме на следващото
            if (!IsUnsolved(startPiece))
                continue;

            //Ако съм намерила парче, което не си е на мястото
            int idx = int.Parse(startPiece.name);
            int col = idx % dimensions.x;
            int row = idx / dimensions.x;

            // В ляво
            if (col > 0 && IsUnsolved(pieces[idx - 1]))
            {
                resultToReturn.Add(idx);
                resultToReturn.Add(idx - 1);
                return resultToReturn;

            }
            // В дясно
            if (col < dimensions.x - 1 && IsUnsolved(pieces[idx + 1]))
            {
                resultToReturn.Add(idx);
                resultToReturn.Add(idx + 1);
                return resultToReturn;

            }
            //Отгоре
            if (row < dimensions.y - 1 && IsUnsolved(pieces[idx + dimensions.x]))
            {
                resultToReturn.Add(idx);
                resultToReturn.Add(idx + dimensions.x);
                return resultToReturn;

            }
            //Отдолу
            if (row > 0 && IsUnsolved(pieces[idx - dimensions.x]))
            {
                resultToReturn.Add(idx);
                resultToReturn.Add(idx - dimensions.x);
                return resultToReturn;

            }

        }

        return null;
    }

    private Transform FindPieceByIndex(int index)
    {
        foreach (var p in pieces)
        {
            if (p.name == index.ToString())
            {
                return p;
            }
        }
        return null;
    }
    private void MergeGroupsByIndex(List<int> indexes)
    {

        Transform firstPiece = FindPieceByIndex(indexes[0]);
        Transform secondPiece = FindPieceByIndex(indexes[1]);

        Transform groupFirst = pieceToGroup[firstPiece];
        Transform groupSecond = pieceToGroup[secondPiece];

        //И двете парчета да станат с правилна ориентация

        if (currentLevel.randomOrientation)
        {
            groupFirst.localEulerAngles = Vector3.zero;
            groupSecond.localEulerAngles = Vector3.zero;
        }
        //Преместване на второто парче да иде при първото

        // правилният офсет между двете парчета за да са на правилното място
        Vector3 offsetLocal = correctLocalPos[indexes[1]] - correctLocalPos[indexes[0]];

        // превръщаме офсета в глобални координати
        Vector3 offsetGlobal = gameHolder.TransformVector(offsetLocal);

        //пресмятаме къде трябва да е второто парче
        Vector3 targetSecondGlobal = firstPiece.position + offsetGlobal;

        // местим втората група
        Vector3 delta = targetSecondGlobal - secondPiece.position;
        groupSecond.position += delta;


        //Сливане на групите

        secondPiece.SetParent(groupFirst);
        pieceToGroup[secondPiece] = groupFirst;
        groupToPieces[groupFirst].Add(secondPiece);


        groupToPieces.Remove(groupSecond);
        Destroy(groupSecond.gameObject);

    }
    string getPictureId(int id)
    {
        switch (id)
        {
            case 1: return "prehistoric";
            case 2: return "egypt";
            case 3: return "knights";
            case 4: return "future";

        }
        return null;
    }

    void CreateTrianglePieces(Texture2D jigsawTexture)
    {
        // 1) Размери като при квадратните парчета (клетките)
        height = 1f / dimensions.y;
        float aspect = (float)jigsawTexture.width / jigsawTexture.height;
        width = aspect / dimensions.x;

        // 2) Триъгълниците са 2 * брой клетки
        int totalSquares = dimensions.x * dimensions.y;
        int totalTriangles = totalSquares * 2;

        correctLocalPos = new Vector3[totalTriangles];
        pieces.Clear();

        float uStep = 1f / dimensions.x;
        float vStep = 1f / dimensions.y;

        for (int row = 0; row < dimensions.y; row++)
        {
            for (int col = 0; col < dimensions.x; col++)
            {
                // Центърът на клетката (същата логика като при Rect)
                Vector3 cellCenterLocal = new Vector3(
                    (-width * dimensions.x / 2f) + (width / 2f) + (width * col),
                    (-height * dimensions.y / 2f) + (height / 2f) + (height * row),
                    -1f
                );

                int squareIndex = (row * dimensions.x) + col;

                // UV ъгли на клетката
                Vector2 uvBL = new Vector2(uStep * col, vStep * row);             // bottom-left
                Vector2 uvBR = new Vector2(uStep * (col + 1), vStep * row);       // bottom-right
                Vector2 uvTL = new Vector2(uStep * col, vStep * (row + 1));       // top-left
                Vector2 uvTR = new Vector2(uStep * (col + 1), vStep * (row + 1)); // top-right

                // --- Триъгълник 0 (по диагонал BL->TR): BL, BR, TR
                {
                    int triIndex = squareIndex * 2 + 0;

                    Transform piece = Instantiate(piecePrefab, gameHolder);
                    piece.localPosition = cellCenterLocal;
                    piece.localScale = new Vector3(width, height, 1f);
                    piece.name = triIndex.ToString();

                    BuildTriangleMesh(piece,
                        new Vector3(-0.5f, -0.5f, 0f),  // BL
                        new Vector3(0.5f, -0.5f, 0f),  // BR
                        new Vector3(0.5f, 0.5f, 0f),  // TR
                        uvBL, uvBR, uvTR,
                        jigsawTexture
                    );

                    correctLocalPos[triIndex] = piece.localPosition;
                    pieces.Add(piece);
                }

                // --- Триъгълник 1 (по диагонал BL->TR): BL, TR, TL
                {
                    int triIndex = squareIndex * 2 + 1;

                    Transform piece = Instantiate(piecePrefab, gameHolder);
                    piece.localPosition = cellCenterLocal;
                    piece.localScale = new Vector3(width, height, 1f);
                    piece.name = triIndex.ToString();

                    BuildTriangleMesh(piece,
                        new Vector3(-0.5f, -0.5f, 0f),  // BL
                        new Vector3(0.5f, 0.5f, 0f),  // TR
                        new Vector3(-0.5f, 0.5f, 0f),  // TL
                        uvBL, uvTR, uvTL,
                        jigsawTexture
                    );

                    correctLocalPos[triIndex] = piece.localPosition;
                    pieces.Add(piece);
                }
            }
        }
    }

    private void BuildTriangleMesh(
    Transform piece,
    Vector3 v0, Vector3 v1, Vector3 v2,
    Vector2 uv0, Vector2 uv1, Vector2 uv2,
    Texture2D tex)
    {
        MeshFilter mf = piece.GetComponent<MeshFilter>();
        MeshRenderer mr = piece.GetComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] { v0, v1, v2 };
        mesh.uv = new Vector2[] { uv0, uv1, uv2 };
        mesh.triangles = new int[] { 0, 1, 2 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.mesh = mesh;

        // Вземаме Unlit shader, който съществува в проекта (URP или Built-in)
        Shader sh =
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Universal Render Pipeline/Unlit Shader") ??
            Shader.Find("Sprites/Default") ??
            Shader.Find("Unlit/Texture");

        if (sh == null)
        {
            Debug.LogError("NO suitable Unlit shader found. Project may be using a different pipeline.");
            return;
        }

        // Създаваме нов материал за всяко парче (за тест е най-сигурно)
        Material mat = new Material(sh);

        // Различните shader-и ползват различни property-та
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);

        mr.material = mat;

        // Да са отпред като sorting (важно в 2D сцени)
        mr.sortingLayerName = "Default";
        mr.sortingOrder = 10;
    }


}