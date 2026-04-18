import json
import random
import os

def generate_random_mlp():
    layers = []
    
    # 定义网络结构
    layer_shapes = [
        (4, 8),  # 输入4，隐藏层8
        (8, 8),  # 隐藏层8，隐藏层8
        (8, 2)   # 隐藏层8，输出2
    ]

    for in_dim, out_dim in layer_shapes:
        # 1. 使用标准 random 库生成权重
        # 为了让 C# 的 JsonUtility 也能解析，我们将二维权重“扁平化”为一维数组
        # 总数 = 输出层节点数 * 输入层节点数
        weights_flat = [random.gauss(0, 1) for _ in range(out_dim * in_dim)]
        
        # 2. 生成偏置
        biases = [random.gauss(0, 1) for _ in range(out_dim)]
        
        layer_data = {
            "type": "dense",
            "in_size": in_dim,
            "out_size": out_dim,
            "rows": out_dim,      # 告诉 C# 这个矩阵有几行
            "cols": in_dim,       # 告诉 C# 这个矩阵有几列
            "weights": weights_flat, # 扁平化的权重
            "biases": biases
        }
        layers.append(layer_data)
    
    model_data = {
        "model_name": "Car_v1_NoNumpy",
        "layers": layers
    }
    
    # 确保目录存在
    os.makedirs("models", exist_ok=True)
    
    with open("models/car_brain.json", "w") as f:
        json.dump(model_data, f, indent=4)
        
    print("模型生成成功！(已解决 numpy 依赖问题)")

if __name__ == "__main__":
    generate_random_mlp()
