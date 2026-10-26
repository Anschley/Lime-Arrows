using KModkit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEngine;

public class LimeArrowsScript : MonoBehaviour
{
	// VARIABLES

	// Keyword
	int currentKeywordTableIndex;
	string selectedKeyword;
	// Those two tables are populated by Ruleseed
	string[] keywordTable;
	string[] movementDirectionsTable;
	public TextMesh keywordTextRenderer;
	string targetExitDirection;

	string serialNumberWindDirection = "D";
	string serialNumberWindlessDirection = "R";

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
	string startingCoordinate;
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
	bool shouldUseWindCondition;

	// Ruleseed
	public KMRuleSeedable ruleseedManager;

	// Logging Data - Formatting and naming from Royal_Flu$h
	static int moduleIdCounter = 1;
	int moduleId;
	private bool moduleSolved;


	float time;
	// Buttons gathering & GetComponents
	void Awake()
	{
		time = Time.realtimeSinceStartup;

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

		HandleRuleseed();

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
        selectedKeyword = keywordTable[currentKeywordTableIndex];
        keywordTextRenderer.text = selectedKeyword;
		Debug.LogFormat("[Lime Arrows #{0}] The keyword {1} has been selected.", moduleId, selectedKeyword);


        DetermineTargetExitDirection();
        SelectStartingLocation();

        ShowCurrentCoordinate();

        // Determining in which direction to move across the table
        string serialNumber = bombInfo.GetSerialNumber();
        shouldUseWindCondition = Regex.IsMatch(serialNumber, "[WIND]");

        if (shouldUseWindCondition)
        {
            Debug.LogFormat("[Lime Arrows #{0}] Serial Number contains a letter from “WIND”, moving {1} across the table.",
				moduleId, GetFullDirectionName(serialNumberWindDirection));
        }
        else
        {
            Debug.LogFormat("[Lime Arrows #{0}] Serial Number doesn't contain a letter from “WIND”, moving {1} across the table.",
				moduleId, GetFullDirectionName(serialNumberWindlessDirection));
        }
    }

	

