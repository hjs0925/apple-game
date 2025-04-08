using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Linq;

public class GameControl : MonoBehaviour
{
    public enum GameMode { Add, Subtract, AP, GP }

    [Header("Editor Setting")]
    public GameObject canvas;
    public GameObject apple;
    public RectTransform selectBox;
    public RectTransform collideBox;
    public TextMeshProUGUI scoreText;
    public GameObject gameOver;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI addScoreText;
    public RectTransform hintBox;
    public TextMeshProUGUI modeText;
    public TimeManager TM;
    public Button Hint;
    public TextMeshProUGUI noHintText;

    [Header("Game Settings")]
    public int horizontalLength;
    public int verticalLength;
    public float comboTime;

    [Header("Only for Read")]
    public int nothing;
    public int score = 0;
    public float comboDelta = 0f;
    public int comboCnt;
    public GameMode currentMode = GameMode.Add;

    private Vector3 zeroPos;
    private Vector2 startpos = Vector2.zero;
    private Rect selectRect = new Rect();
    private List<AppleMeta> applesList = new List<AppleMeta>();
    private List<AppleMeta> selectList = new List<AppleMeta>();
    private int[,] mapApple;
    public List<Vector4> hints = new List<Vector4>();
    private List<Vector2Int> answerCoors = new List<Vector2Int>();
    private Dictionary<GameMode, BoardState> savedStates = new();
    private bool usedHintInAdd = false;
    private bool usedHintInSubtract = false;
    private bool usedHintInAP = false;
    private bool usedHintInGP = false;

    private void Awake()
    {
        zeroPos = apple.transform.position;

        if (currentMode == GameMode.Subtract && horizontalLength % 2 != 0)
            horizontalLength--;

        mapApple = new int[verticalLength, horizontalLength];
        
        LoadGameMode();
        
        InitBoard();
    }

    private void Start()
    {
        Screen.SetResolution(1920, 1080, true);
        foreach (AppleMeta a in applesList) { a.gameObject.SetActive(true); a.isOn = true; }
        gameOver.SetActive(false);
        FindHints();
        UpdateModeUI();
    }

    private void Update()
    {
        if (TM.isEnd)
        {
            if (!gameOver.activeSelf)
            {
                gameOver.SetActive(true);
                clearGame();
            }
            return;
        }

        DragSystem();
        SelectSystem();
        calculateAnswer();
        scoreText.text = $"Score: {score}";
    }

    private void clearGame()
    {
        hintBox.gameObject.SetActive(false);
        foreach (var apple in applesList) apple.isOn = false;
        gameOverText.text = $"Game Over\n\nYour Score: {score}";
    }

    // 선택한 사과들의 계산을 수행하는 함수
    private void calculateAnswer()
    {
        // 마우스 버튼이 떼어지지 않았으면 계산하지 않음
        if (!Input.GetMouseButtonUp(0)) return;

        // 선택된 사과의 개수
        int cnt = selectList.Count;
        int result = 0;

        // 더하기 모드: 선택한 사과들의 숫자 합이 10인지 확인
        if (currentMode == GameMode.Add)
        {
            result = selectList.Sum(a => a.number);
            if (result == 10) OnCorrectAnswer(cnt);
        }
        // 빼기 모드: 가장 큰 숫자에서 나머지 숫자들의 합을 뺀 결과가 0인지 확인
        else if (currentMode == GameMode.Subtract && cnt > 1)
        {
            var maxApple = selectList.OrderByDescending(a => a.number).First();
            result = maxApple.number - selectList.Where(a => a != maxApple).Sum(a => a.number);
            if (result == 0) OnCorrectAnswer(cnt);
        }
        // 등차수열 모드: 선택한 사과들이 등차수열을 이루는지 확인 (최소 3개 이상)
        else if (currentMode == GameMode.AP && cnt >= 3)
        {
            // 세로줄인지 확인 (모든 사과가 같은 열에 있는지)
            bool isVertical = selectList.All(a => a.coor.y == selectList[0].coor.y);
            
            List<int> values;
            if (isVertical)
            {
                // 세로줄인 경우 위에서 아래로 정렬
                values = selectList.OrderBy(a => a.coor.x).Select(a => a.number).ToList();
            }
            else
            {
                // 가로줄인 경우 원래 순서 유지
                values = selectList.Select(a => a.number).ToList();
            }
            
            if (IsValidAP(values))
            {
                OnCorrectAnswer(cnt);
            }
        }
        // 등비수열 모드: 선택한 사과들이 등비수열을 이루는지 확인 (최소 3개 이상)
        else if (currentMode == GameMode.GP && cnt >= 3)
        {
            // 세로줄인지 확인 (모든 사과가 같은 열에 있는지)
            bool isVertical = selectList.All(a => a.coor.y == selectList[0].coor.y);
            
            List<int> values;
            if (isVertical)
            {
                // 세로줄인 경우 위에서 아래로 정렬
                values = selectList.OrderBy(a => a.coor.x).Select(a => a.number).ToList();
            }
            else
            {
                // 가로줄인 경우 원래 순서 유지
                values = selectList.Select(a => a.number).ToList();
            }
            
            if (IsValidGP(values))
            {
                OnCorrectAnswer(cnt);
            }
        }

        // 선택 목록 초기화
        selectList.Clear();
        answerCoors.Clear();
    }

