using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	public enum TurnSide
	{
		Player,
		Enemy
	}

	public enum UnitTurnStep
	{
		StartTurn,
		SelectUnit,
		ChooseOption,
		MoveSelectTarget,
		ActionSelectTarget,
		ChooseFacing,
		EndTurn
	}

	private enum TurnOption
	{
		Move,
		Attack,
		Wait
	}

	public static GameManager Instance;
	public float PlayerMoveSpeed;
	public int FrameCount;
	private GameObject cursor;
	private GameObject mapObject;

	private Vector3[] cursorNextMove = new Vector3[4];
	private Tile currentTile;
	private Tile previousTile;
	private Tile originTile;
	private Player currentPlayer;
	private Player currentTarget;
	public List<Tile> TilesQueueForPlayer = new List<Tile>();
	private List<Tile> highlightedMovementTiles = new List<Tile>();
	public Vector3 prePosition;
	public int preJumpStype;
	public List<Tile> map = new List<Tile>();
	public List<Player> players = new List<Player>();
	private Camera mainCamera;
	private bool choosingTarget;
	public bool attacking;
	private const float mouseTileSelectDistance = 0.8f;
	public TurnSide activeTurnSide;
	public UnitTurnStep currentTurnStep;
	public bool hasMoved;
	public bool hasActed;
	private int selectedOptionIndex;
	private int chooseOptionEnteredFrame = -1;


	private void Awake()
	{
		mainCamera = Camera.main;
		Instance = this;
		PlayerMoveSpeed = 3.5f;
		FrameCount = 0;
		mapObject = transform.Find("mapObject").gameObject;
		activeTurnSide = TurnSide.Player;
		currentTurnStep = UnitTurnStep.SelectUnit;
		hasMoved = false;
		hasActed = false;
		selectedOptionIndex = 0;

	}
	private void Start()
	{
		SaveLoad.LoadMap (map, mapObject);
		//createMap();
		CreateCursor();
		SetCursorPosition();
		var p1 = ((GameObject)Instantiate(PrefabHolder.Instance.UserPlayer)).GetComponent<UserPlayer>();
		var p2 = ((GameObject)Instantiate(PrefabHolder.Instance.AIPlayer)).GetComponent<AIPlayer>();
		p1.transform.position = map[0].transform.position;
		players.Add(p1);
		p2.transform.position = map[4].transform.position;
		players.Add(p2);
	}

	private void Update()
	{
		if (choosingTarget)
		{

		}
		else
		{
			MoveCurrentPlayer();
		}

		KeyControll();
		MouseControll();

		IncreateFrameCount();
	}

	private void IncreateFrameCount()
	{
		FrameCount++;
		if (FrameCount > 10000) FrameCount = 0;
	}

	public void MoveCurrentPlayer()
	{
		if (TilesQueueForPlayer.Count > 0)
		{
			Vector3 currentPosition = TilesQueueForPlayer[0].transform.position;

			prePosition = transform.position;
			currentPlayer.transform.position += (currentPosition.Vector2() - currentPlayer.transform.position.Vector2()).normalized * Time.deltaTime * PlayerMoveSpeed;
			if (Vector3.Distance(currentPosition.Vector2(), currentPlayer.transform.position.Vector2()) <= 1.2f * PlayerMoveSpeed * Time.deltaTime)
			{
				currentPlayer.transform.position = currentPosition;
				previousTile = TilesQueueForPlayer[0];
				if (TilesQueueForPlayer.Count > 1) GetMovingDirection();
				TilesQueueForPlayer.RemoveAt(0);
				preJumpStype = Mathf.Abs(1 - preJumpStype);
				if (TilesQueueForPlayer.Count == 0)
				{
					currentTile = previousTile;
					ClearMovementHighlights();
					currentPlayer.MovingAnimation(currentPlayer.faceDirection, currentTile, currentTile);
					hasMoved = true;
					currentTurnStep = UnitTurnStep.ChooseFacing;
					previousTile = currentPlayer.currentTile();
				}
			}
			if (TilesQueueForPlayer.Count > 0) currentPlayer.MovingAnimation(currentPlayer.faceDirection, previousTile, TilesQueueForPlayer[0]);
		}
	}

	private void GetMovingDirection()
	{
		for (int i = 0; i < TilesQueueForPlayer[0].neighbours.Count(); i++)
		{
			if (TilesQueueForPlayer[0].neighbours[i] == TilesQueueForPlayer[1])
			{
				currentPlayer.faceDirection = i;
				break;
			}
		}
	}

	private void CreateCursor()
	{
		Debug.Log(map.Count);
		cursor = (GameObject)Instantiate(PrefabHolder.Instance.Cursor, map[0].transform.position, Quaternion.identity);
	}

	private void SetCursorPosition()
	{
		cursorNextMove[0] = map[0].transform.position;
		GetCursorNextMove(0);
	}

	private void GetCursorNextMove(int direction)
	{
		SetCursorTile(map.Where(x => x.transform.position == cursorNextMove[direction]).First());
	}

	private void SetCursorTile(Tile tile)
	{
		cursor.transform.position = tile.transform.position;
		MoveCamera(cursor.transform.position);
		currentTile = tile;
		for (int i = 0; i < 4; i++)
		{
			if (currentTile.neighbours[i] != null)
				cursorNextMove[i] = currentTile.neighbours[i].transform.position;
		}
	}

	private void MoveCamera(Vector3 follower)
	{
		if (follower.x - mainCamera.transform.position.x > 5.5f)
		{
			mainCamera.transform.position += Vector3.right;
		}
		else if (follower.x - mainCamera.transform.position.x < -5)
		{
			mainCamera.transform.position += Vector3.left;
		}

		if (follower.y - mainCamera.transform.position.y > 3.5f)
		{
			mainCamera.transform.position += Vector3.up;
		}
		else if (follower.y - mainCamera.transform.position.y < -2.5f)
		{
			mainCamera.transform.position += Vector3.down;
		}
	}

	public void KeyControll()
	{
		if (Input.GetKeyDown(KeyCode.S))
		{
			SaveLoad.SaveMapData(map, Path.Combine(Application.dataPath, @"Resources\" + "Maps.Xml"));
		}

		if (currentTurnStep == UnitTurnStep.ChooseFacing)
		{
			if (Input.GetKeyDown(KeyCode.UpArrow))
			{
				SetFacingDirection(1);
			}
			else if (Input.GetKeyDown(KeyCode.DownArrow))
			{
				SetFacingDirection(3);
			}
			else if (Input.GetKeyDown(KeyCode.LeftArrow))
			{
				SetFacingDirection(0);
			}
			else if (Input.GetKeyDown(KeyCode.RightArrow))
			{
				SetFacingDirection(2);
			}
			else if (Input.GetKeyDown(KeyCode.Space))
			{
				HandleSelectionAndMovement();
			}
			return;
		}

		if (Input.GetKeyDown(KeyCode.UpArrow))
		{
			GetCursorNextMove(1);
		}
		else if (Input.GetKeyDown(KeyCode.DownArrow))
		{
			GetCursorNextMove(3);
		}
		else if (Input.GetKeyDown(KeyCode.LeftArrow))
		{
			GetCursorNextMove(0);
		}
		else if (Input.GetKeyDown(KeyCode.RightArrow))
		{
			GetCursorNextMove(2);
		}
		else if (Input.GetKeyDown(KeyCode.P))
		{
			Tile current = map.Where(x => x.transform.position == cursor.transform.position).First();
			List<Tile> highlightTile = Highlight.Movement(current, 4, new List<Tile>());
			OnHighlightTiles(highlightTile, true);
		}
		else if (Input.GetKeyUp(KeyCode.P))
		{
			Tile current = map.Where(x => x.transform.position == cursor.transform.position).First();
			List<Tile> highlightTile = Highlight.Movement(current, 4, new List<Tile>());
			OnHighlightTiles(highlightTile, false);
		}
		else if (Input.GetKeyDown(KeyCode.Space))
		{
			HandleSelectionAndMovement();
		}
		else if (Input.GetKeyDown(KeyCode.Q))
		{
			CycleTurnOption(-1);
		}
		else if (Input.GetKeyDown(KeyCode.E))
		{
			CycleTurnOption(1);
		}
	}

	private void MouseControll()
	{
		Debug.Log("MouseControll() running");
		if (!Input.GetMouseButtonDown(0))
			return;

		Debug.Log("Left click detected");
		Debug.Log("Mouse screen position: " + Input.mousePosition);
		Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
		mouseWorldPosition.z = 0;
		Debug.Log("Mouse world position: " + mouseWorldPosition);
		Tile clickedTile = map.OrderBy(x => Vector3.Distance(x.transform.position.Vector2(), mouseWorldPosition.Vector2())).First();
		Debug.Log("Nearest tile found: " + (clickedTile != null));
		if (clickedTile != null)
			Debug.Log("Nearest tile position: " + clickedTile.transform.position);
		if (Vector3.Distance(clickedTile.transform.position.Vector2(), mouseWorldPosition.Vector2()) > mouseTileSelectDistance)
			return;

		SetCursorTile(clickedTile);
		Debug.Log("Calling HandleSelectionAndMovement() from mouse input");
		HandleSelectionAndMovement();
	}

	private void HandleSelectionAndMovement()
	{
		if (currentTurnStep == UnitTurnStep.ChooseFacing)
		{
			ConfirmFacingAndEndTurn();
			return;
		}

		if (currentTurnStep == UnitTurnStep.ChooseOption)
		{
			if (chooseOptionEnteredFrame == FrameCount)
				return;

			ConfirmCurrentOption();
			return;
		}
		else if (currentTurnStep == UnitTurnStep.SelectUnit)
		{
			if (players.Where(x => x.transform.position.x == currentTile.transform.position.x && x.transform.position.y == currentTile.transform.position.y).Count() > 0)
			{
				currentPlayer = players.Where(x => x.transform.position.x == currentTile.transform.position.x && x.transform.position.y == currentTile.transform.position.y).First();
					previousTile = currentTile;
					originTile = currentTile;
					currentTurnStep = UnitTurnStep.ChooseOption;
					selectedOptionIndex = 0;
					chooseOptionEnteredFrame = FrameCount;
					Debug.Log("Entered ChooseOption. Selected option: Move");
			}
			return;
		}

		if (choosingTarget)
		{
			if (players.Where(x => x.currentTile() == currentTile).Count() > 0)
			{
				currentTarget = players.Where(x => x.currentTile() == currentTile).First();
				StartCoroutine(Attacking());
			}
			else
			{
				Debug.Log("Invalid Tile!");
			}
		}
		else if (originTile != currentTile && currentTurnStep == UnitTurnStep.MoveSelectTarget)
		{
			if (highlightedMovementTiles.Contains(currentTile))
			{
				TilesQueueForPlayer = Highlight.FindPath(originTile, currentTile, new List<Tile>());
				GetMovingDirection();
			}
		}
	}

	private void CycleTurnOption(int direction)
	{
		if (currentTurnStep != UnitTurnStep.ChooseOption)
			return;

		int optionCount = System.Enum.GetValues(typeof(TurnOption)).Length;
		selectedOptionIndex = (selectedOptionIndex + direction + optionCount) % optionCount;
		Debug.Log("Cycling options. Selected option: " + ((TurnOption)selectedOptionIndex));
	}

	private void ConfirmCurrentOption()
	{
		TurnOption selectedOption = (TurnOption)selectedOptionIndex;
		Debug.Log("Confirming option: " + selectedOption);
		if (selectedOption == TurnOption.Move)
		{
			currentTurnStep = UnitTurnStep.MoveSelectTarget;
			originTile = currentPlayer.currentTile();
			previousTile = originTile;
			highlightedMovementTiles = Highlight.Movement(originTile, currentPlayer.movement, new List<Tile>());
			OnHighlightTiles(highlightedMovementTiles, true);
		}
		else if (selectedOption == TurnOption.Attack)
		{
			hasActed = true;
			Debug.Log("Attack selected placeholder");
			currentTurnStep = UnitTurnStep.ChooseFacing;
		}
		else if (selectedOption == TurnOption.Wait)
		{
			currentTurnStep = UnitTurnStep.ChooseFacing;
		}
	}

	private void SetFacingDirection(int faceDirection)
	{
		if (currentPlayer == null)
			return;

		currentPlayer.faceDirection = faceDirection;
	}

	private void ConfirmFacingAndEndTurn()
	{
		Debug.Log("Facing confirmed. Turn ended.");
		ClearMovementHighlights();
		TilesQueueForPlayer = new List<Tile>();
		currentTarget = null;
		currentPlayer = null;
		choosingTarget = false;
		hasMoved = false;
		hasActed = false;
		selectedOptionIndex = 0;
		currentTurnStep = UnitTurnStep.SelectUnit;
	}




	private void OnGUI()
	{
		if (currentTurnStep == UnitTurnStep.ChooseOption)
		{
			TurnOption selectedOption = (TurnOption)selectedOptionIndex;
			string menuText = (selectedOption == TurnOption.Move ? "> " : "  ") + "Move\n"
				+ (selectedOption == TurnOption.Attack ? "> " : "  ") + "Attack\n"
				+ (selectedOption == TurnOption.Wait ? "> " : "  ") + "Wait";

			GUI.Box(new Rect(10f, 10f, 180f, 90f), "Command");
			GUI.Label(new Rect(20f, 35f, 160f, 60f), menuText);
		}
		else if (currentTurnStep == UnitTurnStep.ChooseFacing)
		{
			GUI.Box(new Rect(10f, 10f, 220f, 90f), "Choose Facing");
			GUI.Label(new Rect(20f, 35f, 200f, 20f), "Arrow Keys: Face Direction");
			GUI.Label(new Rect(20f, 55f, 200f, 20f), "Space: Confirm");
		}
	}

	IEnumerator Attacking()
	{
		attacking = true;
		int damage = Calculation.Dam(currentPlayer, currentTarget);
		bool hit = Random.Range(1, 100) < Calculation.Hit(currentPlayer, currentTarget);
		Debug.Log("attack this player");
		yield return new WaitForSeconds(0);
		StartCoroutine(currentPlayer.AttackAnimation());
		StartCoroutine(currentTarget.TargetAnimation(hit));
	}

	private void OnHighlightTiles(List<Tile> tiles, bool status)
	{
		for (int i = 0; i < tiles.Count; i++)
		{
			tiles[i].Highlight(status);
		}
	}

	private void ClearMovementHighlights()
	{
		OnHighlightTiles(highlightedMovementTiles, false);
		highlightedMovementTiles = new List<Tile>();
	}
}
