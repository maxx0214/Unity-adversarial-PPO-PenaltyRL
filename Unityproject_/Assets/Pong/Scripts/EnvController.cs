// 202110844 문경수 (코드 구현)
// 환경 구현 코드
using UnityEngine;


public class EnvController : MonoBehaviour
{
    public GameObject Ball;

    // Agent_A를 Agent_K로, Agent_B를 Agent_G로 변경합니다.
    public KickerAgent Agent_K;
    public PongAgent Agent_G;

    private Rigidbody RbBall;
    private Vector3 ResetPosBall;

    // 공 속도
    private float max_ball_speed = 3f;
    private float min_ball_speed = 2f;
    private float ball_x_vel_old = 0f;

    // MaxStep 넘기면 종료조건
    private int resetTimer;
    public int MaxEnvironmentSteps;

    void Start()
    {
        ResetPosBall = Ball.transform.localPosition;
        RbBall = Ball.GetComponent<Rigidbody>();
        ResetScene();
    }

    private void FixedUpdate()
    {
        resetTimer += 1;
        // MAX_STEP(5000)보다 크면 에피소드 종료
        if (resetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            Agent_K.EpisodeInterrupted();
            Agent_G.EpisodeInterrupted();
            ResetScene();
        }
        else
        {
            // 공의 X 속도 제한 로직
            if (Mathf.Abs(RbBall.linearVelocity.x) <= min_ball_speed)
            {
                if (RbBall.linearVelocity.x > 0) //공이 움직이기 시작하면, 제한 속도 걸기.
                {
                    RbBall.linearVelocity = new Vector3(min_ball_speed, max_ball_speed, RbBall.linearVelocity.z);
                }
                else if (RbBall.linearVelocity.x < 0) // 0일 때는 가만히 있는다.
                {
                    RbBall.linearVelocity = new Vector3(-min_ball_speed, 0, RbBall.linearVelocity.z);
                }
            }

            // 골 체크 로직 (Pong 로직) -> 3차 피드백 후 수정,
            // Agent_K의 골대 (x < -10.5f)
            // Agent_K가 골을 성공시킨다면
            if (Ball.transform.localPosition.x < -10.5f)
            {
                Agent_K.AddReward(10f);  // 공격 성공!
                Agent_G.AddReward(-10f); // 방어 실패!
                Agent_K.EndEpisode();
                Agent_G.EndEpisode();
                ResetScene();
            }
            // Agent_G가 공을 방어하고, 일정 이상 튕겨나갔을때.
            else if (Ball.transform.localPosition.x > 0.0000001f)
            {
                // Agent_K.AddReward(-1f);  // 공격 실패시, 패널티가 존재하면 아예 공격을 안하려고 함.
                Agent_G.AddReward(10f); // 대신, 방어 성공시 점수 극대화!
                Agent_K.EndEpisode();
                Agent_G.EndEpisode();
                ResetScene();
            }

        }
    }

    public void ResetScene()
    {
        resetTimer = 0;

        // 1. 공 위치 초기화
        Ball.transform.localPosition = ResetPosBall;

        // 2. 속도 초기화 (핵심: 공을 움직이지 않게 합니다)
        RbBall.linearVelocity = Vector3.zero;
        RbBall.angularVelocity = Vector3.zero;
        Ball.transform.rotation = Quaternion.identity;

        ball_x_vel_old = 0f;
    }
}