    // 정답일 때 호출되는 함수
    private void OnCorrectAnswer(int cnt)
    {
        // 힌트 박스 숨기기
        hintBox.gameObject.SetActive(false);
        
        // 콤보 카운트 계산 (일정 시간 내에 연속으로 정답을 맞추면 콤보 증가)
        comboCnt = (Time.time - comboDelta < comboTime && comboDelta != 0) ? comboCnt + 1 : 0;
        comboDelta = Time.time;

        // 점수 계산: 선택한 사과 개수 + (3개 이상 선택 시 보너스 10점) + (콤보 × 5점)
        score += cnt + (cnt >= 3 ? 10 : 0) + comboCnt * 5;
        
        // 정답 애니메이션 표시
        animateCorrect(cnt, comboCnt, cnt >= 3);

        // 정답 사과들을 보드에서 제거
        foreach (Vector2Int coor in answerCoors)
            mapApple[coor.x, coor.y] = 0;

        // 힌트 다시 찾기
        hints.Clear();
        FindHints();
    }

    // 정답 애니메이션 표시 함수
    private void animateCorrect(int scoreValue, int combo, bool isMany)
    {
        // 선택한 사과들에 애니메이션 적용
        foreach (var a in selectList) a.isAnimated = true;
        
        // 선택 박스 중앙 위치 계산
        Vector2 midPos = (Vector2)selectBox.position + selectBox.rect.center;
        
        // 획득 점수 텍스트 생성 및 표시
        var addScore = Instantiate(addScoreText, midPos, Quaternion.identity, canvas.transform);
        addScore.gameObject.SetActive(true);
        addScore.GetComponent<AddScore>().print(scoreValue, combo, isMany);
    }

    // 등차수열 검증 함수
    private bool IsValidAP(List<int> values)
    {
        // 최소 3개 이상의 숫자가 필요
        if (values.Count < 3) return false;
        
        // 정렬하지 않고 원래 순서대로 검증
        // 첫 번째와 두 번째 숫자의 차이를 공차로 사용
        int d = values[1] - values[0];
        
        // 모든 연속된 숫자 쌍의 차이가 동일한지 확인
        for (int i = 1; i < values.Count; i++)
        {
            if (values[i] - values[i-1] != d)
                return false;
        }
        
        return true;
    }

    // 등비수열 검증 함수
    private bool IsValidGP(List<int> values)
    {
        // 최소 3개 이상의 숫자가 필요
        if (values.Count < 3) return false;
        
        // 모든 값이 양수인지 확인 (등비수열에 필요)
        if (values.Any(v => v <= 0)) return false;
        
        // 정렬하지 않고 원래 순서대로 검증
        // 첫 번째와 두 번째 숫자의 비율을 공비로 사용
        float r = (float)values[1] / values[0];
        
        // 모든 연속된 숫자 쌍의 비율이 동일한지 확인
        for (int i = 1; i < values.Count; i++)
        {
            float currentRatio = (float)values[i] / values[i-1];
            if (Mathf.Abs(currentRatio - r) > 0.0001f)
                return false;
        }
        
        return true;
    }

    // 빼기 모드에서 유효한 조합인지 확인하는 함수
    private bool IsValidSubtractCombo(List<int> values)
    {
        // 최소 2개 이상의 숫자가 필요
        if (values.Count < 2) return false;
        
        // 가장 큰 숫자를 찾아서 나머지 숫자들의 합을 뺀 결과가 0인지 확인
        int max = values.Max();
        values.Remove(max);
        return max - values.Sum() == 0;
    }

