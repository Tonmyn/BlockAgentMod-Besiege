using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Modding;

namespace DriverAgentBlockMod
{
    public class Mod : ModEntryPoint
    {
        public static GameObject mod;

        public override void OnLoad()
        {

            mod = new GameObject("Driver Agent Block Mod");

            UnityEngine.Object .DontDestroyOnLoad(mod);
            mod.AddComponent<Chat>();

        }

    }
}
