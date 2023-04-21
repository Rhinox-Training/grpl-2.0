using Rhinox.Lightspeed;
using Rhinox.XR.Grapple.It;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimonSaysColor : MonoBehaviour
{

    [SerializeField] private List<GRPLButtonInteractable> _buttons = new List<GRPLButtonInteractable>();
    [SerializeField] private GRPLButtonInteractable _startButton = null;

    [SerializeField] private List<MeshRenderer> _outputImages = new List<MeshRenderer>();
    [SerializeField] private TextMeshPro _textScore = null;

    [SerializeField] private List<Material> _onMaterials = new List<Material>();
    [SerializeField] private List<Material> _offMaterials = new List<Material>();

    [SerializeField] private float _sequenceStartDelay = 3.0f;
    [SerializeField] private float _sequenceDelayOn = 1.0f;
    [SerializeField] private float _sequenceDelayOff = 0.5f;
    //public float inputTimeout = 2.0f;
    public int _score = 0;


    private List<int> _sequence = new List<int>();
    private int _inputIndex = 0;
    private bool _isPlayingSequence = false;
    private int sequenceStartSize = 4;

    private int _sequenceIndex = 0;
    private const int _scoreWhenWon = 10;
    //private float lastInputTime = 0.0f;

    //// Start is called before the first frame update
    void Start()
    {
        _textScore.text = "0";

        foreach (var button in _buttons)
        {
            button.ButtonDown += ProcessBtnDown;
        }

        _startButton.ButtonDown += StartGame;
    }

    private void StartGame(GRPLButtonInteractable button)
    {
        if (!_isPlayingSequence)
        {
            GenerateNewSequence();

            //reset all back to off
            for (int index = 0; index < _outputImages.Count; ++index)
                _outputImages[index].material = _offMaterials[index];

            _isPlayingSequence = true;
            _sequenceIndex = 0;
            StartCoroutine(PlaySequence());
        }
    }

    private void GenerateNewSequence()
    {
        _sequence.Clear();

        for (int idx = 0; idx < sequenceStartSize; idx++)
        {
            _sequence.Add(Random.Range(0, _buttons.Count));
        }
    }

    // Update is called once per frame
    void Update()
    {
        return;

        // If the _sequence is being played, don't allow player input
        //if (_isPlayingSequence)
        //{
        //    return;
        //}

        //// Check for player input
        //for (int i = 0; i < _buttons.Count; i++)
        //{
        //    if (_buttons[i].interactable && Input.GetKeyDown(KeyCode.Alpha1 + i))
        //    {
        //        //playerSequence.Add(_buttons[i].GetComponent<Image>().color);
        //        lastInputTime = Time.time;
        //        _buttons[i].interactable = false;
        //        StartCoroutine(ResetButton(i));
        //    }
        //}

        //// Check if player input matches the _sequence
        //if (playerSequence.Count > 0 && playerSequence[playerSequence.Count - 1] != _sequence[playerSequence.Count - 1])
        //{
        //    Debug.Log("Wrong!");
        //    playerSequence.Clear();
        //    _sequenceIndex = 0;
        //    _isPlayingSequence = true;
        //    StartCoroutine(PlaySequence());
        //}

        //// Check if player has completed the _sequence
        //if (playerSequence.Count == _sequence.Count)
        //{
        //    Debug.Log("Correct!");
        //    playerSequence.Clear();
        //    _sequenceIndex = 0;
        //    _isPlayingSequence = true;
        //    score += 10;
        //    StartCoroutine(PlaySequence());
        //}

        //// Check for input timeout
        //if (Time.time - lastInputTime > inputTimeout && playerSequence.Count > 0)
        //{
        //    Debug.Log("Timeout!");
        //    playerSequence.Clear();
        //    _sequenceIndex = 0;
        //    _isPlayingSequence = true;
        //    StartCoroutine(PlaySequence());
        //}
    }

    private void ProcessBtnDown(GRPLButtonInteractable button)
    {
        if (!_isPlayingSequence)
        {
            int idx = _buttons.FindIndex(i => i == button);

            if (_sequence[_inputIndex] != idx)
            {
                GameFailed();
                return;
            }

            ++_inputIndex;

            if (_inputIndex == _sequence.Count)
            {
                NextLevel();
            }
        }
    }

    /// <summary>
    /// Add score, add next item in the sequence, play the new extended sequence
    /// </summary>
    private void NextLevel()
    {
        _score += _scoreWhenWon;
        _textScore.text = _score.ToString();

        _inputIndex = 0;
        _sequence.Add(Random.Range(0, _buttons.Count));

        //show extended sequence
        _isPlayingSequence = true;
        _sequenceIndex = 0;
        StartCoroutine(PlaySequence());
    }

    private void GameFailed()
    {
        _inputIndex = 0;
        _score = 0;

        _textScore.text = _score.ToString();

        for (int index = 0; index < _outputImages.Count; ++index)
        {
            //set all to red
            _outputImages[index].material = _offMaterials[0];
        }
    }


    private IEnumerator PlaySequence()
    {
        //when starting to play sequence wait a bit longer to make sure player is ready
        if (_sequenceIndex == 0)
            yield return new WaitForSeconds(_sequenceStartDelay);

        //set all outputs to OFF and turn only the one from the sequence to ON
        for (int index = 0; index < _outputImages.Count; ++index)
        {
            if (index == _sequence[_sequenceIndex])
                _outputImages[index].material = _onMaterials[index];
            else
                _outputImages[index].material = _offMaterials[index];
        }

        yield return new WaitForSeconds(_sequenceDelayOn);

        //reset all outputs back to OFF
        for (int index = 0; index < _outputImages.Count; ++index)
        {
            _outputImages[index].material = _offMaterials[index];
        }

        yield return new WaitForSeconds(_sequenceDelayOff);

        //goto next part of sequence or end playing of sequence
        _sequenceIndex++;
        if (_sequenceIndex < _sequence.Count)
        {
            StartCoroutine(PlaySequence());
        }
        else
        {
            //reset all back to off
            for (int index = 0; index < _outputImages.Count; ++index)
            {
                _outputImages[index].material = _offMaterials[index];
            }

            _isPlayingSequence = false;
        }
    }
}