    public void OnToggleModeButton()
    {
        
    }

    private void UpdateHintButtonVisibility()
    {
        Hint.gameObject.SetActive(!(currentMode == GameMode.Add && usedHintInAdd || currentMode == GameMode.Subtract && usedHintInSubtract || currentMode == GameMode.AP && usedHintInAP|| currentMode == GameMode.GP && usedHintInGP));
        noHintText.gameObject.SetActive(false);
        hintBox.gameObject.SetActive(false);
    }

    private void UpdateModeUI()
    {
        if (modeText != null)
        {
            switch (currentMode)
            {
                case GameMode.Add:
                    modeText.text = "현재 모드: 더하기";
                    break;
                case GameMode.Subtract:
                    modeText.text = "현재 모드: 빼기";
                    break;
                case GameMode.AP:
                    modeText.text = "현재 모드: 등차수열";
                    break;
                case GameMode.GP:
                    modeText.text = "현재 모드: 등비수열";
                    break;
            }
        }
    }

    private void DragSystem()
    {
        if (Input.GetMouseButtonDown(0)) { selectBox.gameObject.SetActive(true); startpos = Input.mousePosition; }
        if (Input.GetMouseButtonUp(0)) { selectBox.gameObject.SetActive(false); }

        if (Input.GetMouseButton(0))
        {
            selectRect = Rect.MinMaxRect(
                Mathf.Clamp(Mathf.Min(Input.mousePosition.x, startpos.x), 0, Screen.width),
                Mathf.Clamp(Mathf.Min(Input.mousePosition.y, startpos.y), 0, Screen.height),
                Mathf.Clamp(Mathf.Max(Input.mousePosition.x, startpos.x), 0, Screen.width),
                Mathf.Clamp(Mathf.Max(Input.mousePosition.y, startpos.y), 0, Screen.height));
        }

        selectBox.offsetMin = selectRect.min;
        selectBox.offsetMax = selectRect.max;

        Vector2 worldMin = Camera.main.ScreenToWorldPoint(selectRect.min);
        Vector2 worldMax = Camera.main.ScreenToWorldPoint(selectRect.max);
        Vector2 padding = new Vector2(0.5f, 0.5f);
        collideBox.position = (worldMin + worldMax) / 2f;
        collideBox.sizeDelta = worldMax - worldMin + padding;
    }

    private void SelectSystem()
    {
        if (Input.GetMouseButtonUp(0))
        {
            foreach (var a in applesList) if (a.isSelected) selectList.Add(a);
        }
        else
        {
            foreach (var a in applesList) a.isSelected = false;
        }

        if (selectBox.gameObject.activeSelf)
        {
            foreach (var a in applesList) a.isSelected = false;
            var hit = Physics2D.OverlapBoxAll(collideBox.anchoredPosition, collideBox.sizeDelta, 0f);
            foreach (var i in hit)
            {
                if (!i.CompareTag("apple")) continue;
                var hitApple = i.GetComponent<AppleMeta>();
                hitApple.isSelected = true;
            }
        }
    }

    private AppleMeta GetAppleAt(int x, int y)
    {
        return applesList.FirstOrDefault(a => a.coor.x == x && a.coor.y == y && a.isOn);
    }

    // 보드에서 가능한 모든 힌트를 찾는 함수
    private void FindHints()
    {
        // 기존 힌트 목록 초기화
        hints.Clear();
        
        // 보드의 모든 위치에서 가로/세로 방향으로 힌트 검사
        for (int x = 0; x < verticalLength; x++)
        {
            for (int y = 0; y < horizontalLength; y++)
            {
                // 가로 방향으로 힌트 검사 (dx=1, dy=0)
                CheckHintLine(x, y, 1, 0, horizontalLength - y);
                // 세로 방향으로 힌트 검사 (dx=0, dy=1)
                CheckHintLine(x, y, 0, 1, verticalLength - x);
            }
        }
    }

