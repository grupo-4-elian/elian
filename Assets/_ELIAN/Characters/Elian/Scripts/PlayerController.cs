using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 12f;

    [Header("Deteccion de suelo")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Disparo")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform firePointUp;
    [SerializeField] private Transform firePointCrouch;

    [Header("Arrastrarse (tuneles de techo bajo)")]
    [Tooltip("Velocidad al avanzar agachado dentro de un tunel (CrawlZone).")]
    [SerializeField] private float crawlSpeed = 3f;
    [Tooltip("Alto del collider mientras se arrastra (de pie mide ~3.2).")]
    [SerializeField] private float crawlColliderHeight = 2f;

    private Rigidbody2D rb;
    private Animator animator;
    private PlayerControls controls;
    private BoxCollider2D bodyCollider;
    private Vector2 standingColliderSize;
    private Vector2 standingColliderOffset;

    // Cantidad de CrawlZone en las que esta el jugador (pueden solaparse).
    private int crawlZones;

    // Dentro de un tunel Elian queda agachado: solo avanza o retrocede,
    // puede disparar, pero no puede saltar ni ponerse de pie.
    public bool IsCrawling => crawlZones > 0;

    private Vector2 moveInput = Vector2.zero;

    private bool isGrounded = false;
    private bool isCrouching = false;
    private bool isAimingUp = false;
    private bool facingRight = true;

    private bool jumpRequested = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        controls = new PlayerControls();

        bodyCollider = GetComponent<BoxCollider2D>();
        if (bodyCollider != null)
        {
            standingColliderSize = bodyCollider.size;
            standingColliderOffset = bodyCollider.offset;
        }
    }

    public void EnterCrawlZone()
    {
        crawlZones++;
        UpdateCrawlCollider();
    }

    public void ExitCrawlZone()
    {
        crawlZones = Mathf.Max(0, crawlZones - 1);
        UpdateCrawlCollider();
    }

    // Achica el collider al arrastrarse para pasar bajo el techo del tunel,
    // manteniendo los pies en el mismo lugar.
    private void UpdateCrawlCollider()
    {
        if (bodyCollider == null)
            return;

        if (IsCrawling)
        {
            float height = Mathf.Min(crawlColliderHeight, standingColliderSize.y);
            bodyCollider.size = new Vector2(standingColliderSize.x, height);
            bodyCollider.offset = new Vector2(
                standingColliderOffset.x,
                standingColliderOffset.y - (standingColliderSize.y - height) * 0.5f);
        }
        else
        {
            bodyCollider.size = standingColliderSize;
            bodyCollider.offset = standingColliderOffset;
        }
    }

    private void OnEnable()
    {
        EnsureControls();
        controls.Player.Enable();
    }

    private void OnDisable()
    {
        controls?.Player.Disable();
    }

    // Si Unity recompila scripts durante Play Mode, recarga el codigo sin
    // volver a llamar a Awake y "controls" queda en null (NullReference en
    // cada frame). Lo recreamos en ese caso.
    private void EnsureControls()
    {
        if (controls != null)
            return;

        controls = new PlayerControls();
        controls.Player.Enable();
    }

    private void Update()
    {
        EnsureControls();

        // Con el menu de pausa abierto, sus teclas (Espacio, Enter...) no
        // deben hacer saltar ni disparar a Elian.
        if (PauseMenu.IsPaused)
            return;

        // El input se lee cada frame para que responda inmediatamente.
        moveInput = controls.Player.Move.ReadValue<Vector2>();

        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );

        // En un tunel se va siempre agachado y no se puede apuntar arriba.
        isCrouching = IsCrawling || (moveInput.y < 0f && isGrounded);
        isAimingUp = !IsCrawling && moveInput.y > 0f;

        float horizontal = GetHorizontalInput();

        // Arrastrandose se mantiene la pose agachada (Speed 0) aunque avance.
        animator.SetFloat("Speed", IsCrawling ? 0f : Mathf.Abs(horizontal));
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsCrouching", isCrouching);
        animator.SetBool("AimUp", isAimingUp);

        if (horizontal > 0.01f && !facingRight)
            Flip();
        else if (horizontal < -0.01f && facingRight)
            Flip();

        // Registramos el salto inmediatamente.
        if (controls.Player.Jump.WasPressedThisFrame() && isGrounded && !IsCrawling)
        {
            jumpRequested = true;
            Sfx.Play(Sfx.Salto);
        }

        // El disparo no necesita esperar al ciclo de fisica.
        if (controls.Player.Fire.WasPressedThisFrame())
        {
            Shoot();
        }
    }

    private void FixedUpdate()
    {
        if (jumpRequested)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                jumpForce
            );

            jumpRequested = false;
        }

        float speed = IsCrawling ? crawlSpeed : moveSpeed;

        rb.linearVelocity = new Vector2(
            GetHorizontalInput() * speed,
            rb.linearVelocity.y
        );
    }

    // Agachado normal: quieto. Arrastrandose en un tunel: avanza y retrocede.
    private float GetHorizontalInput()
    {
        if (IsCrawling)
            return moveInput.x;

        return isCrouching ? 0f : moveInput.x;
    }

    private void Shoot()
    {
        animator.SetTrigger("Attack");

        Transform spawn = firePoint;

        if (isAimingUp)
            spawn = firePointUp;
        else if (isCrouching)
            spawn = firePointCrouch;

        Instantiate(
            bulletPrefab,
            spawn.position,
            spawn.rotation
        );

        Sfx.Play(Sfx.Disparo);
    }

    private void Flip()
    {
        facingRight = !facingRight;
        transform.Rotate(0f, 180f, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(
            groundCheck.position,
            groundCheckRadius
        );
    }
}
