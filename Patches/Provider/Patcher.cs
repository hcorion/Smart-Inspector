using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEditor;
using UnityEngine;

namespace AV.Inspector
{
	internal static class Patcher
	{
		static readonly string HarmonyID = typeof(Patcher).Assembly.GetName().Name;

		static Harmony harmony;
		static List<PatchBase> patches;
		
		static FieldInfo currentSkinField;

		
		[InitializeOnLoadMethod]
		static async void Init()
		{
			// https://github.com/neon-age/Smart-Inspector/issues/5
			if (Application.isBatchMode)
				return;
			
			GetPatches();
			
			ApplyPatches(PatchBase.Apply.OnLoad);
			
			while (!GUISkinHasLoaded())
				await Task.Delay(1);
			
			ApplyPatches(PatchBase.Apply.OnGUI);
		}

		static bool GUISkinHasLoaded()
		{
			if (currentSkinField == null)
				currentSkinField = typeof(GUISkin).GetField("current", BindingFlags.Static | BindingFlags.NonPublic);
			
			var skin = (GUISkin)currentSkinField.GetValue(null);

			return skin != null && skin.name != "GameSkin";
		}

		static void GetPatches()
		{
			if (patches != null) 
				return;
			patches = new List<PatchBase>();
				
			foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(PatchBase)))
			{
				if (type.IsAbstract) 
					continue;
					
				var instance = Activator.CreateInstance(type) as PatchBase;
				patches.Add(instance);
			}
		}

		static void ApplyPatches(PatchBase.Apply apply)
		{
			if (harmony == null)
				harmony = new Harmony(HarmonyID);
			
			foreach (var patch in patches)
				patch.ApplyPatches(harmony, apply);
		}
		

		internal static void RemovePatches()
		{
			if (harmony == null) 
				return;
			
			foreach (var patch in patches)
				patch.UnpatchAll(harmony);
		}
	}
}