    void HandleRuleseed()
	{
		MonoRandom Rng = ruleseedManager.GetRNG();
        Debug.LogFormat("[Lime Arrows #{0}] Using Ruleseed {1}:", moduleId, Rng.Seed);

		// Loading data for Rule Seed 1
		if (Rng.Seed == 1)
		{
            movementDirectionsTable = new string[36] {
			    "R", "U", "D", "R", "L", "U",
			    "D", "R", "U", "L", "U", "R",
			    "L", "D", "R", "D", "L", "U",
			    "D", "U", "L", "R", "D", "L",
			    "U", "L", "D", "U", "R", "D",
			    "L", "R", "U", "L", "D", "R"
			};

            keywordTable = new string[36]
			{
			    "ACTIVE", "ENERGY", "INSANE", "MELODY", "PORTAL", "TECHNO",
			    "ALWAYS", "EXPERT", "ISLAND", "NARROW", "QUARKS", "TOGGLE",
			    "BIONIC", "FAULTY", "JETWAY", "NOISES", "RECOIL", "UPLOAD",
			    "CANYON", "GLOBAL", "KARATE", "OFFSET", "REPLAY", "WORLDS",
			    "CLOVER", "GRAVEL", "LETHAL", "OUTPUT", "STREAK", "YOGURT",
			    "DECODE", "HORNET", "LOUNGE", "PIGEON", "STRING", "ZENITH"
			};

            serialNumberWindDirection = "D";
            serialNumberWindlessDirection = "R";
            return;
        }

		// Simply generates the first Grid it finds
		int?[] grid = RecurseThroughGrid(new int?[36], Rng);

        string[] directionTranslation = new string[4] { "U", "D", "L", "R" };
        movementDirectionsTable = grid.Select(x => directionTranslation[x.Value]).ToArray();

        Debug.LogFormat("<Lime Arrows #{0}> Letter Grid: {1}", moduleId, movementDirectionsTable.Join());



        // Keywordtable
        // All words that can appear due to Ruleseed
        string[][] allowedWords = new string[25][] {
		    new string[]{"ABSORB", "ACTIVE", "ALWAYS", "AMIDST", "APPEAR", "ARTIST", "AXIOMS"},
		    new string[]{"BEHAVE", "BETTER", "BIONIC", "BOUNCE", "BRONZE", "BUBBLE", "BUTTER"},
		    new string[]{"CANYON", "CLOVER", "CREATE", "CYBORG", "CLOVER", "COBWEB"},
		    new string[]{"DAZZLE", "DECODE", "DITHER", "DIVIDE", "DOCTOR", "DRAGON", "DYNAMO"},
		    new string[]{"ECHOES", "ENERGY", "ENIGMA", "ERASER", "ESTATE", "EXPERT"},
		    new string[]{"FACTOR", "FATHOM", "FAULTY", "FENCER", "FILLER", "FJORDS", "FOLDER"},
		    new string[]{"GADGET", "GALAXY", "GENOME", "GLOBAL", "GLOOMY", "GRAVEL", "GROUND"},
		    new string[]{"HADRON", "HEALTH", "HELIUM", "HICCUP", "HOLLOW", "HORNET"},
		    new string[]{"ICEBOX", "ICONIC", "IGNITE", "IONIZE", "INFECT", "INSANE", "ISLAND"},
		    new string[]{"JACKET", "JETLAG", "JESTER", "JETWAY", "JUNIOR"},
		    new string[]{"KABOOM", "KARATE", "KELVIN", "KIDNEY", "KINDLE", "KLAXON", "KNIGHT"},
		    new string[]{"LAPTOP", "LEGATO", "LETHAL", "LESSON", "LICHEN", "LOUNGE", "LUXURY"},
		    new string[]{"MAGNET", "MARTYR", "MATRIX", "MELODY", "MIXING", "MURDER", "MYTHIC"},
		    new string[]{"NARROW", "NATURE", "NAVAJO", "NETHER", "NOISES", "NOUGHT"},
		    new string[]{"OBLONG", "OCCUPY", "OFFSET", "OUTPUT", "OXYGEN"},
		    new string[]{"PIGEON", "PISTOL", "PORTRY", "PORTAL", "PRAXIS", "PRINCE", "PURPLE"},
		    new string[]{"QUAINT", "QUARKS", "QUARTZ", "QUEBEC", "QUENCH", "QUICHE", "QUOTES"},
		    new string[]{"RAPTOR", "RECOIL", "REPLAY", "REMEDY", "ROTTEN"},
		    new string[]{"SCIONS", "SILVER", "SOCIAL", "SOURCE", "STREAK", "STRING"},
		    new string[]{"TECHNO", "THESIS", "TICKET", "TIMBER", "TOGGLE", "TUMBLE"},
		    new string[]{"UNIQUE", "UPDATE", "UPLOAD", "URCHIN", "USEFUL", "UTOPIA"},
		    new string[]{"VALLEY", "VELVET", "VIABLE", "VICTOR", "VIRTUE", "VISUAL"},
		    new string[]{"WAITER", "WEALTH", "WEIGHT", "WRITER", "WORLDS"},
		    new string[]{"YELLED", "YELLOW", "YIELDS", "YOGURT", "YONDER"},
		    new string[]{"ZAGGED", "ZENITH", "ZIPPER", "ZODIAC", "ZOOMED", "ZOMBIE"}
		};

		// How many words of each of the 25 letters will appear in the final table
		int[] wordsPerLetter = Rng.ShuffleFisherYates(new int[] { 2, 1, 1, 1, 2, 1, 2, 1, 2, 1, 1, 2, 1, 2, 2, 2, 1, 2, 2, 2, 1, 1, 1, 1, 1 });

		List<string> words = new List<string>();
		for (int i = 0; i < 25; i++)
		{
			string[] shuffledWords = Rng.ShuffleFisherYates(allowedWords[i]);
			for (int j = 0; j < wordsPerLetter[i]; j++)
			{
				words.Add(shuffledWords[j]);
			}
		}
		words.Sort();
        keywordTable = new string[36];
		// Transpose the table since words in the table are ordered vertically and not in reading order
		for (int i = 0; i < 6; i++)
		{
			for (int j = 0; j < 6; j++)
			{
				keywordTable[i * 6 + j] = words[j * 6 + i];
			}
		}

        Debug.LogFormat("<Lime Arrows #{0}> Word Grid: {1}", moduleId, keywordTable.Join());


		// Direction for moving inside of the table
        string[] selectedDirections = new string[4] { "U", "D", "L", "R"};
		Rng.ShuffleFisherYates(selectedDirections);
		serialNumberWindDirection = selectedDirections[0];
		serialNumberWindlessDirection = selectedDirections[1];

        Debug.LogFormat("<Lime Arrows #{0}> Direction if Serial Number contains WIND: {1}; otherwise: {2}", moduleId,
			GetFullDirectionName(serialNumberWindDirection), GetFullDirectionName(serialNumberWindlessDirection));
    }

    // Current worst Ruleseed found: 9267.
    // 10,232 recursions for a final grid in 0.200s

