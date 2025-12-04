using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public enum ItemType
{
    Player, Ground, Enemy, Goal, Obstacle, Fireball,
    PowerupHP, PowerupInvincible, PowerupAmmo
}

public class PlatformerController : MonoBehaviour
{
    [Header("Rendering")]
    [SerializeField] private Material playerMat;
    [SerializeField] private Material playerRainbowMat;
    [SerializeField] private Material playerTransparentMat;
    [SerializeField] private Material groundMat;
    [SerializeField] private Material groundTopMat;
    [SerializeField] private Material enemyMat;
    [SerializeField] private Material obstacleMat;
    [SerializeField] private Material fireballMat;
    [SerializeField] private Material goalMat;

    [Header("Powerup Materials")]
    [SerializeField] private Material powerupHPMat;
    [SerializeField] private Material powerupInvincibleMat;
    [SerializeField] private Material powerupAmmoMat;

    [Header("UI")]
    [SerializeField] private Text uiText;
    [SerializeField] private Text resultText;
    [SerializeField] private GameObject restartButton;

    [Header("Settings")]
    [SerializeField] private int levelLength;
    [SerializeField] private float speed;
    [SerializeField] private float jumpForce;
    [SerializeField] private float lowJumpMultiplier = 2.5f;
    [SerializeField] private float gravity;
    [SerializeField] private float fallGravityMultiplier;
    [SerializeField] private float airSpeedMultiplier;
    [SerializeField] private float invincibilityDuration;
    [SerializeField] private float fireballSpeed;

    [Header("Platforming Feel")]
    [SerializeField] private float coyoteTimeDuration;
    [SerializeField] private float visibilityDotThreshold;
    [SerializeField] private float visibilityDistance;

    private Mesh cubeMesh;
    private Dictionary<int, ItemType> typeMap = new Dictionary<int, ItemType>();

    private List<int> groundIDs = new List<int>();
    private List<int> enemyIDs = new List<int>();
    private List<int> obstacleIDs = new List<int>();
    private List<int> fireballIDs = new List<int>();
    private List<int> hpPowerupIDs = new List<int>();
    private List<int> invPowerupIDs = new List<int>();
    private List<int> ammoPowerupIDs = new List<int>();

    private Dictionary<int, float> fireballDirs = new Dictionary<int, float>();
    private int goalID = -1;

    private Dictionary<int, float> enemyDirs = new Dictionary<int, float>();
    private float enemySpeed = 5f;

    private int playerID;
    private Vector3 playerPos;
    private Vector3 playerVel;
    private float playerFacingDir = 1f;
    private bool isGrounded;
    private float coyoteTimer;
    private int hp = 3;
    private int maxHp = 3;
    private float timeElapsed;
    private bool gameOver;

    private int score = 0;
    private int ammo = 10;
    private int maxAmmo = 10;

    private bool isInvincible;
    private float invincibleTimer;
    private float superInvincibleTimer;
    private float unlimitedAmmoTimer;
    private float knockbackTimer;

    private void Start()
    {
        if (Camera.main != null)
            Camera.main.transform.position = new Vector3(0, 10, -30);

        if (resultText) resultText.gameObject.SetActive(false);
        if (restartButton) restartButton.SetActive(false);

        if (playerMat) playerMat.enableInstancing = true;
        if (playerRainbowMat) playerRainbowMat.enableInstancing = true;
        if (playerTransparentMat) playerTransparentMat.enableInstancing = true;
        if (groundMat) groundMat.enableInstancing = true;
        if (groundTopMat) groundTopMat.enableInstancing = true;
        if (enemyMat) enemyMat.enableInstancing = true;
        if (obstacleMat) obstacleMat.enableInstancing = true;
        if (fireballMat) fireballMat.enableInstancing = true;
        if (goalMat) goalMat.enableInstancing = true;
        if (powerupHPMat) powerupHPMat.enableInstancing = true;
        if (powerupInvincibleMat) powerupInvincibleMat.enableInstancing = true;
        if (powerupAmmoMat) powerupAmmoMat.enableInstancing = true;

        GenerateMesh();
        CreateWorld();
    }

