using Modding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using ModIO;
using System.Collections;

namespace DriverAgentBlockMod
{
    class CarBlockScript : BlockScript
    {
        public override bool EmulatesAnyKeys => true;

        public Guid Guid { get; private set; } = Guid.NewGuid();
        public string modelPath = @"AI-Prototype/models/car_brain.json";
        public float updateInterval = 0.5f; // 每 0.1 秒思考一次 (10Hz)

        // --- 内部变量 ---
        private MiniMLP brain;
        private Rigidbody rb;
        private float lastThinkTime = 0f;

        // 目标点 (简单的测试：假设你想让车朝 Z 轴正方向一直开)
        // 实际使用时可以用你的“加减分球”逻辑来获取目标位置
        private Vector3 targetPosition = new Vector3(0, 0, 500f);

        MKey key_Forward;
        MKey key_Backward;
        MKey key_Left;
        MKey key_Right;
        MKey key_Accelerator;
        MKey key_Brake;
        



        public override void SafeAwake()
        {
            ConsoleController.ShowMessage("safeawake...");
            key_Forward = AddEmulatorKey("forward", "forward", UnityEngine.KeyCode.UpArrow);
            key_Backward = AddEmulatorKey("backward", "backward", UnityEngine.KeyCode.DownArrow);
            key_Left = AddEmulatorKey("left", "left", UnityEngine.KeyCode.LeftArrow);
            key_Right = AddEmulatorKey("right", "right", UnityEngine.KeyCode.RightArrow);
            

        }

