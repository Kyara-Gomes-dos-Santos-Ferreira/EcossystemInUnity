using UnityEngine;

public class FreeCameraController : MonoBehaviour
{
    [Header("Velocidade de Movimento")]
    public float normalSpeed = 20f;
    public float fastSpeed = 60f;
    public float acceleration = 5f;

    [Header("Sensibilidade do Mouse")]
    public float mouseSensitivity = 3f;

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        // Pega a rotação inicial da câmera para não dar tranco ao dar play
        Vector3 rot = transform.localEulerAngles;
        rotationY = rot.y;
        rotationX = rot.x;
    }

    void Update()
    {
        HandleRotation();
        HandleMovement();
    }

    void HandleRotation()
    {
        // Quando segura o Botão Direito do Mouse, trava o cursor e permite olhar ao redor
        if (Input.GetMouseButtonDown(1))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Enquanto estiver segurando o Botão Direito, move a câmera
        if (Input.GetMouseButton(1))
        {
            rotationY += Input.GetAxis("Mouse X") * mouseSensitivity;
            rotationX -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);

            transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0f);
        }

        // Quando solta o Botão Direito, o cursor destrava e reaparece
        if (Input.GetMouseButtonUp(1))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void HandleMovement()
    {
        // Define a velocidade direto sem depender de lerp engasgar no pause
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? fastSpeed : normalSpeed;

        // Captura as entradas WASD ou Setas
        float moveX = Input.GetAxisRaw("Horizontal"); // Usando GetAxisRaw para resposta imediata
        float moveZ = Input.GetAxisRaw("Vertical");

        Vector3 moveDir = new(moveX, 0f, moveZ);
        moveDir = transform.TransformDirection(moveDir);

        // Movimento no plano usando Time.unscaledDeltaTime
        transform.position += moveDir * (currentSpeed * Time.unscaledDeltaTime);

        // Subir (E) e Descer (Q)
        if (Input.GetKey(KeyCode.E))
        {
            transform.position += Vector3.up * (currentSpeed * Time.unscaledDeltaTime);
        }
        if (Input.GetKey(KeyCode.Q))
        {
            transform.position += Vector3.down * (currentSpeed * Time.unscaledDeltaTime);
        }
    }
}