// 202110844 문경수 (코드 구현)
// Behavior G

using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class PongAgent : Agent
{
    public GameObject enemy;
    public GameObject ball;

    private Rigidbody RbAgent;
    private Rigidbody RbBall;

    private const int Stay = 0;
    private const int UP = 1;
    private const int DOWN = 2;

    private Vector3 ResetPosAgent;

    public override void Initialize()
    {
        ResetPosAgent = transform.position;
        RbAgent = GetComponent<Rigidbody>();
        RbBall = ball.GetComponent<Rigidbody>();
        Academy.Instance.AgentPreStep += WaitTimeInference;
    }

    // 관측 6개 -> 3차 시도 후 관측 6 -> 7개로 변경
    public override void CollectObservations(VectorSensor sensor)
    {
        // 1. Goalkeeper 자신의 위치 - 1개
        sensor.AddObservation(transform.localPosition.z);

        // 2. Kicker의 위치 (X, Z) - 2개 (상대 x축 움직임 관측 추가)
        sensor.AddObservation(enemy.transform.localPosition.x);
        sensor.AddObservation(enemy.transform.localPosition.z);

        // 3. 공의 위치 (X, Z) - 2개
        sensor.AddObservation(ball.transform.localPosition.x);
        sensor.AddObservation(ball.transform.localPosition.z);

        // 4. 공의 속도 (X, Z) - 2개
        sensor.AddObservation(RbBall.linearVelocity.x);
        sensor.AddObservation(RbBall.linearVelocity.z);
    }

    // 행동, 보상, 게임 종료 조건 설정
    // 이산 -> 연속
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Goalkeeper는 Z축만 제어하므로 Index 0만 사용
        float z_input = actionBuffers.ContinuousActions[0];

        // 움직임 속도를 정의합니다.
        const float MoveSpeed = 30f;

        // Translate를 사용하여 속도 적용 (X축 움직임 없음)
        // z_input 값에 따라 -10f ~ 10f 사이의 속도로 움직입니다.
        // .normalized는 이미 -1.0f ~ 1.0f 값이므로 필요하지 않습니다.
        Vector3 moveDirection = new Vector3(0f, 0f, z_input);

        transform.Translate(moveDirection * MoveSpeed * Time.fixedDeltaTime);

        // Z축 위치 제한 (Clamp) 로직 유지
        transform.localPosition = new Vector3(
            transform.localPosition.x,
            transform.localPosition.y,
            Mathf.Clamp(transform.localPosition.z, -3.7f, 3.7f)
        );
    }

    // 에피소드 시작 시 초기화
    public override void OnEpisodeBegin()
    {
        transform.localPosition = ResetPosAgent;
        RbAgent.linearVelocity = Vector3.zero;
        RbAgent.angularVelocity = Vector3.zero;
    }

    // 사람이 조작할 때
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;

        // Z-Movement (Index 0): W/S 키
        if (Input.GetKey(KeyCode.W))
        {
            continuousActionsOut[0] = 1.0f; // 앞으로 이동 (최대 속도)
        }
        else if (Input.GetKey(KeyCode.S))
        {
            continuousActionsOut[0] = -1.0f; // 뒤로 이동 (최대 속도)
        }
        else
        {
            continuousActionsOut[0] = 0.0f; // 정지
        }

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