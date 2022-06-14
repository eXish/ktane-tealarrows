using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class TealArrowsScript : MonoBehaviour {

    public KMAudio audio;
    public KMBombInfo bomb;
    public KMColorblindMode Colorblind;

    public KMSelectable[] buttons;
    public Material[] colors;
    public GameObject numDisplay;
    public GameObject colorblindText;

    private string[][] table = new string[][]
    {
        new string[]{ "UP", "RIGHT", "DOWN", "LEFT" },
        new string[]{ "LEFT", "DOWN", "UP", "RIGHT" },
        new string[]{ "DOWN", "LEFT", "RIGHT", "UP" },
        new string[]{ "RIGHT", "UP", "LEFT", "DOWN" }
    };
    private string[] arrowNames = new string[] { "LEFT", "RIGHT", "UP", "DOWN" };
    private string correctButton = "";

    private int[] flashingArrows = new int[2];
    private int presses = 0;

    private bool lightson = false;
    private bool isanimating = false;
    private bool colorblindActive = false;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        moduleSolved = false;
        colorblindActive = Colorblind.ColorblindModeActive;
        foreach (KMSelectable obj in buttons){
            KMSelectable pressed = obj;
            pressed.OnInteract += delegate () { PressButton(pressed); return false; };
        }
        GetComponent<KMBombModule>().OnActivate += OnActivate;
    }

    void Start () {
        Debug.LogFormat("[Teal Arrows #{0}] Correct Presses: {1}", moduleId, presses);
        randomizeFlashing();
        determineCorrect(presses);
    }

    void Update()
    {
        if (lightson && !moduleSolved)
        {
            List<int> possible = new List<int>() { 0, 1, 2, 3 };
            if ((int)bomb.GetTime() % 2 != 0)
            {
                buttons[flashingArrows[0]].gameObject.GetComponent<Renderer>().material = colors[0];
                possible.Remove(flashingArrows[0]);
            }
            else if ((int)bomb.GetTime() % 2 == 0)
            {
                buttons[flashingArrows[1]].gameObject.GetComponent<Renderer>().material = colors[0];
                possible.Remove(flashingArrows[1]);
            }
            for (int i = 0; i < possible.Count; i++)
                buttons[possible[i]].gameObject.GetComponent<Renderer>().material = colors[1];
        }
    }

    void OnActivate()
    {
        lightson = true;
        if (colorblindActive)
            colorblindText.SetActive(true);
    }

    void PressButton(KMSelectable pressed)
    {
        if (moduleSolved != true && isanimating != true && lightson == true)
        {
            pressed.AddInteractionPunch(0.25f);
            audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
            if (correctButton.Equals("UP") && pressed != buttons[2])
            {
                GetComponent<KMBombModule>().HandleStrike();
                Debug.LogFormat("[Teal Arrows #{0}] 'UP' was not pressed and was expected! Strike! Resetting module...", moduleId);
                presses = 0;
                Start();
            }
            else if (correctButton.Equals("DOWN") && pressed != buttons[3])
            {
                GetComponent<KMBombModule>().HandleStrike();
                Debug.LogFormat("[Teal Arrows #{0}] 'DOWN' was not pressed and was expected! Strike! Resetting module...", moduleId);
                presses = 0;
                Start();
            }
            else if (correctButton.Equals("LEFT") && pressed != buttons[0])
            {
                GetComponent<KMBombModule>().HandleStrike();
                Debug.LogFormat("[Teal Arrows #{0}] 'LEFT' was not pressed and was expected! Strike! Resetting module...", moduleId);
                presses = 0;
                Start();
            }
            else if (correctButton.Equals("RIGHT") && pressed != buttons[1])
            {
                GetComponent<KMBombModule>().HandleStrike();
                Debug.LogFormat("[Teal Arrows #{0}] 'RIGHT' was not pressed and was expected! Strike! Resetting module...", moduleId);
                presses = 0;
                Start();
            }
            else
            {
                presses++;
                if (presses == 5)
                {
                    moduleSolved = true;
                    buttons[flashingArrows[0]].gameObject.GetComponent<Renderer>().material = colors[1];
                    buttons[flashingArrows[1]].gameObject.GetComponent<Renderer>().material = colors[1];
                    StartCoroutine(victory());
                    Debug.LogFormat("[Teal Arrows #{0}] '{1}' pressed successfully!", moduleId, correctButton);
                    Debug.LogFormat("[Teal Arrows #{0}] Reached 5 correct presses! Module Disarmed!", moduleId);
                }
                else
                {
                    Debug.LogFormat("[Teal Arrows #{0}] '{1}' pressed successfully!", moduleId, correctButton);
                    Start();
                }
            }
        }
    }

    private void randomizeFlashing()
    {
        flashingArrows[0] = UnityEngine.Random.Range(0, 4);
        flashingArrows[1] = UnityEngine.Random.Range(0, 4);
        for (int i = 0; i < 2; i++)
            Debug.LogFormat("[Teal Arrows #{0}] The '{1}' arrow is off when the last digit of the timer is {2}", moduleId, arrowNames[flashingArrows[i]], i == 0 ? "odd" : "even");
    }

    private void determineCorrect(int n)
    {
        if (n == 0)
        {
            correctButton = table[flashingArrows[1]][flashingArrows[0]];
        }
        else if (n % 2 != 0)
        {
            correctButton = table[Array.IndexOf(arrowNames, correctButton)][flashingArrows[0]];
        }
        else if (n % 2 == 0)
        {
            correctButton = table[flashingArrows[1]][Array.IndexOf(arrowNames, correctButton)];
        }
        Debug.LogFormat("[Teal Arrows #{0}] The correct button to press is '{1}'", moduleId, correctButton);
    }

    private IEnumerator victory()
    {
        isanimating = true;
        for (int i = 0; i < 100; i++)
        {
            int rand1 = UnityEngine.Random.Range(0, 10);
            if (i < 50)
            {
                numDisplay.GetComponent<TextMesh>().text = rand1 + "";
            }
            else
            {
                numDisplay.GetComponent<TextMesh>().text = "G" + rand1;
            }
            yield return new WaitForSeconds(0.025f);
        }
        numDisplay.GetComponent<TextMesh>().text = "GG";
        isanimating = false;
        GetComponent<KMBombModule>().HandlePass();
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} up/right/down/left [Presses the specified arrow button] | Words can be substituted as one letter (Ex. right as r)";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*up\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) || Regex.IsMatch(command, @"^\s*u\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            buttons[2].OnInteract();
        }
        if (Regex.IsMatch(command, @"^\s*down\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) || Regex.IsMatch(command, @"^\s*d\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            buttons[3].OnInteract();
        }
        if (Regex.IsMatch(command, @"^\s*left\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) || Regex.IsMatch(command, @"^\s*l\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            buttons[0].OnInteract();
        }
        if (Regex.IsMatch(command, @"^\s*right\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) || Regex.IsMatch(command, @"^\s*r\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            buttons[1].OnInteract();
        }
        if (moduleSolved) { yield return "solve"; }
        yield break;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!lightson) { yield return true; };
        int start = presses;
        for (int i = start; i < 5; i++)
        {
            buttons[Array.IndexOf(arrowNames, correctButton)].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        while (isanimating) { yield return true; };
    }
}