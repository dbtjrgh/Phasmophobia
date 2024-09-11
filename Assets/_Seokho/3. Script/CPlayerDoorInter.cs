using GameFeatures;
using System.Collections;
using UnityEngine;

public class CPlayerDoorInter : MonoBehaviour
{
    public bool IsDoorFullyOpened = false;  // 문이 완전히 열렸는지 여부
    public bool IsDoorClosed = false;       // 문이 완전히 닫혔는지 여부

    public Transform InteractionTransform;  // 상호작용 위치
    public GameObject handprintPrefab;      // 손발자국 프리팹
    public float detectionRadius = 3.0f;    // 플레이어 감지 범위
    private bool handprintCreated = false;  // 손자국이 한 번만 생성되도록 하는 플래그
    private Collider[] colliders;           // 감지된 충돌체들

    public float forceAmmount = 15f;        // 힘의 양
    public float distance = 1.5f;           // 거리
    public float lockHoldTime = 1.0f;       // 잠금/해제를 위한 T키 홀드 시간

    private Collider col;
    private Rigidbody rb;
    Ghost ghost;

    private Camera cam;
    private bool isInterracting = false;    // 문과 상호작용 중인지 여부
    private bool isDoorLocked = false;      // 문이 잠겨 있는지 여부
    private HingeJoint hinge;               // 문에 부착된 HingeJoint
    private float openedRotation;           // 문이 완전히 열린 상태의 회전
    private const float epsilon = 1f;       // 회전 각도를 비교하기 위한 오차 범위
    private WaitForSeconds waitForDoorStateCheck;  // 문 상태 확인을 위한 대기 시간
    private const float checkDoorStateCD = 0.3f;   // 문 상태 확인 주기

    private float tKeyHoldTime = 0f;        // T 키 누른 시간


    private void Awake()
    {
        cam = Camera.main;
        hinge = GetComponent<HingeJoint>();
        col = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        ghost = FindAnyObjectByType<Ghost>();

        if (hinge == null)
        {
            Debug.Log("HingeJoint가 없으므로 상호작용을 비활성화합니다.");
            enabled = false;
            return;
        }

        waitForDoorStateCheck = new WaitForSeconds(checkDoorStateCD);
        openedRotation = hinge.limits.max * hinge.axis.y;
        if (openedRotation < -1f) openedRotation += 360f;

        StartCoroutine(CheckDoorState());
    }


    private void Update()
    {
        // 플레이어가 주변에 있는지 감지
        bool isPlayerNearby = DetectPlayerNearby();

        // 플레이어가 있을 때만 문 조작 가능
        if (isPlayerNearby)
        {
            // 왼쪽 마우스 버튼으로 상호작용 시작/종료
            if (Input.GetMouseButtonDown(0) && !isInterracting && !isDoorLocked)  // 왼쪽 클릭으로 시작
            {
                OnDragBegin();
            }
            if (Input.GetMouseButtonUp(0) && isInterracting)  // 왼쪽 클릭을 놓으면 상호작용 종료
            {
                OnDragEnd();
            }

            // T 키를 누르고 있는 시간 측정 (잠금/해제)
            if (Input.GetKey(KeyCode.T))
            {
                tKeyHoldTime += Time.deltaTime;

                if (tKeyHoldTime >= lockHoldTime)  // T키를 일정 시간 이상 눌렀을 때
                {
                    if (isDoorLocked)
                    {
                        UnlockTheDoor();
                        Debug.Log("Door Unlocked");
                    }
                    else
                    {
                        LockTheDoor();
                        Debug.Log("Door Locked");
                    }

                    tKeyHoldTime = 0f;  // 시간 초기화
                }
            }

            // T키에서 손을 뗐을 때, 타이머 초기화
            if (Input.GetKeyUp(KeyCode.T))
            {
                tKeyHoldTime = 0f;
            }
        }
        else if (isInterracting) // 플레이어가 멀어졌을 때 상호작용 종료
        {
            OnDragEnd(); // 상호작용 강제 종료
        }
        // 한 번만 손자국 생성이 되도록 설정
       
    }

    private void FixedUpdate()
    { 
        if (!handprintCreated)
        {
            DetectPlayerNearby();
        }
        if (isInterracting && !isDoorLocked)
        {
            // 상호작용 중에도 플레이어가 계속 근처에 있는지 확인
            if (DetectPlayerNearby())
            {
                DragDoor();  // 문을 드래그
            }
            else
            {
                OnDragEnd(); // 플레이어가 멀어지면 상호작용 종료
            }
        }
    }

    private IEnumerator CheckDoorState()
    {
        while (true)
        {
            CheckDoorRotation();
            yield return waitForDoorStateCheck;
        }
    }

    private void CheckDoorRotation()
    {
        float doorCurrRotation = transform.localEulerAngles.y;
        if (doorCurrRotation < -5f) doorCurrRotation += 360;

        if (Mathf.Abs(doorCurrRotation) <= epsilon)
        {
            IsDoorClosed = true;
            IsDoorFullyOpened = false;
        }
        else if (Mathf.Abs(doorCurrRotation - openedRotation) <= epsilon)
        {
            IsDoorClosed = false;
            IsDoorFullyOpened = true;
        }
        else
        {
            IsDoorClosed = false;
            IsDoorFullyOpened = false;
        }
    }

    private void DragDoor()
    {
        Ray playerAim = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 nextPos = cam.transform.position + playerAim.direction * distance;
        Vector3 currPos = transform.position;

        rb.velocity = (nextPos - currPos) * forceAmmount;
    }

    public void OnDragBegin()
    {
        isInterracting = true;
        rb.isKinematic = false;  // 상호작용 중에는 물리 적용
    }

    public void OnDragEnd()
    {
        isInterracting = false;
        rb.isKinematic = true;  // 상호작용 끝나면 물리 적용 해제
    }

    public void LockTheDoor()
    {
        col.enabled = false;
        isDoorLocked = true;
    }

    public void UnlockTheDoor()
    {
        rb.isKinematic = false;
        isDoorLocked = false;
        col.enabled = true;
    }

    public void LeavePrintsUV()
    {
        Instantiate(handprintPrefab, InteractionTransform.position, InteractionTransform.rotation, InteractionTransform);
    }

    // 플레이어가 주변에 있는지 감지하는 함수
    private bool DetectPlayerNearby()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius);

        foreach (Collider collider in colliders)
        {
            // Ghost 객체가 null인지 먼저 확인
            if (ghost == null)
            {
                return false;
            }

            // Player 태그를 가진 오브젝트가 있는지 확인
            if (collider.CompareTag("Player"))
            {
                return true;  // 플레이어가 근처에 있음
            }

            // Room 컴포넌트가 있는지 확인
            Room room = collider.GetComponent<Room>();
            if (!handprintCreated && collider.CompareTag("Room") && room != null)
            {
                if (room.RoomType == myRooms.Rooms.RoomsEnum.GhostRoom && ghost.ghostType != 0)
                {
                    handprintCreated = true;  // 손자국이 생성되었음을 표시
                    Debug.Log("손자국 프리팹 형성");
                    LeavePrintsUV();  // 손자국 프리팹 생성
                }
            }
        }

        return false;  // 플레이어가 근처에 없음
    }



    private void OnDrawGizmosSelected()
    {
        // 감지 범위를 디버깅으로 시각화
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