    // 특정 위치에서 특정 방향으로 힌트를 찾는 함수
    private void CheckHintLine(int sx, int sy, int dx, int dy, int maxLength)
    {
        // 2개 이상의 연속된 사과부터 검사
        for (int len = 2; len <= maxLength; len++)
        {
            List<int> values = new();
            List<Vector2Int> coords = new();
            bool hasEmpty = false;

            // 현재 방향으로 len개의 사과를 확인
            for (int k = 0; k < len; k++)
            {
                var a = GetAppleAt(sx + k * dx, sy + k * dy);
                if (a == null) { hasEmpty = true; break; }
                values.Add(a.number);
                coords.Add(a.coor);
            }

            // 빈 공간이 있으면 다음 길이 검사
            if (hasEmpty) continue;

            bool isValid = false;
            
            // 세로줄인지 확인 (dx가 0인 경우)
            bool isVertical = dx == 0;
            
            // 등차/등비수열 모드에서 세로줄인 경우 위에서 아래로 정렬
            if (isVertical && (currentMode == GameMode.AP || currentMode == GameMode.GP))
            {
                // 세로줄인 경우 위에서 아래로 정렬
                var sortedIndices = Enumerable.Range(0, coords.Count)
                    .OrderBy(i => coords[i].x)
                    .ToList();
                
                List<int> sortedValues = new();
                foreach (var idx in sortedIndices)
                {
                    sortedValues.Add(values[idx]);
                }
                
                values = sortedValues;
            }
            
            // 현재 게임 모드에 따라 유효한 조합인지 확인
            switch (currentMode)
            {
                case GameMode.Add:
                    // 더하기 모드: 합이 10인지 확인
                    isValid = values.Sum() == 10;
                    break;
                case GameMode.Subtract:
                    // 빼기 모드: 가장 큰 수에서 나머지 합을 뺀 결과가 0인지 확인
                    isValid = IsValidSubtractCombo(values);
                    break;
                case GameMode.AP:
                    // 등차수열 모드: 3개 이상의 연속된 숫자가 등차수열을 이루는지 확인
                    isValid = len >= 3 && IsValidAP(values);
                    break;
                case GameMode.GP:
                    // 등비수열 모드: 3개 이상의 연속된 숫자가 등비수열을 이루는지 확인
                    isValid = len >= 3 && IsValidGP(values);
                    break;
            }
            
            // 유효한 조합이면 힌트 목록에 추가
            if (isValid)
                hints.Add(new Vector4(sx, sy, sx + (len - 1) * dx, sy + (len - 1) * dy));
        }
    }

    // 힌트 버튼 클릭 시 호출되는 함수
    public void OnHintButton()
    {
        // 이미 힌트를 사용한 모드면 무시
        if ((currentMode == GameMode.Add && usedHintInAdd) || 
            (currentMode == GameMode.Subtract && usedHintInSubtract) ||
            (currentMode == GameMode.AP && usedHintInAP) ||
            (currentMode == GameMode.GP && usedHintInGP)) return;
        
        // 가능한 힌트 찾기
        FindHints();
        
        // 힌트가 없으면 메시지 표시
        if (hints.Count == 0)
        {
            hintBox.gameObject.SetActive(false);
            StartCoroutine(ShowNoHintMessage());
            return;
        }

        // 현재 모드에 따라 힌트 사용 표시
        switch (currentMode)
        {
            case GameMode.Add:
                usedHintInAdd = true;
                break;
            case GameMode.Subtract:
                usedHintInSubtract = true;
                break;
            case GameMode.AP:
                usedHintInAP = true;
                break;
            case GameMode.GP:
                usedHintInGP = true;
                break;
        }

        // 힌트 버튼 비활성화
        Hint.gameObject.SetActive(false);

        // 첫 번째 힌트 표시
        Vector4 hint = hints[0];
        Vector2 hintOffsetMin = new Vector2(zeroPos.x + hint.y - 0.5f, zeroPos.y + hint.x - 0.5f);
        Vector2 hintOffsetMax = new Vector2(zeroPos.x + hint.w + 0.5f, zeroPos.y + hint.z + 0.5f);
        hintBox.offsetMin = Camera.main.WorldToScreenPoint(hintOffsetMin);
        hintBox.offsetMax = Camera.main.WorldToScreenPoint(hintOffsetMax);
        hintBox.gameObject.SetActive(true);
    }

    // 힌트가 없을 때 메시지를 표시하는 코루틴
    private IEnumerator ShowNoHintMessage()
    {
        // 힌트 박스 숨기기
        hintBox.gameObject.SetActive(false);
        noHintText.gameObject.SetActive(true);
        noHintText.text = "힌트 없음!";
        
        // 페이드 아웃 효과를 위한 CanvasGroup 컴포넌트 가져오기
        CanvasGroup cg = noHintText.GetComponent<CanvasGroup>();
        if (cg == null) cg = noHintText.gameObject.AddComponent<CanvasGroup>();
        
        // 2초 동안 페이드 아웃
        float duration = 2f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }
        