        public override void OnSimulateStart()
        {
            base.OnSimulateStart();

      

            brain = new MiniMLP();
            ConsoleController.ShowMessage(".1.");
            // 拼接完整路径 (假设模型放在 Besiege 根目录或 Mods 目录)
            // 为了测试方便，你可以直接写死绝对路径，比如 "C:/Users/.../car_brain.json"
            //string fullPath = Path.Combine(Application.dataPath, "../" + modelPath);
            try
            {
                ConsoleController.ShowMessage(".2.");
                // 检查文件是否存在（相对于Mod根目录）
                if (Modding.ModIO.ExistsFile(modelPath))
                {
                    ConsoleController.ShowMessage(".3.");
                    // 读取所有文本内容
                    //string json = Modding.ModIO.ReadAllText(modelPath);
                    ConsoleController.ShowMessage(".4.");
                    // 解析并加载模型
                    if (!brain.LoadModel(modelPath))
                    {
                        ConsoleController.ShowMessage("[CarAgent] 加载模型失败！");
                    }
                    else
                    {
                        ConsoleController.ShowMessage("[CarAgent] 成功从 " + modelPath + " 加载模型。");
                    }
                    ConsoleController.ShowMessage(".5.");
                }
                else
                {
                    ConsoleController.ShowMessage("[CarAgent] 未找到模型文件: " + modelPath + "。将使用随机初始化的模型。");
                    //brain.InitializeRandomWeights(); // 一个备选方案
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[CarAgent] 读取模型文件时发生异常: " + e.Message);
            }
            //string fullPath = ModIO.

            //brain.LoadModel(fullPath);

            rb = GetComponent<Rigidbody>();
            if (rb == null) rb = GetComponentInParent<Rigidbody>();
        }

        public override void SimulateUpdateAlways()
        {
            base.SimulateUpdateAlways();

            // 1. 定时思考
            if (Time.time - lastThinkTime > updateInterval)
            {
                Think();
                lastThinkTime = Time.time;
                ExecuteControls();
            }

            // 2. 实时执行 (根据上一次思考的结果平滑执行)
            // 注意：这里为了简单，我们让 AI 直接输出控制值。
            // 如果你想用“队列”模式，这里就读取队列里当前时间的指令。
            //ExecuteControls();

            
        }

     

        void Think()
        {
            // --- A. 获取状态 ---
            float speed = rb.velocity.magnitude;
            float heading = transform.eulerAngles.y; // 0-360

            Vector3 toTarget = targetPosition - transform.position;
            float distToTarget = toTarget.magnitude;
            float angleToTarget = Vector3.Angle(transform.right, toTarget); // -180 到 180

            ConsoleController.ShowMessage(angleToTarget.ToString());

            // 构造输入向量 [速度, 当前朝向, 目标距离, 目标角度]
            // 注意：神经网络对数值范围敏感，最好做一点归一化
            float[] inputs = new float[4];
            inputs[0] = speed / 50f;           // 假设最大速度 50
            inputs[1] = heading / 360f;        // 归一化 0-1
            inputs[2] = distToTarget / 1000f;   // 归一化距离
            inputs[3] = angleToTarget / 180f;   // 归一化角度 -1 到 1

            // --- B. 神经网络推理 ---
            float[] outputs = brain.Forward(inputs);

            // --- C. 记录输出结果 ---
            // outputs[0] -> Throttle (0~1, 由 Tanh 映射转换)
            // outputs[1] -> Steering (-1~1)

            // 这里为了简单直接存到变量，如果想用队列，可以在这里生成 ActionQueue 并写入文件
            currentThrottle = (outputs[0] + 1f) / 2f; // 将 -1~1 转换为 0~1 (油门)
            currentSteering = outputs[1];              // -1 (左) ~ 1 (右)
        }

        // 这两个变量存储 AI 的决策
        private float currentThrottle = 0f;
        private float currentSteering = 0f;

        void ExecuteControls()
        {
            // --- D. 实际控制小车 ---
            // 这里需要你根据 Besiege 的 Mod API 来写。
            // 假设你的车有一个 Steering Block 和一个 Powered Wheel。

            // 示例伪代码 (你需要替换成真实的方块查找和按键模拟代码):
            /*
            // 1. 查找控制方块
            var steeringBlock = FindBlock("SteeringHinge");
            var powerBlock = FindBlock("PoweredWheel");

            // 2. 模拟输入 (直接修改方块 Input 值)
            if(steeringBlock != null) {
                steeringBlock.InputValue = currentSteering; 
            }

            if(powerBlock != null) {
                // 0.5 是中位，currentThrottle 0~1 映射到 0.5~1.0 (假设 0.5 是停止，1.0 是最大前进)
                powerBlock.InputValue = 0.5f + (currentThrottle * 0.5f); 
            }
            */
            EmulateKeys(key_Forward, currentThrottle * 0.5f);
            //EmulateKeys(key_Forward, false);
            TurnRound(key_Left, key_Right, currentSteering);

            // 调试用：打印到 Unity Console，看 AI 有没有输出
            // Debug.Log(string.Format("AI Action: Throttle={0:0.00}, Steering={1:0.00}", currentThrottle, currentSteering));

            ConsoleController.ShowMessage(string.Format("AI Action: Throttle={0:0.00}, Steering={1:0.00}", currentThrottle, currentSteering));
        }

        void EmulateKeys(MKey mKey, bool emulate)
        {

            EmulateKeys(new MKey[0], mKey, emulate);


        }

        void EmulateKeys(MKey mKey, float time)
        {
            
            int index = TimeToFrameCount(time);

            EmulateKeys(new MKey[0], mKey, true);

            StartCoroutine(methon(index));
            
            IEnumerator methon(int i)
            {


                for (int timer = 0; timer < i; timer++)
                {
                   // ConsoleController.ShowMessage("-" + timer.ToString() + " " + i);
                    yield return Time.fixedDeltaTime;
                }

                EmulateKeys(new MKey[0], mKey, false);
               // ConsoleController.ShowMessage(".stop");
                yield break;

            }
        }

        void TurnRound(MKey left,MKey right,float steering)
        {
            var sign = (int)Mathf.Abs(steering);

            if (sign > 0)
            {
                EmulateKeys(left, steering);
            }
            else
            {
                EmulateKeys(right, Mathf.Abs(steering));
            }
        
        }

        public int TimeToFrameCount(float time)
        {
            int index = Application.targetFrameRate;
            int num = Mathf.CeilToInt(time * index);
            return (num < 1) ? 1 : num;
        }

    }
}
