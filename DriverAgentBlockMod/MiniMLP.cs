using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;


namespace DriverAgentBlockMod
{
    // 为了兼容 Unity 5.7，我们尽量用基础类型
    public class MiniMLP
    {
        public class Layer
        {
            public float[,] weights;    // 内部计算仍建议用二维数组方便索引
            public float[] biases;
        }

        private List<Layer> layers = new List<Layer>();

        // 从 JSON 文件加载模型
        public bool LoadModel(string filePath)
        {
            ConsoleController.ShowMessage("load modle .1.");
            if (!Modding.ModIO.ExistsFile(filePath))
            {
                ConsoleController.ShowMessage("load modle .1.5.");
                ConsoleController.ShowMessage("[MiniMLP] 模型文件不存在: " + filePath);
                return false;
            }
            ConsoleController.ShowMessage("load modle .2.");
            try
            {
                ConsoleController.ShowMessage("load modle .3.");
                string json = Modding.ModIO.ReadAllText(filePath);
                // 注意：Unity 5.7 自带 JsonUtility，但它不支持解析二维数组 float[,]
                // 简单起见，我们假设 JSON 里的 weights 是 数组的数组
                // 这里为了代码简洁，我们用一个 Wrapper 类来辅助解析

                ConsoleController.ShowMessage("load modle .4.  ");

                ModelData data = JsonConvert.DeserializeObject<ModelData>(json);
                
                layers.Clear();

                ConsoleController.ShowMessage("load modle .5. " + data.model_name.ToString() +"  " +data.layers.Count);
                // 手动转换数据结构 (因为 JsonUtility 比较笨)
                foreach (var l in data.layers)
                {
                    ConsoleController.ShowMessage("load modle .5.5 " + (l == null).ToString());

                    Layer layer = new Layer();
                    layer.biases = l.biases;

                    // 将扁平的 weights 还原为二维数组
                    if (l.weights.Length != l.rows * l.cols)
                    {
                        Debug.LogError("[MiniMLP] 权重尺寸不匹配");
                        return false;
                    }

                    layer.weights = new float[l.rows, l.cols];
                    int index = 0;
                    for (int r = 0; r < l.rows; r++)
                    {
                        for (int c = 0; c < l.cols; c++)
                        {
                            layer.weights[r, c] = l.weights[index++];
                        }
                    }

                    layers.Add(layer);
                }
                ConsoleController.ShowMessage("load modle .7.");
                ConsoleController.ShowMessage("[MiniMLP] 模型加载成功! 层数: " + layers.Count);
                return true;
            }
            catch (Exception e)
            {
                ConsoleController.ShowMessage("[MiniMLP] 加载模型失败: " + e.Message);
                return false;
            }
        }

        // 前向推理
        public float[] Forward(float[] inputs)
        {
            float[] current = inputs;

            for (int i = 0; i < layers.Count; i++)
            {
                Layer layer = layers[i];
                int outDim = layer.biases.Length;
                float[] next = new float[outDim];

                // 矩阵乘法 + 偏置
                for (int o = 0; o < outDim; o++)
                {
                    float sum = layer.biases[o];
                    for (int k = 0; k < current.Length; k++)
                    {
                        sum += layer.weights[o, k] * current[k];
                    }

                    // 激活函数
                    if (i < layers.Count - 1)
                    {
                        // 隐藏层用 ReLU
                        next[o] = Mathf.Max(0, sum);
                    }
                    else
                    {
                        // 输出层用 Tanh (限制在 -1 到 1 之间)
                        next[o] = (float)Math.Tanh(sum);
                    }
                }
                current = next;
            }

            return current;
        }

        // 辅助数据结构，用于 JsonUtility 解析
        [Serializable]
        public class ModelData
        {
            public string model_name;
            public List<LayerData> layers;
        }

        [Serializable]
        public class LayerData
        {
            // Newtonsoft.Json 可以直接反序列化二维数组！
            // 对应 Python 的 [[...], [...]]
            public float[] weights;
            public float[] biases;
            public string type;
            public int in_size;
            public int out_size;
            public int rows;
            public int cols;
        }
    }
}
