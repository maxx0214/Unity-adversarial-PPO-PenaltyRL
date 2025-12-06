// 202110844 문경수
// Behavior K

using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;


public class KickerAgent : Agent
{
    // 거리 기반 보상 상수 (1차 피드백 후 추가)
    // EnvController가 아닌 KickerAgent가 직접 공을 참조해야 거리 계산이 편합니다.
    public GameObject Ball;

    // 최대 거리 보상 강도 설정 (1차 피드백 후 추가)
    // (2차 피드백 후 수치 변경 0.0005 -> 0.001)
    // (6차 피드백 후 수치 변경 0.001 -> 0.1)
    // (7차 피드백 후 수치 변경 0.1 -> 1)
    private const float MAX_DIST_REWARD = -0.01f;

    // Kicker가 공에 대해 가질 수 있는 최대 거리 (1차 피드백 후 추가)
    private const float MAX_DISTANCE = 25f;

    // 이전 PongAgent와 동일한 변수들을 유지
    public GameObject enemy;
    public GameObject ball;

    private Rigidbody RbAgent;
    private Rigidbody RbBall;

    // Z-Movement Actions (Branch 0)
    private const int Z_UP = 1;
    private const int Z_DOWN = 2;

    // X-Movement Actions (Branch 1) - Agent_K를 위해 추가
    private const int X_LEFT = 1; // X- (왼쪽)
    private const int X_RIGHT = 2; // X+ (오른쪽)

    private Vector3 ResetPosAgent;

    // 이동 속도 상수를 설정합니다.
    private const float MoveSpeed = 15f;
    // 패들의 이동 제한 범위
    private const float Z_Clamp = 3.7f; // Z축 제한 (기존과 동일)
    private const float X_Clamp = 10.0f; // 3차 피드백 후 X축 제한 변경 (가운데 공까지만)

    public override void Initialize()
    {
        ResetPosAgent = transform.position;
        RbAgent = GetComponent<Rigidbody>();
        RbBall = ball.GetComponent<Rigidbody>();
        Academy.Instance.AgentPreStep += WaitTimeInference;
    }

    // 관찰 영역 : 기존 6 -> 7개로 변경 : agent_k 자신의  X(가로) 위치 추가
    public override void CollectObservations(VectorSensor sensor)
    {
        // 1. 에이전트 자신의 Z 위치
        sensor.AddObservation(transform.localPosition.z);
        // 2. 에이전트 자신의 X 위치 (추가)
        sensor.AddObservation(transform.localPosition.x);
        // 3. 상대방의 Z 위치
        sensor.AddObservation(enemy.transform.localPosition.z);
        // 4. 공의 X 위치
        sensor.AddObservation(ball.transform.localPosition.x);
        // 5. 공의 Z 위치
        sensor.AddObservation(ball.transform.localPosition.z);
        // 6. 공의 X 속도
        sensor.AddObservation(RbBall.linearVelocity.x);
        // 7. 공의 Z 속도
        sensor.AddObservation(RbBall.linearVelocity.z);

        // 총 7개의 관찰 값
    }

    // 행동 3차 피드백 후 이산 -> 연속 변경
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var vectorAction = actionBuffers.DiscreteActions;

        float zAction = actionBuffers.ContinuousActions[0]; // Z축 움직임 (-1.0 ~ 1.0)
        float xAction = actionBuffers.ContinuousActions[1]; // X축 움직임 (-1.0 ~ 1.0)
        RbAgent.linearVelocity = new Vector3(xAction * MoveSpeed, 0f, zAction * MoveSpeed);

        // 1. Z축 움직임 세로방향 (UP/DOWN)
        switch (zAction)
        {
            case Z_UP:
                transform.Translate(Vector3.forward * Time.fixedDeltaTime * MoveSpeed);
                break;
            case Z_DOWN:
                transform.Translate(Vector3.back * Time.fixedDeltaTime * MoveSpeed);
                break;
        }

        // 2. X축 움직임 가로방향 (LEFT/RIGHT) - 행동 추가
        switch (xAction)
        {
            case X_RIGHT:
                transform.Translate(Vector3.right * Time.fixedDeltaTime * MoveSpeed);
                break;
            case X_LEFT:
                transform.Translate(Vector3.left * Time.fixedDeltaTime * MoveSpeed);
                break;
        }

        // 3. 경계 제한) 3차 피드백 후 kicker의 x축 추가 제한 (공까지만 이동 가능) (X_Clamp : 12.5 -> 10.0) 
        // 5차 피드백 후 X축 제한 범위 수정 (0f -> 0.9f)
        transform.localPosition = new Vector3(
            Mathf.Clamp(transform.localPosition.x, 0.9f, X_Clamp), // X축 제한
            transform.localPosition.y,
            Mathf.Clamp(transform.localPosition.z, -Z_Clamp, Z_Clamp)  // Z축 제한
        );

        // 4. 거리 기반 보상 부여 (공에 가까워질수록 보상 증가) -> 7차 피드백 후 고민
        // 4-1. Kicker와 공 사이의 거리 계산
        float distanceToBall = Vector3.Distance(transform.localPosition, Ball.transform.localPosition);

        // 4-2. 거리를 정규화 (0 ~ 1 사이 값)
        float normalizedDistance = Mathf.Clamp(distanceToBall / MAX_DISTANCE, 0.01f, 1f);

        // 4-3. 거리가 가까울수록 보상을 크게 부여 (1.0 - normalizedDistance)
        // 거리가 0이면 보상 Max (MAX_DIST_REWARD), 거리가 Max이면 보상 0에 가까워짐
        // 거리와 보상은 반비례 관계
        float approachReward = MAX_DIST_REWARD * (1f - normalizedDistance);

        // 보상 부여
        AddReward(approachReward);
    }

    // --- 휴리스틱 함수 (Heuristic) ---
    // KickerAgent.cs 내부 Heuristic 함수

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // 연속 행동 설정
        var continuousActionsOut = actionsOut.ContinuousActions;

        // Z-Movement (Index 0): Up/Down Arrow 키
        if (Input.GetKey(KeyCode.UpArrow))
        {
            continuousActionsOut[0] = 1.0f; // 앞으로 이동 (최대 속도)
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            continuousActionsOut[0] = -1.0f; // 뒤로 이동 (최대 속도)
        }
        else
        {
            continuousActionsOut[0] = 0.0f; // 정지
        }

        // X-Movement (Index 1): Left/Right Arrow 키
        if (Input.GetKey(KeyCode.RightArrow))
        {
            continuousActionsOut[1] = 1.0f; // 오른쪽으로 이동 (최대 속도)
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            continuousActionsOut[1] = -1.0f; // 왼쪽으로 이동 (최대 속도)
        }
        else
        {
            continuousActionsOut[1] = 0.0f; // 정지
        }
    }

    // --- 에피소드 초기화 및 추론 대기 로직 ---
    public override void OnEpisodeBegin()
    {
        transform.localPosition = ResetPosAgent;
        RbAgent.linearVelocity = Vector3.zero;
        RbAgent.angularVelocity = Vector3.zero;
    }

    float DecisionWaitingTime = 0.02f;
    float m_currentTime = 0f;

    public void WaitTimeInference(int action)
    {
        if (Academy.Instance.IsCommunicatorOn)
        {
            RequestDecision();
        }
        else
        {
            if (m_currentTime >= DecisionWaitingTime)
            {
                m_currentTime = 0f;
                RequestDecision();
            }
            else
            {
                m_currentTime += Time.fixedDeltaTime;
            }
        }
    }
}