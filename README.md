#  Penalty Shootout Reinforcement Learning Project  
## 승부차기 강화학습 프로젝트 (Unity ML-Agents + Self-Play PPO)

---

# 1. 프로젝트 개요 (Project Overview)

본 프로젝트는 Unity ML-Agents와 PyTorch PPO를 기반으로  
**스트라이커(Kicker)** 와 **골키퍼(Goalkeeper)** 가 경쟁하는  
**Self-Play Penalty Shootout 강화학습 환경**을 구축하는 것을 목표로 한다.

This project implements a **Zero-sum, multi-agent penalty shootout RL environment** built with  
**Unity ML-Agents + Python PPO (Self-Play)**.

Unity는 실제 축구 승부차기처럼 물리 기반 공 이동 및 에이전트 이동을 시뮬레이션하며,  
Python PPO는 두 에이전트(Kicker / Keeper)의 정책을 반복적으로 개선한다.

---

# 2. 환경 및 파일 구성 (Project Structure)

```
root/
 ├── UnityProject/                 # Unity 환경 전체
 │      ├── Assets/
 │      ├── Packages/
 │      ├── ProjectSettings/
 │
 ├── python/
 │      ├── agents/
 │      │     └── adversarial_ppo.py
 │      ├── config/ppo/
 │      │     └── Pong_adversarial.yaml
 │      ├── envs/
 │      │     └── Pong_Windows/
 │      │           ├── Pong.exe
 │      │           ├── Pong_Data/
 │      └── run_train.py
 │
 ├── README.md                     # (본 보고서)
 └── .gitignore
```

---

# 3. 학습 실행 방법 (How to Run Training)

## ✔ Unity 없이 실행 (Using Built Executable)

```
pip install torch mlagents_envs numpy tensorboard
cd python
python run_train.py
```

Unity 빌드 파일:

```
python/envs/Pong_Windows/Pong.exe
```

PPO 학습이 즉시 시작됨.

---

# 4. Unity 환경 실행 (Running Unity Environment)

1. Unity Hub → Open → `UnityProject/` 선택  
2. 씬(Scene) 실행  
3. ▶ 버튼 클릭  
4. 환경 수정 후 다시 Build → `python/envs/`에 복사하여 교체

---

# 5. MDP 정의 (MDP Definition)  
### *Based on latest KickerAgent.cs, PongAgent.cs, EnvController.cs*

---

## 5.1 상태 공간 (State Space, S)

### 1) KickerAgent (Striker) – 7차원  
(출처: KickerAgent.cs CollectObservations)

| idx | 관측값 | 설명 |
|----|--------|------|
| 0 | Kicker Z-pos | 키커의 Z 위치 |
| 1 | Kicker X-pos | 키커의 X 위치 |
| 2 | GK Z-pos | 골키퍼 위치(Z) |
| 3 | Ball X-pos | 공 X 위치 |
| 4 | Ball Z-pos | 공 Z 위치 |
| 5 | Ball Vx | 공 X 속도 |
| 6 | Ball Vz | 공 Z 속도 |

---

### 2) Goalkeeper Agent – 8차원  
(출처: PongAgent.cs CollectObservations)

| idx | 관측값 |
|-----|--------|
| 0 | GK X-pos |
| 1 | GK Z-pos |
| 2 | Kicker X-pos |
| 3 | Kicker Z-pos |
| 4 | Ball X-pos |
| 5 | Ball Z-pos |
| 6 | Ball Vx |
| 7 | Ball Vz |

---

## 5.2 행동 공간 (Action Space, A)

### 1) KickerAgent 행동 – 연속(Continuous) 2차원  

- X축(좌/우 이동)  
- Z축(앞/뒤 이동)  
- MoveSpeed × action 값으로 이동

### 2) Goalkeeper 행동 – 연속 1차원  


- 골키퍼는 Z축 이동만 가능  
- X축은 고정

---

## 5.3 전이 함수 (Transition Function, P)


전이는 Unity 물리 엔진으로 구현됨:

- Rigidbody 기반 이동  
- 공 속도 최소값 필터 적용  
- 공이 골라인(x < –10.5 or x > 10.5)을 통과하면 episode 종료  
- MaxEnvironmentSteps 초과하면 강제 종료  

---

## 5.4 보상 함수 (Reward Function, R)

### 1) 접근(거리) 보상 — Kicker  
(출처: KickerAgent.cs)

- 공에 가까워질수록 보상 증가

---

### 3) 득점/실점 Outcome Reward  
(출처: EnvController.cs)

### 골키퍼 골대(x < –10.5)  
| Agent | Reward |
|-------|--------|
| Kicker | –1 |
| Keeper | +1 |

### 스트라이커 골대(x > 10.5)  
| Agent | Reward |
|--------|--------|
| Kicker | +1 |
| Keeper | –1 |

득점/실점 시:
- 두 Agent 모두 EndEpisode() 호출  
- ResetScene() 실행  

---

## 5.5 할인율 (Discount Factor)

\[
\gamma = 0.99
\]

---

# 6. Self-Play 구조 (Multi-Agent Self-Play)

- 두 에이전트(Kicker, Keeper)는 독립적인 PPO 정책을 가짐  
- 각자 자신의 reward를 기반으로 업데이트  
- Zero-sum 성질로 서로 강화  
- Python PPO는 두 네트워크를 병렬 학습하며 경쟁적 성장을 유도  

---

# 7. 강화학습 알고리즘 (PPO Algorithm Summary)

사용된 기법:

- PPO (Clipped Surrogate Objective)  
- GAE(λ)  
- n-step Rollout (512-step)  
- Actor-Critic 구조  
- Dual-Agent 학습  

하이퍼파라미터:

```
learning_rate = 5e-4
gamma = 0.99
lambda = 0.95
clip_range = 0.2
batch_size = 128
buffer_size = 512
n_epoch = 3
```

Config 파일:  
`python/config/ppo/Pong_adversarial.yaml`

---

# 8. 결론 및 향후 연구 방향 (Conclusion & Future Work)

본 프로젝트는 강화학습 기반 승부차기 환경을 구축하고  
Striker–Keeper Self-play를 통해 경쟁적 정책 학습이 가능함을 보였다.

향후 확장:
 
- Continuous shooting force/angle 모델링  
- Human motion feature 추가(킥 모션 데이터)  
- Keeper 반응 딜레이 모델링  

---

# 9. 제출 정보 (Submission Info)

- 과목명: 강화학습(Reinforcement Learning)
- 제출자: 노재형, 문경수, 이승윤
- 기간: 2025학년도  
- 목적: 학습 환경 구축 및 Self-Play PPO 성능 분석  

---


