using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Threading;
using UnityEngine;

public class SatelliteFlightScript : MonoBehaviour
{
    public const int SATELLITE_FLIGHT_RADIUS = 9;
    public const int REQUIRED_RESEARCH = 12;

    public List<KMSelectable> Buttons;

    public TextMesh Display;

    private bool _simulatorMode;

    private SatellitePuzzleState _puzzle;

    private List<int> _solution;

    private SatellitePuzzleState _currentRun;

    private int _infoIndex;

    private bool _solved;
    private int _moduleId;
    private static int _moduleIdCounter = 1;

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        KMBombModule module = GetComponent<KMBombModule>();
        KMAudio audio = GetComponent<KMAudio>();

        StartCoroutine(LoadPuzzle());

        for (int i = 0; i < 4; i++)
        {
            int i2 = i;
            Buttons[i].OnInteract += () =>
            {
                Buttons[i2].AddInteractionPunch();
                audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, Buttons[i2].transform);

                if (_solved || _puzzle == null)
                    return false;

                if (_simulatorMode)
                {
                    int research = _currentRun.TargetResearchLeft;

                    Log("You sent {0} signal.", new string[] { "an idle", "a thrust", "a right turn", "a left turn" });
                    if (i2 != 0 && _currentRun.PlayerFuel <= 0)
                    {
                        Log("You are out of fuel, so the satellite will idle instead.");
                    }

                    _currentRun = _currentRun.GenerateContinuation(_currentRun.PlayerFuel > 0 ? i2 - 1 : -1);
                    if (_currentRun.TargetResearchLeft < research)
                    {
                        //research done, maybe feedback?
                        Log("Research progress has been made.");

                        if (_currentRun.TargetResearchLeft == 0)
                        {
                            _solved = true;
                            SetDisplay("Solved");

                            module.HandlePass();
                            Log("You gathered the data. Module solved!");

                            return false;
                        }
                    }

                    SetDisplay(_currentRun.PlayerPosition + ", " + (_currentRun.PlayerRotation % 6 + 6) % 6 + "\n" + _currentRun.PlayerFuel + "/" + _puzzle.PlayerFuel + " " + _currentRun.GameAge + "/48\n" + (_puzzle.TargetResearchLeft - _currentRun.TargetResearchLeft) + "/" + _puzzle.TargetResearchLeft);

                    if (_currentRun.PlayerPosition.RingLength() > SATELLITE_FLIGHT_RADIUS || _currentRun.DyingPoint >= 0 || _currentRun.GameAge > 48)
                    {
                        if (_currentRun.PlayerPosition.RingLength() > SATELLITE_FLIGHT_RADIUS)
                            Log("You exited bounds at {0} and can no longer control the satellite. Strike!", _currentRun.PlayerPosition);
                        if (_currentRun.DyingPoint >= 0)
                            Log("You crashed at {0} and no longer have a satellite. Strike!", _currentRun.PlayerPosition);
                        if (_currentRun.GameAge > 48)
                            Log("You exceeded the satellite's lifetime and can no longer connect with the satellite. Strike!");
                        _simulatorMode = false;
                        _currentRun = null;

                        module.HandleStrike();

                        _infoIndex = 0;
                        SetDisplay(_puzzle.EntityDescriptions[0]);
                    }

                    return false;
                }

                switch (i2)
                {
                    case 0:
                        break;
                    case 1:
                        _simulatorMode = true;
                        _currentRun = new SatellitePuzzleState(_puzzle);
                        SetDisplay(_currentRun.PlayerPosition + ", " + (_currentRun.PlayerRotation % 6 + 6) % 6 + "\n" + _currentRun.PlayerFuel + "/" + _puzzle.PlayerFuel + " " + _currentRun.GameAge + "/48\n" + (_puzzle.TargetResearchLeft - _currentRun.TargetResearchLeft) + "/" + _puzzle.TargetResearchLeft);
                        break;
                    case 2:
                        _infoIndex = (_infoIndex + 1) % _puzzle.EntityDescriptions.Count;
                        SetDisplay(_puzzle.EntityDescriptions[_infoIndex]);
                        break;
                    case 3:
                        _infoIndex = (_infoIndex + _puzzle.EntityDescriptions.Count - 1) % _puzzle.EntityDescriptions.Count;
                        SetDisplay(_puzzle.EntityDescriptions[_infoIndex]);
                        break;
                    default:
                        break;
                }

                return false;
            };
        }
    }

    private bool _isThreading = false;
    private static bool _threadClaimed = false;
    public IEnumerator LoadPuzzle()
    {
        while (_puzzle == null)
        {
            SatellitePuzzleState puzzle = GeneratePuzzle();

            yield return null;
            while (_threadClaimed)
                yield return null;
            _threadClaimed = true;
            _isThreading = true;

            _solution = new List<int>();
            new Thread(() =>
            {
                _solution = EvaluatePuzzle(puzzle);
                _isThreading = false;
            }).Start();

            yield return new WaitUntil(() => !_isThreading);
            _threadClaimed = false;

            if (_solution == null || _solution.Count(x => x >= 0) < 4)
                continue;

            Log("The local space has the following entities: {0}.", puzzle.EntityDescriptions.Select(x => x.Replace("\n", ": ")).Join(", "));

            Log("An identified solution goes: {0}.", _solution.Select(x => new string[] { "idle", "thrust", "right", "left" }[x + 1]).Join(", "));

            _puzzle = puzzle;
            _puzzle.PlayerFuel = _solution.Count(x => x >= 0) + 3;
        }



        _infoIndex = 0;
        SetDisplay(_puzzle.EntityDescriptions[0]);
    }

    public void OnDestroy()
    {
        if (_isThreading)
        {
            _isThreading = false;
            _threadClaimed = false;
        }
    }

    public void SetDisplay(string text)
    {
        //Debug.Log("Displaying: " + text);
        Display.text = text;
    }

    public SatellitePuzzleState GeneratePuzzle()
    {
        SatelliteGravityGrid gravity = new SatelliteGravityGrid(SATELLITE_FLIGHT_RADIUS);
        int[] countPerType = { UnityEngine.Random.Range(7, 10), UnityEngine.Random.Range(4, 6) };

        List<SatelliteVector> vectors = new List<SatelliteVector>();
        for (int i = -SATELLITE_FLIGHT_RADIUS; i <= SATELLITE_FLIGHT_RADIUS; i++)
        {
            for (int j = -SATELLITE_FLIGHT_RADIUS; j <= SATELLITE_FLIGHT_RADIUS; j++)
            {
                SatelliteVector vector = new SatelliteVector(i, j, 0);
                if (vector.RingLength() > SATELLITE_FLIGHT_RADIUS)
                    continue;
                vectors.Add(vector);
            }
        }

        SatelliteVector goalVector;
        while (true)
        {
            goalVector = vectors.PickRandom();
            if (goalVector.RingLength() >= SATELLITE_FLIGHT_RADIUS || goalVector.RingLength() < 3)
                continue;

            gravity.AddGravityWell(goalVector, false);

            break;
        }

        List<int> toAdd = new List<int>();
        for (int i = 0; i < countPerType.Length; i++)
        {
            toAdd.AddRange(Enumerable.Repeat(i, countPerType[i]));
        }
        toAdd.Shuffle();

        List<SatelliteVector> planets = new List<SatelliteVector>();
        planets.Add(goalVector);
        List<SatelliteVector> stars = new List<SatelliteVector>();

        foreach (int item in toAdd)
        {
            int attempts = 100;
            while (attempts-- > 0)
            {
                SatelliteVector vector = vectors.PickRandom();
                if (vector.RingLength() < 3 ||
                    stars.Any(x => (x - vector).RingLength() <= 2) || planets.Any(x => (x - vector).RingLength() <= item + 1))
                    continue;

                switch (item)
                {
                    case 0:
                        gravity.AddGravityWell(vector, false);
                        planets.Add(vector);
                        break;
                    case 1:
                        gravity.AddGravityWell(vector, true);
                        stars.Add(vector);
                        break;
                    default:
                        break;
                }
                break;
            }
        }

        List<string> entities = new List<string>();
        entities.Add("Target\n" + goalVector);
        foreach (SatelliteVector planet in planets.Skip(1))
        {
            entities.Add("Planet\n" + planet);
        }
        foreach (SatelliteVector star in stars)
        {
            entities.Add("Star\n" + star);
        }


        SatellitePuzzleState puzzle = new SatellitePuzzleState(6, goalVector, REQUIRED_RESEARCH, gravity, planets.Concat(stars).ToList(), entities);

        return puzzle;
    }

    public List<int> EvaluatePuzzle(SatellitePuzzleState puzzle)
    {
        //int counter = 10000;
        List<int> solutionSequence = null;

        Dictionary<string, int> gameStates = new Dictionary<string, int>();

        Queue<SatellitePuzzleState> idleEval = new Queue<SatellitePuzzleState>();
        Queue<SatellitePuzzleState> movingEval = new Queue<SatellitePuzzleState>();
        Queue<List<int>> idleEvalMoves = new Queue<List<int>>();
        Queue<List<int>> movingEvalMoves = new Queue<List<int>>();

        SatellitePuzzleState currentState = new SatellitePuzzleState(puzzle);
        List<int> currentMoves = new List<int>();
        while (true)
        {
            //if (counter-- == 0)
            //    return null;
            //Debug.Log(currentMoves.Select(x => "-x><"[x + 1]).Join("") + ":" + currentState.GetData());
            string currentStateString = currentState.GetData();

            if (!(gameStates.ContainsKey(currentStateString) && gameStates[currentStateString] <= currentState.GameAge) && currentState.GameAge <= 36)
            {
                if (gameStates.ContainsKey(currentStateString))
                    gameStates[currentStateString] = currentState.GameAge;
                else
                    gameStates.Add(currentStateString, currentState.GameAge);

                if (currentState.TargetResearchLeft == 0 && (solutionSequence == null || solutionSequence.Count > currentMoves.Count))
                    solutionSequence = currentMoves;

                if (currentState.DyingPoint < 0 && currentState.PlayerPosition.RingLength() <= SATELLITE_FLIGHT_RADIUS)
                {
                    if (currentState.PlayerFuel > 0)
                    {
                        //Debug.Log("Add movement");
                        for (int i = 0; i < 3; i++)
                        {
                            movingEval.Enqueue(currentState.GenerateContinuation(i));
                            movingEvalMoves.Enqueue(currentMoves.Concat(new int[] { i }).ToList());
                        }
                    }
                    //saving performance by skipping a clone operation
                    SatellitePuzzleState idleState = currentState;
                    idleState.Iterate();
                    idleEval.Enqueue(idleState);
                    idleEvalMoves.Enqueue(currentMoves.Concat(new int[] { -1 }).ToList());
                }
            }

            if (!idleEval.Any())
            {
                if (solutionSequence != null)
                    return solutionSequence;

                List<SatellitePuzzleState>[] priorityOrdered = Enumerable.Range(0, 38).Select(_ => new List<SatellitePuzzleState>()).ToArray();
                List<List<int>>[] priorityOrderedMoves = Enumerable.Range(0, 38).Select(_ => new List<List<int>>()).ToArray();
                while (movingEval.Any())
                {
                    SatellitePuzzleState eval = movingEval.Dequeue();
                    priorityOrdered[eval.GameAge].Add(eval);
                    priorityOrderedMoves[eval.GameAge].Add(movingEvalMoves.Dequeue());
                }
                for (int i = 0; i < 37; i++)
                {
                    foreach (SatellitePuzzleState state in priorityOrdered[i])
                    {
                        idleEval.Enqueue(state);
                    }
                    foreach (List<int> stateMoves in priorityOrderedMoves[i])
                    {
                        idleEvalMoves.Enqueue(stateMoves);
                    }
                }

                movingEval = new Queue<SatellitePuzzleState>();
                movingEvalMoves = new Queue<List<int>>();

                if (!idleEval.Any())
                    return null;
            }
            currentState = idleEval.Dequeue();
            currentMoves = idleEvalMoves.Dequeue();
        }
    }

    private void Log(string text, params object[] args)
    {
        Debug.LogFormat("[Satellite Flight #{0}] {1}", _moduleId, string.Format(text, args));
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "'!{0} press lrum' to press the left, right, up, and middle buttons respectively.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        yield return null;
        if (_puzzle == null)
        {
            yield return "sendtochaterror Please wait for the module to load.";
            yield break;
        }

        command = command.ToLowerInvariant();
        string[] commands = command.Split(' ');

        if (commands.Length == 2 && commands[0] == "press" && commands[1].RegexMatch(@"^[murl]+$"))
        {
            foreach (char c in commands[1])
            {
                Buttons["murl".IndexOf(c)].OnInteract();
                yield return new WaitForSeconds(0.25f);
            }
        }
        else
        {
            yield return "sendtochaterror Invalid command.";
            yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        if (_solved)
            yield break;

        yield return null;

        while (_puzzle == null)
            yield return true;

        _simulatorMode = false;
        _currentRun = null;

        Buttons[1].OnInteract();
        yield return new WaitForSeconds(0.25f);
        foreach (int action in _solution)
        {
            Buttons[action + 1].OnInteract();
            yield return new WaitForSeconds(0.25f);
        }
    }
}