    // int totalSteps;
    int?[] RecurseThroughGrid(int?[] currentGrid, MonoRandom Rng)
	{		
		// If all cells have a value => we have a valid board and we can return it!! 
		if (currentGrid.All(d => d.HasValue))
		{ return currentGrid; }

		// totalSteps++;


        int row;
		int col;
		int bestCell = -1;
		int fewestPossibilities = int.MaxValue;

		int _evaluationPossibilities;

        // Make a verification pass to fill the cells with the lowest amount
        // of available directions (using limited rules)
        // This ensures that we maximize efficiency by not filling cells that are "free"
        // that end up constraining other cells into impossibility; which makes less iterations overall
        for (int i = 0; i < 36; i++)
		{
            if (currentGrid[i].HasValue) { continue; }

            row = i / 6;
            col = i % 6;

			// Check, for all 4 direction possibilities, how many are valid,
			// using the quickest rule to test that's also restrictive: immediate neighbors
			_evaluationPossibilities = new int[4] { 0, 1, 2, 3 }.Count(x =>
				(row == 0 || currentGrid[i - 6] != x) && // Up
				(row == 5 || currentGrid[i + 6] != x) && // Down
				(col == 0 || currentGrid[i - 1] != x) && // Left
				(col == 5 || currentGrid[i + 1] != x)); // Right)


            // We found a cell that's the least free
            if (_evaluationPossibilities < fewestPossibilities)
			{
				bestCell = i;
				// If this cell has only 1 possibility, fill it immediately
				if (_evaluationPossibilities == 1)
				{
					break;
				}

                fewestPossibilities = _evaluationPossibilities;
            }
        }

        // Then, test this singular cell to determine if it's correct!
        row = bestCell / 6;
        col = bestCell % 6;
        
        int[] fullRow = new int[6] { 6 * row, 6 * row + 1, 6 * row + 2, 6 * row + 3, 6 * row + 4, 6 * row + 5 };
        int[] fullCol = new int[6] { col, col + 6, col + 12, col + 18, col + 24, col + 30 };

		// Then, we cycle through all possible directions to put in the cell
		// and determine if one passes all tests to keep the cell and grid state as valid
		// We still need to verify every condition
		// Using Ruleseed to get which colour to start with
		int colourOffset = Rng.Next(0, 4);

        for (int i = 0; i < 4; i++)
        {
			int dir = (i + colourOffset) % 4;

            // Test B: None of its neighbors are already the same
            // Check that they are all valid, then invert
            if (((row == 0 || currentGrid[bestCell - 6] != dir) && // Up
                 (row == 5 || currentGrid[bestCell + 6] != dir) && // Down
                 (col == 0 || currentGrid[bestCell - 1] != dir) && // Left
                 (col == 5 || currentGrid[bestCell + 1] != dir)) == false) // Right
            { continue; }

            
            // Test C.i: The current row doesn't contain this value twice already
            if (fullRow.Count(x => currentGrid[x] == dir) == 2) { continue; }
            // Test C.ii: ditto for the columns
            if (fullCol.Count(x => currentGrid[x] == dir) == 2) { continue; }


            // Test A: The grid shouldn't have 9 of it already
            if (currentGrid.Count(x => x == dir) == 9) { continue; }


            // Test D.i: The current row will have enough room to have the other values inside of it
            // For those tests, add the colour and check the validity
            currentGrid[bestCell] = dir;

            int numberOfDirections = fullRow.Where(x => currentGrid[x].HasValue).Select(x => currentGrid[x]).Distinct().Count();
            if (fullRow.Count(x => currentGrid[x] == null) < 4 - numberOfDirections)
            {
                currentGrid[bestCell] = null; continue;
            }
            // Test D.ii: ditto for the columns
            numberOfDirections = fullCol.Where(x => currentGrid[x].HasValue).Select(x => currentGrid[x]).Distinct().Count();
            if (fullCol.Count(x => currentGrid[x] == null) < 4 - numberOfDirections)
            {
                currentGrid[bestCell] = null; continue;
            }

            // If we land here, we have a valid colour!
            // Check the branches, hoping we get a valid one
            if (RecurseThroughGrid(currentGrid, Rng) != null)
            {
                // If we're here, the branches have returned something valid and found a valid grid that we pass back up
                return currentGrid;
            }

            // If we're here, the colour WAS valid, but all possibilities down the road are worse-off
            // We need to erase the current grid before anything!
            currentGrid[bestCell] = null;
            continue;
        }

        // If we land here, we have tried every direction, so this cell is invalid in this current puzzle configuration and we must quit instantly
        return null;
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
		startingCoordinate = ConvertIndexToCoordinate(currentPositionIndex);
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

		string dir;
		if (shouldUseWindCondition)
		{ dir = serialNumberWindDirection; }
        else
        { dir = serialNumberWindlessDirection; }

		switch (dir)
		{
			case "U":
                if (currentKeywordTableIndex <= 5)
                { currentKeywordTableIndex += 30; }
                else { currentKeywordTableIndex -= 6; }
                break;
			case "D":
                if (currentKeywordTableIndex >= 30)
                { currentKeywordTableIndex -= 30; }
                else { currentKeywordTableIndex += 6; }
                break;
			case "L":
                if (currentKeywordTableIndex % 6 == 0)
                { currentKeywordTableIndex += 5; }
                else { currentKeywordTableIndex -= 1; }
                break;
			case "R":
                if (currentKeywordTableIndex % 6 == 5)
                { currentKeywordTableIndex -= 5; }
                else { currentKeywordTableIndex += 1; }
                break;
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

		if (moduleSolved)
		{
			yield return "solve";
		}
    }


    void TwitchHandleForcedSolve()
    {
		Debug.LogFormat("[Lime Arrows #{0}] Force solved via TP solve command.", moduleId);
        ModuleGetSolved();
    }
}