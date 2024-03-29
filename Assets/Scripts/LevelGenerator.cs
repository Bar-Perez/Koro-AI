using UnityEngine;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    [Header("Refrences")]
    [SerializeField] GameObject _tilePrefab;
    [SerializeField] GameObject _platformPrefab;
    [SerializeField] GameObject _trapTilePrefab;
    [SerializeField] GameObject _trapPlatformsPrefab;
    [SerializeField] GameObject _doorPlatformsPrefab;
    [SerializeField] GameObject _keyPrefab;
    [SerializeField] GameObject _coinPlatformPrefab;
    [SerializeField] GameObject _coinFloorPrefab;
    [SerializeField] Transform _blocksParent;
    [SerializeField] Transform _platformsParent;
    [SerializeField] Transform _trapsParent;
    [SerializeField] Transform _doorParent;
    [SerializeField] Transform _keyParent;

    [Header("Room inner size")]
    [SerializeField] int _roomLength = 5;
    [SerializeField] int _roomWidth = 5;
    [SerializeField] int _roomHeight = 5;

    [Header("Parameters for platform generation")]
    [SerializeField] int _maxPlatforms = 10;
    [SerializeField] int _verticalLevels = 5;
    [SerializeField] float _maxHorizontalJump = 5f;
    [SerializeField] float _minRadius = 3.0f;

    [Header("Maximum attempts to find a valid platform position")]
    [SerializeField] int _maxAttempts = 100;

    [Header("Interactable")]
    [SerializeField] int _floorTraps = 10;
    [SerializeField] int _platformTraps = 5;
    [SerializeField] int _platformCoins = 10;
    [SerializeField] int _floorCoins = 5;

    [Header("Collectable")]
    [SerializeField] int _coinAmount = 15;

    private Vector3 _minInnerRoomBounds;
    private Vector3 _maxInnerRoomBounds;
    private Vector3 lastSpawnPosition;
    private int _platformCounter = 0;
    private List<GameObject> _floorTilesList = new();
    private List<GameObject> _platformsList = new();
    private HashSet<int> _usedSpotsFloor = new HashSet<int>();
    private HashSet<int> _usedSpotsPlatform = new HashSet<int>();

    void Start()
    {
        GenerateRoom();
        GeneratePlatforms();
        GenerateEndDoor(_doorPlatformsPrefab);
        GenerateKey(_keyPrefab);
        GenerateTraps();
        GenerateCoins();
    }

    void GenerateRoom()
    {
        if (_tilePrefab == null)
            throw new System.NullReferenceException("Tile Prefab is not assigned!");

        // Get the size of the tile prefab
        Vector3 tileSize = _tilePrefab.GetComponent<Renderer>().bounds.size;

        // Calculate the outer size by adding 2 to each dimension
        int outerLength = _roomLength + 2;
        int outerWidth = _roomWidth + 2;
        int outerHeight = _roomHeight + 1; // Exclude the top face



        // Calculate the bounds of the inner part of the room
        _minInnerRoomBounds = new Vector3(tileSize.x, tileSize.y, tileSize.z);
        _maxInnerRoomBounds = new Vector3(tileSize.x * _roomLength, tileSize.y * _roomHeight, tileSize.z * _roomWidth);

        for (int x = 0; x < outerLength; x++)
        {
            for (int y = 0; y < outerHeight; y++)
            {
                for (int z = 0; z < outerWidth; z++)
                {
                    // Check if the current position is on the outer layer and not the top face
                    if (x == 0 || y == 0 || z == 0 || x == outerLength - 1 || z == outerWidth - 1)
                    {
                        Vector3 tilePosition = new Vector3(x * tileSize.x, y * tileSize.y, z * tileSize.z);
                        if(y ==0 && x >= _minInnerRoomBounds.x && x <= _maxInnerRoomBounds.x && z >= _minInnerRoomBounds.z && z <= _maxInnerRoomBounds.z)
                        {
                            var go = Instantiate(_tilePrefab, tilePosition, Quaternion.identity, _blocksParent);
                            _floorTilesList.Add(go);
                        }
                        else
                            Instantiate(_tilePrefab, tilePosition, Quaternion.identity, _blocksParent);

                    }
                }
            }
        }
    }
    void GenerateLeftOversPlatforms(float xExtent, float zExtent, Vector3 platformSize, float platformLevelHeight)
    { 
        var iteration = _maxPlatforms - _platformCounter;

        InitialRandomPosition(xExtent, zExtent);

        for (int i = 1; i <= iteration; i++)
        {
            GeneratePlatform(lastSpawnPosition, xExtent, zExtent);
            lastSpawnPosition.y = Mathf.Clamp(_minInnerRoomBounds.y + (platformLevelHeight * i), _minInnerRoomBounds.y, _maxInnerRoomBounds.y);
        }

    }
    void GeneratePlatforms()
    {
        if (_platformPrefab == null)
            throw new System.NullReferenceException("Platform Prefab is not assigned!");

        //impossible to have more vertical levels then max platforms
        _verticalLevels = _verticalLevels < _maxPlatforms ? _verticalLevels : _maxPlatforms;

        // Get the size of the platform prefab
        Vector3 platformSize = _platformPrefab.GetComponent<Renderer>().bounds.size;

        // Calculate the extent of each direction
        float xExtent = (platformSize.x / 2);
        float zExtent = (platformSize.z / 2);

        InitialRandomPosition(xExtent, zExtent);

        var platformPerLevel = _maxPlatforms / _verticalLevels;
        var platformLevelHeight = _maxInnerRoomBounds.y / _verticalLevels;

        for (int i = 1; i < _verticalLevels; i++)
        {
            GeneratePlatform(lastSpawnPosition, xExtent, zExtent);
            for (int j = 1; j < platformPerLevel; j++)
            {
                GeneratePlatform(lastSpawnPosition, xExtent, zExtent);
            }
            // increase the height of the next platforms
            lastSpawnPosition.y = Mathf.Clamp(_minInnerRoomBounds.y + (platformLevelHeight * i), _minInnerRoomBounds.y, _maxInnerRoomBounds.y);
        }

        if (_platformCounter <= 0)
            throw new System.Exception("Room is too small,no platforms Generated");

        GenerateLeftOversPlatforms(xExtent, zExtent, platformSize, platformLevelHeight);
    }
    void GeneratePlatform(Vector3 spawnPosition, float xExtent, float zExtent)
    {
        int attempts = 0;
        float xPosition;
        float yPosition;
        float zPosition;

        do
        {
            // Set the range of the platform position to be in jumping distance and room bounds
            xPosition = Mathf.Clamp(Random.Range(spawnPosition.x - _maxHorizontalJump, spawnPosition.x + _maxHorizontalJump), _minInnerRoomBounds.x + xExtent, _maxInnerRoomBounds.x - xExtent);
            yPosition = spawnPosition.y;
            zPosition = Mathf.Clamp(Random.Range(spawnPosition.x - _maxHorizontalJump, spawnPosition.x + _maxHorizontalJump), _minInnerRoomBounds.z + zExtent, _maxInnerRoomBounds.z - zExtent);

            var center = new Vector3(xPosition, yPosition, zPosition);
            // Check if the new position is not overlapping with the last platform and is within the bounds
            if (!CheckOverlap(center) && CheckDistance(center))
            {
                // Generate the new platform at the calculated position
                var go =Instantiate(_platformPrefab, center, Quaternion.identity, _platformsParent);
                _platformsList.Add(go);
                lastSpawnPosition = center;
                _platformCounter++;
                return; // Exit the loop if a valid platform position is found
            }

            attempts++;

        } while (attempts < _maxAttempts);

        Debug.LogWarning("Failed to find a valid platform position after " + _maxAttempts + " attempts.");
    }

    private void InitialRandomPosition(float xExtent, float zExtent)
    {
        float xPosition = Random.Range(_minInnerRoomBounds.x + xExtent, _maxInnerRoomBounds.x - xExtent);
        float yPosition = _minInnerRoomBounds.y * 2;
        float zPosition = Random.Range(_minInnerRoomBounds.z + zExtent, _maxInnerRoomBounds.z - zExtent);

        lastSpawnPosition = new Vector3(xPosition, yPosition, zPosition);
    }

    //Utiliy Method
    bool CheckOverlap(Vector3 position)
    {
        var shpereCheckRange = _minInnerRoomBounds.y;
        Collider[] colliders = Physics.OverlapSphere(position, shpereCheckRange);
        return colliders.Length > 0;
    }

    //Utiliy Method
    bool CheckDistance(Vector3 position)
    {
        var distance = Vector3.Distance(lastSpawnPosition, position);

        if (distance > _maxHorizontalJump || distance < _minRadius)
            return false;
        return true;
    }
    void GenerateTraps()
    {
        GenerateObjects(_floorTraps, _floorTilesList, _trapTilePrefab, _usedSpotsFloor);
        GenerateObjects(_platformTraps, _platformsList, _trapPlatformsPrefab, _usedSpotsPlatform);
    }
    void GenerateObjects(float amount,List<GameObject> list, GameObject type,HashSet<int> hashSet)
    {
        for (int i = 0; i < amount; i++)
        {
            int rnd;

            // Ensure the generated index is unique
            do
            {
                rnd = Random.Range(0, list.Count);
            } while (hashSet.Contains(rnd));


            hashSet.Add(rnd);

            var position = list[rnd].transform.position;
            Destroy(list[rnd]);
            list[rnd] = Instantiate(type, position, Quaternion.identity, _trapsParent);
        }

    }
    void GenerateEndDoor(GameObject doorType)
    {
        //start from the middle to save time
        int startIndex = _platformsList.Count / 2;
        // spawn at top 1/3 of max height
        float spawnPositionY = _maxInnerRoomBounds.y - (_maxInnerRoomBounds.y / 3f);
        while (startIndex < _platformsList.Count)
        {
            var position = _platformsList[startIndex].transform.position;

            if (position.y >= spawnPositionY)
            {
                Destroy(_platformsList[startIndex]);
                _platformsList[startIndex] = Instantiate(doorType, position, Quaternion.identity, _doorParent);
                _usedSpotsPlatform.Add(startIndex);
                return;
            }
            startIndex++;
        }
    }
    void GenerateCoins()
    {
        GenerateObjects(_floorCoins, _floorTilesList, _coinFloorPrefab, _usedSpotsFloor);
        GenerateObjects(_platformCoins, _platformsList, _coinPlatformPrefab, _usedSpotsPlatform);
    }
    void GenerateKey(GameObject keyType)
    {
        var rnd = Random.Range(0, _platformsList.Count);
        var position = _platformsList[rnd].transform.position;
        Destroy(_platformsList[rnd]);
        _platformsList[rnd] = Instantiate(keyType, position, Quaternion.identity, _keyParent);
        _usedSpotsPlatform.Add(rnd);
    }

}
