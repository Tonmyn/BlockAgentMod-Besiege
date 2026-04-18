using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Modding;
using PluginManager.Plugin;

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
            //BlockAgentMod.AddComponent<Chat>();

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



    }


}
