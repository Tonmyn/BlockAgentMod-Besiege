import json
import random

def generate_random_mlp():
    layers = []
    
    # 定义网络结构
    layer_shapes = [
        (4, 8),  # 输入4，隐藏层8
        (8, 8),  # 隐藏层8，隐藏层8
        (8, 2)   # 隐藏层8，输出2
    ]

    for i, (in_dim, out_dim) in enumerate(layer_shapes):
        # 1. 生成随机权重 (out_dim 行, in_dim 列)
        # 注意：这里我们生成一个扁平的列表，而不是嵌套列表
        weights_flat = [random.gauss(0, 1) for _ in range(out_dim * in_dim)]
        
        # 2. 生成随机偏置
        biases = [random.gauss(0, 1) for _ in range(out_dim)]
        
        layer_data = {
            "type": "dense",
            "in_size": in_dim,    # 改名避开关键字
            "out_size": out_dim,  # 改名避开关键字
            "rows": out_dim,      # 告诉C#有几行
            "cols": in_dim,       # 告诉C#有几列
            "weights": weights_flat, # 扁平化的权重
            "biases": biases
        }
        layers.append(layer_data)
    
    model_data = {
        "model_name": "Car_v0_Flattened",
        "layers": layers
    }
    
    with open("models/car_brain.json", "w") as f:
        json.dump(model_data, f, indent=4)
        
    print("模型已生成 (兼容 JsonUtility 版本)")

if __name__ == "__main__":
    generate_random_mlp()
