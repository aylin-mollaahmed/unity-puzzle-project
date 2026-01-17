using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    [Header("Help UI")]
    [SerializeField] private Button helpButton;

    [Header("Finish UI")]
    [SerializeField] private GameObject finalPanel;
    [SerializeField] private TMP_Text finalText;
    [SerializeField] private Button playAgainButton;


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


    private int helpUsed = 0;
    private int scorePoints = 0;
    private Vector3[] correctLocalPos;

    private void Awake()
    {
        rep = new LevelRepository(levelConfigXml);
        // currentLevel = rep.GetConfigByDifficulty(UserAndGameDetailsManager.Instance.CurrentGame.difficulty);
        int demoDi = 2;
        currentLevel = rep.GetConfigByDifficulty(demoDi);
        pieces = new List<Transform>();
        // title.text = "Level " + UserAndGameDetailsManager.Instance.CurrentGame.difficulty;

        title.text = "Level " + demoDi;

        scorePoints = 0;
        CorrectToShow.text = "Points: " + scorePoints;


        UpdateTimerUI();
        UpdateProgressUI();

        if (helpButton != null)
            helpButton.interactable = (currentLevel.help != null && currentLevel.help.enabled);


        if (finalPanel != null)
            finalPanel.SetActive(false);



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
                scorePoints = 0;
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

        // Инициализираме вектора с правилните позиции на всяко парче
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

                //Да се върна тук оправям Help-a
                //int index = (row * dimensions.x) + col;
                //correctLocalPos[index] = piece.localPosition;

                // Да се изтрие ако нямаме нужда от него
                //piece.name = $"Piece {(row * dimensions.x) + col}";

                //Добавяме парчето в масива от парчета
                pieces.Add(piece);

                // Ще използваме  UV координати, за да определим коя точно част от картинката да се покаже върху парчето
                // При тези координати винаги се намираме между 0 и 1 и гледаме частите като колко процента от ширината и коко проценти от височината да покажа 
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
        orthoHeight -= pieceHeight;
        orthoWidth -= pieceWidth;

        //Минаваме през всяко парче и ми задаваме случайни координати в позволения диапазон
        //Ако нивото позволява му даваме и произволна ориентация 
        foreach (Transform piece in pieces)
        {
            float x = Random.Range(-orthoWidth, orthoWidth);
            float y = Random.Range(-orthoHeight, orthoHeight);

            //Въртене ако нивото го изисква
            if (currentLevel.randomOrientation)
            {

                int[] angles = { 0, 90, 180, 270 };
                float z = angles[Random.Range(0, angles.Length)];
                //Въртим парчетата по z
                piece.eulerAngles = new Vector3(0, 0, z);
            }
            else
            {
                //Парчета с правилна ориентация
                piece.eulerAngles = new Vector3(0, 0, 0);
            }

            //Без значение от това дали е завъртяно или не, позиционираме парчето някъде в позволената граница
            piece.position = new Vector3(x, y, -1);
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
                draggingPiece = hit.transform;

                //offset = позиция на парчето − позиция на мишката
                //Тоест offset e разстоянието между точката, където сме кликнали, и центъра на парчето
                //Това е вектор на отместването, идеята е да изглежда сякаш влачим парчето там, където сме го хванали, а не да да "скача" до центъра
                offset = draggingPiece.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);

                //Vector3.back = new Vector3(0, 0, -1), искаме парчето визуално да ходи на другите докато го влачим
                offset += Vector3.back;
            }
        }

        // Въртене ako има хванато парче и ако нивото позволява въртене
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
            // Застопоряваме парчето и забраняваме вече да се мърда ако е достатъчно близо до правилното място
            SnapAndDisableIfCorrect();

            //Преместваме го да е зад всички други парчета
            //Vector3.forward = new Vector3(0, 0, 1)
            draggingPiece.position += Vector3.forward;

            //Парчето е пуснатом, тоест вече нямаме текущо парче и не искам е да влачим нищо
            draggingPiece = null;
        }

        // Влачене, ако има хванато парче
        if (draggingPiece)
        {
            //Взимаме текущата позиция на мишката 
            Vector3 newPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            //Добавяме вектора на отместването за да изглежда "правилно" влаченето 
            newPosition += offset;

            //Преместваме центъра с което реално преместваме и парчето
            draggingPiece.position = newPosition;
        }
    }

    //Щракване и заключване на парче ако е на правилното място
    private void SnapAndDisableIfCorrect()
    {

        //Намираме кое точно парче сме пуснали като индекс
        int pieceIndex = pieces.IndexOf(draggingPiece);

        //Намираме на кой ред и коя колона е парчето
        int col = pieceIndex % dimensions.x;
        int row = pieceIndex / dimensions.x;

        //Пресмята точните му координати, където трябва да бъде
        Vector2 targetPosition = new Vector2(
            (-width * dimensions.x / 2) + (width * col) + (width / 2),
            (-height * dimensions.y / 2) + (height * row) + (height / 2)
        );

        // Ако разстоянието между текущата позиция и правилната позиция е достчтънчо малко и парчето е с правилна ориентация
        if (Vector2.Distance(draggingPiece.localPosition, targetPosition) < (width / 2)
                                                    && IsRotationCorrect(draggingPiece))
        {
            //Слагаме парчето на точната позиция 
            draggingPiece.localPosition = targetPosition;
            
            //Изключва collider-а за да не се хваща и мести вече парчето
            draggingPiece.GetComponent<BoxCollider2D>().enabled = false;

            piecesCorrect++;
            scorePoints++;
            UpdateProgressUI();
            CorrectToShow.text = "Points: " + scorePoints;

            //Ако нивото е завършено се появявава панела за край на играта и новите точки се добавят на потребителя във файла
            if (piecesCorrect == pieces.Count)
            {
                timerRunning = false;
                var repo = new XmlUserRepository();
                // repo.AddPoints(UserAndGameDetailsManager.Instance.CurrentUser.username, scorePoints + int.Parse(UserAndGameDetailsManager.Instance.CurrentUser.totalPoints));
                repo.AddPoints("test", scorePoints );
                ShowFinalPanel();

            }
        }


    }
    private void UpdateTimerUI()
    {
        int totalSeconds = Mathf.FloorToInt(currentTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        if (Timer != null)
        {
            Timer.text = "Timer: " + $"{minutes:00}:{seconds:00}";
        }
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
        if (!currentLevel.randomOrientation) return true;

        float z = piece.localEulerAngles.z;
        z = Mathf.Round(z / 90f) * 90f;
        z = (z + 360f) % 360f;

        return Mathf.Approximately(z, 0f);
    }


    private void ShowFinalPanel()
    {
        if (finalText != null)
        {
            finalText.text =
                "Поздравления! " +
                "Завършихте нивото\n" +
                $"Спечелени точки: {piecesCorrect}";
        }

        finalPanel.SetActive(true);
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene("HomePage");
    }

 
    public void RequestHelp()
    {
       
    }
   
  
   

}



