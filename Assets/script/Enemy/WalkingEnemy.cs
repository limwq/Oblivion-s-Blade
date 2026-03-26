using UnityEngine;

public class WalkingEnemy : BaseEnemy {
    [Header("Walking Enemy Specifics")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;

    [Header("Edge & Wall Detection")]
    public Transform edgeCheck;
    public Transform wallCheck;
    public float checkDistance = 0.5f;
    public LayerMask groundLayer;

    private int facingDirection = 1;

    protected override void Awake() {
        base.Awake();
        facingDirection = transform.localScale.x > 0 ? 1 : -1;
    }

    protected override void HandlePatrol() {
        // Calculate the absolute world position of the current offset point
        Vector2 worldTarget = startPosition + patrolPoints[currentPointIndex];

        // Determine which way the waypoint is
        float dirToTarget = Mathf.Sign(worldTarget.x - transform.position.x);

        if (dirToTarget != facingDirection && Mathf.Abs(worldTarget.x - transform.position.x) > 0.1f) {
            Flip();
        }

        Move(patrolSpeed);

        // Check if we arrived at the waypoint's X position (with a small 0.2f buffer)
        bool arrivedAtX = Mathf.Abs(transform.position.x - worldTarget.x) < 0.2f;

        // Trigger the wait state if we arrived, OR if we hit a cliff (safety net)
        if (arrivedAtX || HasReachedEdgeOrWall()) {
            StartWaitTimer();
        }
    }

    protected override void HandleChase() {
        if (targetPlayer == null) return;

        float dirToPlayer = Mathf.Sign(targetPlayer.position.x - transform.position.x);

        if (dirToPlayer != facingDirection) {
            Flip();
        }

        if (!HasReachedEdgeOrWall()) {
            Move(chaseSpeed);
        } else {
            rb.velocity = new Vector2(0, rb.velocity.y);
            if (anim != null) anim.SetBool("isRunning", false);
        }
    }

    private void Move(float speed) {
        rb.velocity = new Vector2(facingDirection * speed, rb.velocity.y);
        if (anim != null) anim.SetBool("isRunning", true);
    }

    private bool HasReachedEdgeOrWall() {
        if (edgeCheck == null || wallCheck == null) return false;

        bool isTouchingWall = Physics2D.Raycast(wallCheck.position, Vector2.right * facingDirection, checkDistance, groundLayer);
        bool isNearEdge = !Physics2D.Raycast(edgeCheck.position, Vector2.down, checkDistance, groundLayer);

        return isTouchingWall || isNearEdge;
    }

    private void Flip() {
        facingDirection *= -1;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    protected override void OnDrawGizmosSelected() {
        base.OnDrawGizmosSelected();

        if (edgeCheck != null) {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(edgeCheck.position, edgeCheck.position + Vector3.down * checkDistance);
        }
        if (wallCheck != null) {
            Gizmos.color = Color.blue;
            float dir = transform.localScale.x > 0 ? 1 : -1;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + (Vector3.right * dir * checkDistance));
        }
    }
}