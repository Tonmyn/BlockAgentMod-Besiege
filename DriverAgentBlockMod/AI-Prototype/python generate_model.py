import json
import random # 改用 Python 自带的随机库，无需安装

def generate_random_mlp():
    # 定义网络结构: 输入4维 -> 隐藏层8维 -> 隐藏层8维 -> 输出2维
    
    layers = []
    
    # 辅助函数：生成随机矩阵，模拟 np.random.randn
    def get_random_matrix(rows, cols):
        # 使用高斯分布（正态分布）生成随机数，均值为0，标准差为1
        return [[random.gauss(0, 1) for _ in range(cols)] for _ in range(rows)]

    # 辅助函数：生成随机向量
    def get_random_biases(size):
        return [random.gauss(0, 1) for _ in range(size)]

    # Layer 1: Input(4) -> Hidden(8)
    W1 = get_random_matrix(8, 4)
    B1 = get_random_biases(8)
    layers.append({"type": "dense", "in": 4, "out": 8, "weights": W1, "biases": B1})
    
    # Layer 2: Hidden(8) -> Hidden(8)
    W2 = get_random_matrix(8, 8)
    B2 = get_random_biases(8)
    layers.append({"type": "dense", "in": 8, "out": 8, "weights": W2, "biases": B2})
    
    # Layer 3: Hidden(8) -> Output(2)
    W3 = get_random_matrix(2, 8)
    B3 = get_random_biases(2)
    layers.append({"type": "dense", "in": 8, "out": 2, "weights": W3, "biases": B3})
    
    model_data = {
        "model_name": "Car_v0_Random_NoNumPy",
        "input_size": 4,
        "output_size": 2,
        "layers": layers
    }
    
    # 写入 JSON
    with open("models/car_brain.json", "w") as f:
        json.dump(model_data, f, indent=4)
        
    print("模型已生成: models/car_brain.json (无需 NumPy 版本)")

if __name__ == "__main__":
    generate_random_mlp()