        // 메시지 숨기기
        noHintText.gameObject.SetActive(false);
        cg.alpha = 1f;
    }

    public void OnResetButton() => SceneManager.LoadScene("Main");
    public void OnHomeButton() => SceneManager.LoadScene("Title");

    private void SaveCurrentModeState()
    {
        savedStates[currentMode] = new BoardState(applesList, mapApple);
    }

    private void LoadOrInitModeState()
    {
        if (savedStates.TryGetValue(currentMode, out var state))
        {
            applesList = new();
            mapApple = (int[,])state.map.Clone();
            foreach (var meta in state.apples)
            {
                GameObject go = Instantiate(apple, meta.position, Quaternion.identity);
                AppleMeta am = go.GetComponent<AppleMeta>();
                am.number = meta.number;
                am.coor = meta.coor;
                am.isOn = meta.isOn;
                go.name = $"Apple ({meta.coor.x}, {meta.coor.y})";
                go.SetActive(meta.isOn);
                applesList.Add(am);
            }
            FindHints();
        }
        else InitBoard();
    }

    private void InitBoard()
    {
        applesList.Clear();
        selectList.Clear();
        mapApple = new int[verticalLength, horizontalLength];

        if (currentMode == GameMode.Subtract)
        {
            // 빼기 모드 초기화
            List<int> numbers = new();
            int pairCount = (horizontalLength * verticalLength) / 2;

            for (int i = 0; i < pairCount; i++)
            {
                // 랜덤한 숫자 쌍 생성 (차이가 10인 두 숫자)
                int baseNum = Random.Range(11, 20);
                int secondNum = baseNum - 10;
                if (Random.value < 0.5f) { numbers.Add(baseNum); numbers.Add(secondNum); }
                else { numbers.Add(secondNum); numbers.Add(baseNum); }
            }

            List<Vector2Int> coords = new();
            for (int i = 0; i < verticalLength; i++)
                for (int j = 0; j < horizontalLength; j++)
                    coords.Add(new(i, j));

            // 좌표 랜덤 섞기
            for (int i = 0; i < coords.Count; i++)
            {
                int r = Random.Range(i, coords.Count);
                (coords[i], coords[r]) = (coords[r], coords[i]);
            }

            for (int i = 0; i < coords.Count && i < numbers.Count; i++)
            {
                Vector2Int coor = coords[i];
                Vector3 newPos = new(zeroPos.x + coor.y, zeroPos.y + coor.x, zeroPos.z);
                GameObject go = Instantiate(apple, newPos, Quaternion.identity);
                AppleMeta am = go.GetComponent<AppleMeta>();
                am.number = numbers[i]; am.coor = coor; am.isOn = true;
                go.name = $"Apple ({coor.x}, {coor.y})"; go.SetActive(true);
                mapApple[coor.x, coor.y] = am.number;
                applesList.Add(am);
            }

            if (numbers.Count < coords.Count)
            {
                Vector2Int coor = coords[^1];
                Vector3 newPos = new(zeroPos.x + coor.y, zeroPos.y + coor.x, zeroPos.z);
                GameObject go = Instantiate(apple, newPos, Quaternion.identity);
                AppleMeta am = go.GetComponent<AppleMeta>();
                am.number = Random.Range(1, 10); am.coor = coor; am.isOn = false;
                go.name = $"Apple ({coor.x}, {coor.y})"; go.SetActive(true);
                mapApple[coor.x, coor.y] = am.number;
                applesList.Add(am);
            }
        }
        else if (currentMode == GameMode.AP)
        {
            // 등차수열 모드 초기화
            List<int> numbers = new();
            int sequencesCount = (horizontalLength * verticalLength) / 3;
            
            for (int i = 0; i < sequencesCount; i++)
            {
                // 등차수열 생성 (공차가 같은 연속된 숫자들)
                int startNum = Random.Range(1, 8);
                int diff = Random.Range(1, 5);
                int seqLength = Random.Range(3, 6);
                
                for (int j = 0; j < seqLength; j++)
                {
                    numbers.Add(startNum + j * diff);
                }
            }
            
            // 나머지 공간을 랜덤 숫자로 채우기
            while (numbers.Count < horizontalLength * verticalLength)
            {
                numbers.Add(Random.Range(1, 20));
            }
            
            // 숫자 랜덤 섞기
            for (int i = numbers.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int temp = numbers[i];
                numbers[i] = numbers[j];
                numbers[j] = temp;
            }
            
            // 보드에 숫자 배치
            int index = 0;
            for (int i = 0; i < verticalLength; i++)
            {
                for (int j = 0; j < horizontalLength; j++)
                {
                    Vector3 pos = new(zeroPos.x + j, zeroPos.y + i, zeroPos.z);
                    GameObject go = Instantiate(apple, pos, Quaternion.identity);
                    AppleMeta am = go.GetComponent<AppleMeta>();
                    am.number = numbers[index++];
                    am.coor = new(i, j);
                    am.isOn = true;
                    go.name = $"Apple ({i}, {j})";
                    go.SetActive(true);
                    mapApple[i, j] = am.number;
                    applesList.Add(am);
                }
            }
        }
        else if (currentMode == GameMode.GP)
        {
            // 등비수열 모드 초기화
            List<int> numbers = new();
            int sequencesCount = (horizontalLength * verticalLength) / 3;
            
            for (int i = 0; i < sequencesCount; i++)
            {
                // 등비수열 생성 (공비가 같은 연속된 숫자들)
                int startNum = Random.Range(1, 5);
                int ratio = Random.Range(2, 4);
                int seqLength = Random.Range(3, 5);
                
                int currentNum = startNum;
                for (int j = 0; j < seqLength; j++)
                {
                    numbers.Add(currentNum);
                    currentNum *= ratio;
                }
            }
            
            // 나머지 공간을 랜덤 숫자로 채우기
            while (numbers.Count < horizontalLength * verticalLength)
            {
                numbers.Add(Random.Range(1, 20));
            }
            
            // 숫자 랜덤 섞기
            for (int i = numbers.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int temp = numbers[i];
                numbers[i] = numbers[j];
                numbers[j] = temp;
            }
            
            // 보드에 숫자 배치
            int index = 0;
            for (int i = 0; i < verticalLength; i++)
            {
                for (int j = 0; j < horizontalLength; j++)
                {
                    Vector3 pos = new(zeroPos.x + j, zeroPos.y + i, zeroPos.z);
                    GameObject go = Instantiate(apple, pos, Quaternion.identity);
                    AppleMeta am = go.GetComponent<AppleMeta>();
                    am.number = numbers[index++];
                    am.coor = new(i, j);
                    am.isOn = true;
                    go.name = $"Apple ({i}, {j})";
                    go.SetActive(true);
                    mapApple[i, j] = am.number;
                    applesList.Add(am);
                }
            }
        }
        else
        {
            // 더하기 모드 초기화
            for (int i = 0; i < verticalLength; i++)
            {
                for (int j = 0; j < horizontalLength; j++)
                {
                    Vector3 pos = new(zeroPos.x + j, zeroPos.y + i, zeroPos.z);
                    GameObject go = Instantiate(apple, pos, Quaternion.identity);
                    AppleMeta am = go.GetComponent<AppleMeta>();
                    // 랜덤 숫자 생성 (1~9)
                    int randNum = Random.Range(1, 10);
                    am.number = randNum; am.coor = new(i, j); am.isOn = true;
                    go.name = $"Apple ({i}, {j})"; go.SetActive(true);
                    mapApple[i, j] = randNum;
                    applesList.Add(am);
                }
            }
        }

        FindHints();
    }

    private void LoadGameMode()
    {
        if (PlayerPrefs.HasKey("GameMode"))
        {
            int modeIndex = PlayerPrefs.GetInt("GameMode");
            if (System.Enum.IsDefined(typeof(GameMode), modeIndex))
            {
                currentMode = (GameMode)modeIndex;
            }
        }
    }
    
    public static void SaveGameMode(GameMode mode)
    {
        PlayerPrefs.SetInt("GameMode", (int)mode);
        PlayerPrefs.Save();
    }

    [System.Serializable]
    public class AppleMetaData
    {
        public int number;
        public Vector2Int coor;
        public Vector3 position;
        public bool isOn;
    }

    [System.Serializable]
    public class BoardState
    {
        public List<AppleMetaData> apples;
        public int[,] map;

        public BoardState(List<AppleMeta> appleList, int[,] mapApple)
        {
            apples = appleList.Select(am => new AppleMetaData
            {
                number = am.number,
                coor = am.coor,
                position = am.transform.position,
                isOn = am.isOn
            }).ToList();
            map = (int[,])mapApple.Clone();
        }
    }
}
