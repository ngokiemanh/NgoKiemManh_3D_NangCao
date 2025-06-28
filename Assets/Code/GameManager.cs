using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Canvas")]
    public GameObject canvasHome;
    public GameObject canvasHowToPlay;

    [Header("Level Prefabs")]
    public GameObject[] levelPrefabs;

    [Header("UI Score")]
    public TextMeshProUGUI scoreText;
    public GameObject scoreTextCanvas;

    [Header("Audio Clips")]
    public AudioClip buttonClickSound;
    public AudioClip winSound;
    public AudioClip loseSound;
    public AudioClip backgroundMusic;

    private int currentLevelIndex = 0;
    private GameObject currentLevel;

    private int score = 0;
    private int bestScore = int.MaxValue;
    private TextMeshProUGUI bestScoreTextInWinCanvas;

    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;

        if (backgroundMusic != null)
        {
            audioSource.clip = backgroundMusic;
            audioSource.Play();
        }

        ShowHome();
    }

    public void ShowHome()
    {
        canvasHome.SetActive(true);
        canvasHowToPlay?.SetActive(false);

        if (scoreTextCanvas != null)
            scoreTextCanvas.SetActive(false);

        UnloadCurrentLevel();
    }

    public void ShowHowToPlay()
    {
        canvasHome?.SetActive(false);
        canvasHowToPlay?.SetActive(true);
        PlayClickSound();
    }

    public void ExitHowToPlay()
    {
        ShowHome();
        PlayClickSound();
    }

    public void PlayGame()
    {
        LoadLevel(0);
        PlayClickSound();
    }

    public void LoadLevel(int levelIndex)
    {
        UnloadCurrentLevel();

        if (levelIndex >= 0 && levelIndex < levelPrefabs.Length)
        {
            currentLevelIndex = levelIndex;
            currentLevel = Instantiate(levelPrefabs[levelIndex]);

            canvasHome.SetActive(false);
            canvasHowToPlay.SetActive(false);
            SetupLevelUIButtons();

            score = 0;
            bestScore = PlayerPrefs.GetInt("BestScore_Level" + currentLevelIndex, int.MaxValue);
            UpdateScoreUI();

            if (scoreTextCanvas != null)
                scoreTextCanvas.SetActive(true);

            if (scoreText != null)
                scoreText.text = "Score: 0";

            if (currentLevel != null)
            {
                TextMeshProUGUI[] allTexts = currentLevel.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (TextMeshProUGUI txt in allTexts)
                {
                    if (txt.CompareTag("BestScoreText"))
                    {
                        bestScoreTextInWinCanvas = txt;
                        bestScoreTextInWinCanvas.gameObject.SetActive(false);
                        break;
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("Level index out of range!");
        }
    }

    public void ReplayLevel()
    {
        LoadLevel(currentLevelIndex);
        PlayClickSound();
    }

    public void NextLevel()
    {
        LoadLevel(currentLevelIndex + 1);
        PlayClickSound();
    }

    private void UnloadCurrentLevel()
    {
        if (currentLevel != null)
        {
            Destroy(currentLevel);
            currentLevel = null;
        }

        foreach (var obj in GameObject.FindGameObjectsWithTag("Player"))
        {
            Destroy(obj);
        }
    }

    private void SetupLevelUIButtons()
    {
        if (currentLevel == null) return;

        Button[] buttons = currentLevel.GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            btn.onClick.RemoveAllListeners();

            string btnName = btn.gameObject.name.ToLower();

            // Không gán âm thanh cho các nút điều hướng
            bool isMovementButton = btnName.Contains("up") || btnName.Contains("down") || btnName.Contains("left") || btnName.Contains("right");

            if (!isMovementButton)
                btn.onClick.AddListener(PlayClickSound);

            if (btn.CompareTag("ButtonHome"))
            {
                btn.onClick.AddListener(() =>
                {
                    foreach (Canvas c in FindObjectsOfType<Canvas>())
                    {
                        if (c.gameObject != canvasHome)
                            c.gameObject.SetActive(false);
                    }
                    ShowHome();
                });
            }
            else if (btn.CompareTag("ButtonReplay"))
            {
                btn.onClick.AddListener(() => ReplayLevel());
            }
            else if (btn.CompareTag("ButtonNextLevel"))
            {
                btn.onClick.AddListener(() => NextLevel());
            }
            else if (btn.CompareTag("PlayGame"))
            {
                btn.onClick.AddListener(() => PlayGame());
            }
            else if (btn.CompareTag("HowToPlay"))
            {
                btn.onClick.AddListener(() => ShowHowToPlay());
            }
        }
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
    }

    public void OnLevelWin()
    {
        if (score < bestScore)
        {
            bestScore = score;
            PlayerPrefs.SetInt("BestScore_Level" + currentLevelIndex, bestScore);
            PlayerPrefs.Save();
        }

        if (bestScoreTextInWinCanvas != null)
        {
            bestScoreTextInWinCanvas.text = "Best: " + bestScore;
            bestScoreTextInWinCanvas.gameObject.SetActive(true);
        }

        PlayWinSound();
    }

    public void OnLevelLose()
    {
        PlayLoseSound();
    }

    public void PlayClickSound()
    {
        if (buttonClickSound != null)
            AudioSource.PlayClipAtPoint(buttonClickSound, Camera.main.transform.position);
    }

    public void PlayWinSound()
    {
        if (winSound != null)
            AudioSource.PlayClipAtPoint(winSound, Camera.main.transform.position);
    }

    public void PlayLoseSound()
    {
        if (loseSound != null)
            AudioSource.PlayClipAtPoint(loseSound, Camera.main.transform.position);
    }

}
