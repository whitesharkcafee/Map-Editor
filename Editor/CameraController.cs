using UnityEngine;

namespace MapEditor.Editor
{
    public class CameraController: MonoBehaviour
    {
        private float movementSpeed = 10f;
        public float shiftMultiplier = 2.5f;

        public float mouseSensitivity = 2f;
        public float maxPitchAngle = 90f;

        private float _yaw;
        private float _pitch;
        private bool _isLooking;

        void Start()
        {
            Vector3 angles = transform.eulerAngles;
            _yaw = angles.y;
            _pitch = angles.x;
        }

        void Update()
        {
            HandleMouse();
            if(_isLooking)
            {
                HandleRotation();
                HandleMovement();
            }
        }

        private void HandleMouse()
        {
            if(Input.GetMouseButtonDown(1))
            {
                _isLooking = true;
                MenuController.ShowCursor(false);
            }
            if(Input.GetMouseButtonUp(1))
            {
                _isLooking = false;
                MenuController.ShowCursor(true);
            }
        }

        private void HandleRotation()
        {
            float mouseX = InControlSingleton.Instance.playerActions.MouseOnly.X * float.Parse(OptionsController.Instance.sensitivityXLabel.text);
            float mouseY = InControlSingleton.Instance.playerActions.MouseOnly.Y * float.Parse(OptionsController.Instance.sensitivityYLabel.text);

            _yaw += mouseX;
            _pitch -= mouseY;

            _pitch = Mathf.Clamp(_pitch, -maxPitchAngle, maxPitchAngle);

            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0);
        }

        private void HandleMovement()
        {
            float currentSpeed = movementSpeed;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                currentSpeed *= shiftMultiplier;
            }

            float moveForward = Input.GetAxis("Vertical");  // W/S
            float moveSideways = Input.GetAxis("Horizontal"); // A/D

            float moveUpDown = 0f;
            if (Input.GetKey(KeyCode.E)) moveUpDown = 1f;
            if (Input.GetKey(KeyCode.Q)) moveUpDown = -1f;

            Vector3 moveDirection =
                (transform.forward * moveForward) +
                (transform.right * moveSideways) +
                (transform.up * moveUpDown);

            transform.position += moveDirection * currentSpeed * Time.deltaTime;
        }
    }
}
