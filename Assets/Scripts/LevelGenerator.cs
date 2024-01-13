using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("Refrences")]
    [SerializeField] GameObject _tilePrefab;
    [SerializeField] GameObject _platformPrefab;
    [SerializeField] Transform _blocksParent;
    [SerializeField] Transform _platformsParent;

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

    
    private Vector3 _minInnerRoomBounds;
    private Vector3 _maxInnerRoomBounds;
    private Vector3 lastSpawnPosition;
    private int _platformCounter = 0;

    void Start()
    {
        GenerateRoom();
        GeneratePlatforms();
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
                        Instantiate(_tilePrefab, tilePosition, Quaternion.identity, _blocksParent);
                    }
                }
            }
        }
    }
    void GenerateLeftOvers(float xExtent, float zExtent, Vector3 platformSize, float platformLevelHeight)
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

        GenerateLeftOvers(xExtent, zExtent, platformSize, platformLevelHeight);
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
                Instantiate(_platformPrefab, center, Quaternion.identity, _platformsParent);
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

    //Tests
    bool CheckOverlap(Vector3 position)
    {
        var shpereCheckRange = _minInnerRoomBounds.y;
        Collider[] colliders = Physics.OverlapSphere(position, shpereCheckRange);
        return colliders.Length > 0;
    }

    bool CheckDistance(Vector3 position)
    {
        var distance = Vector3.Distance(lastSpawnPosition, position);

        if (distance > _maxHorizontalJump || distance < _minRadius)
            return false;
        return true;
    }

}
