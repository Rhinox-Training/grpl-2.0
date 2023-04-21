using Rhinox.XR.Grapple;
using Rhinox.XR.Grapple.It;
using System.Collections;
using TMPro;
using UnityEngine;

public enum RPSHandState
{
    Rock,
    Paper,
    Scissors
}

public class RockPaperScissors : MonoBehaviour
{
    [Header("Hand Models")]
    [SerializeField] private GameObject _rockHand;
    [SerializeField] private GameObject _paperHand;
    [SerializeField] private GameObject _scissorsHand;

    [Header("Hand Materials")]
    [SerializeField] private Material _ghostMat = null;
    [SerializeField] private Material _normalMat = null;

    [Header("Buttons")]
    [SerializeField] private GRPLButtonInteractable _startButton = null;
    [SerializeField] private GRPLButtonInteractable _stopButton = null;

    [Header("Cycle settings")]
    [SerializeField] private float _handCycleDelay = 0.5f;
    [SerializeField] private float _handCycleDuration = 3f;

    [Header("Score Output")]
    [SerializeField] private TextMeshPro _playerScoreText = null;
    [SerializeField] private TextMeshPro _computerScoreText = null;

    [Header("Gesture Output")]
    [SerializeField] private TextMeshPro _currentGestureVisual = null;

    [Header("Other")]
    [SerializeField] private GRPLTeleport _teleportScript = null;

    private SkinnedMeshRenderer _rockHandRenderer = null;
    private SkinnedMeshRenderer _paperHandRenderer = null;
    private SkinnedMeshRenderer _scissorsHandRenderer = null;


    private RPSHandState _playerHandState;
    private RPSHandState _computerHandState;

    private int _playerScore = 0;
    private int _computerScore = 0;
    private const float _timeBetweenGames = 1.5f;

    private const int _drawScore = 1;
    private const int _wonScore = 3;

    private Coroutine _gameLoop = null;
    private bool _initialized = false;

    private void Start()
    {
        _startButton.ButtonDown += StartGame;
        _stopButton.ButtonDown += StopGame;

        _currentGestureVisual.text = "";
        _playerScoreText.text = _playerScore.ToString();
        _computerScoreText.text = _computerScore.ToString();

        _rockHandRenderer = _rockHand.GetComponentInChildren<SkinnedMeshRenderer>();
        _paperHandRenderer = _paperHand.GetComponentInChildren<SkinnedMeshRenderer>();
        _scissorsHandRenderer = _scissorsHand.GetComponentInChildren<SkinnedMeshRenderer>();

        _rockHandRenderer.material = _ghostMat;

        _rockHand.SetActive(false);
        _paperHand.SetActive(false);
        _scissorsHand.SetActive(false);

        GRPLGestureRecognizer.GlobalInitialized += GestureRecognizerInitialized;

        _initialized = true;
    }

    private void OnEnable()
    {
        if (!_initialized)
            return;

        _startButton.ButtonDown += StartGame;
        _stopButton.ButtonDown += StopGame;

        GRPLGestureRecognizer.GlobalInitialized += GestureRecognizerInitialized;
    }

    private void OnDisable()
    {
        if (!_initialized)
            return;

        _startButton.ButtonDown -= StartGame;
        _stopButton.ButtonDown -= StopGame;

        GRPLGestureRecognizer.GlobalInitialized -= GestureRecognizerInitialized;
    }

    private void GestureRecognizerInitialized(GRPLGestureRecognizer gestureRecognizer)
    {
        gestureRecognizer.OnGestureRecognized.AddListener(GestureRecognized);
    }

    private void GestureRecognized(RhinoxHand _, string gestureName)
    {
        if (gestureName == "Grab") //grab gesture is basically a rock gesture
            _playerHandState = RPSHandState.Rock;
        else if (gestureName == "Paper")
            _playerHandState = RPSHandState.Paper;
        else if (gestureName == "Teleport") //teleport gesture is basically a scissor gesture
            _playerHandState = RPSHandState.Scissors;

        _currentGestureVisual.text = _playerHandState.ToString();
    }

    private void StartGame(GRPLButtonInteractable _)
    {
        if (_gameLoop == null)
        {
            //disable the teleport script
            _teleportScript.enabled = false;

            _playerScore = 0;
            _computerScore = 0;

            _playerScoreText.text = _playerScore.ToString();
            _computerScoreText.text = _computerScore.ToString();

            _gameLoop = StartCoroutine(GameLoop());
        }
    }

    private void StopGame(GRPLButtonInteractable _)
    {
        if (_gameLoop != null)
        {
            StopCoroutine(_gameLoop);
            _gameLoop = null;

            _rockHand.SetActive(false);
            _paperHand.SetActive(false);
            _scissorsHand.SetActive(false);

            _teleportScript.enabled = true;
        }
    }

    private IEnumerator GameLoop()
    {
        while (true)
        {
            // Cycle through the three hand models
            yield return CycleHands();

            //draw
            if (_playerHandState == _computerHandState)
            {
                _playerScore += _drawScore;
                _computerScore += _drawScore;
                _playerScoreText.text = _playerScore.ToString();
                _computerScoreText.text = _computerScore.ToString();
            }
            //did player win?
            else if ((_playerHandState == RPSHandState.Rock && _computerHandState == RPSHandState.Scissors)
                || (_playerHandState == RPSHandState.Paper && _computerHandState == RPSHandState.Rock)
                || (_playerHandState == RPSHandState.Scissors && _computerHandState == RPSHandState.Paper))
            {
                _playerScore += _wonScore;
                _playerScoreText.text = _playerScore.ToString();
            }
            //player lost
            else
            {
                _computerScore += _wonScore;
                _computerScoreText.text = _computerScore.ToString();
            }

            // Wait a short time before cycling hands again
            yield return new WaitForSeconds(_timeBetweenGames);
        }
    }

    private IEnumerator CycleHands()
    {
        _rockHandRenderer.material = _ghostMat;
        _paperHandRenderer.material = _ghostMat;
        _scissorsHandRenderer.material = _ghostMat;

        //Debug.Log("Hands cycle");

        float totalTimepast = 0f;

        var startTime = System.DateTime.Now;

        while (totalTimepast <= _handCycleDuration)
        {
            RPSHandState newHandState = (RPSHandState)Random.Range(0, 3);

            _rockHand.SetActive(newHandState == RPSHandState.Rock);
            _paperHand.SetActive(newHandState == RPSHandState.Paper);
            _scissorsHand.SetActive(newHandState == RPSHandState.Scissors);

            _computerHandState = newHandState;

            yield return new WaitForSeconds(_handCycleDelay);

            totalTimepast += (System.DateTime.Now - startTime).Seconds;
        }

        _rockHandRenderer.material = _normalMat;
        _paperHandRenderer.material = _normalMat;
        _scissorsHandRenderer.material = _normalMat;

        yield return null;
    }
}