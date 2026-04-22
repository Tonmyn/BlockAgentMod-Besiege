using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Modding;
using PluginManager.Plugin;
using System.IO;

namespace BlockAgentMod
{
    [OnGameInit]
    public class Mod : MonoBehaviour
    {
        public GameObject mod;

        public void Start()
        {

            mod = SingleInstance<BlockAgentMod>.Instance.gameObject;

          

            UnityEngine.Object .DontDestroyOnLoad(mod);
            mod.AddComponent<ChatUI>();

        }

    }

    public class BlockAgentMod : SingleInstance<BlockAgentMod>
    {
        public override string Name
        {
            get
            {
                return "Block Agent Mod - UPM Edition";
            }
        }

        public string ModFilePath { get {return System.Reflection.Assembly.GetExecutingAssembly().Location; } }

        public string ModDirPath { get { return Path.Combine(Environment.CurrentDirectory, "Plugins"); } }
    }


}
