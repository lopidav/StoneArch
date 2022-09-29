using BepInEx;
using BerryLoaderNS;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;


namespace StoneArchNS
{

	[BepInPlugin("StoneArch", "StoneArch", "1.2.0")]
	[BepInDependency("BerryLoader")]
	public class StoneArchPlugin : BaseUnityPlugin
	{
		public static BepInEx.Logging.ManualLogSource L;
		public static Harmony HarmonyInstance;
		public static bool NormalStrangePortalSpawnDisabled = false;
		
		private void Awake()
		{
			L = Logger;
			HarmonyInstance = new Harmony("StoneArchPlugin");
			HarmonyInstance.PatchAll(typeof(StoneArchPlugin));
			HarmonyInstance.Patch(
				AccessTools.Method(typeof(EndOfMonthCutscenes).GetNestedType("<SpecialEvents>d__13", BindingFlags.NonPublic), "MoveNext"),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(StoneArchPlugin), "TranspilerSpecialEvent")));
			if (!Harmony.HasAnyPatches(HarmonyInstance.Id)) L.LogInfo("Patching failed");
		}

		public static void Log(string s) => L.LogInfo(s);
		
		[HarmonyPatch(typeof(WorldManager), "CreateCard", new Type[] { typeof(Vector3), typeof(string), typeof(bool), typeof(bool), typeof(bool)})]
		[HarmonyPrefix]
		public static void CreateCardPrefix(WorldManager __instance, out GameCard __state, ref Vector3 position, string cardId, bool faceUp, ref bool checkAddToStack, bool playSound)
		{
			__state = null;
			if (cardId == "strange_portal")
			{
				foreach (GameCard allCard in __instance.AllCards)
				{
					if (allCard.MyBoard.IsCurrent && allCard.CardData.Id == "sa_stone_arch" && allCard.Child == null)
					{
						position = allCard.transform.position;
						__state = allCard;
					}
				}
			}
		}
		
		[HarmonyPatch(typeof(WorldManager), "CreateCard", new Type[] { typeof(Vector3), typeof(string), typeof(bool), typeof(bool), typeof(bool)})]
		[HarmonyPostfix]
		public static void CreateStrangePortalOnStoneArchPostfix(CardData __result, GameCard __state, ref Vector3 position, string cardId, bool faceUp, bool checkAddToStack, bool playSound)
		{
			if (__state != null && __result.MyGameCard.Parent == null && __state.Child == null)
			{
				__result.MyGameCard.Parent = __state;
				__state.Child = __result.MyGameCard;
			}
		}
		
		[HarmonyPatch(typeof(EndOfMonthCutscenes), "SpecialEvents")]
		[HarmonyPostfix]
		public static IEnumerator CustomStrangePortalSpawn(IEnumerator enumerator)
		{
				
			if (NormalStrangePortalSpawnDisabled
				&& WorldManager.instance.CurrentMonth > 6
				&& !WorldManager.instance.CurrentRunOptions.IsPeacefulMode
				&& WorldManager.instance.CurrentBoard.Id != "island") // when not island
			{
				
				List<CardData> Arches = WorldManager.instance.GetCards("sa_stone_arch");
				int NumToSpawn = 0;
				
				if (WorldManager.instance.CurrentMonth % Mathf.Max(6 - Arches.Count * 2, 1) == 0)
				{
					NumToSpawn = Mathf.Max(2 * (Arches.Count - 3), 1);
					if (Arches.Count >= 6) NumToSpawn = Arches.Count;
					// arch count -- portal spawned
					// 0 -- every 6 month
					// 1 -- every 4
					// 2 -- every 2
					// 3 -- every month end
					// 4 -- 2 portals every month end
					// 5 -- 4 portals every month end
					// 6 -- all arches filled every month end  
				}
				
				for (int i = 0; i < NumToSpawn; i++)
				{
					Vector3 randomSpawnPosition = WorldManager.instance.GetRandomSpawnPosition();
					CardData cardData = WorldManager.instance.CreateCard(randomSpawnPosition, "strange_portal", faceUp: true, checkAddToStack: false);
					WorldManager.instance.EndOfMonthStatus = SokLoc.Translate("label_strange_portal_appeared");
					GameCamera.instance.TargetPositionOverride = cardData.transform.position;
					yield return new WaitForSeconds(Mathf.Max(0.01f, (4 - i) * 0.5f));
					GameCamera.instance.TargetPositionOverride = null;
				}
				if (NumToSpawn > 0 ) yield return Cutscenes.WaitForContinueClicked(SokLoc.Translate("label_uh_oh"));
				
			}
			if (NormalStrangePortalSpawnDisabled
				&& WorldManager.instance.CurrentMonth > 6
				&& !WorldManager.instance.CurrentRunOptions.IsPeacefulMode
				&& WorldManager.instance.CurrentBoard.Id == "island") // when island
			{
				List<CardData> Arches = WorldManager.instance.GetCards("sa_stone_arch");
				int NumToSpawn = 0;
				
				if (Arches.Count > 0 && WorldManager.instance.CurrentMonth % Mathf.Max(14 - Arches.Count * 2, 1) == 0)
				{
					NumToSpawn = Mathf.Max(2 * (Arches.Count - 7), 1);
					if (Arches.Count >= 14) NumToSpawn = Arches.Count;
					// arch count -- portal spawned
					// 0 -- never
					// 1 -- every 12
					// 2 -- every 10
					// 3 -- every 8
					// 4 -- every 6
					// 5 -- every 4
					// 6 -- every 2
					// 7 -- every month end
					// 8 -- 2 portals every month end
					// 9 -- 4 portals every month end
					// 10 -- 6 portals every month end
					// 11 -- 8 portals every month end
					// 12 -- 10 portals every month end
					// 13 -- 12 portals every month end
					// 14 -- all arches filled every month end  
				}
				
				for (int i = 0; i < NumToSpawn; i++)
				{
					Vector3 randomSpawnPosition = WorldManager.instance.GetRandomSpawnPosition();
					CardData cardData = WorldManager.instance.CreateCard(randomSpawnPosition, "strange_portal", faceUp: true, checkAddToStack: false);
					WorldManager.instance.EndOfMonthStatus = SokLoc.Translate("label_strange_portal_appeared");
					GameCamera.instance.TargetPositionOverride = cardData.transform.position;
					yield return new WaitForSeconds(Mathf.Max(0.1f, (4 - i) * 0.5f));
					GameCamera.instance.TargetPositionOverride = null;
				}
				if (NumToSpawn > 0 ) yield return Cutscenes.WaitForContinueClicked(SokLoc.Translate("label_uh_oh"));
			}
			
		}
		
		[HarmonyPatch(typeof(GameDataLoader), MethodType.Constructor)]
		[HarmonyPostfix]
		public static void InsertDrops(GameDataLoader __instance, ref Dictionary<SetCardBag, string> ___result)
		{
			var existing = SetCardBagHelper.BasicBuildingIdea;
			
			if (___result.TryGetValue(SetCardBag.BasicBuildingIdea, out var value))
			{
				existing = value;
			}

			___result[SetCardBag.AdvancedBuildingIdea] =  existing + ", blueprint_sa_stone_arch";
		}
		
		static IEnumerable<CodeInstruction> TranspilerSpecialEvent(IEnumerable<CodeInstruction> instructions)
   		{
					// Disables normal Strange Portal spawn by making it seem like it's always month 0
        		var codes = new List<CodeInstruction>(instructions);
        		for (var i = 0; i < codes.Count - 7; i++)
        		{ 
        			if (codes[i].opcode == OpCodes.Call
        				&& codes[i+1].opcode == OpCodes.Ldc_I4_6
        				&& codes[i+2].opcode == OpCodes.Ble
        				&& codes[i+3].opcode == OpCodes.Call
        				&& codes[i+4].opcode == OpCodes.Ldc_I4_6
        				&& codes[i+5].opcode == OpCodes.Rem
        				&& codes[i+6].opcode == OpCodes.Ldc_I4_0
        				&& codes[i+7].opcode == OpCodes.Ceq)
				{
					codes[i].opcode = OpCodes.Ldc_I4_0;
					NormalStrangePortalSpawnDisabled = true;
				}
        			
				//IL_0032: call int32 EndOfMonthCutscenes::get_CurrentMonth()
				//IL_0037: ldc.i4.6
				//IL_0038: ble.s IL_0046
        			
				//IL_003a: call int32 EndOfMonthCutscenes::get_CurrentMonth()
				//IL_003f: ldc.i4.6
				//IL_0040: rem
				//IL_0041: ldc.i4.0
				//IL_0042: ceq
				//IL_0044: br.s IL_0047
				
        		}
        		
        		if (!NormalStrangePortalSpawnDisabled)
        		{
        			Log("Failed to disable normal Strange Portal spawning");
        		}
        		
        		return codes;
		}
		
		
	}

	class StoneArch : CardData
	{
		protected override bool CanHaveCard(CardData otherCard) =>
				otherCard.Id == Cards.STRANGE_PORTAL;
	}
	
}
