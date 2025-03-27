import cv2
import mediapipe as mp
import socket
import json  # JSON 직렬화

# OpenCV 및 Mediapipe 설정
mp_pose = mp.solutions.pose

cap = cv2.VideoCapture("danceTestVideo.mp4")  # 영상 파일 사용

# 소켓 서버 설정
server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server_socket.bind(('127.0.0.1', 5050))  # 로컬호스트, 포트 5050
server_socket.listen(1)
print("Waiting for Unity connection...")

conn, addr = server_socket.accept()
print(f"Connected to {addr}")

with mp_pose.Pose(min_detection_confidence=0.5, min_tracking_confidence=0.5) as pose:
    while cap.isOpened():
        success, frame = cap.read()
        if not success:
            break

        frame_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        results = pose.process(frame_rgb)

        # 원본 영상 출력 (파이썬 창에서 확인)
        cv2.imshow("Original Video", frame)

        # 좌표 데이터 추출
        landmarks = []
        if results.pose_landmarks:
            for lm in results.pose_landmarks.landmark:
                landmarks.append({"x": lm.x, "y": lm.y, "z": lm.z})  # JSON 형식으로 변환

        # POSE_CONNECTIONS을 문자열 리스트로 변환
        connections = [f"{a},{b}" for a, b in mp_pose.POSE_CONNECTIONS] if results.pose_landmarks else []

        # 🚀 JSON 데이터 출력 (디버깅용)
        json_data = {"landmarks": landmarks, "connections": connections}
        data = json.dumps(json_data) + '\n'

        try:
            conn.sendall(data.encode('utf-8'))  # UTF-8로 인코딩 후 전송
        except BrokenPipeError:
            print("Unity 연결이 끊어졌습니다. 프로그램을 종료합니다.")
            break

        # ESC 키를 누르면 종료
        if cv2.waitKey(1) & 0xFF == 27:
            break

cap.release()
cv2.destroyAllWindows()
conn.close()
server_socket.close()
