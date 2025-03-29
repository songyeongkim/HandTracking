import cv2
import mediapipe as mp
import socket
import json
import threading
from flask import Flask, request, jsonify
import numpy as np
import tensorflow as tf

# 📌 MediaPipe 초기화
mp_hands = mp.solutions.hands
hands = mp_hands.Hands(
    static_image_mode=False,
    max_num_hands=2,
    min_detection_confidence=0.5,
    min_tracking_confidence=0.5,
)

# 📌 Unity로 보내는 소켓 서버 설정
def socket_server():
    cap = cv2.VideoCapture(0)
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind(('127.0.0.1', 5050))
    server_socket.listen(1)
    print("🎮 Waiting for Unity connection...")

    conn, addr = server_socket.accept()
    print(f"✅ Connected to Unity: {addr}")

    while cap.isOpened():
        success, frame = cap.read()
        if not success:
            break

        frame_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        results = hands.process(frame_rgb)

        landmarks_all = []
        if results.multi_hand_landmarks:
            for hand_landmarks in results.multi_hand_landmarks:
                landmarks = []
                for lm in hand_landmarks.landmark:
                    landmarks.append({"x": lm.x, "y": lm.y, "z": lm.z})
                landmarks_all.append({"landmarks": landmarks})

        json_data = json.dumps({"hands": landmarks_all}) + '\n'
        try:
            conn.sendall(json_data.encode('utf-8'))
        except BrokenPipeError:
            print("🚫 Unity disconnected.")
            break

    cap.release()
    conn.close()
    server_socket.close()

# 📌 Flask 서버 설정
app = Flask(__name__)
model = tf.keras.models.load_model("sign_gesture_model.h5")
labels = ["안녕하세요", "감사합니다", "사랑해요"]
MAX_SEQ_LEN = 30

@app.route("/predict", methods=["POST"])
def predict():
    data = request.get_json()
    frames = data.get("sequence", [])

    input_seq = []
    for frame in frames:
        flat = []
        for lm in frame["landmarks"]:
            flat.extend([lm["x"], lm["y"], lm["z"]])
        input_seq.append(flat)

    while len(input_seq) < MAX_SEQ_LEN:
        input_seq.append([0.0] * 63)
    input_seq = input_seq[:MAX_SEQ_LEN]

    input_np = np.array([input_seq])
    pred = model.predict(input_np)
    label_index = np.argmax(pred[0])
    result = labels[label_index]

    return jsonify({"gesture": result})

# 📌 두 개의 서버를 병렬 실행
if __name__ == '__main__':
    threading.Thread(target=socket_server, daemon=True).start()
    print("🚀 Flask server running on http://127.0.0.1:8000")
    app.run(host="127.0.0.1", port=8000)