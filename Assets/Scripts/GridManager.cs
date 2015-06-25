using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GridManager : MonoBehaviour {
  private static int rows = 4;
  private static int cols = 4;
  private static int lowestNewTileValue = 2;
  private static int highestNewTileValue = 4;
  private static float borderOffset = 0.05f;
  private static float horizontalSpacingOffset = -1.65f;
  private static float verticalSpacingOffset = 1.65f;
  private static float borderSpacing = 0.1f;
  private static float halfTileWidth = 0.55f;
  private static float spaceBetweenTiles = 1.1f;

	private static string swipeDirection;



  private Touch initialTouch = new Touch();
  private float distance = 0;
  private bool hasSwiped = false;

  private int points;
  private List<GameObject> tiles;
  private Rect resetButton;
  private Rect gameOverButton;


  public GameObject noTile;
  public GameObject[] tilePrefabs;
  public LayerMask backgroundLayer;

	public Text scoreText;
	public Text gameOverText;

  private enum State {
    Loaded, 
    WaitingForInput, 
    CheckingMatches,
    GameOver
  }

  private State state;

  #region monodevelop
  void Awake() {
    tiles = new List<GameObject>();
    state = State.Loaded;
		gameOverText.enabled = false;

  }


  void Update() {

	SetSwipeDirection ();
    
	if (state == State.Loaded) {
      state = State.WaitingForInput;
      GenerateRandomTile();
      GenerateRandomTile();
    } else if (state == State.WaitingForInput) {
      if (swipeDirection == "Left" && hasSwiped) {
        if (MoveTilesLeft()) {
          state = State.CheckingMatches;
        }
			swipeDirection = null;
		} else if (swipeDirection == "Right" && hasSwiped) {
        if (MoveTilesRight()) {
          state = State.CheckingMatches;
        }
				swipeDirection = null;
				
			} else if (swipeDirection == "Up" && hasSwiped) {
        if (MoveTilesUp()) {
          state = State.CheckingMatches;
        }
				swipeDirection = null;
				
      } else if (swipeDirection == "Down" && hasSwiped) {
        if (MoveTilesDown()) {
          state = State.CheckingMatches;
        }
				swipeDirection = null;
				
      } 
    } else if (state == State.CheckingMatches) {
      GenerateRandomTile();
      if (CheckForMovesLeft()) {
        ReadyTilesForUpgrading();
        state = State.WaitingForInput;
      } else {
		gameOverText.enabled = true;
		gameOverText.text = "Game Over!";
		state = State.GameOver;
      }
    }
  }
  #endregion

  #region class methods
  private static Vector2 GridToWorldPoint(int x, int y) {
    return new Vector2(x + horizontalSpacingOffset + borderSpacing * x, 
                       -y + verticalSpacingOffset - borderSpacing * y);
  }
  
  private static Vector2 WorldToGridPoint(float x, float y) {
    return new Vector2((x - horizontalSpacingOffset) / (1 + borderSpacing),
                       (y - verticalSpacingOffset) / -(1 + borderSpacing));
  }
  #endregion

  #region private methods
  private bool CheckForMovesLeft() {
    if (tiles.Count < rows * cols) {
      return true;
    }
    
    for (int x = 0; x < cols; x++) {
      for (int y = 0; y < rows; y++) {
        Tile currentTile = GetObjectAtGridPosition(x, y).GetComponent<Tile>();
        Tile rightTile = GetObjectAtGridPosition(x + 1, y).GetComponent<Tile>();
        Tile downTile = GetObjectAtGridPosition (x, y + 1).GetComponent<Tile>();
        
        if (x != cols - 1 && currentTile.value == rightTile.value) {
          return true;
        } else if (y != rows - 1 && currentTile.value == downTile.value) {
          return true;
        }
      }
    }
    return false;
  }

  public void GenerateRandomTile() {
    if (tiles.Count >= rows * cols) {
      throw new UnityException("Unable to create new tile - grid is already full");
    }
    
    int value;
    // find out if we are generating a tile with the lowest or highest value
    float highOrLowChance = Random.Range(0f, 0.99f);
    if (highOrLowChance >= 0.9f) {
      value = highestNewTileValue;
    } else {
      value = lowestNewTileValue;
    }
    
    // attempt to get the starting position
    int x = Random.Range(0, cols);
    int y = Random.Range(0, rows);
    
    // starting from the random starting position, loop through
    // each cell in the grid until we find an empty positio
    bool found = false;
    while (!found) {
      if (GetObjectAtGridPosition(x, y) == noTile) {
        found = true;
        Vector2 worldPosition = GridToWorldPoint(x, y);
        GameObject obj;
        if (value == lowestNewTileValue) {
          obj = (GameObject) Instantiate(tilePrefabs[0], worldPosition, transform.rotation);
        } else {
          obj = (GameObject) Instantiate(tilePrefabs[1], worldPosition, transform.rotation);
        }
        
        tiles.Add(obj);
        TileAnimationHandler tileAnimManager = obj.GetComponent<TileAnimationHandler>();
        tileAnimManager.AnimateEntry();
      }
      
      x++;
      if (x >= cols) {
        y++;
        x = 0;
      }
      
      if (y >= rows) {
        y = 0;
      }
    }
  }

  private GameObject GetObjectAtGridPosition(int x, int y) {
    RaycastHit2D hit = Physics2D.Raycast(GridToWorldPoint(x, y), Vector2.right, borderSpacing);
    
    if (hit && hit.collider.gameObject.GetComponent<Tile>() != null) {
      return hit.collider.gameObject;
    } else {
      return noTile;
    }
  }

  private bool MoveTilesDown() {
    bool hasMoved = false;
    for (int y = rows - 1; y >= 0; y--) {
      for (int x = 0; x < cols; x++) {
        GameObject obj = GetObjectAtGridPosition(x, y);
        
        if (obj == noTile) {
          continue;
        }
        
        Vector2 raycastOrigin = obj.transform.position;
        raycastOrigin.y -= halfTileWidth;
        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, -Vector2.up, Mathf.Infinity);
        if (hit.collider != null) {
          GameObject hitObject = hit.collider.gameObject;
          if (hitObject != obj) {
            if (hitObject.tag == "Tile") {
              Tile thatTile = hitObject.GetComponent<Tile>();
              Tile thisTile = obj.GetComponent<Tile>();
              if (thisTile.power == thatTile.power && !thisTile.upgradedThisTurn && !thatTile.upgradedThisTurn) {
                UpgradeTile(obj, thisTile, hitObject, thatTile);
                hasMoved = true;
              } else {
                Vector3 newPosition = hitObject.transform.position;
                newPosition.y += spaceBetweenTiles;
                if (!Mathf.Approximately(obj.transform.position.y, newPosition.y)) {
                  obj.transform.position = newPosition;
                  hasMoved = true;
                }
              }
            } else if (hitObject.tag == "Border") {
              Vector3 newPosition = obj.transform.position;
              newPosition.y = hit.point.y + halfTileWidth + borderOffset;
              if (!Mathf.Approximately(obj.transform.position.y, newPosition.y)) {
                obj.transform.position = newPosition;
                hasMoved = true;
              }
            } 
          }
        }
      }
    }
    
    return hasMoved;
  }

  private bool MoveTilesLeft() {
    bool hasMoved = false;
    for (int x = 1; x < cols; x++) {
      for (int y = 0; y < rows; y++) {
        GameObject obj = GetObjectAtGridPosition(x, y);
        
        if (obj == noTile) {
          continue;
        }
        
        Vector2 raycastOrigin = obj.transform.position;
        raycastOrigin.x -= halfTileWidth;
        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, -Vector2.right, Mathf.Infinity);
        if (hit.collider != null) {
          GameObject hitObject = hit.collider.gameObject;
          if (hitObject != obj) {
            if (hitObject.tag == "Tile") {
              Tile thatTile = hitObject.GetComponent<Tile>();
              Tile thisTile = obj.GetComponent<Tile>();
              if (thisTile.power == thatTile.power && !thisTile.upgradedThisTurn && !thatTile.upgradedThisTurn) {
                UpgradeTile(obj, thisTile, hitObject, thatTile);
                hasMoved = true;
              } else {
                Vector3 newPosition = hitObject.transform.position;
                newPosition.x += spaceBetweenTiles;
                if (!Mathf.Approximately(obj.transform.position.x, newPosition.x)) {
                  obj.transform.position = newPosition;
                  hasMoved = true;
                }
              }
            } else if (hitObject.tag == "Border") {
              Vector3 newPosition = obj.transform.position;
              newPosition.x = hit.point.x + halfTileWidth + borderOffset;
              if (!Mathf.Approximately(obj.transform.position.x, newPosition.x)) {
                obj.transform.position = newPosition;
                hasMoved = true;
              }
            } 
          }
        }
      }
    }
    
    return hasMoved;
  }

  private bool MoveTilesRight() {
    bool hasMoved = false;
    for (int x = cols - 1; x >= 0; x--) {
      for (int y = 0; y < rows; y++) {
        GameObject obj = GetObjectAtGridPosition(x, y);
        
        if (obj == noTile) {
          continue;
        }
        
        Vector2 raycastOrigin = obj.transform.position;
        raycastOrigin.x += halfTileWidth;
        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, Vector2.right, Mathf.Infinity);
        if (hit.collider != null) {
          GameObject hitObject = hit.collider.gameObject;
          if (hitObject != obj) {
            if (hitObject.tag == "Tile") {
              Tile thatTile = hitObject.GetComponent<Tile>();
              Tile thisTile = obj.GetComponent<Tile>();
              if (thisTile.power == thatTile.power && !thisTile.upgradedThisTurn && !thatTile.upgradedThisTurn) {
                UpgradeTile(obj, thisTile, hitObject, thatTile);
                hasMoved = true;
              } else {
                Vector3 newPosition = hitObject.transform.position;
                newPosition.x -= spaceBetweenTiles;
                if (!Mathf.Approximately(obj.transform.position.x, newPosition.x)) {
                  obj.transform.position = newPosition;
                  hasMoved = true;
                }
              }
            } else if (hitObject.tag == "Border") {
              Vector3 newPosition = obj.transform.position;
              newPosition.x = hit.point.x - halfTileWidth - borderOffset;
              if (!Mathf.Approximately(obj.transform.position.x, newPosition.x)) {
                obj.transform.position = newPosition;
                hasMoved = true;
              }
            } 
          }
        }
      }
    }
    
    return hasMoved;
  }

  private bool MoveTilesUp() {
    bool hasMoved = false;
    for (int y = 1; y < rows; y++) {
      for (int x = 0; x < cols; x++) {
        GameObject obj = GetObjectAtGridPosition(x, y);
        
        if (obj == noTile) {
          continue;
        }
        
        Vector2 raycastOrigin = obj.transform.position;
        raycastOrigin.y += halfTileWidth;
        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, Vector2.up, Mathf.Infinity);
        if (hit.collider != null) {
          GameObject hitObject = hit.collider.gameObject;
          if (hitObject != obj) {
            if (hitObject.tag == "Tile") {
              Tile thatTile = hitObject.GetComponent<Tile>();
              Tile thisTile = obj.GetComponent<Tile>();
              if (thisTile.power == thatTile.power && !thisTile.upgradedThisTurn && !thatTile.upgradedThisTurn) {
                UpgradeTile(obj, thisTile, hitObject, thatTile);
                hasMoved = true;
              } else {
                Vector3 newPosition = hitObject.transform.position;
                newPosition.y -= spaceBetweenTiles;
                if (!Mathf.Approximately(obj.transform.position.y, newPosition.y)) {
                  obj.transform.position = newPosition;
                  hasMoved = true;
                }
              }
            } else if (hitObject.tag == "Border") {
              Vector3 newPosition = obj.transform.position;
              newPosition.y = hit.point.y - halfTileWidth - borderOffset;
              if (!Mathf.Approximately(obj.transform.position.y, newPosition.y)) {
                obj.transform.position = newPosition;
                hasMoved = true;
              }
            } 
          }
        }
      }
    }
    
    return hasMoved;
  }

  private void ReadyTilesForUpgrading() {
    foreach (var obj in tiles) {
      Tile tile = obj.GetComponent<Tile>();
      tile.upgradedThisTurn = false;
    }
  }

  public void Reset() {
    foreach (var tile in tiles) {
      Destroy(tile);
    }
	
    tiles.Clear();
    points = 0;
    scoreText.text = "0";
	gameOverText.text = "";
	gameOverText.enabled = false;
	state = State.Loaded;
  }

  private void UpgradeTile(GameObject toDestroy, Tile destroyTile, GameObject toUpgrade, Tile upgradeTile) {
    Vector3 toUpgradePosition = toUpgrade.transform.position;

    tiles.Remove(toDestroy);
    tiles.Remove(toUpgrade);
    Destroy(toDestroy);
    Destroy(toUpgrade);

    // create the upgraded tile
    GameObject newTile = (GameObject) Instantiate(tilePrefabs[upgradeTile.power], toUpgradePosition, transform.rotation);
    tiles.Add(newTile);
    Tile tile = newTile.GetComponent<Tile>();
    tile.upgradedThisTurn = true;

    points += upgradeTile.value * 2;
    scoreText.text = points.ToString();

    TileAnimationHandler tileAnim = newTile.GetComponent<TileAnimationHandler>();
    tileAnim.AnimateUpgrade();
  }


	private void SetSwipeDirection(){
		foreach(Touch t in Input.touches)
		{
			if (t.phase == TouchPhase.Began)
			{
				initialTouch = t;
			}
			else if (t.phase == TouchPhase.Moved && !hasSwiped)
			{
				float deltaX = initialTouch.position.x - t.position.x;
				float deltaY = initialTouch.position.y - t.position.y;
				distance = Mathf.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
				bool swipedSideways = Mathf.Abs(deltaX) > Mathf.Abs(deltaY);



				if (distance > 100f)
				{
					if (swipedSideways && deltaX > 0) //swiped left
					{
						swipeDirection = "Left";
					}
					else if (swipedSideways && deltaX <= 0) //swiped right
					{
						swipeDirection = "Right";
					}
					else if (!swipedSideways && deltaY > 0) //swiped down
					{
						swipeDirection = "Down";
					}
					else if (!swipedSideways && deltaY <= 0)  //swiped up
					{
						swipeDirection = "Up";
					}
					
					hasSwiped = true;
				}
				
			}
			else if (t.phase == TouchPhase.Ended)
			{
				initialTouch = new Touch();
				hasSwiped = false;
			}
		}
	}

}

  #endregion
