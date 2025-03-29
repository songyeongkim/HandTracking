# train_model.py
import os, json, numpy as np
from sklearn.model_selection import train_test_split
from sklearn.preprocessing import LabelEncoder
from tensorflow.keras.utils import to_categorical
from tensorflow.keras.models import Sequential
from tensorflow.keras.layers import LSTM, Dense, Masking

DATA_DIR = 'C:/Users/redjack11/Desktop/BodyTracking/TrackingProject/HandTracking/EasyOCRTest/Assets/GestureData'  # Unity에서 저장한 위치

X, y = [], []

for file in os.listdir(DATA_DIR):
    if file.endswith('.json'):
        with open(os.path.join(DATA_DIR, file), 'r', encoding='utf-8') as f:
            data = json.load(f)
            label = data['label']
            sequence = data['sequence']
            frames = []
            for frame in sequence:
                coords = []
                for lm in frame['landmarks']:
                    coords.extend([lm['x'], lm['y'], lm['z']])
                frames.append(coords)
            X.append(frames)
            y.append(label)

MAX_SEQ_LEN = max(len(seq) for seq in X)
for i in range(len(X)):
    pad = [[0]*63] * (MAX_SEQ_LEN - len(X[i]))
    X[i] = X[i] + pad if len(X[i]) < MAX_SEQ_LEN else X[i][:MAX_SEQ_LEN]

X = np.array(X)
le = LabelEncoder()
y_encoded = le.fit_transform(y)
y_categorical = to_categorical(y_encoded)

X_train, X_val, y_train, y_val = train_test_split(X, y_categorical, test_size=0.2)

model = Sequential([
    Masking(mask_value=0.0, input_shape=(MAX_SEQ_LEN, 63)),
    LSTM(64, return_sequences=True),
    LSTM(64),
    Dense(64, activation='relu'),
    Dense(y_categorical.shape[1], activation='softmax')
])
model.compile(optimizer='adam', loss='categorical_crossentropy', metrics=['accuracy'])

model.fit(X_train, y_train, validation_data=(X_val, y_val), epochs=30, batch_size=16)

model.save('sign_gesture_model.h5')
print('✅ 모델 저장 완료: sign_gesture_model.h5')
