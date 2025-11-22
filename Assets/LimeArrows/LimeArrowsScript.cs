using KModkit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class LimeArrowsScript : MonoBehaviour
{
	// VARIABLES

	// Keyword
	int currentKeywordTableIndex;
	string selectedKeyword;
	string[] allPossibleKeywords = new string[36]
	{
		"ACTIVE", "ENERGY", "INSANE", "MELODY", "PORTAL", "TECHNO",
		"ALWAYS", "EXPERT", "ISLAND", "NARROW", "QUARKS", "TOGGLE",
		"BIONIC", "FAULTY", "JETWAY", "NOISES", "RECOIL", "UPLOAD",
		"CANYON", "GLOBAL", "KARATE", "OFFSET", "REPLAY", "WORLDS",
		"CLOVER", "GRAVEL", "LETHAL", "OUTPUT", "STREAK", "YOGURT",
		"DECODE", "HORNET", "LOUNGE", "PIGEON", "STRING", "ZENITH"
	};
	string[] movementDirectionsTable = new string[36]
	{
		"R", "U", "D", "R", "L", "U",
		"D", "R", "U", "L", "U", "R",
		"L", "D", "R", "D", "L", "U",
		"D", "U", "L", "R", "D", "L",
		"U", "L", "D", "U", "R", "D",
		"L", "R", "U", "L", "D", "R"
	};
	public TextMesh keywordTextRenderer;
	string targetExitDirection;

	// Maze
	string[] maze = new string[81]
	{
		"UL", "U", "UR", "UL", "", "U", "U", "U", "UDR",
		"LR", "LR", "DL", "", "R", "DL", "R", "L", "UR",
		"DL", "", "U", "D", "", "U", "D", "", "R",
		"UL", "R", "L", "UR", "L", "", "UD", "R", "LR",
		"D", "", "D", "", "R", "L", "UR", "L", "",
		"UL", "", "UD", "R", "L", "D", "D", "", "R",
		"LR", "DL", "U", "", "", "UD", "U", "", "DR",
		"L", "U", "", "R", "L", "U", "R", "L", "UR",
		"DLR", "DL", "DR", "DL", "", "D", "D", "D", "DR"
	};
	int currentPositionIndex;
	string[] mazeColumns = new string[9]
	{
		"A", "B", "C", "D", "E", "F", "G", "H", "I"
	};

	List<int> possibleSpawnPointsForExitUp = new List<int>()
	{
		36, 37, 43, 44, 45, 46, 47, 51, 52, 53, 55, 56, 57, 58, 59, 60, 61, 65, 66, 67, 68, 69
	};
	List<int> possibleSpawnPointsForExitDown = new List<int>()
	{
		11, 12, 13, 14, 15, 19, 20, 21, 22, 23, 24, 25, 27, 28, 29, 33, 34, 35, 36, 37, 43, 44
	};
	List<int> possibleSpawnPointsForExitLeft = new List<int>()
	{
		04, 05, 13, 14, 15, 23, 24, 25, 33, 34, 42, 43, 51, 52, 59, 60, 61, 67, 68, 69, 76, 77
	};
	List<int> possibleSpawnPointsForExitRight = new List<int>()
	{
		03, 04, 11, 12, 13, 19, 20, 21, 28, 29, 37, 38, 46, 47, 55, 56, 57, 65, 66, 67, 75, 76
	};
	public TextMesh cellCoordinateTextRenderer;
	public KMBombModule BombModule;

	// Arrow Buttons
	public KMSelectable UpButton;
	public KMSelectable DownButton;
	public KMSelectable LeftButton;
	public KMSelectable RightButton;
	private bool lightsOutModule;

	// Strike
	bool hasStruckThisMovement;

	// Solve Animation
	string[] alphabet = new string[26]
	{
		"A", "B", "C", "D", "E", "F", "G", "H", "I",
		"J", "K", "L", "M", "N", "O", "P", "Q", "R",
		"S", "T", "U", "V", "W", "X", "Y", "Z"
	};

	// Audio
	public KMAudio ModuleAudio;

	// Colorblind Mode
	public KMColorblindMode colorblindMode;
	public GameObject colorblindModeTextRenderer;

	// Edgework
	public KMBombInfo bombInfo;
	bool shouldGoDown;

	// Logging Data - Formatting and naming from Royal_Flu$h
	static int moduleIdCounter = 1;
	int moduleId;
	private bool moduleSolved;



	// Buttons gathering & GetComponents
	void Awake()
	{
		// Initialize logging
		moduleId = moduleIdCounter++;

		// Initialize Delegates for Button Presses
		UpButton.OnInteract += delegate () { HandleButtonPress("U", UpButton); return false; };
		DownButton.OnInteract += delegate () { HandleButtonPress("D", DownButton); return false; };
		LeftButton.OnInteract += delegate () { HandleButtonPress("L", LeftButton); return false; };
		RightButton.OnInteract += delegate () { HandleButtonPress("R", RightButton); return false; };

		lightsOutModule = true;
		cellCoordinateTextRenderer.text = " ";
		keywordTextRenderer.text = "------";
		BombModule.OnActivate += OnActivate;
	}

	// Puzzle initialization
	void Start()
	{
		Debug.LogFormat("[Lime Arrows #{0}] Initializing Module.", moduleId);

        // Handle Colorblind Mode
        StartCoroutine(HandleColorblindMode());
	}

	void OnActivate()
	{
		StartCoroutine(AwakeningDelay());
    }

	// Delays displayed word and coordinate
	IEnumerator AwakeningDelay()
	{
		yield return null;
		yield return new WaitForSeconds(0.5f);
        lightsOutModule = false;
        // Randomly select keyword from list of possible options
        currentKeywordTableIndex = UnityEngine.Random.Range(0, 36);
        selectedKeyword = allPossibleKeywords[currentKeywordTableIndex];
        keywordTextRenderer.text = selectedKeyword;
		Debug.LogFormat("[Lime Arrows #{0}] The keyword {1} has been selected.", moduleId, selectedKeyword);
        DetermineTargetExitDirection();
        SelectStartingLocation();
        ShowCurrentCoordinate();
        // Determining in which direction to move across the table
        string serialNumber = bombInfo.GetSerialNumber();
        shouldGoDown = Regex.IsMatch(serialNumber, "[WIND]");

        if (shouldGoDown)
        {
            Debug.LogFormat("[Lime Arrows #{0}] Serial Number contains a letter from “WIND”, moving down across the table.", moduleId);
        }
        else
        {
            Debug.LogFormat("[Lime Arrows #{0}] Serial Number doesn't contain a letter from “WIND”, moving right across the table.", moduleId);
        }
    }

	// Full Direction Name for Log
	string GetFullDirectionName(string shortendName)
	{
		if (shortendName == "U")
		{
			return "Up";
		}
		if (shortendName == "D")
		{
			return "Down";
		}
		if (shortendName == "L")
		{
			return "Left";
		}
		if (shortendName == "R")
		{
			return "Right";
		}
		else
		{
			return "";
		}
	}

	// Convert Maze Cell Index to Maze Cell Coordinate
	string ConvertIndexToCoordinate(int cellIndex)
	{
		int cellColumn;
		int cellRow;
		string cellCoordinate;

		cellColumn = cellIndex % 9;
		cellRow = Mathf.FloorToInt(cellIndex / 9f) + 1;
		cellCoordinate = mazeColumns[cellColumn] + cellRow;
		return cellCoordinate;
	}

	// Randomly selects the starting location from a hardcoded pool of possible indices
	void SelectStartingLocation()
	{
		switch (targetExitDirection)
		{
			case "U":
				currentPositionIndex = possibleSpawnPointsForExitUp[Random.Range(0, possibleSpawnPointsForExitUp.Count)];
				if (Random.value <= 0.01f)
				{
					currentPositionIndex = 13;
					Debug.LogFormat("[Lime Arrows #{0}] CONGRATULATIONS!!! You are our 1000th visitor of this module! As a reward, your starting location is {1}, just 2 spaces away from the correct exit!", moduleId, ConvertIndexToCoordinate(currentPositionIndex));
				}
				break;

			case "D":
				currentPositionIndex = possibleSpawnPointsForExitDown[Random.Range(0, possibleSpawnPointsForExitDown.Count)];
				if (Random.value <= 0.01f)
				{
					currentPositionIndex = 67;
					Debug.LogFormat("[Lime Arrows #{0}] CONGRATULATIONS!!! You are our 1000th visitor of this module! As a reward, your starting location is {1}, just 2 spaces away from the correct exit!", moduleId, ConvertIndexToCoordinate(currentPositionIndex));
				}
				break;

			case "L":
				currentPositionIndex = possibleSpawnPointsForExitLeft[Random.Range(0, possibleSpawnPointsForExitLeft.Count)];
				if (Random.value <= 0.01f)
				{
					currentPositionIndex = 37;
					Debug.LogFormat("[Lime Arrows #{0}] CONGRATULATIONS!!! You are our 1000th visitor of this module! As a reward, your starting location is {1}, just 2 spaces away from the correct exit!", moduleId, ConvertIndexToCoordinate(currentPositionIndex));
				}
				break;

			case "R":
				currentPositionIndex = possibleSpawnPointsForExitRight[Random.Range(0, possibleSpawnPointsForExitRight.Count)];
				if (Random.value <= 0.01f)
				{
					currentPositionIndex = 43;
					Debug.LogFormat("[Lime Arrows #{0}] CONGRATULATIONS!!! You are our 1000th visitor of this module! As a reward, your starting location is {1}, just 2 spaces away from the correct exit!", moduleId, ConvertIndexToCoordinate(currentPositionIndex));
				}
				break;

			default:
				currentPositionIndex = 40;
				break;
		}
		Debug.LogFormat("[Lime Arrows #{0}] Starting location is {1}.", moduleId, ConvertIndexToCoordinate(currentPositionIndex));
	}

	// Determines Maze Exit based on the direction of the starting word
	void DetermineTargetExitDirection()
	{
		targetExitDirection = movementDirectionsTable[currentKeywordTableIndex];
		Debug.LogFormat("[Lime Arrows #{0}] The target exit direction is {1}.", moduleId, GetFullDirectionName(targetExitDirection));

		// Blocks false exits by adding walls to them
		switch (targetExitDirection)
		{
			case "U":
				maze.SetValue("DL", 36);
				maze.SetValue("D", 76);
				maze.SetValue("R", 44);
				break;

			case "D":
				maze.SetValue("DL", 36);
				maze.SetValue("U", 4);
				maze.SetValue("R", 44);
				break;

			case "L":
				maze.SetValue("U", 4);
				maze.SetValue("D", 76);
				maze.SetValue("R", 44);
				break;

			case "R":
				maze.SetValue("DL", 36);
				maze.SetValue("D", 76);
				maze.SetValue("U", 4);
				break;
		}
	}

	void ShowCurrentCoordinate()
	{
		cellCoordinateTextRenderer.text = ConvertIndexToCoordinate(currentPositionIndex);
	}

	void MoveInMaze(string direction, bool isDoneByPlayer)
	{
		if (moduleSolved)
		{
			return;
		}
		// Checks if the cell that the defuser wants to move to has a wall
		bool hasWall = Regex.IsMatch(maze[currentPositionIndex], "[" + direction + "]");
		if (hasWall)
		{
			if (hasStruckThisMovement)
			{
				Debug.LogFormat("[Lime Arrows #{0}] 2 strikes were registered, because the wind pushed you into a wall as well, but only 1 strike was given to save your sanity.", moduleId);
				return;
            }
            BombModule.HandleStrike();
			hasStruckThisMovement = true;
            if (isDoneByPlayer)
			{
                Debug.LogFormat("[Lime Arrows #{0}] You tried to go {1} from {2}, but ran into a wall. Strike!", moduleId, GetFullDirectionName(direction), ConvertIndexToCoordinate(currentPositionIndex));
            }
			else
			{
                Debug.LogFormat("[Lime Arrows #{0}] The wind pushed you {1} into a wall. Strike!", moduleId, GetFullDirectionName(direction), ConvertIndexToCoordinate(currentPositionIndex));
            }
		}
		// Verifies Conditions for Module Solve
		else if (targetExitDirection == "U" && currentPositionIndex == 4 && direction == "U")
		{
			ModuleGetSolved();
		}
		else if (targetExitDirection == "D" && currentPositionIndex == 76 && direction == "D")
		{
			ModuleGetSolved();
		}
		else if (targetExitDirection == "L" && currentPositionIndex == 36 && direction == "L")
		{
			ModuleGetSolved();
		}
		else if (targetExitDirection == "R" && currentPositionIndex == 44 && direction == "R")
		{
			ModuleGetSolved();
		}
		else
		{
			// Movement in the Maze
			switch (direction)
			{
				case "U":
					currentPositionIndex -= 9;
					break;

				case "D":
					currentPositionIndex += 9;
					break;

				case "L":
					currentPositionIndex -= 1;
					break;

				case "R":
					currentPositionIndex += 1;
					break;
			}
            ShowCurrentCoordinate();
			// Logs which move is done by either player or wind
            if (isDoneByPlayer)
			{
                Debug.LogFormat("[Lime Arrows #{0}] You moved {1} to Coordinate {2}.", moduleId, GetFullDirectionName(direction), ConvertIndexToCoordinate(currentPositionIndex));
            }
			else
			{
                Debug.LogFormat("[Lime Arrows #{0}] The wind pushed you {1} to Coordinate {2}.", moduleId, GetFullDirectionName(direction), ConvertIndexToCoordinate(currentPositionIndex));
            }
		}
	}

	void HandleButtonPress(string buttonDirection, KMSelectable Button)
	{
		if (lightsOutModule)
		{
			return;
		}

        if (moduleSolved)
        {
            return;
        }

        hasStruckThisMovement = false;
		MoveInMaze(buttonDirection, true);
		Button.AddInteractionPunch(1);
		WindMovement();
        ModuleAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		StartCoroutine(ButtonAnimation(Button));
    }

	// Animation for Button Press
	IEnumerator ButtonAnimation(KMSelectable Button)
	{
        Button.transform.localPosition -= new Vector3(0, 0.004f, 0);

        yield return new WaitForSeconds(0.15f);

        Button.transform.localPosition += new Vector3(0, 0.004f, 0);
    }

	void WindMovement()
	{
		if (moduleSolved)
		{
			return;
		}
			MoveInMaze(movementDirectionsTable[currentKeywordTableIndex], false);
		if (shouldGoDown)
		{
			if (currentKeywordTableIndex >=30)
			{
				currentKeywordTableIndex -= 30;
			}
			else
			{
				currentKeywordTableIndex += 6;
            }
		}
		else
		{
			if (currentKeywordTableIndex % 6 == 5)
			{
				currentKeywordTableIndex -= 5;
			}
			else
			{
                currentKeywordTableIndex += 1;
            }
		}
	}

	void ModuleGetSolved()
	{
		moduleSolved = true;
        StartCoroutine(victory());
        Debug.LogFormat("[Lime Arrows #{0}] You successfully reached the correct exit of the maze. Module solved!", moduleId);
    }

	IEnumerator victory()
    {
        for (int i = 0; i < 100; i++)
        {
			string rand1 = alphabet[Random.Range(0, 26)];
            int rand2 = UnityEngine.Random.Range(0, 10);
            if (i < 50)
            {
                cellCoordinateTextRenderer.GetComponent<TextMesh>().text = rand1 + "" + rand2;
            }
            else
            {
                cellCoordinateTextRenderer.GetComponent<TextMesh>().text = "G" + rand2;
            }
            yield return new WaitForSeconds(0.025f);
        }
        cellCoordinateTextRenderer.GetComponent<TextMesh>().text = "GG";
        ModuleAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
        BombModule.HandlePass();
    }

    // Colorblind Mode Check
    IEnumerator HandleColorblindMode()
	{
        yield return new WaitForSecondsRealtime(0.5f);
		if (colorblindMode.ColorblindModeActive)
		{
			colorblindModeTextRenderer.SetActive(true);
		}
		else
		{
			colorblindModeTextRenderer.SetActive(false);
		}
    }

	// TwitchPlays
	#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"“!{0} DRUDDLDRU”. Submit a sequence of presses using letters with their corresponding direction without spaces. U is up, D is down, L is left, R is right.";
	#pragma warning restore 414


    IEnumerator ProcessTwitchCommand(string command)
    {
		// Credits to eXish (Red Arrows)
        string[] parameters = command.Split(' ');
        string checks = "";
        for (int j = 0; j < parameters.Length; j++)
        {
            checks += parameters[j];
        }
        var buttonsToPress = new List<KMSelectable>();
        for (int i = 0; i < checks.Length; i++)
        {
            if (checks.ElementAt(i).Equals('u') || checks.ElementAt(i).Equals('U'))
                buttonsToPress.Add(UpButton);
            else if (checks.ElementAt(i).Equals('d') || checks.ElementAt(i).Equals('D'))
                buttonsToPress.Add(DownButton);
            else if (checks.ElementAt(i).Equals('l') || checks.ElementAt(i).Equals('L'))
                buttonsToPress.Add(LeftButton);
            else if (checks.ElementAt(i).Equals('r') || checks.ElementAt(i).Equals('R'))
                buttonsToPress.Add(RightButton);
            else
                yield break;
        }

        yield return null;
        yield return buttonsToPress;
    }


    void TwitchHandleForcedSolve()
    {
		Debug.LogFormat("[Lime Arrows #{0}] Force solved via TP solve command.", moduleId);
        ModuleGetSolved();
    }
}