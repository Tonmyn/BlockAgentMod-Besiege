import json
import numpy as np

def generate_random_mlp():
    # 定义网络结构: 输入4维 -> 隐藏层8维 -> 隐藏层8维 -> 输出2维
    # 输入: [速度, 朝向Y角, 目标距离, 目标角度差]
    # 输出: [油门(0~1), 转向(-1~1)]
    
    layers = []
    
    # Layer 1: Input(4) -> Hidden(8)
    W1 = np.random.randn(8, 4).tolist()
    B1 = np.random.randn(8).tolist()
    layers.append({"type": "dense", "in": 4, "out": 8, "weights": W1, "biases": B1})
    
    # Layer 2: Hidden(8) -> Hidden(8)
    W2 = np.random.randn(8, 8).tolist()
    B2 = np.random.randn(8).tolist()
    layers.append({"type": "dense", "in": 8, "out": 8, "weights": W2, "biases": B2})
    
    # Layer 3: Hidden(8) -> Output(2)
    W3 = np.random.randn(2, 8).tolist()
    B3 = np.random.randn(2).tolist()
    layers.append({"type": "dense", "in": 8, "out": 2, "weights": W3, "biases": B3})
    
    model_data = {
        "model_name": "Car_v0_Random",
        "input_size": 4,
        "output_size": 2,
        "layers": layers
    }
    
    # 写入 JSON
    with open("models/car_brain.json", "w") as f:
        json.dump(model_data, f, indent=4)
        
    print("模型已生成: models/car_brain.json")

if __name__ == "__main__":
    generate_random_mlp()