    private void Update()
    {
        if (gameOver) return;
        timeElapsed += Time.deltaTime;

        HandlePowerupTimers();
        HandlePlayer();
        HandleEnemies();
        HandleFireballs();
        RenderWorld();
        UpdateUI();

        if (isInvincible)
        {
            invincibleTimer -= Time.deltaTime;
            if (invincibleTimer <= 0) isInvincible = false;
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void TriggerGameOver(bool win)
    {
        if (gameOver) return;
        gameOver = true;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (resultText)
        {
            resultText.gameObject.SetActive(true);
            if (win)
            {
                score += 1000;
                resultText.text = "YOU WIN!";
                resultText.color = Color.green;
            }
            else
            {
                resultText.text = "GAME OVER";
                resultText.color = Color.red;
            }
        }

        if (restartButton) restartButton.SetActive(true);
    }

    private void HandlePowerupTimers()
    {
        float dt = Time.deltaTime;

        if (superInvincibleTimer > 0)
        {
            superInvincibleTimer -= dt;
            float hue = Mathf.PingPong(Time.time * 2f, 1f);
            if (playerRainbowMat) playerRainbowMat.color = Color.HSVToRGB(hue, 1f, 1f);
        }

        if (unlimitedAmmoTimer > 0)
        {
            unlimitedAmmoTimer -= dt;
            if (unlimitedAmmoTimer <= 0) ammo = maxAmmo;
        }
    }

    private void HandlePlayer()
    {
        float dt = Time.deltaTime;

        float gravityThisFrame = gravity;

        if (playerVel.y < 0)
        {
            gravityThisFrame *= fallGravityMultiplier;
        }
        else if (playerVel.y > 0 && !Input.GetKey(KeyCode.Space))
        {
            gravityThisFrame *= lowJumpMultiplier;
        }

        playerVel.y -= gravityThisFrame * dt;

        if (knockbackTimer > 0) knockbackTimer -= dt;

        float moveX = 0f;

        if (knockbackTimer > 0)
        {
            moveX = playerVel.x;
        }
        else
        {
            float rawMove = Input.GetAxisRaw("Horizontal");
            if (rawMove > 0) playerFacingDir = 1f;
            else if (rawMove < 0) playerFacingDir = -1f;

            moveX = rawMove * speed;
            if (!isGrounded) moveX *= airSpeedMultiplier;

            playerVel.x = 0;
        }

        Vector3 nextPos = playerPos + new Vector3(moveX * dt, 0, 0);

        if (HasCollision(playerID, nextPos, out int hitID))
        {
            ItemType type = typeMap[hitID];

            if (type == ItemType.Ground) { /* Stop */ }
            else if (type == ItemType.Obstacle || type == ItemType.Enemy)
            {
                if (superInvincibleTimer > 0)
                {
                    DestroyItem(hitID); score += 50; playerPos.x = nextPos.x;
                }
                else ProcessHit(hitID);
            }
            else if (IsPowerup(type) || type == ItemType.Fireball || type == ItemType.Goal)
            {
                ProcessHit(hitID);
                playerPos.x = nextPos.x;
            }
        }
        else playerPos.x = nextPos.x;

        nextPos = playerPos + new Vector3(0, playerVel.y * dt, 0);
        if (HasCollision(playerID, nextPos, out hitID))
        {
            ItemType type = typeMap[hitID];

            if (type == ItemType.Ground || type == ItemType.Obstacle)
            {
                if (type == ItemType.Obstacle)
                {
                    if (superInvincibleTimer > 0) { DestroyItem(hitID); score += 50; }
                    else ProcessHit(hitID);
                }

                if (playerVel.y < 0) { isGrounded = true; playerVel.y = 0; }
                else playerVel.y = 0;
            }
            else if (type == ItemType.Enemy)
            {
                if (superInvincibleTimer > 0) { DestroyItem(hitID); score += 50; }
                else ProcessHit(hitID);
            }
            else if (IsPowerup(type) || type == ItemType.Goal)
            {
                ProcessHit(hitID);
                playerPos.y = nextPos.y;
                isGrounded = false;
            }
        }
        else
        {
            playerPos.y = nextPos.y;
            isGrounded = false;
        }

        if (isGrounded) coyoteTimer = coyoteTimeDuration;
        else coyoteTimer -= Time.deltaTime;

        if (knockbackTimer <= 0 && Input.GetKeyDown(KeyCode.Space) && coyoteTimer > 0)
        {
            playerVel.y = jumpForce;
            isGrounded = false;
            coyoteTimer = 0;
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (ammo > 0 || unlimitedAmmoTimer > 0)
            {
                SpawnFireball(playerPos, playerFacingDir);
                if (unlimitedAmmoTimer <= 0) ammo--;
            }
        }

        if (playerPos.y < -20) hp = 0;

        CollisionManager.Instance.UpdateBox(playerID, playerPos, Vector3.one);

        if (Camera.main != null)
        {
            Vector3 camPos = Camera.main.transform.position;
            camPos.x = Mathf.Lerp(camPos.x, playerPos.x, 5f * Time.deltaTime);

            float targetY = playerPos.y + 3f;
            camPos.y = Mathf.Lerp(camPos.y, targetY, 5f * Time.deltaTime);

            Camera.main.transform.position = camPos;
        }

        if (hp <= 0)
        {
            TriggerGameOver(false);
        }
    }

    private void HandleEnemies()
    {
        float dt = Time.deltaTime;

        foreach (int id in enemyIDs)
        {
            if (!enemyDirs.ContainsKey(id)) enemyDirs[id] = (Random.value > 0.5f) ? 1f : -1f;

            Matrix4x4 m = CollisionManager.Instance.GetMatrix(id);
            Vector3 pos = m.GetPosition();
            Vector3 size = CollisionManager.Instance.GetSize(id);
            float dir = enemyDirs[id];

            bool isGrounded = HasCollision(id, pos + Vector3.down * 0.1f, out int gID);
            if (!isGrounded)
            {
                float dropAmount = gravity * dt;
                Vector3 nextPos = pos;
                nextPos.y -= dropAmount;

                if (HasCollision(id, nextPos, out int hitID))
                {
                    BoxCollider3D groundCol = CollisionManager.Instance.GetCollider(hitID);
                    if (groundCol != null) pos.y = groundCol.Max.y + (size.y * 0.5f);
                }
                else pos.y = nextPos.y;
            }

            if (isGrounded)
            {
                Vector3 wallCheck = pos + new Vector3(dir * 0.7f, 0.25f, 0);
                Vector3 cliffCheck = pos + new Vector3(dir * 0.8f, -1.2f, 0);

                bool hitWall = HasCollision(id, wallCheck, out int wallID);
                bool hitGroundAhead = HasCollision(id, cliffCheck, out int floorID);

                bool turn = false;
                if (hitWall)
                {
                    ItemType t = typeMap[wallID];
                    if (t == ItemType.Ground || t == ItemType.Obstacle) turn = true;
                }
                if (!hitGroundAhead) turn = true;

                if (turn) { dir *= -1; enemyDirs[id] = dir; }
                else { pos.x += dir * enemySpeed * dt; }
            }

            if (Vector3.Distance(pos, playerPos) < 0.95f)
            {
                if (superInvincibleTimer <= 0) ProcessHit(id);
            }

            CollisionManager.Instance.UpdateBox(id, pos, size);
        }
    }

    private void HandleFireballs()
    {
        float dt = Time.deltaTime;
        var fbList = new List<int>(fireballIDs);

        foreach (int id in fbList)
        {
            if (!CollisionManager.Instance.HasCollider(id)) continue;
            if (!fireballDirs.ContainsKey(id)) { RemoveFireball(id); continue; }

            float dir = fireballDirs[id];
            Matrix4x4 m = CollisionManager.Instance.GetMatrix(id);
            Vector3 pos = m.GetPosition();
            Vector3 size = CollisionManager.Instance.GetSize(id);

            bool isSuperBullet = (size.x > 0.5f);
            float currentSpeed = isSuperBullet ? (fireballSpeed * 1.5f) : fireballSpeed;

            Vector3 next = pos + new Vector3(dir * currentSpeed * dt, 0, 0);
            CollisionManager.Instance.UpdateBox(id, next, size);

            List<int> hits = CollisionManager.Instance.CheckCollisions(id, next);
            if (hits.Count > 0)
            {
                foreach (int hit in hits)
                {
                    if (hit == playerID) continue;
                    if (!typeMap.ContainsKey(hit)) continue;
                    ItemType type = typeMap[hit];

                    if (type == ItemType.Enemy)
                    {
                        DestroyItem(hit);
                        score += 100;
                        if (!isSuperBullet) { RemoveFireball(id); break; }
                    }
                    else if (type == ItemType.Ground || type == ItemType.Obstacle)
                    {
                        if (!isSuperBullet) { RemoveFireball(id); break; }
                    }
                }
            }

            if (Vector3.Distance(next, playerPos) > 200f) RemoveFireball(id);
        }
    }

    private bool HasCollision(int selfID, Vector3 pendingPos, out int hitID)
    {
        hitID = -1;
        List<int> hits = CollisionManager.Instance.CheckCollisions(selfID, pendingPos);
        if (hits.Count > 0)
        {
            hitID = hits[0];
            return true;
        }
        return false;
    }

    private bool IsPowerup(ItemType t)
    {
        return t == ItemType.PowerupHP || t == ItemType.PowerupInvincible || t == ItemType.PowerupAmmo;
    }

    private void ProcessHit(int id)
    {
        if (!typeMap.ContainsKey(id)) return;
        ItemType type = typeMap[id];

        if (type == ItemType.Obstacle)
        {
            hp = 0;
        }
        else if (type == ItemType.Enemy)
        {
            if (!isInvincible)
            {
                hp--;

                playerVel.y = 15f;
                BoxCollider3D enemyBox = CollisionManager.Instance.GetCollider(id);
                float pushDir = (playerPos.x < enemyBox.Center.x) ? -1f : 1f;
                playerVel.x = pushDir * 20f;
                knockbackTimer = 0.3f;

                isInvincible = true;
                invincibleTimer = invincibilityDuration;
            }
        }
        else if (type == ItemType.PowerupHP)
        {
            hp++; if (hp > maxHp) hp = maxHp;
            score += 25; DestroyItem(id);
        }
        else if (type == ItemType.PowerupInvincible)
        {
            superInvincibleTimer = 8f; score += 50; DestroyItem(id);
        }
        else if (type == ItemType.PowerupAmmo)
        {
            unlimitedAmmoTimer = 8f; ammo = maxAmmo; score += 50; DestroyItem(id);
        }
        else if (type == ItemType.Goal)
        {
            TriggerGameOver(true);
        }
    }

    private void CreateWorld()
    {
        groundIDs.Clear(); enemyIDs.Clear(); obstacleIDs.Clear();
        fireballIDs.Clear(); typeMap.Clear();

        hpPowerupIDs.Clear(); invPowerupIDs.Clear(); ammoPowerupIDs.Clear();

        score = 0;
        ammo = maxAmmo;

        float airTime = 2f * (jumpForce / gravity);
        float maxJumpDist = speed * airTime;

        float startX = -10f;
        float safeY = 6f;
        float safeWidth = 20f;

        float safeBottom = -50f;
        float safeBlockHeight = safeY - safeBottom;
        float safeCenterY = safeY - (safeBlockHeight / 2f);
        float safeCenterX = startX + (safeWidth / 2f);

        CreateBox(new Vector3(safeCenterX, safeCenterY, 0),
                  new Vector3(safeWidth, safeBlockHeight, 5),
                  ItemType.Ground);

        playerPos = new Vector3(safeCenterX, safeY + 2f, 0);
        playerID = CollisionManager.Instance.AddBox(playerPos, Vector3.one);
        typeMap[playerID] = ItemType.Player;
        hp = 3;

        float currentX = startX + safeWidth;
        float currentY = 0f;

        for (int i = 0; i < levelLength; i++)
        {
            bool makeGap = (i > 0) && (Random.value < 0.40f);
            float gapSize = 0f;
            float yChange = 0f;

            if (Random.value < 0.45f)
            {
                float change = Random.Range(-3f, 3f);
                if (Mathf.Abs(change) < 1f) change = (change > 0 ? 1f : -1f);
                yChange = change;
            }

            float nextY = Mathf.Clamp(currentY + yChange, -5f, 10f);

            if (makeGap)
            {
                float maxGap = maxJumpDist * 0.7f;
                if (nextY > currentY) maxGap *= 0.6f;
                gapSize = Random.Range(2.5f, maxGap);
            }

            currentX += gapSize;
            currentY = nextY;

            float chunkWidth = Random.Range(10f, 16f);
            float zDepth = Random.Range(4f, 12f);

            float groundTop = currentY;
            float groundBottom = -50f;
            float groundHeight = groundTop - groundBottom;
            float groundCenterY = groundTop - (groundHeight / 2f);
            float centerX = currentX + (chunkWidth / 2f);

            Vector3 groundPos = new Vector3(centerX, groundCenterY, 0);
            Vector3 groundSize = new Vector3(chunkWidth, groundHeight, zDepth);

            CreateBox(groundPos, groundSize, ItemType.Ground);

            if (chunkWidth > 6f)
            {
                float enemyY = currentY + 0.5f;
                float obstacleY = currentY + 0.75f;

                int attempts = Mathf.FloorToInt(chunkWidth / 7f);

                attempts += Random.Range(0, 2);

                for (int k = 0; k < attempts; k++)
                {
                    float margin = 1.5f;
                    float halfRange = (chunkWidth / 2f) - margin;
                    float randomXOffset = Random.Range(-halfRange, halfRange);
                    float itemX = centerX + randomXOffset;

                    float roll = Random.value;
                    Vector3 checkSize = new Vector3(1.5f, 0.1f, 1.5f);

                    if (roll < 0.50f)
                    {
                        Vector3 spawnPos = new Vector3(itemX, enemyY, 0);
                        if (!CollisionManager.Instance.IsSpaceOccupied(spawnPos + Vector3.up * 0.5f, checkSize))
                            CreateBox(spawnPos, Vector3.one, ItemType.Enemy);
                    }
                    else if (roll < 0.80f)
                    {
                        Vector3 spawnPos = new Vector3(itemX, obstacleY, 0);
                        Vector3 obsSize = new Vector3(1, 1.5f, 1);
                        if (!CollisionManager.Instance.IsSpaceOccupied(spawnPos + Vector3.up * 0.5f, checkSize))
                            CreateBox(spawnPos, obsSize, ItemType.Obstacle);
                    }
                    else
                    {
                        Vector3 spawnPos = new Vector3(itemX, currentY + 0.5f, 0);
                        if (!CollisionManager.Instance.IsSpaceOccupied(spawnPos + Vector3.up * 0.5f, checkSize))
                        {
                            float pRoll = Random.value;
                            ItemType pType = ItemType.PowerupHP;
                            if (pRoll > 0.6f) pType = ItemType.PowerupAmmo;
                            else if (pRoll > 0.3f) pType = ItemType.PowerupInvincible;

                            CreateBox(spawnPos, Vector3.one * 0.5f, pType);
                        }
                    }
                }
            }
            currentX += chunkWidth;
        }

        goalID = CreateBox(new Vector3(currentX + 5, currentY + 2, 0), new Vector3(2, 4, 2), ItemType.Goal);
    }

    private int CreateBox(Vector3 pos, Vector3 size, ItemType type)
    {
        int id = CollisionManager.Instance.AddBox(pos, size);
        typeMap[id] = type;

        if (type == ItemType.Ground) groundIDs.Add(id);
        else if (type == ItemType.Enemy) enemyIDs.Add(id);
        else if (type == ItemType.Obstacle) obstacleIDs.Add(id);
        else if (type == ItemType.Fireball) fireballIDs.Add(id);
        else if (type == ItemType.Goal) goalID = id;
        else if (type == ItemType.PowerupHP) hpPowerupIDs.Add(id);
        else if (type == ItemType.PowerupInvincible) invPowerupIDs.Add(id);
        else if (type == ItemType.PowerupAmmo) ammoPowerupIDs.Add(id);

        return id;
    }

    private void DestroyItem(int id)
    {
        if (!typeMap.ContainsKey(id)) return;
        ItemType t = typeMap[id];

        if (t == ItemType.Enemy) enemyIDs.Remove(id);
        else if (t == ItemType.Obstacle) obstacleIDs.Remove(id);
        else if (t == ItemType.Fireball)
        {
            fireballIDs.Remove(id);
            if (fireballDirs.ContainsKey(id)) fireballDirs.Remove(id);
        }
        else if (t == ItemType.PowerupHP) hpPowerupIDs.Remove(id);
        else if (t == ItemType.PowerupInvincible) invPowerupIDs.Remove(id);
        else if (t == ItemType.PowerupAmmo) ammoPowerupIDs.Remove(id);

        typeMap.Remove(id);
        CollisionManager.Instance.RemoveBox(id);
    }

    private void SpawnFireball(Vector3 centerPos, float dir)
    {
        Vector3 spawnPos = centerPos + new Vector3(dir * 0.6f, 0.5f, 0);
        float size = (unlimitedAmmoTimer > 0) ? 0.8f : 0.4f;
        int id = CreateBox(spawnPos, Vector3.one * size, ItemType.Fireball);
        fireballDirs[id] = dir;
    }

    private void RemoveFireball(int id)
    { DestroyItem(id); }

    private void RenderWorld()
    {
        DrawGround();
        DrawListWithFacade(enemyIDs, enemyMat);
        DrawListWithFacade(obstacleIDs, obstacleMat);
        DrawListWithFacade(fireballIDs, fireballMat);

        DrawListWithFacade(hpPowerupIDs, powerupHPMat);
        DrawListWithFacade(invPowerupIDs, powerupInvincibleMat);
        DrawListWithFacade(ammoPowerupIDs, powerupAmmoMat);

        List<int> pList = new List<int> { playerID };

        Material currentPlayerMat = playerMat;

        if (superInvincibleTimer > 0 && playerRainbowMat != null)
        {
            currentPlayerMat = playerRainbowMat;
        }
        else if (isInvincible && playerTransparentMat != null)
        {
            currentPlayerMat = playerTransparentMat;
        }

        DrawListWithFacade(pList, currentPlayerMat);

        if (goalID != -1 && goalMat != null)
        {
            List<int> gList = new List<int> { goalID };
            DrawListWithFacade(gList, goalMat);
        }
    }

    private void DrawGround()
    {
        if (groundMat == null || groundIDs.Count == 0) return;

        List<Matrix4x4> dirtMatrices = new List<Matrix4x4>();
        List<Matrix4x4> grassMatrices = new List<Matrix4x4>();

        Vector3 camPos = Camera.main.transform.position;
        Vector3 camFwd = Camera.main.transform.forward;
        float grassHeight = 0.8f;

        foreach (int id in groundIDs)
        {
            if (!CollisionManager.Instance.HasCollider(id)) continue;
            Matrix4x4 m = CollisionManager.Instance.GetMatrix(id);
            Vector3 pos = m.GetPosition();
            Vector3 size = CollisionManager.Instance.GetSize(id);

            float dist = Vector3.Distance(pos, camPos);
            if (dist > visibilityDistance) continue;

            Vector3 dir = (pos - camPos).normalized;
            if (Vector3.Dot(dir, camFwd) < visibilityDotThreshold && dist > 20f) continue;

            float dirtHeight = size.y - grassHeight;

            if (dirtHeight > 0)
            {
                Vector3 dirtPos = pos;
                dirtPos.y -= (grassHeight / 2f);
                Vector3 dirtSize = new Vector3(size.x, dirtHeight, size.z);
                AddBoxWithFacades(dirtPos, dirtSize, dirtMatrices);
            }
            else
            {
                AddBoxWithFacades(pos, size, dirtMatrices);
            }

            if (groundTopMat != null)
            {
                Vector3 grassPos = pos;
                grassPos.y = (pos.y + (size.y / 2f)) - (grassHeight / 2f);
                Vector3 grassSize = new Vector3(size.x * 1.01f, grassHeight, size.z * 1.01f);
                AddBoxWithFacades(grassPos, grassSize, grassMatrices);
            }
        }

        if (dirtMatrices.Count > 0) Graphics.DrawMeshInstanced(cubeMesh, 0, groundMat, dirtMatrices.ToArray());
        if (grassMatrices.Count > 0 && groundTopMat != null) Graphics.DrawMeshInstanced(cubeMesh, 0, groundTopMat, grassMatrices.ToArray());
    }

    private void DrawListWithFacade(List<int> ids, Material mat)
    {
        if (mat == null || ids.Count == 0) return;
        List<Matrix4x4> matrices = new List<Matrix4x4>();

        Vector3 camPos = Camera.main.transform.position;
        Vector3 camFwd = Camera.main.transform.forward;

        foreach (int id in ids)
        {
            if (!CollisionManager.Instance.HasCollider(id)) continue;
            Matrix4x4 m = CollisionManager.Instance.GetMatrix(id);
            Vector3 pos = m.GetPosition();
            Vector3 size = CollisionManager.Instance.GetSize(id);

            float dist = Vector3.Distance(pos, camPos);
            if (dist > visibilityDistance) continue;

            Vector3 dir = (pos - camPos).normalized;
            if (Vector3.Dot(dir, camFwd) > visibilityDotThreshold || dist < 15f)
            {
                AddBoxWithFacades(pos, size, matrices);
            }
        }
        if (matrices.Count > 0) Graphics.DrawMeshInstanced(cubeMesh, 0, mat, matrices.ToArray());
    }

    private void AddBoxWithFacades(Vector3 pos, Vector3 size, List<Matrix4x4> list)
    {
        list.Add(Matrix4x4.TRS(pos, Quaternion.identity, size));

        float thick = 0.1f;
        float overlap = 1.02f;

        float offZ = (size.z / 2f) + (thick / 2f) - 0.01f;
        float offX = (size.x / 2f) + (thick / 2f) - 0.01f;
        float offY = (size.y / 2f) + (thick / 2f) - 0.01f;

        list.Add(Matrix4x4.TRS(pos - Vector3.forward * offZ, Quaternion.identity,
            new Vector3(size.x * overlap, size.y * overlap, thick)));

        list.Add(Matrix4x4.TRS(pos + Vector3.forward * offZ, Quaternion.identity,
            new Vector3(size.x * overlap, size.y * overlap, thick)));

        list.Add(Matrix4x4.TRS(pos - Vector3.right * offX, Quaternion.identity,
            new Vector3(thick, size.y * overlap, size.z * overlap)));

        list.Add(Matrix4x4.TRS(pos + Vector3.right * offX, Quaternion.identity,
            new Vector3(thick, size.y * overlap, size.z * overlap)));

        list.Add(Matrix4x4.TRS(pos + Vector3.up * offY, Quaternion.identity,
            new Vector3(size.x * overlap, thick, size.z * overlap)));
    }

    private void GenerateMesh()
    {
        cubeMesh = new Mesh();
        Vector3[] v = new Vector3[] {
            new Vector3(-0.5f,-0.5f,0.5f), new Vector3(0.5f,-0.5f,0.5f), new Vector3(0.5f,0.5f,0.5f), new Vector3(-0.5f,0.5f,0.5f),
            new Vector3(0.5f,-0.5f,-0.5f), new Vector3(-0.5f,-0.5f,-0.5f), new Vector3(-0.5f,0.5f,-0.5f), new Vector3(0.5f,0.5f,-0.5f),
            new Vector3(-0.5f,0.5f,0.5f), new Vector3(0.5f,0.5f,0.5f), new Vector3(0.5f,0.5f,-0.5f), new Vector3(-0.5f,0.5f,-0.5f),
            new Vector3(-0.5f,-0.5f,-0.5f), new Vector3(0.5f,-0.5f,-0.5f), new Vector3(0.5f,0.5f,0.5f), new Vector3(-0.5f,-0.5f,0.5f),
            new Vector3(-0.5f,-0.5f,-0.5f), new Vector3(-0.5f,-0.5f,0.5f), new Vector3(-0.5f,0.5f,0.5f), new Vector3(-0.5f,0.5f,-0.5f),
            new Vector3(0.5f,-0.5f,0.5f), new Vector3(0.5f,-0.5f,-0.5f), new Vector3(0.5f,0.5f,-0.5f), new Vector3(0.5f,0.5f,0.5f)
        };
        int[] t = new int[] { 0, 2, 1, 0, 3, 2, 4, 6, 5, 4, 7, 6, 8, 10, 9, 8, 11, 10, 12, 14, 13, 12, 15, 14, 16, 18, 17, 16, 19, 18, 20, 22, 21, 20, 23, 22 };
        cubeMesh.vertices = v;
        cubeMesh.triangles = t;
        cubeMesh.RecalculateNormals();
    }

    private void UpdateUI()
    {
        if (uiText)
        {
            string buffs = "";
            if (!gameOver)
            {
                if (superInvincibleTimer > 0) buffs += $" | STAR ({superInvincibleTimer:F1})";
                if (unlimitedAmmoTimer > 0) buffs += $" | AMMO++ ({unlimitedAmmoTimer:F1})";
            }
            uiText.text = $"HP: {hp} | Ammo: {ammo} | Score: {score} | Time: {timeElapsed:F1}s" + buffs;
        }
    }
}