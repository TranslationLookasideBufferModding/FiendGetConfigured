using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace FiendGetConfigured
{
    internal static class Patches
    {
        private static bool strict = true;

        internal static void Install(Harmony harmony, bool requireKnownCounts)
        {
            MethodInfo[] methods = Validate(requireKnownCounts);
            MethodInfo ai=methods[0], grab=methods[1], contact=methods[2], rpc=methods[3], awake=methods[4], spawned=methods[5];
            harmony.Patch(awake, postfix: H("Prepare"), transpiler: H("ModernInstructions"));
            harmony.Patch(spawned, postfix: H("Prepare"), transpiler: H("ModernInstructions"));
            harmony.Patch(ai, prefix: H("Prepare"), postfix: H("AfterAi"), transpiler: H("AiInstructions"));
            harmony.Patch(grab, transpiler: H("GrabInstructions"));
            harmony.Patch(contact, prefix: H("Contact"), transpiler: H("ContactInstructions"));
            harmony.Patch(rpc, transpiler: H("RpcInstructions"));
            harmony.Patch(Runtime.NeedMethod(Runtime.FiendType, "TeleportServerRpc", 0), prefix: H("Teleport"));
            harmony.Patch(Runtime.NeedMethod(Runtime.FiendType, "SetLungLightServerRpc", 0), prefix: H("Apparatus"));
            harmony.Patch(Runtime.NeedMethod(Runtime.FiendType, "BreakDoorServerRpc", 0), prefix: H("Doors"));
            harmony.Patch(Runtime.NeedMethod(Runtime.FiendType, "BreakerBoxBreakServerRpc", 0), prefix: H("Breaker"));
            harmony.Patch(Runtime.NeedMethod(Runtime.FiendType, "FearedServerRpc", 2), prefix: H("Fear"));
        }

        internal static MethodInfo[] Validate(bool requireKnownCounts)
        {
            strict = requireKnownCounts;
            MethodInfo ai = Runtime.NeedMethod(Runtime.FiendType, "DoAIInterval", 0);
            Type iterator = Runtime.FiendType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
                .Single(t => t.Name.StartsWith("<Grabbing>"));
            MethodInfo grab = Runtime.NeedMethod(iterator, "MoveNext", 0);
            MethodInfo contact = Runtime.NeedMethod(Runtime.FiendType, "OnTriggerStay", 1);
            MethodInfo rpc = Runtime.NeedMethod(Runtime.FiendType, "GrabServerRpc", 1);
            MethodInfo awake = Runtime.NeedMethod(Runtime.FiendType, "Awake", 0);
            MethodInfo spawned = Runtime.NeedMethod(Runtime.FiendType, "OnNetworkSpawn", 0);
            // Validate every instruction pattern before installing any patch.
            AiInstructions(PatchProcessor.GetOriginalInstructions(ai)).ToList();
            GrabInstructions(PatchProcessor.GetOriginalInstructions(grab)).ToList();
            ContactInstructions(PatchProcessor.GetOriginalInstructions(contact)).ToList();
            RpcInstructions(PatchProcessor.GetOriginalInstructions(rpc)).ToList();
            ModernInstructions(PatchProcessor.GetOriginalInstructions(awake)).ToList();
            ModernInstructions(PatchProcessor.GetOriginalInstructions(spawned)).ToList();
            return new MethodInfo[] { ai, grab, contact, rpc, awake, spawned };
        }

        private static HarmonyMethod H(string name) { return new HarmonyMethod(typeof(Patches), name); }
        private static MethodInfo Own(string name) { return AccessTools.Method(typeof(Patches), name); }
        private static MethodInfo Run(string name) { return AccessTools.Method(typeof(Runtime), name); }
        private static void Prepare(object __instance) { Runtime.Prepare(__instance); }
        private static void AfterAi(object __instance) { Runtime.AfterAi(__instance); }
        private static bool Contact(object __instance, Collider __0) { return Runtime.ContactPrefix(__instance, __0); }
        private static bool Teleport() { return Runtime.Current.TeleportEnabled; }
        private static bool Apparatus() { return Runtime.Current.ApparatusRageEnabled; }
        private static bool Doors() { return Runtime.Current.BreakDoorsEnabled; }
        private static bool Breaker() { return Runtime.Current.BreakerBoxDestructionEnabled; }
        private static void Fear(ref bool __0) { if (!Runtime.Current.TemporaryRageEnabled) __0 = false; }
        private static int Damage() { return Runtime.Current.Damage; }
        private static int ExecutionThreshold() { return Runtime.Current.ExecutionHealthThreshold; }
        private static float AttackDistance() { return Runtime.Current.AttackDistance; }
        private static float Cooldown() { return Runtime.Current.AttackCooldownSeconds; }
        private static float Knockback() { return Runtime.Current.KnockbackForce; }
        private static bool ApparatusRage() { return Runtime.Current.ApparatusRageEnabled; }
        private static int LateHour() { return Runtime.Current.LateGameBoostEnabled ? Runtime.Current.LateGameStartHour : int.MaxValue; }
        private static int LateMultiplier() { return Runtime.Current.LateGameMultiplier; }

        private static bool IsCall(CodeInstruction code, string name)
        {
            MethodInfo method = code.operand as MethodInfo;
            return (code.opcode == OpCodes.Call || code.opcode == OpCodes.Callvirt) && method != null && method.Name == name;
        }

        private static bool IsField(CodeInstruction code, string name)
        {
            FieldInfo field = code.operand as FieldInfo;
            return field != null && field.Name == name;
        }

        private static bool Integer(CodeInstruction code, int number)
        {
            if (code.opcode == OpCodes.Ldc_I4 || code.opcode == OpCodes.Ldc_I4_S) return Convert.ToInt32(code.operand) == number;
            return number == 2 && code.opcode == OpCodes.Ldc_I4_2;
        }

        private static bool Single(CodeInstruction code, float number)
        {
            return code.opcode == OpCodes.Ldc_R4 && (float)code.operand == number;
        }

        private static void Call(CodeInstruction instruction, MethodInfo method)
        {
            instruction.opcode = OpCodes.Call;
            instruction.operand = method;
        }

        private static void Count(string label, int actual, int expected)
        {
            if ((strict && actual != expected) || (!strict && actual < 1))
                throw new InvalidOperationException(label + ": expected " + (strict ? expected.ToString() : "at least 1") + ", found " + actual + ". Refusing a partial patch.");
        }

        private static List<CodeInstruction> Copy(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> list = instructions.Select(i => new CodeInstruction(i)).ToList();
            Dictionary<OpCode, OpCode> branches = new Dictionary<OpCode, OpCode> {
                {OpCodes.Br_S,OpCodes.Br},{OpCodes.Brfalse_S,OpCodes.Brfalse},{OpCodes.Brtrue_S,OpCodes.Brtrue},
                {OpCodes.Beq_S,OpCodes.Beq},{OpCodes.Bne_Un_S,OpCodes.Bne_Un},{OpCodes.Bge_S,OpCodes.Bge},
                {OpCodes.Bge_Un_S,OpCodes.Bge_Un},{OpCodes.Bgt_S,OpCodes.Bgt},{OpCodes.Bgt_Un_S,OpCodes.Bgt_Un},
                {OpCodes.Ble_S,OpCodes.Ble},{OpCodes.Ble_Un_S,OpCodes.Ble_Un},{OpCodes.Blt_S,OpCodes.Blt},
                {OpCodes.Blt_Un_S,OpCodes.Blt_Un},{OpCodes.Leave_S,OpCodes.Leave}};
            foreach (CodeInstruction code in list)
            {
                OpCode longForm;
                if (branches.TryGetValue(code.opcode, out longForm)) code.opcode = longForm;
            }
            return list;
        }

        private static IEnumerable<CodeInstruction> ModernInstructions(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> list = Copy(instructions);
            int hits = 0;
            foreach (CodeInstruction code in list)
                if (code.opcode == OpCodes.Ldsfld && IsField(code, "EnableLegacyBehaviourConfig"))
                { code.opcode = OpCodes.Ldc_I4_0; code.operand = null; hits++; }
            Count("modern behavior", hits, 1);
            return list;
        }

        private static IEnumerable<CodeInstruction> AiInstructions(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> list = Copy(instructions);
            int targets = 0, speeds = 0, destinations = 0, hours = 0, factors = 0, apparatus = 0;
            List<CodeInstruction> output = new List<CodeInstruction>();
            for (int i = 0; i < list.Count; i++)
            {
                CodeInstruction code = list[i];
                if (IsCall(code, "TargetClosestPlayer")) { Call(code, Run("ChooseTarget")); targets++; }
                else if (IsCall(code, "SetDestinationToPosition")) { Call(code, Run("Navigate")); destinations++; }
                else if (IsCall(code, "set_speed"))
                {
                    CodeInstruction load = new CodeInstruction(OpCodes.Ldarg_0);
                    load.labels.AddRange(code.labels); code.labels.Clear();
                    load.blocks.AddRange(code.blocks); code.blocks.Clear();
                    output.Add(load);
                    Call(code, Run("SetSpeed")); speeds++;
                }
                else if (Integer(code, 15) && i > 0 && IsField(list[i-1], "hour")) { Call(code, Own("LateHour")); hours++; }
                else if (Integer(code, 2) && i > 0 && IsField(list[i-1], "Funky") && i+1 < list.Count && IsCall(list[i+1], "set_Value"))
                { Call(code, Own("LateMultiplier")); factors++; }
                else if (code.opcode == OpCodes.Ldsfld && IsField(code, "WillRageAfterApparatusConfig"))
                { Call(code, Own("ApparatusRage")); apparatus++; }
                output.Add(code);
            }
            Count("target acquisition", targets, 5); Count("movement", speeds, 9);
            Count("last-seen navigation", destinations, 6); Count("late-game hour", hours, 2);
            Count("late-game factor", factors, 2); Count("apparatus rage", apparatus, 2);
            return output;
        }

        private static IEnumerable<CodeInstruction> ContactInstructions(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> list = Copy(instructions);
            int hits = 0;
            for (int i = 1; i < list.Count; i++)
                if (Single(list[i], 4f) && IsCall(list[i-1], "Distance")) { Call(list[i], Own("AttackDistance")); hits++; }
            Count("contact distance", hits, 1);
            return list;
        }

        private static IEnumerable<CodeInstruction> GrabInstructions(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> list = Copy(instructions);
            int thresholds = 0, damage = 0, cooldown = 0, knockback = 0;
            for (int i = 1; i < list.Count; i++)
            {
                if (Integer(list[i], 50) && IsField(list[i-1], "health"))
                { Call(list[i], Own("ExecutionThreshold")); thresholds++; }
                if (IsCall(list[i], "StartCooldown"))
                {
                    int timer=i-1;
                    for (; timer>=Math.Max(0,i-4); timer--) if(Single(list[timer],1f)) break;
                    if(timer<Math.Max(0,i-4)) throw new InvalidOperationException("Original hit cooldown literal not found.");
                    Call(list[timer], Own("Cooldown")); cooldown++;
                }
                if (IsCall(list[i], "DamagePlayer"))
                {
                    int j = i - 1;
                    for (; j >= Math.Max(0,i-16); j--) if (Integer(list[j], 50)) break;
                    if (j < Math.Max(0,i-16)) throw new InvalidOperationException("Original Fiend damage literal not found.");
                    Call(list[j], Own("Damage")); damage++;
                    for (j=i+1; j<Math.Min(list.Count-1,i+40); j++)
                        if (Single(list[j],30f) && IsCall(list[j+1],"op_Multiply"))
                        { Call(list[j], Own("Knockback")); knockback++; break; }
                }
            }
            Count("execution threshold", thresholds, 1); Count("normal damage", damage, 1);
            Count("hit cooldown", cooldown, 1); Count("normal-hit knockback", knockback, 1);
            return list;
        }

        private static IEnumerable<CodeInstruction> RpcInstructions(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> output = new List<CodeInstruction>();
            int hits = 0;
            foreach (CodeInstruction code in Copy(instructions))
            {
                if (IsCall(code,"GrabClientRpc"))
                {
                    Type referenceType = ((MethodInfo)code.operand).GetParameters()[0].ParameterType;
                    if (!referenceType.IsValueType) throw new InvalidOperationException("Expected network reference struct.");
                    CodeInstruction box = new CodeInstruction(OpCodes.Box,referenceType);
                    box.labels.AddRange(code.labels); code.labels.Clear();
                    box.blocks.AddRange(code.blocks); code.blocks.Clear();
                    output.Add(box);
                    Call(code, Run("SendGrab")); hits++;
                }
                output.Add(code);
            }
            Count("server attack gate",hits,1);
            return output;
        }
    }
}
