using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Animator))]
public class NinjaController2D : MonoBehaviour {
    [Header("Movement")]
    public float moveSpeed = 8f;

    [Header("Jumping & Collision")]
    public float jumpForce = 14f;
    public float fastFallSpeed = -25f;
    public LayerMask groundLayer;
    public float rayLength = 0.2f;
    public float rayInset = 0.05f;

    [Header("Camera Spring Integration")]
    public float minFallSpeedForPunch = -10f;
    public float landingPunchMultiplier = 0.5f;

    [Header("Rolling")]
    public float rollSpeed = 15f;
    public float rollDuration = 0.4f;
    public float rollCooldown = 1f;
    private float rollCooldownTimer;
    private bool isRolling;
    private bool usedAirDash;

    [Header("Ladder Climbing")]
    public float climbSpeed = 5f;
    public LayerMask ladderLayer;
    private float ladderDropTimer;

    [Header("Collision Bumpers")]
    public LayerMask enemyLayer;
    public float bumperDistance = 0.5f;

    [Header("Phase Progression Locks")]
    public bool abilitiesLocked = false;
    public bool infiniteStamina = false;

    [Header("Looping Audio")]
    public AudioSource runAudioSource;
    public AudioSource climbAudioSource;

    // --- PLATFORM & STATE TRACKING ---
    [HideInInspector] public Vector2 platformVelocity;
    [HideInInspector] public bool isOnMovingPlatform;
    [HideInInspector] public bool isDead = false;

    // --- STUN TRACKING ---
    private float stunTimer = 0f;

    private Rigidbody2D rb;
    private Collider2D col;
    private Animator anim;
    private HealthSystem healthSys;

    private float horizontalInput;
    private float verticalInput;
    private float defaultGravity;

    public bool IsGrounded { get; private set; }
    private bool wasGrounded;
    private float lastYVelocity;

