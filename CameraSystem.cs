using UnityEngine;
using Cinemachine;
using UnityEngine.EventSystems;

public class CameraSystem : MonoBehaviour
{
    public static CameraSystem instance;

    public bool CAN_MOVE;

    [SerializeField] PlayerActionsController PlayerActions;
    [SerializeField] DefaultCameraData DATA;
    [SerializeField] CinemachineVirtualCamera cinemachineVirtualCamera;

    Vector3 movementVelocity;
    Vector3 followOffset;
    Vector3 moveDir;

    float shakeTimer;
    float shakeTimerTotal;
    float startingIntensity;
    float heightPercentage;

    Vector3 rotateVelocity;
    Vector3 rotateDir;

    CinemachineBasicMultiChannelPerlin perlin;

    void Awake()
    {
        instance = this;

        perlin = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        followOffset = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset;
        moveDir = new(0, 0, 0);

#if UNITY_EDITOR
        DATA.useScreenEdge = false;
#else
            DATA.useScreenEdge = true;
#endif
    }

    public void Initialize(Vector3 startPos)
    {
        transform.position = startPos;
        movementVelocity = Vector3.zero;
        rotateVelocity = Vector3.zero;
        CAN_MOVE = true;
    }

    void CheckProfiles()
    {
        if (BuildingSystem.instance.IS_BUILDING)
        {
            return;
        }

        if (followOffset.y < DATA.heightBounds.x + 5f)
        {
            if (GameManager.instance.IS_PROFILES_ACTIVE)
            {
                GameManager.instance.HideProfiles();
                return;
            }
        }

        if (!GameManager.instance.IS_PROFILES_ACTIVE)
            GameManager.instance.ShowProfiles();
    }

    void HandleShake()
    {
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;

            perlin.m_AmplitudeGain = Mathf.Lerp(startingIntensity, 0f, 1 - shakeTimer / shakeTimerTotal);
        }
    }

    void Update()
    {
        CheckProfiles();

        if (!CAN_MOVE)
        {
            if (perlin.m_AmplitudeGain != 0)
                perlin.m_AmplitudeGain = 0;

            return;
        }

        heightPercentage = followOffset.y / DATA.heightBounds.y;

        HandleShake();
        HandleCameraMovementEdgeScrolling();
        HandleCameraMovement();
        HandleCameraRotation();
        HandleCameraZoom();
    }

    void HandleCameraMovement()
    {
        float amount = DATA.moveAmount;

        moveDir += PlayerActions.Movement.y * transform.forward;
        moveDir += PlayerActions.Movement.x * transform.right;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            amount *= 1.5f;
        }

        // Higher zoom-out =>  faster movement, Higher zoom-in => normal movement
        Vector3 targetPos = ((heightPercentage * amount) + (heightPercentage < .3f ? amount / 3 : heightPercentage < .6f ? amount * .6f : amount)) * moveDir;

        if (targetPos.sqrMagnitude > 0.1f)
        {
            transform.position += targetPos * Time.deltaTime;
        }
        else
        {
            movementVelocity = Vector3.Lerp(movementVelocity, Vector3.zero, Time.deltaTime * DATA.damping);
            transform.position += movementVelocity * Time.deltaTime;
        }

        moveDir = Vector3.zero;
    }

    void HandleCameraMovementEdgeScrolling()
    {
        if (!DATA.useScreenEdge || EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.mousePosition.x < DATA.edgeTolerance * Screen.width)
            moveDir -= transform.right;
        if (Input.mousePosition.y < DATA.edgeTolerance * Screen.height)
            moveDir -= transform.forward;
        if (Input.mousePosition.x > (1f - DATA.edgeTolerance) * Screen.width)
            moveDir += transform.right;
        if (Input.mousePosition.y > (1f - DATA.edgeTolerance) * Screen.height)
            moveDir += transform.forward;
    }

    bool IsInBounds(Vector3 position)
    {
        bool isRightValid = position.x + transform.position.x < DATA.rightBound;
        bool isLeftValid = position.x + transform.position.x > DATA.leftBound;
        bool isTopValid = position.z + transform.position.z < DATA.topBound;
        bool isBottomValid = position.z + transform.position.z > DATA.bottomBound;

        return isRightValid && isLeftValid && isTopValid && isBottomValid;
    }

    void HandleCameraRotation()
    {
        float amount = DATA.rotateAmount;

        // Update the rotation direction => Vector3(0,0,1) moves forward
        rotateDir += new Vector3(PlayerActions.Rotate.y, PlayerActions.Rotate.x, 0);

        // If there is rotation direction
        if (rotateDir.magnitude > 0.1f)
        {
            rotateVelocity = amount * Time.deltaTime * rotateDir;
            transform.eulerAngles += rotateVelocity;
        }
        else  // If there is not any rotation direction
        {
            rotateVelocity = Vector3.Lerp(rotateVelocity, Vector3.zero, DATA.damping * Time.deltaTime);
            transform.eulerAngles += rotateVelocity;
        }

        rotateDir = Vector2.zero;
    }

    void HandleCameraZoom()
    {
        float zoomDir = PlayerActions.Zoom;
        float zoomAmount = heightPercentage * DATA.zoomAmount * zoomDir;

        followOffset.y += zoomAmount;

        followOffset.y = Mathf.Clamp(followOffset.y, DATA.heightBounds.x, DATA.heightBounds.y);

        if (BuildingSystem.instance.IS_BUILDING && BuildingSystem.instance.BUILD_TYPE == BuildingType.SolarPanel)
            followOffset = new Vector3(followOffset.x, followOffset.y, -1f);
        else
            followOffset = new Vector3(followOffset.x, followOffset.y, -20);

        cinemachineVirtualCamera.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset =
            Vector3.Lerp(cinemachineVirtualCamera.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset, followOffset, Time.deltaTime * DATA.zoomSpeed);

    }

    public void Shake(float intensity, float time, float frequency = 1f)
    {
        perlin.m_AmplitudeGain = intensity;
        perlin.m_FrequencyGain = frequency;
        startingIntensity = intensity;
        shakeTimerTotal = time;
        shakeTimer = time;
    }
}