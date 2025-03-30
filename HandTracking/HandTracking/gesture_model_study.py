# train_model_combined.py
import os
import json
import numpy as np
from sklearn.model_selection import train_test_split
from sklearn.preprocessing import LabelEncoder
from tensorflow.keras.models import Sequential
from tensorflow.keras.layers import LSTM, Dense, Dropout

# === CONFIG ===
DATA_DIR = 'C:/Users/redjack11/Desktop/BodyTracking/TrackingProject/HandTracking/EasyOCRTest/Assets/GestureData'
MODEL_OUTPUT = 'sign_gesture_model_v2.h5'
LABELS_OUTPUT = 'label_classes.npy'
MAX_SEQ_LEN = 151  # 고정 시퀀스 길이
FEATURE_DIM = 68   # 63 상대좌표 + 5 손가락 상태

# === 손가락 펴짐 여부 계산 ===
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

# === 전체 데이터 로딩 ===
X, y_raw = [], []

for file in os.listdir(DATA_DIR):
    if file.endswith('.json'):
        with open(os.path.join(DATA_DIR, file), 'r', encoding='utf-8') as f:
            data = json.load(f)
            label = data['label']
            sequence = data['sequence']
            features = [extract_features_from_frame(f) for f in sequence]
            features = features[:MAX_SEQ_LEN]
            while len(features) < MAX_SEQ_LEN:
                features.append([0.0] * FEATURE_DIM)
            X.append(features)
            y_raw.append(label)

X = np.array(X)
le = LabelEncoder()
y = le.fit_transform(y_raw)

# 라벨 클래스 저장
np.save(LABELS_OUTPUT, le.classes_)

# === 학습/검증 분리 ===
X_train, X_val, y_train, y_val = train_test_split(X, y, test_size=0.2, random_state=42)

# === 모델 정의 ===
model = Sequential([
    LSTM(128, return_sequences=True, input_shape=(MAX_SEQ_LEN, FEATURE_DIM)),
    Dropout(0.3),
    LSTM(64),
    Dense(64, activation='relu'),
    Dense(len(le.classes_), activation='softmax')
])
model.compile(optimizer='adam', loss='sparse_categorical_crossentropy', metrics=['accuracy'])

# === 학습 ===
model.fit(X_train, y_train, validation_data=(X_val, y_val), epochs=30, batch_size=16)
model.save(MODEL_OUTPUT)
print(f'✅ 모델 저장 완료: {MODEL_OUTPUT}')
print(f'✅ 라벨 저장 완료: {LABELS_OUTPUT}')