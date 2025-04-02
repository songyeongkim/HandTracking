import cv2
import mediapipe as mp
import socket
import json
import threading
from flask import Flask, request, jsonify
import numpy as np
import tensorflow as tf

# === MediaPipe 초기화 ===
mp_hands = mp.solutions.hands
hands = mp_hands.Hands(
    static_image_mode=False,
    max_num_hands=2,
    min_detection_confidence=0.5,
    min_tracking_confidence=0.5,
)

# === Unity로 전송하는 소켓 서버 ===
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

# === Flask 서버 설정 ===
app = Flask(__name__)
model = tf.keras.models.load_model("sign_gesture_model_v2.h5")
label_classes = np.load("label_classes.npy", allow_pickle=True)
MAX_SEQ_LEN = 91

# === 손가락 펴짐 여부 판단 ===
def is_finger_extended(lm, mcp, pip, tip):
    v1 = np.array([lm[pip]["x"] - lm[mcp]["x"], lm[pip]["y"] - lm[mcp]["y"]])
    v2 = np.array([lm[tip]["x"] - lm[pip]["x"], lm[tip]["y"] - lm[pip]["y"]])
    dot = np.dot(v1, v2)
    norm = np.linalg.norm(v1) * np.linalg.norm(v2)
    angle = np.degrees(np.arccos(np.clip(dot / norm, -1.0, 1.0)))
    return 1.0 if angle > 160 else 0.0

# === 상대좌표 기반 feature 추출 ===
def extract_features_from_frame(frame):
    lm = frame["landmarks"]
    wrist = lm[0]
    rel_coords = []
    for pt in lm:
        rel_coords.extend([
            pt["x"] - wrist["x"],
            pt["y"] - wrist["y"],
            pt["z"] - wrist["z"]
        ])

    fingers = [
        is_finger_extended(lm, 2, 3, 4),
        is_finger_extended(lm, 5, 6, 8),
        is_finger_extended(lm, 9, 10, 12),
        is_finger_extended(lm, 13, 14, 16),
        is_finger_extended(lm, 17, 18, 20)
    ]
    return rel_coords + fingers  # 총 68차원

@app.route("/predict", methods=["POST"])
def predict():
    if model is None:
        return jsonify({"gesture": "모델 없음"}), 503

    try:
        data = request.get_json()
        frames = data.get("sequence", [])

        input_seq = [extract_features_from_frame(f) for f in frames]
        while len(input_seq) < MAX_SEQ_LEN:
            input_seq.append([0.0] * 68)
        input_seq = input_seq[:MAX_SEQ_LEN]

        input_np = np.array([input_seq])
        pred = model.predict(input_np)
        confidence = float(np.max(pred[0]))
        label_index = int(np.argmax(pred[0]))
        result = label_classes[label_index]

        return jsonify({"gesture": result, "confidence": confidence})

    except Exception as e:
        import traceback
        traceback.print_exc()
        return "Internal Server Error", 500

if __name__ == '__main__':
    threading.Thread(target=socket_server, daemon=True).start()
    print("🚀 Flask server running on http://127.0.0.1:8000")
    app.run(host="127.0.0.1", port=8000, debug=True)