    public bool IsClimbing { get; private set; }
    public bool canMove = true;

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        defaultGravity = rb.gravityScale;
        healthSys = GetComponent<HealthSystem>();
    }

    private void Update() {
        if (isDead) return;

        if (rollCooldownTimer > 0) rollCooldownTimer -= Time.deltaTime;
        if (ladderDropTimer > 0) ladderDropTimer -= Time.deltaTime;

        // --- STUN LOCK LOGIC ---
        if (stunTimer > 0) {
            stunTimer -= Time.deltaTime;
            horizontalInput = 0;
            UpdateAnimator();
            return;
        }

        if (!canMove) {
            horizontalInput = 0;
            UpdateAnimator();
            return;
        }

        lastYVelocity = rb.velocity.y;

        CheckCollisions();

        if (IsGrounded) usedAirDash = false;

        HandleInput();
        HandleFlip();
        UpdateAnimator();
        HandleLoopingAudio();
    }

    private void FixedUpdate() {
        if (isDead) return;

        // Stop physics movement during stun!
        if (stunTimer > 0) {
            rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }

        if (!canMove) return;
        if (isRolling) return;

        HandleMovement();
        HandleClimbing();
    }

    public void ApplyStun(float duration = 0.4f) {
        stunTimer = duration;
        isRolling = false;
        IsClimbing = false;
        rb.gravityScale = defaultGravity;
        rb.velocity = new Vector2(0, rb.velocity.y);

        anim.SetBool("isRunning", false);
        anim.SetBool("isClimbing", false);
    }

    private void HandleInput() {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetButtonDown("Jump") && IsClimbing) {
            IsClimbing = false;
            ladderDropTimer = 0.2f;
            rb.velocity = new Vector2(rb.velocity.x, 0);
            return;
        }

        if (Input.GetButtonDown("Jump") && IsGrounded && !IsClimbing && !isRolling) {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            anim.SetTrigger("jump");

            // --- AUDIO TRIGGER ---
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPlayerSFX("PlayerJump");
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && rollCooldownTimer <= 0 && !isRolling && !IsClimbing && !isOnMovingPlatform) {
            if (!IsGrounded && usedAirDash) return;
            if (!IsGrounded) usedAirDash = true;

            StartCoroutine(RollRoutine());
        }
    }

    private void HandleMovement() {
        if (IsClimbing) {
            rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }

        // --- THE BUMPER FIX ---
        float moveDirection = Mathf.Sign(horizontalInput);
        Vector2 chestPosition = new Vector2(col.bounds.center.x, col.bounds.center.y);
        RaycastHit2D bumperHit = Physics2D.Raycast(chestPosition, Vector2.right * moveDirection, bumperDistance, enemyLayer);

        if (bumperHit.collider != null && horizontalInput != 0) {
            rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }

        // --- NEW: FAST FALL LOGIC ---
        float targetYVelocity = rb.velocity.y;

        if (!IsGrounded && verticalInput < -0.5f) {
            targetYVelocity = fastFallSpeed;
        }

        rb.velocity = new Vector2((horizontalInput * moveSpeed) + platformVelocity.x, targetYVelocity);
    }

    private void HandleClimbing() {
        bool wasClimbing = IsClimbing;

        Vector2 boxCenter = col.bounds.center;
        // FIX: Narrowed the box to 0.5f so you don't magically grab ladders beside you!
        Vector2 boxSize = new Vector2(col.bounds.size.x * 0.5f, col.bounds.size.y);
        Collider2D ladderHit = Physics2D.OverlapBox(boxCenter, boxSize, 0f, ladderLayer);

        bool pressingUp = verticalInput > 0.1f;
        bool pressingDown = verticalInput < -0.1f;

        if (!IsClimbing && ladderHit != null && (pressingUp || (pressingDown && IsGrounded)) && ladderDropTimer <= 0) {
            IsClimbing = true;
            rb.velocity = Vector2.zero;
            SnapToLadder(ladderHit);

            if (IsGrounded && pressingDown) {
                transform.position = new Vector2(transform.position.x, transform.position.y - 0.2f);
            }
        } else if (IsClimbing) {
            if (ladderHit == null) {
                IsClimbing = false;
            } else if (IsGrounded && pressingDown && ladderDropTimer <= 0) {
                IsClimbing = false;
                ladderDropTimer = 0.2f;
            }
        }

        if (wasClimbing && !IsClimbing && pressingUp && ladderDropTimer <= 0 && !IsGrounded) {
            float snapDir = horizontalInput != 0 ? Mathf.Sign(horizontalInput) : 1f;
            rb.velocity = new Vector2(snapDir * moveSpeed, jumpForce * 0.6f);
        } else if (wasClimbing && !IsClimbing) {
            rb.velocity = new Vector2(rb.velocity.x, 0);
        }

        if (IsClimbing) {
            rb.gravityScale = 0f;
            rb.velocity = new Vector2(0, verticalInput * climbSpeed);
        } else {
            rb.gravityScale = defaultGravity;
        }
    }

    private void SnapToLadder(Collider2D ladderCollider) {
        // Look for Tilemap on the hit object or its parent (in case of CompositeColliders)
        Tilemap tilemap = ladderCollider.GetComponent<Tilemap>();
        if (tilemap == null) tilemap = ladderCollider.GetComponentInParent<Tilemap>();

        if (tilemap != null) {
            // FIX: Push checkPos slightly up (+0.2f) to avoid the floor boundary causing it to read the wrong tile
            Vector3 checkPos = new Vector3(col.bounds.center.x, col.bounds.min.y + 0.2f, 0f);
            Vector3Int cellPosition = tilemap.WorldToCell(checkPos);

            if (!tilemap.HasTile(cellPosition)) {
                if (tilemap.HasTile(cellPosition + Vector3Int.right))
                    cellPosition += Vector3Int.right;
                else if (tilemap.HasTile(cellPosition + Vector3Int.left))
                    cellPosition += Vector3Int.left;
                else if (tilemap.HasTile(cellPosition + Vector3Int.up)) // Extra safety check up
                    cellPosition += Vector3Int.up;
            }

            Vector3 cellCenterPos = tilemap.GetCellCenterWorld(cellPosition);
            transform.position = new Vector2(cellCenterPos.x, transform.position.y);
        } else {
            transform.position = new Vector2(ladderCollider.bounds.center.x, transform.position.y);
        }
    }

    private System.Collections.IEnumerator RollRoutine() {
        isRolling = true;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPlayerSFX("PlayerRoll");

        if (healthSys != null) {
            StartCoroutine(healthSys.TriggerInvincibility(rollDuration));
        }

        rollCooldownTimer = rollCooldown;
        anim.SetTrigger("roll");


        float facingDir = transform.localScale.x > 0 ? 1f : -1f;
        if (CameraSpring.Instance != null) {
            CameraSpring.Instance.Punch(Vector2.right * facingDir, 3f);
        }

        float targetAngle = facingDir > 0 ? -360f : 360f;
        rb.velocity = new Vector2(facingDir * rollSpeed, 0f);
        rb.gravityScale = 0f;

        float timer = 0f;
        while (timer < rollDuration) {
            timer += Time.deltaTime;
            float currentZRotation = Mathf.Lerp(0f, targetAngle, timer / rollDuration);
            transform.rotation = Quaternion.Euler(0f, 0f, currentZRotation);
            yield return null;
        }

        transform.rotation = Quaternion.identity;
        rb.gravityScale = defaultGravity;
        rb.velocity = Vector2.zero;
        isRolling = false;
    }

    private void CheckCollisions() {
        Bounds bounds = col.bounds;
        float xInset = rayInset;

        bool groundedThisFrame = false;
        Vector2 feetLeft = new Vector2(bounds.min.x + xInset, bounds.min.y);
        Vector2 feetRight = new Vector2(bounds.max.x - xInset, bounds.min.y);

        for (int i = 0; i < 4; i++) {
            float t = i / 3f;
            Vector2 origin = Vector2.Lerp(feetLeft, feetRight, t);

            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, rayLength, groundLayer);
            if (hit.collider != null) {
                groundedThisFrame = true;
                break;
            }
        }

        if (groundedThisFrame && !wasGrounded) {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPlayerSFX("PlayerLand");

            if (lastYVelocity < minFallSpeedForPunch) {
                if (CameraSpring.Instance != null) {
                    float impactForce = Mathf.Abs(lastYVelocity) * landingPunchMultiplier;
                    CameraSpring.Instance.Punch(Vector2.down, impactForce);
                }
            }
        }

        wasGrounded = groundedThisFrame;
        IsGrounded = groundedThisFrame;
    }

    private void HandleFlip() {
        if (IsClimbing) return;

        if (horizontalInput > 0 && transform.localScale.x < 0 ||
            horizontalInput < 0 && transform.localScale.x > 0) {
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    private void UpdateAnimator() {
        anim.SetBool("isGrounded", IsGrounded);
        anim.SetBool("isRunning", Mathf.Abs(horizontalInput) > 0.1f && !isRolling && !IsClimbing);
        anim.SetBool("isClimbing", IsClimbing);

        if (IsClimbing) {
            anim.speed = Mathf.Abs(verticalInput) > 0.1f ? 1f : 0f;
        } else {
            anim.speed = 1f;
        }
    }

    private void HandleLoopingAudio() {
        bool isRunning = Mathf.Abs(horizontalInput) > 0.1f && IsGrounded && !isRolling && canMove;

        if (isRunning && !runAudioSource.isPlaying) {
            runAudioSource.Play();
        } else if (!isRunning && runAudioSource.isPlaying) {
            runAudioSource.Stop();
        }

        bool isMovingOnLadder = IsClimbing && Mathf.Abs(verticalInput) > 0.1f;

        if (isMovingOnLadder && !climbAudioSource.isPlaying) {
            climbAudioSource.Play();
        } else if (!isMovingOnLadder && climbAudioSource.isPlaying) {
            climbAudioSource.Stop();
        }
    }

    private void OnDrawGizmosSelected() {
        if (col == null) col = GetComponent<Collider2D>();
        if (col == null) return;

        Bounds bounds = col.bounds;

        // --- GROUND CHECK GIZMOS ---
        Gizmos.color = Color.green;
        Vector2 feetLeft = new Vector2(bounds.min.x + rayInset, bounds.min.y);
        Vector2 feetRight = new Vector2(bounds.max.x - rayInset, bounds.min.y);
        for (int i = 0; i < 4; i++) {
            Vector2 origin = Vector2.Lerp(feetLeft, feetRight, i / 3f);
            Gizmos.DrawLine(origin, origin + Vector2.down * rayLength);
        }

        // --- LADDER CLIMB GIZMOS ---
        Gizmos.color = Color.cyan;
        Vector2 boxCenter = col.bounds.center;
        Vector2 boxSize = new Vector2(col.bounds.size.x * 0.5f, col.bounds.size.y);
        Gizmos.DrawWireCube(boxCenter, boxSize);
    }
}