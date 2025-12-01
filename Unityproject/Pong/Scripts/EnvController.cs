// 202110844 문경수 (코드 구현)
using UnityEngine;

public class EnvController : MonoBehaviour
{
    public GameObject Ball;

    // Agent_A를 Agent_K로, Agent_B를 Agent_G로 변경합니다.
    public KickerAgent Agent_K;
    public PongAgent Agent_G; // Goalkeeper 역할을 위한 Agent_B (PongAgent) 유지

    private Rigidbody RbBall;

    private Vector3 ResetPosBall;

    // 공 속도
    private float max_ball_speed = 7f;
    private float min_ball_speed = 2f;
    private float ball_x_vel_old = 0f;

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
        if (resetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            // Agent_A -> Agent_K
            Agent_K.EpisodeInterrupted();
            // Agent_B -> Agent_G
            Agent_G.EpisodeInterrupted();
            ResetScene();
        }
        else
        {
            // 공의 X 속도 제한 로직
            if (Mathf.Abs(RbBall.linearVelocity.x) <= min_ball_speed)
            {
                if (RbBall.linearVelocity.x > 0)
                {
                    RbBall.linearVelocity = new Vector3(min_ball_speed, 0, RbBall.linearVelocity.z);
                }
                else if (RbBall.linearVelocity.x < 0) // 0일 때는 가만히 있도록 수정
                {
                    RbBall.linearVelocity = new Vector3(-min_ball_speed, 0, RbBall.linearVelocity.z);
                }
            }

            // 패들 충돌 시 리워드 로직 (Pong 로직)
            // Agent_K (Kicker)는 공이 자신 쪽으로 올 때 쳤을 때 보상
            if (ball_x_vel_old < 0 && RbBall.linearVelocity.x > 0)
            {
                Agent_K.AddReward(0.9f); // Agent_A -> Agent_K
            }
            // Agent_G (Goalkeeper)는 공이 자신 쪽으로 올 때 쳤을 때 보상
            if (ball_x_vel_old > 0 && RbBall.linearVelocity.x < 0)
            {
                Agent_G.AddReward(0.9f); // Agent_B -> Agent_G
            }
            ball_x_vel_old = RbBall.linearVelocity.x;

            // 골 체크 로직 (Pong 로직) -> 3차 피드백 후 수정,
            // Agent_K의 골대 (x < -10.5f) 
            // 이 부분이 필요한가? (4차 피드백 후 검토)
            if (Ball.transform.localPosition.x < -10.5f)
            {
                Agent_K.AddReward(-1f);  // (득점)
                Agent_G.AddReward(1f); // (실점)
                Agent_K.EndEpisode(); // Agent_A -> Agent_K
                Agent_G.EndEpisode(); // Agent_B -> Agent_G
                ResetScene();
            }
            // Agent_G의 골대 (x > 10.5f)
            else if (Ball.transform.localPosition.x > 10.5f)
            {
                Agent_K.AddReward(1f);  // (득점)
                Agent_G.AddReward(-1f); // (실점)
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