import cv2
import mediapipe as mp
import socket
import json

# Mediapipe Hands 설정
mp_hands = mp.solutions.hands

# 비디오 캡처 (웹캠 사용, 숫자 대신 영상 파일 경로로 바꿔도 됨)
cap = cv2.VideoCapture(0)

# 소켓 서버 설정
server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server_socket.bind(('127.0.0.1', 5050))
server_socket.listen(1)
print("✅ Waiting for Unity connection...")

conn, addr = server_socket.accept()
print(f"🤝 Connected to {addr}")

with mp_hands.Hands(
    static_image_mode=False,
    max_num_hands=2,
    min_detection_confidence=0.5,
    min_tracking_confidence=0.5
) as hands:
    while cap.isOpened():
        success, frame = cap.read()
        if not success:
            print("❌ Failed to grab frame.")
            break

        # 이미지 전처리
        frame_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        results = hands.process(frame_rgb)

        # 시각 확인용 창
        cv2.imshow("🖐️ Hand Tracking", frame)

        # 손 좌표 데이터 추출
        landmarks_data = []
        if results.multi_hand_landmarks:
            hands_data = []
            for hand_landmarks in results.multi_hand_landmarks:
                landmarks = [{"x": lm.x, "y": lm.y, "z": lm.z} for lm in hand_landmarks.landmark]
                hands_data.append({"landmarks": landmarks})
        else:
            hands_data = []

        json_data = {
            "hands": hands_data
        }

        # ✅ 디버깅용 JSON 출력
        print(json.dumps(json_data, indent=2))

        # Unity 전송
        data = json.dumps(json_data) + '\n'
        try:
            conn.sendall(data.encode('utf-8'))
        except BrokenPipeError:
            print("💥 Unity 연결이 끊어졌습니다.")
            break

        # ESC 키 누르면 종료
        if cv2.waitKey(1) & 0xFF == 27:
            print("🛑 ESC 누름. 종료합니다.")
            break

# 종료 처리
cap.release()
cv2.destroyAllWindows()
conn.close()
server_socket.close()